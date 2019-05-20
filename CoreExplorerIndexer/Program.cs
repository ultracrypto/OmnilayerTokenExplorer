using BitcoinLib.Services.Coins.Base;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using BitcoinLib;
using System.Linq;
using System.Collections.Generic;
using BitcoinLib.Responses;
using MongoDB.Bson.Serialization.Attributes;
using System.Threading;
using CoreMongoDTO.DTO;
using System.Text;
using System.Net;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
using BitcoinLib.Services.Coins.Bitcoin;
using System.Reflection;

namespace CoreExplorerIndexer
{
    class Program
    {
        public static readonly int VERSION = 1;
        public static ILoggerFactory LoggerFactory;
        public static IConfigurationRoot Configuration;
        public static int MaxCache = 200;
        public static string MongoHost = "localhost";
        public static string MongoPort = "27017";

        public class LastBlock
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("type")]
            public String Type { get; set; }

            [BsonElement("block")]
            public int Block { get; set; }

        }

        public class ClientTransactionAddress
        {
            public String Address { get; set; }
            public Decimal Amount { get; set; }
            public uint BlockTime { get; set; }
            public string RewardType { get; set; }
            public bool isValid { get; set; }
        }

        public class Address
        {
            public string Addr { get; set; }
            public string Transaction { get; set; }
            public int Time { get; set; }
            public Decimal Amount { get; set; }
        }

        public static BsonDocument TransactionToBson(GetOmniTransactionResponse transaction)
        {
            /*
             *   "txid": "5f6662b558ba7f96153b2b74de8d5425a2042c3d499a9162132c65afd6447fd9",
  "fee": "0.00011000",
  "sendingaddress": "162XM1V5dFdGmu4Z2DnNyhTomP77nWn5Hf",
  "referenceaddress": "1Dbzzpf9MGphA2Np24ttZa1yZo7Jb2Lc6M",
  "ismine": false,
  "version": 0,
  "type_int": 0,
  "type": "Simple Send",
  "propertyid": 2,
  "divisible": true,
  "amount": "1.33700000",
  "valid": true,
  "blockhash": "000000000000000049a0914d83df36982c77ac1f65ade6a52bdced2ce312aba9",
  "blocktime": 1399704683,
  "positioninblock": 384,
  "block": 300001,
  "confirmations": 31340
  */
            var mainDocument = new BsonDocument
            {
                {"txid", transaction.txid},
                {"txidlower", transaction.txid.ToLower()},
                {"fee", transaction.fee},
                {"sendingaddress", transaction.sendingaddress},
                {"sendingaddresslower", transaction.sendingaddress.ToLower()},
                {"referenceaddress", transaction.referenceaddress?.ToString() ?? "" },
                {"referenceaddresslower", transaction.referenceaddress?.ToString().ToLower() ?? ""},
                {"ismine", transaction.ismine},
                {"version", transaction.version},
                {"type_int", transaction.type_int},
                {"type", transaction.type },
                {"propertyid", transaction.propertyid },
                {"divisible", transaction.divisible },
                {"amount", transaction.amount },
                {"valid", transaction.valid },
                {"blockhash", transaction.blockhash },
                {"blocktime", transaction.blocktime },
                {"positioninblock", transaction.positioninblock },
                {"block", transaction.block },
                {"confirmations", transaction.confirmations }
            };
            return mainDocument;
        }

        public class Mnwork
        {
            public IMongoDatabase DB { get; set; }
            public IMongoDatabase MAINDB { get; set; }
            public ICoinService CoinService { get; set; }
            public MainCoinModel MainCoinModel { get; set; }
        }

        public static bool ContainsIndex(IMongoDatabase db, string collection, string indexName)
        {
            var query = db.GetCollection<BsonDocument>(collection).Indexes.List();
            var bsonIndexes = query.ToList();
            var indexNames = bsonIndexes.Select(i => i["name"].ToString()).ToList();
            return indexNames.Contains(indexName);
        }

        public static bool Interrupt { get; set; }
        static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Interrupt = true;
            Exchange.AbortDTO.Interrupt = true;
            tokenSource.Cancel();
        }
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (String.IsNullOrWhiteSpace(environment))
            {
                environment = "Production";
            }

            Console.WriteLine("Environment: {0}", environment);
            
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: true);
            if (environment == "Development")
            {

                builder
                    .AddJsonFile(
                        Path.Combine(AppContext.BaseDirectory, string.Format("..{0}..{0}..{0}", Path.DirectorySeparatorChar), $"appsettings.{environment}.json"),
                        optional: true
                    );
            }
            else
            {
                builder
                    .AddJsonFile($"appsettings.{environment}.json", optional: false);
            }

            Configuration = builder.Build();
            MaxCache = Convert.ToInt32(Configuration["maxCache"]);
            MongoHost = Configuration["mongoHost"].ToString();
            MongoPort = Configuration["mongoPort"].ToString();

            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            ServicePointManager.DefaultConnectionLimit = 10000;
            var connectionString = "mongodb://" + MongoHost + ":" + MongoPort;
            var mainClient = new MongoClient(connectionString);
            IMongoDatabase mainDb = mainClient.GetDatabase("explorer");             
            var assets = mainDb.GetCollection<MainCoinModel>("assets");
            MainCoinModel mcm = assets.Find(_ => true).FirstOrDefault();
            if (mcm == null)
            {
                Console.WriteLine("No config found");
                Environment.Exit(1);
            }
            
            string marketStatsName = "aaexchanges_stats";
            bool alreadyExists = mainDb.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == marketStatsName);
            if (!alreadyExists)
            {
                mainDb.CreateCollection(marketStatsName);
            }
            var market_stats = mainDb.GetCollection<BsonDocument>(marketStatsName);
            if (!ContainsIndex(mainDb, marketStatsName, "market_id_ix"))
            {
                market_stats.Indexes.CreateOne(new BsonDocument() { { "market_id", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "market_id_ix" });
            }
            if (!ContainsIndex(mainDb, marketStatsName, "time_ix"))
            {
                market_stats.Indexes.CreateOne(new BsonDocument() { { "time", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "time_ix" });
            }
            if (!ContainsIndex(mainDb, marketStatsName, "in_currency_ix"))
            {
                market_stats.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_ix" });
            }
            if (!ContainsIndex(mainDb, marketStatsName, "in_currency_out_currency_ix"))
            {
                market_stats.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 }, { "out_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_out_currency_ix" });
            }

            string marketRealName = "aaexchanges_real";
            alreadyExists = mainDb.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == marketRealName);
            if (!alreadyExists)
            {
                mainDb.CreateCollection(marketRealName);
            }
            var market_real = mainDb.GetCollection<BsonDocument>(marketRealName);
            if (!ContainsIndex(mainDb, marketRealName, "market_id_ix"))
            {
                market_real.Indexes.CreateOne(new BsonDocument() { { "market_id", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "market_id_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName, "time_ix"))
            {
                market_real.Indexes.CreateOne(new BsonDocument() { { "time", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "time_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName, "in_currency_ix"))
            {
                market_real.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName, "in_currency_out_currency_ix"))
            {
                market_real.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 }, { "out_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_out_currency_ix" });
            }

            string marketRealName2Days = "aaexchanges_real_twodays";
            alreadyExists = mainDb.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == marketRealName2Days);
            if (!alreadyExists)
            {
                mainDb.CreateCollection(marketRealName2Days);
            }
            var marketReal2Days = mainDb.GetCollection<BsonDocument>(marketRealName2Days);
            if (!ContainsIndex(mainDb, marketRealName2Days, "market_id_ix"))
            {
                marketReal2Days.Indexes.CreateOne(new BsonDocument() { { "market_id", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "market_id_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName2Days, "time_ix"))
            {
                marketReal2Days.Indexes.CreateOne(new BsonDocument() { { "time", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "time_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName2Days, "in_currency_ix"))
            {
                marketReal2Days.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealName2Days, "in_currency_out_currency_ix"))
            {
                marketReal2Days.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 }, { "out_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_out_currency_ix" });
            }

            string marketRealEachName = "aaexchanges_real_each";
            alreadyExists = mainDb.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == marketRealEachName);
            if (!alreadyExists)
            {
                mainDb.CreateCollection(marketRealEachName);
            }
            var marketRealEach = mainDb.GetCollection<BsonDocument>(marketRealEachName);
            if (!ContainsIndex(mainDb, marketRealEachName, "market_id_ix"))
            {
                marketRealEach.Indexes.CreateOne(new BsonDocument() { { "market_id", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "market_id_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealEachName, "time_ix"))
            {
                marketRealEach.Indexes.CreateOne(new BsonDocument() { { "time", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "time_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealEachName, "in_currency_ix"))
            {
                marketRealEach.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_ix" });
            }
            if (!ContainsIndex(mainDb, marketRealEachName, "in_currency_out_currency_ix"))
            {
                marketRealEach.Indexes.CreateOne(new BsonDocument() { { "in_currency", 1 }, { "out_currency", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "in_currency_out_currency_ix" });
            }


            Thread exchangeThread = new Thread(Exchange.ExchangeIndex);
            exchangeThread.Start(mainDb);

            var sbMongo = new StringBuilder();
            sbMongo.Append("mongodb://");
            sbMongo.Append(mcm.ServerAddress);
            sbMongo.Append(":");
            sbMongo.Append(mcm.ServerPort);
            var client = new MongoClient(sbMongo.ToString());
            var db = client.GetDatabase(mcm.ServerDB);

            string coinId = mcm.CoinID;
            string coin_address_name = coinId + "_address";
            string coin_address_name_balance = coinId + "_address_balance";
            string coin_transactions_name = coinId + "_transactions";
            string coin_last_block_name = coinId + "_last_block";
            string coin_block_name = coinId + "_block";
            string coin_asset_block_name = coinId + "_asset_block";
            string coin_block_orphan_name = coinId + "_block_orphan";
            string coin_masternodes_name = coinId + "_masternodes";
            string coin_rich_list_name = coinId + "_rich_list";
            string coin_peers_name = coinId + "_peers";

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_address_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_address_name);
                var coin_address = db.GetCollection<BsonDocument>(coin_address_name);
                coin_address.Indexes.CreateOne(new BsonDocument() { { "addrLower", 1 } }, new CreateIndexOptions() { Sparse = true });
                coin_address.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "Height_ix" });
                coin_address.Indexes.CreateOne(new BsonDocument() { { "type", 1 } }, new CreateIndexOptions() { Sparse = true });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_address_name_balance);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_address_name_balance);
                var coin_address_balance = db.GetCollection<BsonDocument>(coin_address_name_balance);
                coin_address_balance.Indexes.CreateOne(new BsonDocument() { { "addrLower", 1 } }, new CreateIndexOptions() { Sparse = true, Unique = true });
                coin_address_balance.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true });
                coin_address_balance.Indexes.CreateOne(new BsonDocument() { { "type", 1 } }, new CreateIndexOptions() { Sparse = true });
            }


            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_masternodes_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_masternodes_name);
                var coin_masternodes = db.GetCollection<BsonDocument>(coin_masternodes_name);
                coin_masternodes.Indexes.CreateOne(new BsonDocument() { { "payeeAddress", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
            }
            if (!ContainsIndex(db, coin_masternodes_name, "RemotePayeeAddress_ix"))
            {
                var coin_masternodes = db.GetCollection<BsonDocument>(coin_masternodes_name);
                coin_masternodes.Indexes.CreateOne(new BsonDocument() { { "RemotePayeeAddress", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "RemotePayeeAddress_ix" });
            }
            if (!ContainsIndex(db, coin_address_name, "Height_ix"))
            {
                var coin_address = db.GetCollection<BsonDocument>(coin_address_name);
                coin_address.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "Height_ix" });
            }
            if (!ContainsIndex(db, coin_masternodes_name, "RemotePayeeAddressLower_ix"))
            {
                var coin_masternodes = db.GetCollection<BsonDocument>(coin_masternodes_name);
                coin_masternodes.Indexes.CreateOne(new BsonDocument() { { "RemotePayeeAddressLower", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "RemotePayeeAddressLower_ix" });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_transactions_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_transactions_name);
                var coin_transactions = db.GetCollection<BsonDocument>(coin_transactions_name);
                coin_transactions.Indexes.CreateOne(new BsonDocument() { { "TxIdLower", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
                coin_transactions.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "Height_ix" });
            }
            if (!ContainsIndex(db, coin_transactions_name, "Height_ix"))
            {
                var coin_transactions = db.GetCollection<BsonDocument>(coin_transactions_name);
                coin_transactions.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true, Name = "Height_ix" });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_block_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_block_name);
                var coin_block = db.GetCollection<BsonDocument>(coin_block_name);
                coin_block.Indexes.CreateOne(new BsonDocument() { { "HashLower", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
                coin_block.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_asset_block_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_asset_block_name);
                var coin_asset_block = db.GetCollection<BsonDocument>(coin_asset_block_name);
                coin_asset_block.Indexes.CreateOne(new BsonDocument() { { "HashLower", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
                coin_asset_block.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
            }


            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_block_orphan_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_block_orphan_name);
                var coin_block_orphan = db.GetCollection<BsonDocument>(coin_block_orphan_name);
                coin_block_orphan.Indexes.CreateOne(new BsonDocument() { { "HashLower", 1 } }, new CreateIndexOptions() { Sparse = true });
                coin_block_orphan.Indexes.CreateOne(new BsonDocument() { { "Height", 1 } }, new CreateIndexOptions() { Sparse = true });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_rich_list_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_rich_list_name);
                var coin_rich_list = db.GetCollection<BsonDocument>(coin_rich_list_name);
                coin_rich_list.Indexes.CreateOne(new BsonDocument() { { "Address", 1 } }, new CreateIndexOptions() { Unique = true, Sparse = true });
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_last_block_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_last_block_name);
            }

            alreadyExists = db.ListCollections().ToList().Any(x => x.GetElement("name").Value.ToString() == coin_peers_name);
            if (!alreadyExists)
            {
                db.CreateCollection(coin_peers_name);
            }


            var sb = new StringBuilder();
            sb.Append(mcm.CoinProtocol);
            sb.Append("://");
            if (!string.IsNullOrEmpty(mcm.CoinAddressLocal))
            {
                sb.Append(mcm.CoinAddressLocal);
            }
            else
            {
                sb.Append(mcm.CoinAddress);
            }
            sb.Append(":");
            sb.Append(mcm.CoinPort);
            ICoinService CoinService = new BitcoinService(sb.ToString(), mcm.CoinLogin, mcm.Coinpassword, mcm.CoinWalletPassowrd);
            CoinService.Parameters.IsClient = true;

            var coinservice = new Mnwork()
            {
                CoinService = CoinService,
                DB = db,
                MAINDB = mainDb,
                MainCoinModel = mcm
            };
            Thread explorerThread = new Thread(Worker.BlockExploredUpdate);                
            explorerThread.Start(coinservice);

            Thread richlistThread = new Thread(Worker.RichList);
            richlistThread.Start(coinservice);

            Thread walletStatusThread = new Thread(Status.StatusIndex);
            walletStatusThread.Start(coinservice);

            Thread peerStatusThread = new Thread(WorkerPeer.PeerIndex);
            peerStatusThread.Start(coinservice);

            while (!tokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine("Please wait process termination");
            Interrupt = true;
            Exchange.AbortDTO.Interrupt = true;
            
        }
    }
}
