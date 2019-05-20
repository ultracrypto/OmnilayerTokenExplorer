using BitcoinLib.Responses;
using System;
using System.Text;

namespace CoreExplorerV3.Models
{
    public class ClientBlockLight : GetBlockResponse
    {
        public object _id { get; set; }

    }
    public class ClientBlock : GetBlockResponse
    {
        public DateTime BlockTime { get; set; }
        public int ConfirmationsClient { get; set; }
        public Decimal Reward { get; set; }
        public int TxCount { get; set; }
        public decimal TotalTransfered { get; set; }
        public Decimal BTCFee { get; set; }
        public Decimal OMNIFee { get; set; }
        public bool IsZerocoin { get; set; }
        public String TimeFromNowUtc
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                var days = (DateTime.UtcNow - BlockTime).Days;
                if (days > 0)
                {
                    sb.Append(days);
                    sb.Append(" d");
                }
                var hours = (DateTime.UtcNow - BlockTime).Hours;
                if (hours > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(hours);
                    sb.Append(" h");
                }
                var min = (DateTime.UtcNow - BlockTime).Minutes;
                if (min > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(min);
                    sb.Append(" min");
                }
                var sec = (DateTime.UtcNow - BlockTime).Seconds;
                if (sec > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(sec);
                    sb.Append(" sec");
                }
                return sb.ToString();
            }
        }
    }
}