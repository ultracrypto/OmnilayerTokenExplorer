using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CoreExplorerV3.Models;
using CoreExplorerV3.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using CoreExplorerV3.Attributes;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class RichController : Controller
    {
        private IConfiguration _configuration;
        public MainCoins mainCoins;
        public RichController(IConfiguration Configuration, MainCoins mc)
        {
            _configuration = Configuration;
            mainCoins = mc;
        }
        
        public IActionResult Index()
        {
            var mc = mainCoins.GetCoin();
            if (!mc.IsCoinDistribution)
            {
                return View("~/Views/Shared/FeatureDisabled.cshtml");
            }
            var richList = mainCoins.GetRichList();
            return View(new RichListViewModel()
            {
                RichList = richList,
                MainCoinModel = mc
            });
        }

        public IActionResult Address()
        {
            var mc = mainCoins.GetCoin();
            if (!mc.IsCoinDistribution)
            {
                return View("~/Views/Shared/FeatureDisabled.cshtml");
            }
            return View(new AddressStatViewModel()
            {
                MainCoinModel = mc,
            });
        }
    }
}
