using CloudFlareUtilities;
using CoreMongoDTO.DTO;
using ExchangesLib;
using ExchangesLib.Exchanges;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using ExchangesLib.DTO;
using System.IO;

namespace CoreExplorerIndexer
{
    public class Exchange
    {
        private static object errorLockExchange = new object();
        public static AbortDTO AbortDTO { get; set; } = new AbortDTO();
        private static object lockExchange = new object();
        public static void ExchangeIndex(object data)
        {
            HashSet<string> locked = new HashSet<string>();
            var obj = data as IMongoDatabase;
            var assets = obj.GetCollection<MainCoinModel>("assets");
            while (true)
            {
                Console.WriteLine("Exchange update process");
                if (Program.Interrupt)
                {
                    break;
                }
                try
                {
                    var collection = obj.GetCollection<Exchanges>("aaexchanges");
                    var collection_stats = obj.GetCollection<ExchangeTicker>("aaexchanges_stats");
                    var collection_real = obj.GetCollection<ExchangeTicker>("aaexchanges_real");
                    var collection_real_2days = obj.GetCollection<ExchangeTicker>("aaexchanges_real_twodays");
                    var collection_real_each = obj.GetCollection<ExchangeTicker>("aaexchanges_real_each");
                    List<Exchanges> exList = collection.Find(_ => _.NextScan < Funcs.DateTimeToUnixTimestamp(DateTime.UtcNow) && _.ExchangeClass != null).ToList();
                    var dictExchange = new Dictionary<string, List<string>>();
                    if (exList.Count > 0)
                    {
                        var cbsConf = assets.Find(_ => _.IsEnabled == true).ToList();
                        foreach (var c in cbsConf)
                        {
                            if (c.IgnoreExchanges.Count > 0)
                            {
                                dictExchange.Add(c.CoinSymbol, c.IgnoreExchanges);
                            }
                        }
                    }
                    foreach (Exchanges ex in exList)
                    {
                        if (Program.Interrupt)
                        {
                            break;
                        }
                        lock (lockExchange)
                        {
                            if (locked.Contains(ex.Id.ToString()))
                            {
                                continue;
                            }
                            locked.Add(ex.Id.ToString());
                        }
                        Thread th = new Thread((exObj) =>
                        {
                            if (Program.Interrupt)
                            {
                                return;
                            }
                            DateTimeOffset dateTimeDaily = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 3, 0, 0,
                                new TimeSpan(0, 0, 0));
                            long unixTimeDaily = Funcs.DateTimeToUnixTimestamp(dateTimeDaily.UtcDateTime);

                            var uptime = Funcs.DateTimeToUnixTimestamp(DateTime.UtcNow);
                            var uptime48h = uptime - 172800;
                            var exTh = exObj as Exchanges;
                            var docId = exTh.Id.ToString();
                            var execType = Type.GetType("ExchangesLib.Exchanges." + exTh.ExchangeClass + ",ExchangesLib");
                            IExchangeLib gr = (IExchangeLib)Activator.CreateInstance(execType);
                            exTh.MarketsCount = 0;
                            try
                            {
                                Console.WriteLine("Exchange update " + exTh.MarketId);
                                var markets = gr.GetMarket(exTh.Markets);
                                var tickers = gr.GetTicker(exTh.Tickers, markets, uptime, exTh.MarketId, AbortDTO);
                                foreach (var ticker in tickers)
                                {
                                    if (Program.Interrupt)
                                    {
                                        break;
                                    }
                                    if (dictExchange.ContainsKey(ticker.InCurrency))
                                    {
                                        if (dictExchange[ticker.InCurrency].Any(a => a == ticker.MarketID))
                                        {
                                            continue;
                                        }
                                    }
                                    var bsDoc = ticker.ToBsonDocument();
                                    bsDoc.Remove("_id");
                                    var doc = new BsonDocument
                                    {
                                    {
                                        "$set", bsDoc
                                    }
                                    };
                                    collection_real.UpdateOne(new BsonDocument() {
                                    { "market_id", ticker.MarketID },
                                    { "in_currency", ticker.InCurrency },
                                    { "out_currency", ticker.OutCurrency }
                                    }, doc, new UpdateOptions { IsUpsert = true });

                                }
                                foreach (var ticker in tickers)
                                {
                                    if (Program.Interrupt)
                                    {
                                        break;
                                    }
                                    if (dictExchange.ContainsKey(ticker.InCurrency))
                                    {
                                        if (dictExchange[ticker.InCurrency].Any(a => a == ticker.MarketID))
                                        {
                                            continue;
                                        }
                                    }
                                    var bsDoc = ticker.ToBsonDocument();
                                    bsDoc.Remove("_id");
                                    bsDoc["time"] = unixTimeDaily;
                                    var doc = new BsonDocument
                                    {
                                    {
                                        "$set", bsDoc
                                    }
                                    };
                                    collection_real_2days.UpdateOne(new BsonDocument() {
                                    { "market_id", ticker.MarketID },
                                    { "in_currency", ticker.InCurrency },
                                    { "out_currency", ticker.OutCurrency },
                                    { "time", unixTimeDaily }
                                    }, doc, new UpdateOptions { IsUpsert = true });
                                }
                                foreach (var ticker in tickers)
                                {
                                    if (Program.Interrupt)
                                    {
                                        break;
                                    }
                                    if (dictExchange.ContainsKey(ticker.InCurrency))
                                    {
                                        if (dictExchange[ticker.InCurrency].Any(a => a == ticker.MarketID))
                                        {
                                            continue;
                                        }
                                    }
                                    ticker.Id = ObjectId.GenerateNewId();
                                    collection_stats.InsertOne(ticker);
                                    exTh.MarketsCount++;
                                }
                                foreach (var ticker in tickers)
                                {
                                    if (Program.Interrupt)
                                    {
                                        break;
                                    }
                                    if (dictExchange.ContainsKey(ticker.InCurrency))
                                    {
                                        if (dictExchange[ticker.InCurrency].Any(a => a == ticker.MarketID))
                                        {
                                            continue;
                                        }
                                    }
                                    var bsDoc = ticker.ToBsonDocument();
                                    bsDoc.Remove("_id");
                                    bsDoc["time"] = unixTimeDaily;
                                    var doc = new BsonDocument
                                    {
                                    {
                                        "$set", bsDoc
                                    }
                                    };
                                    collection_real_each.UpdateOne(new BsonDocument() {
                                    { "market_id", ticker.MarketID },
                                    { "in_currency", ticker.InCurrency },
                                    { "out_currency", ticker.OutCurrency },
                                    { "time", unixTimeDaily }
                                    }, doc, new UpdateOptions { IsUpsert = true });
                                }
                                exTh.Error = 0;
                                if (Program.Interrupt)
                                {
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                lock (errorLockExchange)
                                {
                                    File.AppendAllText("error_exchange.txt", exTh.MarketId + " " + e.ToString() + Environment.NewLine);
                                }
                                exTh.Error++;
                            }
                            try
                            {
                                if (exTh.Error == 0 || exTh.Error > 10)
                                {
                                    collection_real.DeleteMany(new BsonDocument()
                                    {
                                        {
                                            "market_id", exTh.MarketId
                                        },
                                        {
                                            "time", new BsonDocument
                                            { { "$lt", uptime },  }
                                        }
                                    });

                                    collection_real_2days.DeleteMany(new BsonDocument()
                                    {
                                        {
                                            "market_id", exTh.MarketId
                                        },
                                        {
                                            "time", new BsonDocument
                                            { { "$lt", uptime48h },  }
                                        }
                                    });
                                }

                                var docUp = new BsonDocument
                                {
                                    {
                                        "$set", new BsonDocument() { { "exchange_next_scan", uptime + exTh.Scan_interval }, { "exchange_updated", uptime  }, { "exchange_error", exTh.Error }, { "markets_count", exTh.MarketsCount } }
                                    }
                                };
                                collection.UpdateOne(new BsonDocument() { { "_id", ObjectId.Parse(docId) } }, docUp, new UpdateOptions { IsUpsert = true });
                            }
                            catch (Exception e)
                            {
                                return;
                            }
                            lock (lockExchange)
                            {
                                locked.Remove(docId);
                            }
                        });
                        th.Start(ex);
                    }
                    if (Program.Interrupt)
                    {
                        break;
                    }
                    Thread.Sleep(10000);
                }
                catch (Exception e)
                {
                    Thread.Sleep(10000);
                    //Exchanges error
                }
            }
        }
    }
}
