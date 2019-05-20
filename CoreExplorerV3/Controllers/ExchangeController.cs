using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreExplorerV3.Attributes;
using CoreExplorerV3.Models;
using CoreExplorerV3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangesLib.DTO;
using ExchangesLib.Services;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class ExchangeController : Controller
    {
        private IConfiguration configuration;
        private MainCoins mainCoins;
        private Exchange exchange;
        private readonly ILogger _logger;

        public ExchangeController(IConfiguration Configuration, MainCoins mc, ILogger<HomeController> logger,
            Exchange ex)
        {
            exchange = ex;
            configuration = Configuration;
            mainCoins = mc;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var coinInfo = mainCoins.GetCoin();
            ExchangePrice ep = exchange.GetMarketprice(coinInfo.CoinSymbol.ToUpper());
            var trends = exchange.GetGlobalTrends(coinInfo.CoinSymbol.ToUpper());
            Decimal trend = Math.Round(trends[0], 2);
            Decimal trend7 = Math.Round(trends[1], 2);
            Decimal trend14 = Math.Round(trends[2], 2);
            Decimal trend30 = Math.Round(trends[3], 2);
            Decimal trend60 = Math.Round(trends[4], 2);
            Decimal trend90 = Math.Round(trends[5], 2);
            /*
            Decimal trend = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 1), 2);
            Decimal trend7 = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 6), 2);
            Decimal trend14 = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 13), 2);
            Decimal trend30 = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 29), 2);
            Decimal trend60 = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 59), 2);
            Decimal trend90 = Math.Round(exchange.GetGlobalTrend(coinInfo.CoinSymbol.ToUpper(), ep, 89), 2);
            */
            List<ExchangeHistory> sevenDays = mainCoins.GetHistory(coinInfo.CoinSymbol.ToUpper(), 7);
            ExchangeFiat fiat = exchange.GetMarketFiat();
            var em = mainCoins.GetExchanges();
            var markets = mainCoins.GetExchangePrice(coinInfo.CoinSymbol.ToUpper(), em);
            return View(new ExchangeIndexViewModel()
            {
                ExchangeHistoy = sevenDays,
                MainCoinModel = coinInfo,
                ExchangePrice = ep,
                Trend = trend,
                Trend7 = trend7,
                Trend14 = trend14,
                Trend30 = trend30,
                Trend60 = trend60,
                Trend90 = trend90,
                ExchangeFiat = fiat,
                Exchanges = em,
                ExchangePriceList = markets
            });
        }
    }
}