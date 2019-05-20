using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class RichListViewModel
    {
        public List<RichList> RichList { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
    }
}
