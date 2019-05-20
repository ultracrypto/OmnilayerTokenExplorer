using BitcoinLib.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BitcoinLib.Models
{
    public class MasternodeRemoteResponse
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
        public string Tx { get; set; }
        public string TxId { get; set; }
        public string RemotePayeeAddress { get; set; }

        public MasternodeRemoteResponse(string k, string v)
        {
            key = k;
            value = v;
            v = Regex.Replace(v, @"\s+", " ");
            var valSplit = v.Trim().Split(':');
            var keySplit = k.Trim().Split('-');
            RemotePayeeAddress = valSplit[0];
            Tx = keySplit[0];
            TxId = keySplit[1];
        }
        protected MasternodeRemoteResponse()
        {

        }
    }

}
