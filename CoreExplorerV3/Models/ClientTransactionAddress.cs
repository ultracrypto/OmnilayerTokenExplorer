using BitcoinLib.Responses;
using System;
using System.Globalization;
using System.Text;

namespace CoreExplorerV3.Models
{
    public class ClientTransactionAddress
    {
        public String Address { get; set; }
        public Boolean IsReward { get; set; }
        public Boolean IsNonStarndart { get; set; }
        public Boolean IsNullData { get; set; }
        public Boolean IsZerocoin { get; set; }
        public Decimal Amount { get; set; }
    }
}