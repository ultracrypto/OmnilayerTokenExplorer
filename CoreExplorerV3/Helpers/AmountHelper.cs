using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreExplorerV3.Helpers
{
    public class AmountHelper
    {
        static AmountHelper()
        {
            nfi.NumberGroupSeparator = " ";
        }
        static NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();

        public static string AddressToClaim(string address, Dictionary<string, VerifyAddressesDTO> claimDictionary)
        {
            var addrLower = address.ToLower();
            if (claimDictionary.ContainsKey(addrLower))
            {
                return claimDictionary[addrLower].CoinOwner;
            }
            return address;
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                     new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }
        public static string FiatHelper(Decimal amount, string sign)
        {
            var dec = Math.Round(amount, 3);
            if (dec < 0)
            {
                return string.Format("-{0} {1}", sign, (-dec).ToString("#,0.000", nfi));
            }
            else
            {
                return string.Format("{0} {1}", sign, dec.ToString("#,0.000", nfi));
            }
        }

        public static string BigSmallNumber(Decimal amount)
        {
            string s = amount.ToString("0.00######", CultureInfo.InvariantCulture);
            string[] parts = s.Split('.');
            var sb = new StringBuilder();
            sb.Append("<span class='number'>");
            sb.Append(parts[0]);
            sb.Append("<small>.");
            sb.Append(parts[1]);
            sb.Append("</small></span>");
            return sb.ToString();
        }

        public static string BigSmallNumber(double number)
        {
            long IntPart = (long)number;
            double fractionalPart = number - IntPart;
            long fractPart = (long)(fractionalPart * 10);
            var sb = new StringBuilder();
            sb.Append("<span class='number'>");
            sb.Append(IntPart);
            sb.Append("<small>.");
            sb.Append(fractPart);
            sb.Append("</small></span>");
            return sb.ToString();
        }

        public static Decimal FiatHelperDecimal(Decimal amount)
        {
            var dec = Math.Round(amount, 4);
            if (dec < 0)
            {
                return -dec;
            }
            else
            {
                return dec;
            }
        }

        public static string TimeToString(long time)
        {
            var localDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(time).DateTime.ToUniversalTime();
            return localDateTimeOffset.ToString("d/M/yyyy HH:mm:ss", Thread.CurrentThread.CurrentUICulture);
        }

        public static string AvgReward(Decimal time)
        {
            var ls = Convert.ToDouble(time);
            StringBuilder sb = new StringBuilder();
            var lastpaidtime = TimeSpan.FromSeconds(ls);
            var days = lastpaidtime.TotalDays;
            if (days > 1)
            {
                sb.Append(Math.Truncate(days));
                sb.Append(" d");
            }
            var hours = lastpaidtime.Hours;
            if (hours > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(hours);
                sb.Append(" h");
            }
            var min = lastpaidtime.Minutes;
            if (min > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(min);
                sb.Append(" min");
            }
            var sec = lastpaidtime.Seconds;
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

        public static string NextReward(double time)
        {
            var ls = Convert.ToDouble(time);
            if (ls == 0)
            {
                return "Soon";
            }
            StringBuilder sb = new StringBuilder();
            var lastpaidtime = TimeSpan.FromSeconds(time);
            var days = lastpaidtime.TotalDays;
            if (days > 1)
            {
                sb.Append(Math.Truncate(days));
                sb.Append(" d");
            }
            var hours = lastpaidtime.Hours;
            if (hours > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(hours);
                sb.Append(" h");
            }
            var min = lastpaidtime.Minutes;
            if (min > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(min);
                sb.Append(" min");
            }
            var sec = lastpaidtime.Seconds;
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

        public static Decimal BTCHelperDecimal(Decimal amount)
        {
            return Math.Round(amount, 9);
        }
        public static string BTCHelperString(Decimal amount)
        {
            var dec = Math.Round(amount, 9);
            if (dec < 0)
            {
                return string.Format("-{0:0.00000000} BTC", -dec);
            }
            else
            {
                return string.Format("{0:0.00000000} BTC", dec);
            }
        }

        public static string BTCHelperStringAdvanced(Decimal amount)
        {
            var dec = Math.Round(amount, 9);
            if (dec >= 1000)
            {
                return string.Format("{0:0.0} BTC", dec);
            }
            else if (dec >= 100)
            {
                return string.Format("{0:0.00} BTC", dec);
            }
            else if (dec >= 10)
            {
                return string.Format("{0:0.0000} BTC", dec);
            }
            else
            {
                return string.Format("{0:0.00000000} BTC", dec);
            }
        }

        public static string BTCHelperExhcnagesNoSig(Decimal amount)
        {
            var dec = Math.Round(amount, 9);
            if (dec < 0)
            {
                return "no api data";
            }
            else
            {
                return string.Format("{0:0.00000000}", dec);
            }
        }

        public static string MarketVolumePercent(Decimal vol, Decimal totalVol)
        {
            if (totalVol == 0)
            {
                return "0%";
            }
            else
            {
                return string.Format("{0}%", Math.Round((vol * 100) / totalVol, 2));
            }
        }

        public static string BTCHelperStringNoSig(Decimal amount)
        {
            var dec = Math.Round(amount, 9);
            if (dec < 0)
            {
                return string.Format("-{0:0.00000000}", -dec);
            }
            else
            {
                return string.Format("{0:0.00000000}", dec);
            }
        }
        public static string SplitToAmount(Decimal amount)
        {
            var big = Math.Truncate(amount);
            if (amount < 0 && big == 0)
            {
                return "-" + big.ToString();
            }
            var bigAmount = big.ToString();

            string s = amount.ToString("0.00######", CultureInfo.InvariantCulture);
            string[] parts = s.Split('.');
            return String.Format("{0}<small>.{1}</small>", bigAmount, parts[1]);
        }

        public static String Difference(Decimal diff)
        {
            diff = Math.Round(diff, 2);
            if (diff < 0)
            {
                return @"<span class=""text-danger"">" + diff.ToString() + "%</span>";
            }
            else if (diff > 0)
            {
                return @"<span class=""text-success"">+" + diff.ToString() + "%</span>";
            }
            else
            {
                return @"<span>" + diff.ToString() + "%</span>";
            }
        }

        public static String BytesToString(decimal byteCount)
        {
            string[] suf = { " H/s", " KH/s", " MH/s", " GH/s", " TH/s", " PH/s", " EH/s" };
            if (byteCount == 0)
                return "0" + suf[0];
            double bytes = Convert.ToDouble(Math.Abs(byteCount));
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1000)));
            double num = Math.Round(bytes / Math.Pow(1000, place), 3);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public static string PingToMs(double pingSec)
        {
            var pingMs = pingSec * 1000;
            return Math.Round(pingMs, 0) + "ms";
        }

        public static string TimeFromNowUtc(long unixTime)
        {
            var BlockTime = UnixTimeStampToDateTime(unixTime);
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
