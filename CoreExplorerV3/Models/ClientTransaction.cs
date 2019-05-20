using BitcoinLib.Responses;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CoreExplorerV3.Models
{
    public class ClientTransaction : GetOmniTransactionResponse
    {
        public int ConfirmationsClient { get; set; }
        public DateTime BlockDateTime { get; set; }
        public decimal Amount { get; set; }
        public string TimeFromNowUtc
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                var days = (DateTime.UtcNow - BlockDateTime).Days;
                if (days > 0)
                {
                    sb.Append(days);
                    sb.Append(" d");
                }
                var hours = (DateTime.UtcNow - BlockDateTime).Hours;
                if (hours > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(hours);
                    sb.Append(" h");
                }
                var min = (DateTime.UtcNow - BlockDateTime).Minutes;
                if (min > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(min);
                    sb.Append(" min");
                }
                var sec = (DateTime.UtcNow - BlockDateTime).Seconds;
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