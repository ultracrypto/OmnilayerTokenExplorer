using BitcoinLib.Responses;
using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class BlockViewModel
    {
        public ClientBlock Block { get; set; }
        public List<ClientTransaction> ListTransactions { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
        public bool IsConfirmed { get; set; }
        public Pageing Pageing { get; set; }
    }
}
