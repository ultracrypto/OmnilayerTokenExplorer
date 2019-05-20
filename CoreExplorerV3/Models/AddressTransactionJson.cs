using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class AddressTransactionJson
    {
        public string draw { get; set; }
        public long recordsTotal { get; set; }
        public long recordsFiltered { get; set; }
        public List<string[]> data { get; set; }
    }
}
