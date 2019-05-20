using BitcoinLib.Responses;
using CoreMongoDTO.DTO;
using ExchangesLib.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class ExchangeIndexViewModel
    {
        public MainCoinModel MainCoinModel { get; set; }
        public ExchangePrice ExchangePrice { get; set; }
        public Decimal Trend { get; set; }
        public Decimal Trend7 { get; set; }
        public Decimal Trend14 { get; set; }
        public Decimal Trend30 { get; set; }
        public Decimal Trend60 { get; set; }
        public Decimal Trend90 { get; set; }
        public ExchangeFiat ExchangeFiat { get; set; }
        public List<Exchanges> Exchanges { get; set; }
        public List<ExchangesCurrency> ExchangePriceList { get; set; }
        public List<ExchangeHistory> ExchangeHistoy { get; set; }
    }
}
