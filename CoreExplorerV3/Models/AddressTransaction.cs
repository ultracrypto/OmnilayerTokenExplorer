using CoreExplorerV3.Services;
using CoreMongoDTO.DTO;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{ 

    public class AddressTransaction : AddressTransactionMongoDTO
    {        

        [BsonIgnore]
        public DateTime BlockTime
        {
            get
            {
                return MainCoins.UnixTimeStampToDateTime(Time);
            }
        }

        [BsonIgnore]
        public String DiffBig
        {
            get
            {
                var big = Math.Truncate(Amount);
                if (Amount < 0 && big == 0)
                {
                    return "-" + big.ToString();
                }
                return big.ToString();
            }
        }

        [BsonIgnore]
        public String DiffSmall
        {
            get
            {
                string s = Amount.ToString("0.00######", CultureInfo.InvariantCulture);
                string[] parts = s.Split('.');
                return parts[1];
            }
        }

        [BsonIgnore]
        public String DiffBigTotal
        {
            get
            {
                var big = Math.Truncate(Total);
                if (Amount < 0 && big == 0)
                {
                    return "-" + big.ToString();
                }
                return big.ToString();
            }
        }

        [BsonIgnore]
        public String DiffSmallTotal
        {
            get
            {
                string s = Total.ToString("0.00######", CultureInfo.InvariantCulture);
                string[] parts = s.Split('.');
                return parts[1];
            }
        }
    }
}
