using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinLib.Responses
{
    public class MasternodeListSNX
    {
        /*
         * {
        "rank" : 3,
        "txhash" : "36f30ee5acc94c8ca9ae0b0d750d70064cd28ca466fc258ee4a690531335dbda",
        "outidx" : 0,
        "status" : "ENABLED",
        "addr" : "2KN6AtNrGGPrDi3acGKvALauVaEYozeEYaG",
        "version" : 70910,
        "lastseen" : 1518546883,
        "activetime" : 268671,
        "lastpaid" : 1518546001
    },*/

        public int rank { get; set; }
        public string txhash { get; set; }
        public int outidx { get; set; }
        public string status { get; set; }
        public string addr { get; set; }
        public int version { get; set; }
        public int lastseen { get; set; }
        public int activetime { get; set; }
        public int lastpaid { get; set; }
    }
}
