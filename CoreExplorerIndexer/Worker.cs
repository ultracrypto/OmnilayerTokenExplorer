using BitcoinLib.ExceptionHandling.Rpc;
using BitcoinLib.Models;
using BitcoinLib.Responses;
using BitcoinLib.Services.Coins.Base;
using CoreMongoDTO.DTO;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static CoreExplorerIndexer.Program;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;

namespace CoreExplorerIndexer
{
    public class Worker
    {
        public static readonly int LINE_NUM = 5;
        public static readonly int LINE_START = 4;

        public static object errorLock = new object();
        public static object objLock = new object();
        public static object objMnLock = new object();
        public static object objMnLockZero = new object();
        public static object objBlockRewardLock = new object();
        public static object objRichLock = new object();

        public static void WriteLine(string consoleLine)
        {
            Console.WriteLine(consoleLine);
        }

        public class TranCheckInfo
        {
            public string TxIn { get; set; }
            public string TxIn2 { get; set; }
            public string Original { get; set; }
        }

        public class MnPaymentDTO
        {
            public decimal Amount { get; set; }
            public long Time { get; set; }
        }

        public class TranOut
        {
            public string AddressLower { get; set; }
            public string Address { get; set; }
            public Decimal Value { get; set; }
            public string RewardType { get; set; }
            public int outPutSplit { get; set; }
            public bool isValid { get; set; }
        }

        public class AddrOut
        {
            public decimal Received { get; set; }
            public decimal Sent { get; set; }
            public decimal Total { get; set; }
            public decimal Transactions { get; set; }
            public long Height { get; set; }
        }

        public static void BlockExploredUpdate(object data)
        {
            var obj = data as Mnwork;
            var coinService = obj.CoinService as ICoinService;
            var coinName = obj.MainCoinModel.CoinID;
            var coin_address = obj.DB.GetCollection<BsonDocument>(coinName + "_address");
            IMongoCollection<BsonDocument> coin_address_balance = obj.DB.GetCollection<BsonDocument>(coinName + "_address_balance");
            var coin_addressObjDTO = obj.DB.GetCollection<AddressTransactionMongoDTO>(coinName + "_address");
            IMongoCollection<AddressTransactionMongoDTO> coin_addressObjDTOBalance = obj.DB.GetCollection<AddressTransactionMongoDTO>(coinName + "_address_balance");
            var coin_last_block = obj.DB.GetCollection<LastBlock>(coinName + "_last_block");
            var coin_block = obj.DB.GetCollection<BsonDocument>(coinName + "_block");
            var coin_asset_block = obj.DB.GetCollection<BsonDocument>(coinName + "_asset_block");
            var coin_block_orphan = obj.DB.GetCollection<BsonDocument>(coinName + "_block_orphan");
            var coin_transactions = obj.DB.GetCollection<BsonDocument>(coinName + "_transactions");
            var coin_transactionsDTO = obj.DB.GetCollection<CoinTransactionDTO>(coinName + "_transactions");
            var coins = obj.MAINDB.GetCollection<BsonDocument>("assets");
            var coins_obj = obj.MAINDB.GetCollection<MainCoinModel>("assets");
            var ownerAddresses = obj.MAINDB.GetCollection<OwnerAddressesDTO>("bbcoin_address_owner");
            Dictionary<string, CoinTransactionDTO> dictTran = new Dictionary<string, CoinTransactionDTO>();
            Dictionary<string, AddrOut> dictAddr = new Dictionary<string, AddrOut>();
            while (true)
            {
                MainCoinModel currentMcm = null;
                try
                {
                    currentMcm = coins_obj.Find(cn => cn.CoinSymbol == obj.MainCoinModel.CoinSymbol).FirstOrDefault();
                    if (!currentMcm.IsEnabled)
                    {
                        break;
                    }
                }
                catch
                {
                    WriteLine(coinName + " Error communicating main DB. Sleep 10000.");
                    Thread.Sleep(10000);
                    continue;
                }
                long cntBlk = coin_block.Count(_ => true);
                WriteLine(coinName + "_____________________________________________________");
                try
                {
                    uint countBlock = coinService.GetBlockCount() - obj.MainCoinModel.CoinConfirms;
                    var latstBlock = coin_last_block.Find(blk => blk.Type == "block").FirstOrDefault();
                    int start = 1;
                    if (latstBlock != null)
                    {
                        start = latstBlock.Block + 1;
                    }
                    if (start > countBlock)
                    {
                        WriteLine("Sleep 1000. " + coinName + " no new blocks");
                        Thread.Sleep(1000);
                        if (Interrupt)
                        {
                            WriteLine(coinName + " BlockExploredUpdate Interrupted");
                            break;
                        }
                        continue;
                    }
                    var existBlock = coin_block.Find(new BsonDocument() { { "Height", start } }).FirstOrDefault();
                    if (existBlock != null)
                    {
                        //throw exeption to delete blocks
                        throw new MongoBlockFail(start + " exists", start);
                    }
                    WriteLine(string.Format(coinName + " countBlock: {0} latstBlock {1}", countBlock, start));
                    List<Address> addreses = new List<Address>();
                    bool updateHashrate = false;
                    bool updateMoney = false;
                    for (int i = start; i <= countBlock; i++)
                    {
                        if (countBlock - i > 100)
                        {
                            currentMcm = coins_obj.Find(cn => cn.CoinSymbol == obj.MainCoinModel.CoinSymbol).FirstOrDefault();
                        }
                        List<OwnerAddressesDTO> ownerAddressList = ownerAddresses.Find(ca => ca.CoinSymbol == obj.MainCoinModel.CoinSymbol).ToList();
                        var dx = coinService.GetBlockHash(i);
                        if (dx == null)
                        {
                            throw new OrphanException("Orphan!", i, 1);
                        }
                        var block = coinService.GetBlock(dx);
                        if (block == null)
                        {
                            throw new OrphanException("Orphan!", i, 1);
                        }
                        if (cntBlk > 0)
                        {
                            var blkMngPrev = coin_block.Find(new BsonDocument() { { "HashLower", block.PreviousBlockHash.ToLower() } }).FirstOrDefault();
                            if (blkMngPrev == null || Convert.ToUInt32(blkMngPrev["Height"].ToString()) != block.Height - 1)
                            {
                                if (i == countBlock)
                                {
                                    throw new OrphanException("Previous block orphan!", i, 1, true);
                                }
                                else
                                {
                                    throw new OrphanException("Previous block orphan!", i, 1);
                                }
                            }
                        }
                        var blkMng = coin_block.Find(new BsonDocument() { { "HashLower", block.Hash } }).FirstOrDefault();
                        if (blkMng != null)
                        {
                            throw new OrphanException("Orphan!", i, 1);
                        }
                        var omniTxList = coinService.GetOmniListBlockTransactions(block.Height);
                        int y = 0;
                        object _locker = new object();
                        ConcurrentDictionary<string, GetOmniTransactionResponse> trxDict = new ConcurrentDictionary<string, GetOmniTransactionResponse>();
                        var exceptions = new ConcurrentQueue<Exception>();
                        int iLock = 0;
                        object _ilocker = new object();
                        Parallel.ForEach(omniTxList, new ParallelOptions { MaxDegreeOfParallelism = obj.MainCoinModel.MaxDegreeOfParallelism }, (transaction) =>
                        {
                            GetOmniTransactionResponse tx = coinService.GetOmniTransaction(transaction);
                            if (tx == null)
                            {
                                exceptions.Enqueue(new OrphanException("Orphan", block.Height, 1));
                                return;
                            }
                            lock (_ilocker)
                            {
                                iLock++;
                            }
                            trxDict.TryAdd(transaction, tx);
                        });
                        if (iLock != omniTxList.Count)
                        {
                            throw new OrphanException("Orphan", block.Height, 1);
                        }
                        if (exceptions.Count > 0)
                        {
                            throw new OrphanException("Orphan", block.Height, 1);
                        }
                        List<BsonDocument> bsonTxList = new List<BsonDocument>();
                        List<BsonDocument> bsonTrx = new List<BsonDocument>();
                        List<WriteModel<BsonDocument>> bulkOps = new List<WriteModel<BsonDocument>>();
                        List<string> currentAssetTxes = new List<string>();
                        int currentAssetTxCount = 0;
                        if (omniTxList.Count > 0)
                        {
                            foreach (var transaction in omniTxList)
                            {
                                GetOmniTransactionResponse tx = null;
                                if (trxDict.ContainsKey(transaction))
                                {
                                    tx = trxDict[transaction];
                                }
                                else
                                {
                                    throw new OrphanException("Orphan", block.Height, 1);
                                }
                                if (tx == null)
                                {
                                    throw new OrphanException("Orphan", block.Height, 1);
                                }
                                if (tx.propertyid != obj.MainCoinModel.AssetId)
                                {
                                    continue;
                                }
                                currentAssetTxCount++;
                                currentAssetTxes.Add(transaction);
                                List<ClientTransactionAddress> lst = new List<ClientTransactionAddress>();
                                decimal amount = decimal.Parse(tx.amount, CultureInfo.InvariantCulture);
                                uint blocktime = Convert.ToUInt32(tx.blocktime);

                                if (tx.type_int == 50)
                                {

                                }
                                else
                                {
                                    lst.Add(new ClientTransactionAddress()
                                    {
                                        Address = tx.sendingaddress,
                                        Amount = -amount,
                                        BlockTime = blocktime,
                                        RewardType = "OUT",
                                        isValid = tx.valid
                                    });
                                }

                                BsonDocument mainDocument = TransactionToBson(tx);
                                mainDocument.Add("Height", block.Height);
                                bsonTxList.Add(mainDocument);

                                String addreessIn = string.Empty;
                                foreach (var l in lst)
                                {
                                    decimal received = 0;
                                    decimal sent = 0;
                                    decimal total = 0;
                                    decimal transactions = 0;
                                    AddressTransactionMongoDTO addressLast = null;
                                    if (!dictAddr.ContainsKey(l.Address.ToLower()))
                                    {
                                        var addrl = coin_addressObjDTOBalance.Find(a => a.AddrLower == l.Address.ToLower()).FirstOrDefault();
                                        if (addrl != null)
                                        {
                                            addressLast = addrl;
                                        }
                                        else
                                        {
                                            var sort = new BsonDocument
                                        {
                                            {
                                                "_id", -1
                                            },
                                        };
                                            addressLast = coin_addressObjDTO.Find(a => a.AddrLower == l.Address.ToLower()).Sort(sort).Limit(1).FirstOrDefault();
                                        }
                                        dictAddr.Add(l.Address.ToLower(), new AddrOut());
                                    }
                                    else
                                    {
                                        addressLast = new AddressTransactionMongoDTO();
                                        addressLast.Received = dictAddr[l.Address.ToLower()].Received;
                                        addressLast.Sent = dictAddr[l.Address.ToLower()].Sent;
                                        addressLast.Total = dictAddr[l.Address.ToLower()].Total;
                                        addressLast.Transactions = dictAddr[l.Address.ToLower()].Transactions;
                                    }
                                    if (addressLast != null)
                                    {
                                        received = addressLast.Received;
                                        sent = addressLast.Sent;
                                        total = addressLast.Total;
                                        transactions = addressLast.Transactions;
                                    }
                                    if (l.isValid)
                                    {
                                        if (l.Amount > 0)
                                        {
                                            received += l.Amount;
                                        }
                                        else
                                        {
                                            sent += l.Amount;
                                        }
                                        total += l.Amount;
                                    }
                                    transactions++;

                                    if (total < 0 && l.Address != "-1" && l.Address.ToLower() != "unknown")
                                    {
                                        //Bug?
                                        var bug = 1;
                                        throw new FatalException(string.Format("total: {0} l.Address: {1} l.Amount : {2} addressLast.Total: {3}", total, l.Address, l.Amount, addressLast.Total));
                                    }
                                    var document = new BsonDocument
                                    {
                                        {"addr", l.Address},
                                        {"addrLower", l.Address.ToLower()},
                                        {"time", block.Time},
                                        {"amount", l.Amount},
                                        {"transaction", transaction},
                                        {"Height", block.Height},
                                        {"Type", l.RewardType},
                                        {"Received", received},
                                        {"Sent", sent},
                                        {"Total", total},
                                        {"Valid", l.isValid},
                                        {"Transactions", transactions}
                                    };
                                    var documentUpdate = new BsonDocument
                                    {
                                        {"addr", l.Address},
                                        {"addrLower", l.Address.ToLower()},
                                        {"time", block.Time},
                                        {"amount", l.Amount},
                                        {"transaction", transaction},
                                        {"Height", block.Height},
                                        {"Type", l.RewardType},
                                        {"Received", received},
                                        {"Sent", sent},
                                        {"Total", total},
                                        {"Transactions", transactions}
                                    };
                                    try
                                    {
                                        bsonTrx.Add(document);
                                        //coin_address.InsertOneAsync(document);
                                        dictAddr[l.Address.ToLower()].Received = received;
                                        dictAddr[l.Address.ToLower()].Sent = sent;
                                        dictAddr[l.Address.ToLower()].Total = total;
                                        dictAddr[l.Address.ToLower()].Transactions = transactions;
                                        dictAddr[l.Address.ToLower()].Height = block.Height;
                                        var documentBalance = new BsonDocument
                                        {
                                            {
                                                "$set", documentUpdate
                                            }
                                        };
                                        var upsertOne = new UpdateOneModel<BsonDocument>(new BsonDocument() { { "addrLower", l.Address.ToLower() } }, documentBalance) { IsUpsert = true };
                                        //coin_address_balance.UpdateOne(new BsonDocument() { { "addrLower", l.Address.ToLower() } }, documentBalance, new UpdateOptions { IsUpsert = true });
                                        bulkOps.Add(upsertOne);
                                    }
                                    catch (MongoWriteException e)
                                    {
                                        throw e;
                                    }
                                }

                                Dictionary<string, TranOut> trOut = new Dictionary<string, TranOut>();
                                HashSet<string> addrCount = new HashSet<string>();
                                Dictionary<string, decimal> _addrPosPossible = new Dictionary<string, decimal>();
                                if (tx.type_int == 50)
                                {
                                    var keyOut = tx.sendingaddress.ToLower() + "IN";
                                    trOut.Add(keyOut, new TranOut()
                                    {
                                        AddressLower = tx.sendingaddress.ToLower(),
                                        Address = tx.sendingaddress,
                                        Value = amount,
                                        RewardType = "IN",
                                        outPutSplit = 1,
                                        isValid = tx.valid
                                    });
                                }
                                else
                                {
                                    var keyOut = tx.referenceaddress.ToLower() + "IN";
                                    trOut.Add(keyOut, new TranOut()
                                    {
                                        AddressLower = tx.referenceaddress.ToLower(),
                                        Address = tx.referenceaddress,
                                        Value = amount,
                                        RewardType = "IN",
                                        outPutSplit = 1,
                                        isValid = tx.valid
                                    });
                                }
                                foreach (KeyValuePair<string, TranOut> tr in trOut)
                                {
                                    decimal received = 0;
                                    decimal sent = 0;
                                    decimal total = 0;
                                    decimal transactions = 0;
                                    AddressTransactionMongoDTO addressLast = null;
                                    if (!dictAddr.ContainsKey(tr.Value.AddressLower))
                                    {
                                        var addrl = coin_addressObjDTOBalance.Find(a => a.AddrLower == tr.Value.AddressLower).FirstOrDefault();
                                        if (addrl != null)
                                        {
                                            addressLast = addrl;
                                        }
                                        else
                                        {
                                            var sort = new BsonDocument
                                        {
                                            {
                                                "_id", -1
                                            },
                                        };
                                            addressLast = coin_addressObjDTO.Find(a => a.AddrLower == tr.Value.AddressLower).Sort(sort).Limit(1).FirstOrDefault();
                                        }
                                        dictAddr.Add(tr.Value.AddressLower, new AddrOut());
                                    }
                                    else
                                    {
                                        addressLast = new AddressTransactionMongoDTO();
                                        addressLast.Received = dictAddr[tr.Value.AddressLower].Received;
                                        addressLast.Sent = dictAddr[tr.Value.AddressLower].Sent;
                                        addressLast.Total = dictAddr[tr.Value.AddressLower].Total;
                                        addressLast.Transactions = dictAddr[tr.Value.AddressLower].Transactions;
                                    }

                                    if (addressLast != null)
                                    {
                                        received = addressLast.Received;
                                        sent = addressLast.Sent;
                                        total = addressLast.Total;
                                        transactions = addressLast.Transactions;
                                    }

                                    if (tr.Value.isValid)
                                    {
                                        if (tr.Value.Value > 0)
                                        {
                                            received += tr.Value.Value;
                                        }
                                        else
                                        {
                                            sent += tr.Value.Value;
                                        }
                                        total += tr.Value.Value;
                                    }
                                    transactions++;
                                    if (total == 0)
                                    {
                                        total = 0;
                                    }
                                    if (total < 0 && tr.Value.AddressLower != "-1" && tr.Value.AddressLower != "unknown")
                                    {
                                        //Bug?
                                        var bug = 1;
                                        throw new FatalException(string.Format("total: {0} l.Address: {1} l.Amount : {2} addressLast.Total: {3}", total, tr.Value.Address, tr.Value.Value, addressLast.Total));
                                    }
                                    var document = new BsonDocument
                                    {
                                        {"addr", tr.Value.Address},
                                        {"addrLower", tr.Value.AddressLower},
                                        {"time", block.Time},
                                        {"amount", tr.Value.Value},
                                        {"transaction", transaction},
                                        {"Height", block.Height},
                                        {"Type", tr.Value.RewardType},
                                        {"Received", received},
                                        {"Sent", sent},
                                        {"Total", total},
                                        {"Valid", tr.Value.isValid},
                                        {"Transactions", transactions}
                                    };
                                    var documentUpdate = new BsonDocument
                                    {
                                        {"addr", tr.Value.Address},
                                        {"addrLower", tr.Value.AddressLower},
                                        {"time", block.Time},
                                        {"amount", tr.Value.Value},
                                        {"transaction", transaction},
                                        {"Height", block.Height},
                                        {"Type", tr.Value.RewardType},
                                        {"Received", received},
                                        {"Sent", sent},
                                        {"Total", total},
                                        {"Transactions", transactions}
                                    };
                                    try
                                    {
                                        bsonTrx.Add(document);
                                        dictAddr[tr.Value.AddressLower].Received = received;
                                        dictAddr[tr.Value.AddressLower].Sent = sent;
                                        dictAddr[tr.Value.AddressLower].Total = total;
                                        dictAddr[tr.Value.AddressLower].Transactions = transactions;
                                        dictAddr[tr.Value.AddressLower].Height = block.Height;
                                        var documentBalance = new BsonDocument
                                        {
                                            {
                                                "$set", documentUpdate
                                            }
                                        };
                                        var upsertOne = new UpdateOneModel<BsonDocument>(new BsonDocument() { { "addrLower", tr.Value.AddressLower } }, documentBalance) { IsUpsert = true };
                                        bulkOps.Add(upsertOne);
                                        //coin_address_balance.UpdateOne(new BsonDocument() { { "addrLower", tr.Value.AddressLower } }, documentBalance, new UpdateOptions { IsUpsert = true });
                                    }
                                    catch (MongoWriteException e)
                                    {
                                        throw e;
                                    }

                                }
                                y++;
                            }
                        }
                        int blkTxCount = block.Tx.Count;
                        try
                        {
                            if (block.Additional == null)
                            {
                                block.Additional = new Dictionary<string, object>();
                            }
                            block.Additional.Add("txcount", blkTxCount);
                            block.Additional.Add("assettxcount", omniTxList.Count);
                            block.Additional.Add(coinName + "_assettxcount", currentAssetTxCount);
                            block.AssetTX = currentAssetTxes;
                            block.Tx.Clear();
                            var bson = block.ToBsonDocument();
                            bson.Add("HashLower", block.Hash.ToLower());
                            coin_block.InsertOne(bson);
                            if (currentAssetTxCount > 0)
                            {
                                coin_asset_block.InsertOne(bson);
                            }
                            if (bsonTxList.Count > 0)
                            {
                                coin_transactions.BulkWrite(bsonTxList.Select(d => new InsertOneModel<BsonDocument>(d)));
                            }
                            if (bulkOps.Count > 0)
                            {
                                coin_address_balance.BulkWrite(bulkOps);
                            }
                            if (bsonTrx.Count > 0)
                            {
                                coin_address.BulkWrite(bsonTrx.Select(d => new InsertOneModel<BsonDocument>(d)));
                            }
                        }
                        catch (MongoWriteException e)
                        {
                            throw e;
                        }
                        WriteLine(coinName + "_________________________________________________________");
                        WriteLine(coinName + " Block: " + block.Height + " TX Count: " + blkTxCount + " Asset TX Count: " + omniTxList.Count + " Current Asset TX Count: " + currentAssetTxCount);
                        var blkMongo = new LastBlock() { Type = "block", Block = block.Height };
                        try
                        {
                            if (latstBlock == null)
                            {
                                coin_last_block.InsertOne(blkMongo);
                            }
                            else
                            {
                                coin_last_block.UpdateOne(Builders<LastBlock>.Filter.Eq("type", "block"), Builders<LastBlock>.Update.Set("block", block.Height));
                            }

                            var mainDocumentMn = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"dynamic_lastblock", block.Height},
                                            {"dynamic_lastblock_age", block.Time}
                                        }
                                }
                            };
                            coins.UpdateOne(new BsonDocument() { { "coin_id", coinName } }, mainDocumentMn, new UpdateOptions { IsUpsert = true });

                        }
                        catch (MongoWriteException e)
                        {
                            throw e;
                        }
                        latstBlock = blkMongo;
                        if (Interrupt)
                        {
                            WriteLine(coinName + " BlockExploredUpdate Interrupted Inner");
                            break;
                        }
                        updateHashrate = true;
                        updateMoney = true;
                        if (dictTran.Count > MaxCache && dictTran.Count > 1000)
                        {
                            foreach (var k in dictTran.Keys.Take(Math.Max(MaxCache, 1000) / 2).ToList())
                            {
                                dictTran.Remove(k);
                            }
                        }
                        if (dictAddr.Count > 10000)
                        {
                            foreach (var k in dictAddr.Where(dt => dt.Value.Height < block.Height - 1000).Select(k => k.Key).ToList())
                            {
                                dictAddr.Remove(k);
                            }
                        }
                        cntBlk++;
                    }
                    if (updateHashrate)
                    {
                        var miningInfo = coinService.GetMiningInfo();
                        BsonDocument mainDocumentMn;
                        if (miningInfo.DiffList != null && miningInfo.DiffList.Count > 0)
                        {
                            var bsn = new BsonDocument(miningInfo.DiffList);
                            mainDocumentMn = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"proof_of_work", miningInfo.ProofOfWork},
                                            {"proof_of_stake", miningInfo.ProofOfStake},
                                            {"network_hash_ps", miningInfo.NetworkHashPS},
                                            {"proof_of_work_list", bsn}
                                        }
                                }
                            };
                        }
                        else
                        {
                            mainDocumentMn = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"proof_of_work", miningInfo.ProofOfWork},
                                            {"proof_of_stake", miningInfo.ProofOfStake},
                                            {"network_hash_ps", miningInfo.NetworkHashPS}
                                        }
                                }
                            };
                        }
                        coins.UpdateOne(new BsonDocument() { { "coin_id", coinName } }, mainDocumentMn, new UpdateOptions { IsUpsert = true });
                    }

                    if (updateMoney)
                    {
                        try
                        {
                            GetInfoResponseMoney resp = coinService.GetInfoMoney();
                            var suppl = resp.moneysupply.GetValueOrDefault();
                            if (suppl > 0)
                            {
                                var mainDocumentMoneySupply = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"current_money_supply", resp.moneysupply.GetValueOrDefault()}
                                        }
                                }
                            };
                                coins.UpdateOne(new BsonDocument() { { "coin_id", coinName } }, mainDocumentMoneySupply, new UpdateOptions { IsUpsert = true });
                            }
                        }
                        catch
                        {

                        }
                    }

                    WriteLine(coinName + " Sleep 1000");
                    Thread.Sleep(1000);
                    if (Interrupt)
                    {
                        WriteLine(coinName + " BlockExploredUpdate Interrupted");
                        break;
                    }
                }
                catch (ConnectionException e)
                {
                    dictTran.Clear();
                    dictAddr.Clear();
                    WriteLine(coinName + " ConnectionException Sleep 1000");
                    Thread.Sleep(1000);
                    continue;
                }
                catch (MongoBlockFail mongoFail)
                {
                    WriteLine(coinName + " MongoBlockFail " + mongoFail.ToString() + " Blocks will be removed. Sleep");
                    dictTran.Clear();
                    dictAddr.Clear();
                    coin_address.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", mongoFail.RemoveFromBlock } }
                        }
                    });
                    coin_address_balance.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", mongoFail.RemoveFromBlock } }
                        }
                    });
                    coin_transactions.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", mongoFail.RemoveFromBlock } }
                        }
                    });
                    coin_block.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", mongoFail.RemoveFromBlock } }
                        }
                    });
                    coin_asset_block.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", mongoFail.RemoveFromBlock } }
                        }
                    });

                    lock (errorLock)
                    {
                        File.AppendAllText("error_mongo.txt", coinName + " " + mongoFail.ToString() + " block:" + mongoFail.RemoveFromBlock + Environment.NewLine);
                    }
                    WriteLine(coinName + " MongoBlockFail " + mongoFail.ToString() + " Blocks removed. Sleep");
                    Thread.Sleep(1000);
                }
                catch (OrphanException oe)
                {
                    dictTran.Clear();
                    dictAddr.Clear();
                    var removeBlocks = oe.BlockId - oe.RemoveStart;
                    if (oe.Save)
                    {
                        var existBlock = coin_block.Find(new BsonDocument() { { "Height", removeBlocks } }).FirstOrDefault();
                        if (existBlock != null)
                        {
                            coin_block_orphan.InsertOne(existBlock);
                        }
                    }
                    coin_address.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", removeBlocks } }
                        }
                    });
                    coin_address_balance.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", removeBlocks } }
                        }
                    });
                    coin_transactions.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", removeBlocks } }
                        }
                    });
                    coin_block.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", removeBlocks } }
                        }
                    });
                    coin_asset_block.DeleteMany(new BsonDocument()
                    {
                        {
                            "Height", new BsonDocument
                            { { "$gte", removeBlocks } }
                        }
                    });
                    var rm = removeBlocks - 1;
                    if (rm < 1)
                    {
                        rm = 1;
                    }
                    coin_last_block.UpdateOne(Builders<LastBlock>.Filter.Eq("type", "block"), Builders<LastBlock>.Update.Set("block", rm), new UpdateOptions { IsUpsert = true });

                    lock (errorLock)
                    {
                        File.AppendAllText("orphan.txt", coinName + " " + oe.ToString() + Environment.NewLine);
                    }
                    WriteLine(coinName + " Blocks removed. Sleep");
                    Thread.Sleep(1000);

                }
                catch (Exception e)
                {
                    dictTran.Clear();
                    dictAddr.Clear();
                    lock (errorLock)
                    {
                        File.AppendAllText("error.txt", coinName + " " + e.ToString() + Environment.NewLine);
                    }
                    WriteLine(coinName + " General Exception Sleep 10000");
                    Thread.Sleep(10000);
                    continue;
                }
            }
        }

        public static void RichList(object data)
        {
            var obj = data as Mnwork;
            var coinService = obj.CoinService as ICoinService;
            var coinName = obj.MainCoinModel.CoinID;
            var coin_rich_list = obj.DB.GetCollection<BsonDocument>(coinName + "_rich_list");
            var coin_address = obj.DB.GetCollection<BsonDocument>(coinName + "_address");
            IMongoCollection<BsonDocument> coin_address_balance = obj.DB.GetCollection<BsonDocument>(coinName + "_address_balance");
            IMongoCollection<AddressTransactionMongoDTO> coin_addressObjDTOBalance = obj.DB.GetCollection<AddressTransactionMongoDTO>(coinName + "_address_balance");
            var assets = obj.MAINDB.GetCollection<BsonDocument>("assets");
            var assets2 = obj.MAINDB.GetCollection<MainCoinModel>("assets");
            while (true)
            {
                var uptime = Funcs.DateTimeToUnixTimestamp(DateTime.UtcNow);
                MainCoinModel CoinInfo;
                try
                {
                    CoinInfo = assets2.Find(c => c.CoinSymbol == obj.MainCoinModel.CoinSymbol && c.Version == Program.VERSION).First();
                    if (CoinInfo.NextRichlist > uptime)
                    {
                        WriteLine(coinName + " richlist sleep 10000");
                        Thread.Sleep(10000);
                        if (Interrupt)
                        {
                            WriteLine(coinName + " richlist Interrupted sleep");
                            break;
                        }
                        continue;
                    }
                }
                catch (Exception e)
                {
                    lock (errorLock)
                    {
                        File.AppendAllText("richlist_error.txt", coinName + " " + e.ToString() + Environment.NewLine);
                    }
                    WriteLine(coinName + " General exception Sleep 10000");
                    Thread.Sleep(10000);
                    continue;
                }
                try
                {
                    lock (objRichLock)
                    {
                        File.AppendAllText("richilist.txt", coinName + " start - ");
                        var groupSum = new BsonDocument
                        {
                            {
                                "_id", string.Empty
                            },
                            {
                                "Received", new BsonDocument
                                {
                                    {
                                        "$sum", "$Received"
                                    }
                                }
                            },
                            {
                                "Sent", new BsonDocument
                                {
                                    {
                                        "$sum", "$Sent"
                                    }
                                }
                            },
                            {
                                "Total", new BsonDocument
                                {
                                    {
                                        "$sum", "$Total"
                                    }
                                }
                            },
                            {
                                "Transactions", new BsonDocument
                                {
                                    { "$sum", "$Transactions"  }
                                }
                            },
                            {
                                "Address", new BsonDocument
                                {
                                    { "$first", "$addr"  }
                                }
                            }
                        };
                        var totalInfo = coin_address_balance.Aggregate(new AggregateOptions { AllowDiskUse = true }).Group<BsonDocument>(groupSum).FirstOrDefault();
                        Decimal Total = 0.0m;
                        Decimal Received = 0.0m;
                        Decimal Sent = 0.0m;
                        int Transactions = 0;
                        if (totalInfo != null)
                        {
                            Total = Convert.ToDecimal(totalInfo["Total"]);
                            Received = Convert.ToDecimal(totalInfo["Received"]);
                            Sent = Convert.ToDecimal(totalInfo["Sent"]);
                            Transactions = Convert.ToInt32(totalInfo["Transactions"]);
                        }
                        BsonDocument mainDocument;
                        decimal totalWithBurn = Total - CoinInfo.BurnAmount;

                        var addressTotal = coin_addressObjDTOBalance.Find(_ => true).Count();
                        var addressTotalHodl = coin_addressObjDTOBalance.Find(a => a.Total > 0).Count();
                        var addressTotalTime30 = coin_addressObjDTOBalance.Find(a => a.Time > uptime - 30 * 60 * 60 * 24).Count();
                        var addressTotalTime7 = coin_addressObjDTOBalance.Find(a => a.Time > uptime - 7 * 60 * 60 * 24).Count();

                        try
                        {
                            GetInfoResponseMoney resp = coinService.GetInfoMoney();
                            mainDocument = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"dynamic_received", Received},
                                            {"dynamic_sent", Sent},
                                            {"dynamic_total", Total},
                                            {"dynamic_transactions", Transactions},
                                            {"dynamic_updateTime", uptime},
                                            {"next_richlist", uptime + CoinInfo.RichInterval},
                                            {"current_money_supply", resp.moneysupply.GetValueOrDefault()},
                                            {"total_addresses", addressTotal},
                                            {"total_addresses_hodl", addressTotalHodl},
                                            {"total_active_addresses_30", addressTotalTime30},
                                            {"total_active_addresses_7", addressTotalTime7}
                                        }
                                }
                            };
                            if (resp.moneysupply.GetValueOrDefault() > 0)
                            {
                                totalWithBurn = resp.moneysupply.GetValueOrDefault();
                            }
                        }
                        catch
                        {
                            mainDocument = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"dynamic_received", Received},
                                            {"dynamic_sent", Sent},
                                            {"dynamic_total", Total},
                                            {"dynamic_transactions", Transactions},
                                            {"dynamic_updateTime", uptime},
                                            {"next_richlist", uptime + CoinInfo.RichInterval},
                                            {"current_money_supply", 0},
                                            {"total_addresses", addressTotal},
                                            {"total_addresses_hodl", addressTotalHodl},
                                            {"total_active_addresses_30", addressTotalTime30},
                                            {"total_active_addresses_7", addressTotalTime7}
                                        }
                                }
                            };
                        }

                        var sort = new BsonDocument
                        {
                            {
                                "Total", -1
                            }
                        };

                        var address = coin_addressObjDTOBalance.Find(_ => true).Sort(sort).Limit(obj.MainCoinModel.MaxRichList).ToList();

                        foreach (var addr in address)
                        {
                            var total_addr = Convert.ToDecimal(addr.Total);
                            var document = new BsonDocument
                            {
                                {
                                    "$set", new BsonDocument
                                        {
                                            {"Address", addr.Addr},
                                            {"Received", Convert.ToDecimal(addr.Received)},
                                            {"Sent", Convert.ToDecimal(addr.Sent)},
                                            {"Total", total_addr},
                                            {"TotalPercent", total_addr * 100 / totalWithBurn},
                                            {"Transactions", Convert.ToInt32(addr.Transactions)},
                                            {"UpdateTime", uptime}
                                        }
                                }
                            };
                            coin_rich_list.UpdateOne(new BsonDocument() { { "Address", addr.Addr } }, document, new UpdateOptions { IsUpsert = true });
                        }
                        coin_rich_list.DeleteMany(new BsonDocument()
                        {
                            {
                                "UpdateTime", new BsonDocument
                                { { "$lt", uptime } }
                            }
                        });

                        assets.UpdateOne(new BsonDocument() { { "coin_id", obj.MainCoinModel.CoinID } }, mainDocument, new UpdateOptions { IsUpsert = true });
                        File.AppendAllText("richilist.txt", coinName + " end\n");
                    }
                    WriteLine(coinName + " richlist updated");
                    Thread.Sleep(1000);
                    if (Interrupt)
                    {
                        WriteLine(coinName + " richlist Interrupted");
                        break;
                    }
                }
                catch (ConnectionException e)
                {
                    lock (errorLock)
                    {
                        File.AppendAllText("richlist_error.txt", coinName + " " + e.ToString() + Environment.NewLine);
                    }
                    WriteLine(coinName + " ConnectionException Sleep 1000");
                    Thread.Sleep(1000);
                    continue;
                }
                catch (Exception e)
                {
                    lock (errorLock)
                    {
                        File.AppendAllText("richlist_error.txt", coinName + " " + e.ToString() + Environment.NewLine);
                    }
                    WriteLine(coinName + " General exception Sleep 10000");
                    Thread.Sleep(10000);
                    continue;
                }
            }
        }
    }
}
