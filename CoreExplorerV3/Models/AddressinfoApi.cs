using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    ////{"address":"REjXTrh64hKd4x6QvXiQzwTM47qXfWv63R","sent":0,"received":1000,"balance":"1000","last_txs":[{"type":"vout","addresses":"7ea8373b87dee6fc8846b0bd86ddfe2f9874e3a1df8bc33564f893525e397266"}]}
    public class AddressinfoApi
    {
        public string address { get; set; }
        public Decimal sent { get; set; }
        public Decimal received { get; set; }
        public Decimal balance { get; set; }
        public List<AddressinfoApiTx> last_txs { get; set; } = new List<AddressinfoApiTx>();
    }

    public class AddressinfoApiTx
    {
        public string type { get; set; }
        public string addresses { get; set; }
    }
}
