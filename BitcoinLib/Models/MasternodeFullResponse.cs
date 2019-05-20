using BitcoinLib.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BitcoinLib.Models
{
    public class MasternodeFullResponse
    {
        //Print info in format 'status protocol payee lastseen activeseconds lastpaidtime lastpaidblock IP'
        private string key;
        public string Key
        {
            get
            {
                return key;
            }
        }
        private string value;
        public string Value
        {
            get
            {
                return value;
            }
        }
        public string Status { get; set; }
        public string Protocol { get; set; }
        public string PayeeAddress { get; set; }
        public string RemotePayeeAddress { get; set; }
        public string RemotePayeeAddressLower { get; set; }
        public string Lastseenstr { get; set; }
        public string Activesecondsstr { get; set; }
        public int Lastpaidtimestr { get; set; }
        public int Lastpaidblock { get; set; }
        public UriBuilder Url { get; set; }

        public MasternodeFullResponse(string k, string v, int version = 1)
        {
            if (version == 1)
            {
                key = k;
                value = v;
                v = Regex.Replace(v, @"\s+", " ");
                var r = v.Trim().Split(' ');
                Status = r[0];
                Protocol = r[1];
                PayeeAddress = r[2];
                Lastseenstr = r[3];
                Activesecondsstr = r[4];
                Lastpaidblock = Convert.ToInt32(r[6]);
                Url = new UriBuilder(r[7]);
            }
            if (version == 2)
            {
                key = k;
                value = v;
                v = Regex.Replace(v, @"\s+", " ");
                var r = v.Trim().Split(' ');
                Status = r[0];
                Protocol = r[1];
                PayeeAddress = r[2];
                Lastseenstr = r[4];
                Activesecondsstr = r[5];
                Lastpaidblock = Convert.ToInt32(r[6]);
                Url = new UriBuilder(r[3]);
            }
            if (version == 3)
            {
                key = k;
                value = v;
                v = Regex.Replace(v, @"\s+", " ");
                var r = v.Trim().Split(' ');
                Status = r[0];
                Protocol = r[1];
                PayeeAddress = r[2];
                Lastseenstr = r[5];
                Activesecondsstr = r[6];
                Lastpaidblock = 0;
                Url = new UriBuilder(r[4]);
            }
        }
        public MasternodeFullResponse(MasternodeListSNX mms)
        {
            key = mms.txhash;
            value = string.Empty;
            Status = mms.status;
            Protocol = string.Empty;
            PayeeAddress = mms.addr;
            Lastseenstr = mms.lastseen.ToString();
            Activesecondsstr = mms.activetime.ToString();
            Lastpaidtimestr = mms.lastpaid;
            Lastpaidblock = mms.lastpaid;
            Url = new UriBuilder("0.0.0.0:0");
        }
        protected MasternodeFullResponse()
        {

        }
    }

}
