using BitcoinLib.Responses;
using CoreMongoDTO.DTO;
using ExchangesLib.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public AddressInfo AddressInfo { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
        public bool IsUnconfirmed { get; set; }
        public ExchangePrice ExchangePrice { get; set; }
        public ExchangeFiat ExchangeFiat { get; set; }
    }
}
