using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class ExchangeApi
    {
        public decimal currentPriceBTC { get; set; }
        public decimal currentPriceUSD { get; set; }
        public decimal currentPriceEUR { get; set; }
        public decimal btceur { get; set; }
        public decimal btcusd { get; set; }
        public decimal change24h { get; set; }
        public string coinsymbol { get; set; }
        public List<OneExchangeApi> exchangesBTC { get; set; } = new List<OneExchangeApi>();
    }

    public class OneExchangeApi
    {
        public string exchangeID { get; set; }
        public string exchangeName { get; set; }
        public string exchangeDirectURL { get; set; }
        public string exchangeURL { get; set; }
        public long updatetime { get; set; }
        public decimal last { get; set; }
        public decimal buy { get; set; }
        public decimal sell { get; set; }
        public decimal volume { get; set; }
        public decimal change { get; set; }
    }
}
