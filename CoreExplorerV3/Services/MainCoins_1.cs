using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreExplorerV3.Models;
using Microsoft.AspNetCore.Http;
using CoreExplorerV3.DTO;
using BitcoinLib.Models;
using MongoDB.Bson;
using CoreMongoDTO.DTO;
using System.IO;
using CoreExplorerV3.Helpers;
using System.Text;
using ExchangesLib.DTO;
using System.Text.RegularExpressions;
using ExchangesLib.Helper;
using BitcoinLib.Services.Coins.Base;
using System.Collections.Concurrent;
using BitcoinLib.Services.Coins.Bitcoin;

namespace CoreExplorerV3.Services
{
    public partial class MainCoins
    {
        public class MainCoinsDTO
        {
            public MongoClient mainClient = new MongoClient(Startup.ConnectionString);
            public MongoClient client;
            public IMongoDatabase mainDb;
            public IMongoDatabase db;
            public MainCoinModel mainModel;
            public IMongoCollection<MainCoinModel> coins;
            public string coinConf;
        }

        private Dictionary<string, VerifyAddressesDTO> _claims = new Dictionary<string, VerifyAddressesDTO>();

        private MainCoinsDTO _dtoMain;
        private static ConcurrentDictionary<string, MainCoinsDTO> _dtoMainCache = new ConcurrentDictionary<string, MainCoinsDTO>();


        public MainCoins(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            var host = httpContextAccessor.HttpContext.Request.Host.Host;
            if (_dtoMainCache.ContainsKey(host))
            {
                _dtoMain = _dtoMainCache[host];
            }
            else
            {
                _dtoMain = new MainCoinsDTO();
                _dtoMain.mainDb = _dtoMain.mainClient.GetDatabase("explorer");
                var conf = configuration.GetSection("Coins").Get<List<ConfigBinder>>();
                _dtoMain.coinConf = conf.First(c => c.Host == host).CoinID;
                _dtoMain.coins = _dtoMain.mainDb.GetCollection<MainCoinModel>("assets");
                _dtoMain.mainModel = _dtoMain.coins.Find(f => f.CoinID == _dtoMain.coinConf).First();
                var sbMongo = new StringBuilder();
                sbMongo.Append("mongodb://");
                sbMongo.Append(_dtoMain.mainModel.ServerAddress);
                sbMongo.Append(":");
                sbMongo.Append(_dtoMain.mainModel.ServerPort);
                _dtoMain.client = new MongoClient(sbMongo.ToString());
                _dtoMain.db = _dtoMain.client.GetDatabase(_dtoMain.mainModel.ServerDB);
                var claimsDb = _dtoMain.mainDb.GetCollection<VerifyAddressesDTO>("verify_address");
                var claims = claimsDb.Find(f => f.CoinSymbol == _dtoMain.coinConf).ToList();
                foreach (var claim in claims)
                {
                    _claims.Add(claim.CoinAddressLower, claim);
                }

                var sb = new StringBuilder();
                sb.Append(_dtoMain.mainModel.CoinProtocol);
                sb.Append("://");
                sb.Append(_dtoMain.mainModel.CoinAddress);
                sb.Append(":");
                sb.Append(_dtoMain.mainModel.CoinPort);
                CoinService = new BitcoinService(sb.ToString(), _dtoMain.mainModel.CoinLogin, _dtoMain.mainModel.Coinpassword, _dtoMain.mainModel.CoinWalletPassowrd);
                CoinService.Parameters.IsClient = false;
            }
            _dtoMain.mainModel = _dtoMain.coins.Find(f => f.CoinID == _dtoMain.coinConf).First();

        }

        public MainCoinModel GetCoin()
        {
            return _dtoMain.mainModel;
        }

        public Dictionary<string, VerifyAddressesDTO> GetClaims()
        {
            return _claims;
        }

        public List<MainCoinModel> GetCoinSymbols()
        {
            var coins = _dtoMain.mainDb.GetCollection<MainCoinModel>("assets");
            return coins.Find(f => f.IsEnabled).ToList();
        }

        public List<PeerList> GetPeerList(String coinSymbol)
        {
            var peer_list = _dtoMain.db.GetCollection<PeerList>(coinSymbol + "_peers");
            var peers = peer_list.Find(_ => true).SortBy(s => s.PingTime).ToList();
            return peers;
        }
        public List<ExchangesCurrency> GetExchangePrice(string currency, List<Exchanges> extList = null)
        {
            List<Exchanges> exchanges = extList;
            if (exchanges == null)
            {
                exchanges = GetExchanges();
            }
            var tm = AmountHelper.DateTimeToUnixTimestamp(DateTime.UtcNow);
            var exchange_stats = _dtoMain.mainDb.GetCollection<ExchangeTicker>("aaexchanges_real");
            var exchangeStats = exchange_stats.Find(m => m.InCurrency == currency && m.OutCurrency == "BTC").SortByDescending(s => s.Volume).ToList();
            var lst = new List<ExchangesCurrency>();
            if (exchangeStats.Count == 0)
            {
                return lst;
            }
            var trends = GetTrendByMarkets(currency);
            foreach (var ex in exchangeStats)
            {
                var exchange = exchanges.FirstOrDefault(m => m.MarketId == ex.MarketID);
                if (exchange == null)
                {
                    continue;
                }
                string exchangeUrlCoin = currency;
                if (exchange.ExchangeDirectUrlParam == "lower")
                {
                    exchangeUrlCoin = exchangeUrlCoin.ToLower();
                }
                var directUrl = string.Format(exchange.ExchangeDirectUrl, exchangeUrlCoin);
                lst.Add(new ExchangesCurrency()
                {
                    Buy = ex.Buy,
                    Sell = ex.Sell,
                    Last = ex.Last,
                    Volume = ex.Volume,
                    Change = trends.ContainsKey(ex.MarketID) ? trends[ex.MarketID] : 0.0m,
                    Exchange = exchange.Exchange,
                    ExchangeUrl = exchange.ExchangeUrl,
                    ExchangeDirectUrl = directUrl,
                    Time = ex.Time,
                    MarketID = ex.MarketID
                });
            }
            return lst;
        }

        public List<ExchangeHistory> GetHistory(string currency, int days)
        {
            int addDays = 0;
            var tm = AmountHelper.DateTimeToUnixTimestamp(DateTime.UtcNow);
            DateTimeOffset tmCheck = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 3, 0, 0,
                            new TimeSpan(0, 0, 0));
            long tmChekDate = ExchangeHelper.DateTimeToUnixTimestamp(tmCheck.UtcDateTime);
            if (tmChekDate + 60 * 60 < tmChekDate)
            {
                addDays = -1;
            }

            var exchange_stats = _dtoMain.mainDb.GetCollection<ExchangeTicker>("aaexchanges_real_each");
            DateTimeOffset dateTimeYesterday = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0,
                            new TimeSpan(0, 0, 0)).AddDays(-days + 1 + addDays);
            long unixTimeHistory = AmountHelper.DateTimeToUnixTimestamp(dateTimeYesterday.UtcDateTime);

            var exchangeStats = exchange_stats.Find(m => m.InCurrency == currency && m.OutCurrency == "BTC" && m.Time >= unixTimeHistory).ToList();
            var history = new List<ExchangeHistory>();
            var tmp = new Dictionary<long, List<Decimal>>();
            var tmpVolume = new Dictionary<long, Decimal>();
            foreach (var ex in exchangeStats)
            {
                if (!tmpVolume.ContainsKey(ex.Time))
                {
                    tmpVolume.Add(ex.Time, 0.0m);
                }
                tmpVolume[ex.Time] += ex.Volume;
            }
            foreach (var ex in exchangeStats)
            {
                if (tmpVolume.ContainsKey(ex.Time) && tmpVolume[ex.Time] > 0 && ex.Volume * 100 / tmpVolume[ex.Time] < 3)
                {
                    continue;
                }
                if (!tmp.ContainsKey(ex.Time))
                {
                    tmp.Add(ex.Time, new List<decimal>());
                }
                tmp[ex.Time].Add(ex.Last);
            }
            int i = 1;
            foreach (var ex in tmp)
            {
                if (i > days)
                {
                    break;
                }
                var date = UnixTimeStampToDateTime(ex.Key);
                var dateStr = date.Day + "-" + date.Month + "-" + date.Year;
                history.Add(new ExchangeHistory()
                {
                    Date = dateStr,
                    Price = ex.Value.Average()
                });
                i++;
            }
            return history;
        }
        public Dictionary<string, Decimal> GetTrendByMarkets(string currency)
        {
            Dictionary<string, Decimal> markets = new Dictionary<string, Decimal>();
            var tm = AmountHelper.DateTimeToUnixTimestamp(DateTime.UtcNow);
            var exchange_stats = _dtoMain.mainDb.GetCollection<ExchangeTicker>("aaexchanges_real_twodays");
            DateTimeOffset dateTimeDaily = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0,
                            new TimeSpan(0, 0, 0)).AddDays(-1);
            long unixTimeDaily = AmountHelper.DateTimeToUnixTimestamp(dateTimeDaily.UtcDateTime);

            var exchangeStats = exchange_stats.Find(m => m.InCurrency == currency && m.OutCurrency == "BTC" && m.Time >= unixTimeDaily).ToList();

            Dictionary<string, List<ExchangeTicker>> marketsList = new Dictionary<string, List<ExchangeTicker>>();
            foreach (var mrkt in exchangeStats)
            {
                if (!marketsList.ContainsKey(mrkt.MarketID))
                {
                    marketsList.Add(mrkt.MarketID, new List<ExchangeTicker>());
                    marketsList[mrkt.MarketID].Add(mrkt);
                }
                else
                {
                    marketsList[mrkt.MarketID].Add(mrkt);
                }

            }
            foreach (KeyValuePair<string, List<ExchangeTicker>> kv in marketsList)
            {
                Decimal dict24Prev = 0.0m;
                Decimal dict24Now = 0.0m;
                int a = 0, b = 0, i = 0;
                foreach (var ex in kv.Value)
                {
                    if (i == 0)
                    {
                        dict24Prev += ex.Last;
                        a++;
                    }
                    else
                    {
                        dict24Now += ex.Last;
                        b++;
                    }
                    i++;
                }
                if (a == 0 || b == 0 || dict24Prev == 0)
                {
                    markets.Add(kv.Key, 0.0m);
                    continue;
                }
                Decimal trend = (dict24Now / b) * 100 / (dict24Prev / a) - 100;
                markets.Add(kv.Key, trend);
            }
            return markets;
        }

        public Decimal GetTrendByMarket(string currency, string marketId)
        {
            var tm = AmountHelper.DateTimeToUnixTimestamp(DateTime.UtcNow);
            var exchange_stats = _dtoMain.mainDb.GetCollection<ExchangeTicker>("aaexchanges_real_twodays");
            DateTimeOffset dateTimeDaily = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0,
                            new TimeSpan(0, 0, 0)).AddDays(-1);
            long unixTimeDaily = AmountHelper.DateTimeToUnixTimestamp(dateTimeDaily.UtcDateTime);

            var exchangeStats = exchange_stats.Find(m => m.InCurrency == currency && m.OutCurrency == "BTC" && m.MarketID == marketId && m.Time >= unixTimeDaily).ToList();
            Decimal dict24Prev = 0.0m;
            Decimal dict24Now = 0.0m;
            int a = 0, b = 0, i = 0;
            foreach (var ex in exchangeStats)
            {
                if (i == 0)
                {
                    dict24Prev += ex.Last;
                    a++;
                }
                else
                {
                    dict24Now += ex.Last;
                    b++;
                }
                i++;
            }
            if (a == 0 || b == 0 || dict24Prev == 0)
            {
                return 0.0m;
            }
            Decimal trend = (dict24Now / b) * 100 / (dict24Prev / a) - 100;
            return trend;
        }

        public List<Exchanges> GetExchanges()
        {
            var exchangeMarket = _dtoMain.mainDb.GetCollection<Exchanges>("aaexchanges");
            return exchangeMarket.Find(_ => true).ToList();
        }
    }
}
