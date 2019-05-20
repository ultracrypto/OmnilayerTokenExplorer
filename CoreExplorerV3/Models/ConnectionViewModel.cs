using BitcoinLib.Responses;
using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class ConnectionViewModel
    {
        public List<PeerList> PeerInfo { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
        public String AddNode { get; set; }
    }
}
