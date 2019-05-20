using BitcoinLib.Responses;
using CoreMongoDTO.DTO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreExplorerIndexer
{
    public class OrphanException : Exception
    {
        public int BlockId { get; set; }
        public int RemoveStart { get; set; }
        public bool Save { get; set; } = false;
        public OrphanException(string customMessage, int blockId, int removeStart) : base(customMessage)
        {
            BlockId = blockId;
            RemoveStart = removeStart;
        }
        public OrphanException(string customMessage, int blockId, int removeStart, bool save) : base(customMessage)
        {
            BlockId = blockId;
            RemoveStart = removeStart;
            Save = save;
        }
    }

    public class MongoBlockFail : Exception
    {
        public int RemoveFromBlock { get; set; }
        public MongoBlockFail(string customMessage, int removeFromBlock) : base(customMessage)
        {
            RemoveFromBlock = removeFromBlock;
        }
    }

    public class FatalException : Exception
    {
        public FatalException(string customMessage) : base(customMessage)
        {
        }
    }

    public class Funcs
    {
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                     new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }

        public static decimal? GetBlockRewardMongo(Block block, IMongoDatabase db, string CoinName)
        {
            for (int i = 0; i < block.Tx.Count; i++)
            {
                var t = GetRawTransactionMongo(block.Tx[0], db, CoinName);
                if (t == null)
                {
                    return null;
                }
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

        public static decimal? GetBlockRewardMongoPos(Block block, IMongoDatabase db, string CoinName)
        {
            var t = GetRawTransactionMongo(block.Tx[1], db, CoinName);
            if (t == null)
            {
                return null;
            }
            Decimal inAmount = 0;
            Decimal outAmount = 0;
            if (t.Vin != null)
            {
                for (int y = 0; y < t.Vin.Count; y++)
                {
                    var ta = GetRawTransactionMongo(t.Vin[y].TxId, db, CoinName);
                    if (ta == null)
                    {
                        return null;
                    }
                    var xx = ta.Vout[Convert.ToInt32(t.Vin[y].Vout)];
                    inAmount += xx.Value;
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

        public static GetRawTransactionResponseMongo GetRawTransactionMongo(String transaction, IMongoDatabase db, string CoinName)
        {
            var coin_transactions = db.GetCollection<GetRawTransactionResponseMongo>(CoinName + "_transactions");
            var lst = coin_transactions.Find(f => f.TxIdLower == transaction.ToLower()).FirstOrDefault();
            return lst;
        }
    }
}
