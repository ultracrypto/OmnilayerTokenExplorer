using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinLib.Services.Coins.Base;
using BitcoinLib.Responses;
using CoreExplorerV3.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using CoreExplorerV3.DTO;
using CoreMongoDTO.DTO;

namespace CoreExplorerV3.Services
{
    public partial class MainCoins
    {
        private ICoinService CoinService;
        public ICoinService CoinServiceInstance
        {
            get
            {
                return CoinService;
            }
        }

        public GetNetworkInfoResponse GetNetworkInfo()
        {
            var resp = CoinService.GetNetworkInfo();
            return resp;
        }

        public List<GetPeerInfoResponse> GetPeerInfo()
        {
            var resp = CoinService.GetPeerInfo();
            return resp;
        }

        public bool AddAddress(string address, string message, string signature, string ownername, string coinID)
        {
            var currentUtcTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var lowerAddress = address.ToLower();
            var apiCoins = _dtoMain.mainDb.GetCollection<VerifyAddressesDTO>("verify_address");
            var claim = apiCoins.Find(c => c.CoinAddressLower == lowerAddress && c.CoinSymbol == coinID).FirstOrDefault();
            if (claim == null || claim.Update + 24 * 60 * 60 < currentUtcTime)
            {
                var valid = TryClaim(address, message, signature);
                if (valid)
                {
                    try
                    {
                        var verifyAddress = new VerifyAddressesDTO()
                        {
                            Id = ObjectId.GenerateNewId(),
                            CoinAddress = address,
                            CoinAddressLower = lowerAddress,
                            CoinMessage = message,
                            CoinOwner = ownername,
                            CoinSignature = signature,
                            CoinSymbol = coinID,
                            Update = currentUtcTime
                        };
                        apiCoins.ReplaceOneAsync(
                            filter: new BsonDocument() { { "coin_address_lower", lowerAddress }, { "coin_symbol", coinID } },
                            options: new UpdateOptions { IsUpsert = true },
                            replacement: verifyAddress
                        );
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public bool IsValidAddres(String address)
        {
            var resp = CoinService.ValidateAddress(address);
            return resp.IsValid;
        }

        public bool TryClaim(string address, string message, string signature)
        {
            var resp = CoinService.VerifyMessage(address, signature, message);
            return resp;
        }       

        public List<AddressTransaction> GetAddressTransactions(String address, int start, int takeCount)
        {
            /*
             * 
             * db.getCollection('*_address').find(   
                     { addr : "CM1rbhABy86FrPCS3nEuNtini2WM9j99DZ" }
                ).sort({time: -1});
            */
            //db.getCollection('dgb_address').find({ addrLower: "dkbukhncxdmsqlh94fgngo71rdz6rt6far"}).sort({$natural: -1}).limit(50)


            var sort = new BsonDocument
            {
                {
                    "_id", -1
                },
            };
            var coin_address = _dtoMain.db.GetCollection<AddressTransaction>(_dtoMain.mainModel.CoinID + "_address");
            var lst = coin_address.Find(f => f.AddrLower == address.ToLower()).Sort(sort).Skip(start).Limit(takeCount).ToList();
            return lst;
        }

        public long GetAddressTransactionsCount(String address)
        {
            var coin_address = _dtoMain.db.GetCollection<AddressTransaction>(_dtoMain.mainModel.CoinID + "_address");
            var lst = coin_address.Count(f => f.AddrLower == address.ToLower());
            return lst;
        }

        public object SearchAddressInfo(String address)
        {
            var coin_address = _dtoMain.db.GetCollection<BsonDocument>(_dtoMain.mainModel.CoinID + "_address");
            return coin_address.Find(new BsonDocument() { { "addrLower", address.ToLower() } }).FirstOrDefault();
        }

        public AddressTransactionInfo GetAddressInfo(String address)
        {
            var coin_address_balance = _dtoMain.db.GetCollection<AddressTransaction>(_dtoMain.mainModel.CoinID + "_address_balance");
            var addr_balance = coin_address_balance.Find(f => f.AddrLower == address.ToLower()).FirstOrDefault();
            if (addr_balance != null)
            {
                var ret = new AddressTransactionInfo();
                AddressInfo ai = new AddressInfo();
                ai.Address = address;
                ai.Received = addr_balance.Received;
                ai.Sent = addr_balance.Sent;
                ai.Transactions = Convert.ToSingle(addr_balance.Transactions);
                ai.Total = addr_balance.Total;
                ret.AddressInfo = ai;
                ret.AddressTransaction = addr_balance;
                return ret;
            }
            var sort = new BsonDocument
            {
                {
                    "_id", -1
                },
            };
            var coin_address2 = _dtoMain.db.GetCollection<AddressTransaction>(_dtoMain.mainModel.CoinID + "_address");
            var addr = coin_address2.Find(f => f.AddrLower == address.ToLower()).Sort(sort).Limit(1).FirstOrDefault();
            if (addr != null)
            {
                var ret = new AddressTransactionInfo();
                AddressInfo ai = new AddressInfo();
                ai.Address = address;
                ai.Received = addr.Received;
                ai.Sent = addr.Sent;
                ai.Transactions = Convert.ToSingle(addr.Transactions);
                ai.Total = addr.Total;
                ret.AddressInfo = ai;
                ret.AddressTransaction = addr_balance;
                return ret;
            }
            return null;
            /*
             * db.getCollection('*_address').aggregate([    
                 { $match : { addr : "CM1rbhABy86FrPCS3nEuNtini2WM9j99DZ" } },
                 { $group : 
                { _id: '$addr', 
                  'Received' : { $sum : { $cond: [{ $gte: [ '$amount', 0]},'$amount',0]}}, 
                  'Sent' : { $sum : { $cond: [{ $lte: [ '$amount', 0]},'$amount',0]}}, 
                  'Total' : { $sum : '$amount'},
                  'Transactions' : {$sum: 1}
                }}
            ])
            var match = new BsonDocument
            {
                {
                    "addrLower", address.ToLower()
                },
            };
            var group = new BsonDocument
            {
                {
                    "_id", "$addrLower"
                },
                {
                    "Received", new BsonDocument
                    {
                        { "$sum", new BsonDocument
                            {
                                {"$cond", new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            {
                                                "$gte", new BsonArray {"$amount", 0}
                                            }
                                        },
                                        "$amount",
                                        0
                                    }
                                }
                            }
                        }
                    }
                },
                {
                    "Sent", new BsonDocument
                    {
                        { "$sum", new BsonDocument
                            {
                                {"$cond", new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            {
                                                "$lte", new BsonArray {"$amount", 0}
                                            }
                                        },
                                        "$amount",
                                        0
                                    }
                                }
                            }
                        }
                    }
                },
                {
                    "Total", new BsonDocument
                    {
                        {
                            "$sum", "$amount"
                        }
                    }
                },
                {
                    "Transactions", new BsonDocument
                    {
                        { "$sum", 1  }
                    }
                },
                {
                    "Address", new BsonDocument
                    {
                        { "$first", "$addr"  }
                    }
                }
            };
            var coin_address = db.GetCollection<BsonDocument>(_mainModel.CoinID + "_address");
            return coin_address.Aggregate().Match(match).Group<AddressInfo>(group).FirstOrDefault();
            */
        }

        [Obsolete]
        public GetMiningInfoResponse GetHashRate()
        {
            var mininginfo = CoinService.GetMiningInfo();
            return mininginfo;
        }

        public uint GetLastBlock()
        {
            return CoinService.GetBlockCount();
        }

        public uint GetLastBlockMongo()
        {
            var coin_last_block = _dtoMain.db.GetCollection<LastBlock>(_dtoMain.mainModel.CoinID + "_last_block");
            var blk = coin_last_block.Find(l => l.Type == "block").FirstOrDefault();
            if (blk == null)
            {
                return 0;
            }
            return blk.Block;
        }

        [Obsolete]
        public decimal GetBlockRewardPow(ClientBlock block)
        {
            for (int i = 0; i < block.Tx.Count; i++)
            {
                var t = CoinService.GetPublicTransaction(block.Tx[0]);
                if (t.Vin[0].TxId == null)
                {
                    Decimal dc = 0;
                    for (int y = 0; y < t.Vout.Count; y++)
                    {
                        dc += t.Vout[y].Value;
                    }
                    return dc;
                }
            }
            return 0;
        }


        public decimal GetBlockRewardPowMongo(ClientBlock block)
        {
            for (int i = 0; i < block.Tx.Count; i++)
            {
                var t = GetRawTransactionMongo(block.Tx[0]);
                if (string.IsNullOrEmpty(t.Vin[0].TxId))
                {
                    Decimal dc = 0;
                    for (int y = 0; y < t.Vout.Count; y++)
                    {
                        dc += t.Vout[y].Value;
                    }
                    return dc;
                }
            }
            return 0;
        }
        
        public decimal GetBlockRewardPosMongo(ClientBlock block)
        {
            var t = GetRawTransactionMongo(block.Tx[1]);
            Decimal inAmount = 0;
            Decimal outAmount = 0;
            if (t.Vin != null)
            {
                for (int y = 0; y < t.Vin.Count; y++)
                {
                    if (t.Vin[y].TxId == "0000000000000000000000000000000000000000000000000000000000000000" && t.Vin.Count == 1)
                    {
                        return -1;
                    }
                    else
                    {
                        var ta = GetRawTransactionMongo(t.Vin[y].TxId);
                        var xx = ta.Vout[Convert.ToInt32(t.Vin[y].Vout)];
                        inAmount += xx.Value;
                    }
                }
            }
            if (t.Vout != null)
            {
                for (int y = 0; y < t.Vout.Count; y++)
                {
                    outAmount += t.Vout[y].Value;
                }
            }
            return outAmount - inAmount;
        }

        public decimal GetBlockRewardMongo(ClientBlock block)
        {
            for (int i = 0; i < block.Tx.Count; i++)
            {
                var t = GetRawTransactionMongo(block.Tx[0]);
                if (string.IsNullOrEmpty(t.Vin[0].TxId))
                {
                    Decimal dc = 0;
                    for (int y = 0; y < t.Vout.Count; y++)
                    {
                        dc += t.Vout[y].Value;
                    }
                    return dc;
                }
            }
            return 0;
        }

        public ClientBlock GetBlockMongo(string blockHash)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_block");
            var rx = coin_block.Find(l => l.HashLower == blockHash.ToLower()).FirstOrDefault();
            if (rx == null)
            {
                return null;
            }
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional,
                AssetTX = rx.AssetTX
            };
            return blk;
        }

        public ClientBlock GetAssetBlockMongo(string blockHash)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_asset_block");
            var rx = coin_block.Find(l => l.HashLower == blockHash.ToLower()).FirstOrDefault();
            if (rx == null)
            {
                return null;
            }
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional
            };
            return blk;
        }

        public ClientBlock GetOrphanBlockMongo(string block)
        {
            var coin_block_orphan = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_block_orphan");
            var rx = coin_block_orphan.Find(l => l.HashLower == block.ToLower()).FirstOrDefault();
            if (rx == null)
            {
                return null;
            }
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional
            };
            return blk;
        }

        public List<ClientBlock> GetlastXBlocks(int number)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_block");
            var sort = new BsonDocument
            {
                {
                    "_id", -1
                },
            };
            var list = new List<ClientBlock>();
            var rxList = coin_block.Find(_ => true).Sort(sort).Project<ClientBlockLight>("{_id: 1, Confirmations:1, Difficulty: 1, Hash: 1, Height: 1," +
                "Time: 1, Flags: 1, Additional: 1}").Limit(number).ToList();
            if (rxList == null || rxList.Count == 0)
            {
                return list;
            }
            foreach (var rx in rxList)
            {
                var blk = new ClientBlock()
                {
                    Confirmations = rx.Confirmations,
                    Difficulty = rx.Difficulty,
                    Hash = rx.Hash,
                    Height = rx.Height,
                    Time = rx.Time,
                    TxCount = rx.Additional != null && rx.Additional.ContainsKey("oto_assettxcount") ? Convert.ToInt32(rx.Additional["oto_assettxcount"]) : -1,
                    BlockTime = UnixTimeStampToDateTime(rx.Time),
                    Flags = rx.Flags,
                    Additional = rx.Additional
                };
                list.Add(blk);
            }
            return list;
        }

        public List<ClientBlock> GetlastXAssetBlocks(int number)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_asset_block");
            var sort = new BsonDocument
            {
                {
                    "_id", -1
                },
            };
            var list = new List<ClientBlock>();
            var rxList = coin_block.Find(_ => true).Sort(sort).Project<ClientBlockLight>("{_id: 1, Confirmations:1, Difficulty: 1, Hash: 1, Height: 1," +
                "Time: 1, Flags: 1, Additional: 1}").Limit(number).ToList();
            if (rxList == null || rxList.Count == 0)
            {
                return list;
            }
            foreach (var rx in rxList)
            {
                var blk = new ClientBlock()
                {
                    Confirmations = rx.Confirmations,
                    Difficulty = rx.Difficulty,
                    Hash = rx.Hash,
                    Height = rx.Height,
                    Time = rx.Time,
                    TxCount = rx.Additional != null && rx.Additional.ContainsKey("oto_assettxcount") ? Convert.ToInt32(rx.Additional["oto_assettxcount"]) : -1,
                    BlockTime = UnixTimeStampToDateTime(rx.Time),
                    Flags = rx.Flags,
                    Additional = rx.Additional
                };
                list.Add(blk);
            }
            return list;
        }

        public ClientBlock GetBlockByIdMongo(uint block)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_block");
            var rx = coin_block.Find(l => l.Height == block).FirstOrDefault();
            if (rx == null)
            {
                return null;
            }
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional,
                AssetTX = rx.AssetTX,
                TxCount = rx.TxCount
            };
            return blk;
        }

        public ClientBlock GetAssetBlockByIdMongo(uint block)
        {
            var coin_block = _dtoMain.db.GetCollection<MongoBlock>(_dtoMain.mainModel.CoinID + "_asset_block");
            var rx = coin_block.Find(l => l.Height == block).FirstOrDefault();
            if (rx == null)
            {
                return null;
            }
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional,
                AssetTX = rx.AssetTX,
                TxCount = rx.TxCount
            };
            return blk;
        }

        public string GetBlockHashByHeight(long block)
        {
            return CoinService.GetBlockHash(block);
        }

        public ClientBlock GetBlockByHash(string blockhash)
        {
            var rx = CoinService.GetBlock(blockhash);
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional,
                AssetTX = rx.AssetTX
            };
            return blk;
        }

        public ClientBlock GetBlockByHashMongo(string blockhash)
        {
            var rx = GetAssetBlockMongo(blockhash);
            var blk = new ClientBlock()
            {
                Bits = rx.Bits,
                ChainWork = rx.ChainWork,
                Confirmations = rx.Confirmations,
                Difficulty = rx.Difficulty,
                Hash = rx.Hash,
                Height = rx.Height,
                MerkleRoot = rx.MerkleRoot,
                NextBlockHash = rx.NextBlockHash,
                Nonce = rx.Nonce,
                PreviousBlockHash = rx.PreviousBlockHash,
                Size = rx.Size,
                Time = rx.Time,
                Tx = rx.Tx,
                Version = rx.Version,
                BlockTime = UnixTimeStampToDateTime(rx.Time),
                Flags = rx.Flags,
                Additional = rx.Additional,
                AssetTX = rx.AssetTX
            };
            return blk;
        }

        public GetOmniTransactionResponse GetRawTransactionAsset(String transaction)
        {
            return CoinService.GetOmniTransaction(transaction);
        }
        public GetRawTransactionResponse GetRawTransaction(String transaction)
        {
            return CoinService.GetRawTransaction(transaction);
        }   

        public GetRawAssetTransactionResponseMongo GetRawAssetTransactionMongo(String transaction)
        {
            var coin_transactions = _dtoMain.db.GetCollection<GetRawAssetTransactionResponseMongo>(_dtoMain.mainModel.CoinID + "_transactions");
            var lst = coin_transactions.Find(f => f.txid == transaction.ToLower()).FirstOrDefault();
            return lst;
        }

        public GetRawTransactionResponseMongo GetRawTransactionMongo(String transaction)
        {
            var coin_transactions = _dtoMain.db.GetCollection<GetRawTransactionResponseMongo>(_dtoMain.mainModel.CoinID + "_transactions");
            var lst = coin_transactions.Find(f => f.TxIdLower == transaction.ToLower()).FirstOrDefault();
            return lst;
        }

        public List<GetRawAssetTransactionResponseMongo> GetRawAssetTransactionMongoList(int height)
        {
            var coin_transactions = _dtoMain.db.GetCollection<GetRawAssetTransactionResponseMongo>(_dtoMain.mainModel.CoinID + "_transactions");
            var lst = coin_transactions.Find(f => f.Height == height).ToList();
            return lst;
        }

        public Dictionary<string, GetRawTransactionResponseMongo> GetRawTransactionMongoList(int height)
        {
            var coin_transactions = _dtoMain.db.GetCollection<GetRawTransactionResponseMongo>(_dtoMain.mainModel.CoinID + "_transactions");
            var lst = coin_transactions.Find(f => f.Height == height).ToList();
            Dictionary<string, GetRawTransactionResponseMongo> ret = new Dictionary<string, GetRawTransactionResponseMongo>();
            foreach(var tx in lst)
            {
                ret.Add(tx.TxIdLower, tx);
            }
            return ret;
        }

        public List<ClientTransactionAddress> GetInAddressMongo(GetRawTransactionResponseMongo tx, Decimal amount, bool splitFee)
        {
            List<ClientTransactionAddress> txs = new List<ClientTransactionAddress>();
            var voutAddr = tx.Vout.FirstOrDefault();
            string type = "";
            if (voutAddr != null)
            {
                type = voutAddr.ScriptPubKey.Type;
            }
            if (type == "nonstandard" && tx.VinList.Count == 0)
            {
                txs.Add(new ClientTransactionAddress()
                {
                    Address = "Non standard",
                    Amount = amount,
                    IsNonStarndart = true
                });
                return txs;
            }
            if (tx.VinList.Count == 0)
            {
                if (splitFee)
                {
                    txs.Add(new ClientTransactionAddress()
                    {
                        Address = "RWD",
                        Amount = amount,
                        IsReward = true
                    });
                    txs.Add(new ClientTransactionAddress()
                    {
                        Address = "FEE",
                        Amount = amount,
                        IsReward = true
                    });
                }
                else
                {
                    txs.Add(new ClientTransactionAddress()
                    {
                        Address = "RWDFEE",
                        Amount = amount,
                        IsReward = true
                    });
                }
                return txs;
            }
            foreach (var tr in tx.VinList)
            {
                if (!string.IsNullOrEmpty(tr.TxId))
                {
                    txs.Add(new ClientTransactionAddress()
                    {
                        Address = tr.Address,
                        Amount = tr.Amount
                    });
                }
                else
                {
                    throw new Exception("Empty TxId");
                }
            }
            txs = txs.GroupBy(a => a.Address).Select(
                                    g => new ClientTransactionAddress()
                                    {
                                        Address = g.Key,
                                        Amount = g.Sum(s => s.Amount),
                                        IsReward = g.Max(s => s.IsReward)
                                    }).ToList();
            return txs;
        }    

        public double GetDifficultyMongo(uint block)
        {
            return Math.Round(CoinService.GetDifficulty(), 2);
        }
        
        public double GetDifficulty()
        {
            return Math.Round(CoinService.GetDifficulty(), 2);
        }

        public double GetDifficultyApi()
        {
            return CoinService.GetDifficulty();
        }

        public string GetRawQuery(string query, params object[] parameters)
        {
            return CoinService.GetRawJsonQuery(query, parameters);
        }

        [Obsolete]
        public List<ClientBlock> GetLastXTransactions(uint block, uint amount = 10)
        {
            List<ClientBlock> blocks = new List<ClientBlock>();
            for (uint i = amount; i > 0; i--)
            {
                var dx = CoinService.GetBlockHash(block--);
                var rx = CoinService.GetBlock(dx);
                var blk = new ClientBlock()
                {
                    Bits = rx.Bits,
                    ChainWork = rx.ChainWork,
                    Confirmations = rx.Confirmations,
                    Difficulty = rx.Difficulty,
                    Hash = rx.Hash,
                    Height = rx.Height,
                    MerkleRoot = rx.MerkleRoot,
                    NextBlockHash = rx.NextBlockHash,
                    Nonce = rx.Nonce,
                    PreviousBlockHash = rx.PreviousBlockHash,
                    Size = rx.Size,
                    Time = rx.Time,
                    Tx = rx.Tx,
                    Version = rx.Version,
                    BlockTime = UnixTimeStampToDateTime(rx.Time),
                    Flags = rx.Flags,
                    Additional = rx.Additional
                };
                blocks.Add(blk);
            }
            return blocks;
        }

        public double GetMoneySupply()
        {
            return 0;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public List<RichList> GetRichList()
        {
            var coin_transactions = _dtoMain.db.GetCollection<RichList>(_dtoMain.mainModel.CoinID + "_rich_list");
            var lst = coin_transactions.Find(_ => true).SortByDescending(orderby => orderby.Total).ToList();
            return lst;
        }

        private static Dictionary<uint, int> _mappings = new Dictionary<uint, int>()
        {
            { 100, 2 }, { 500, 101 }, { 1000, 501 }
        };
        
    }
}
