using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiMon.Models;
using CoreExplorerV3.Attributes;
using CoreExplorerV3.Services;
using ExchangesLib.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class ClaimController : Controller
    {
        private IConfiguration configuration;
        private Exchange exchange;
        private MainCoins mainCoins;
        private readonly ILogger _logger;
        private IMemoryCache _cache;
        private IHttpContextAccessor _accessor;
        private string _ip;
        private MemoryCacheEntryOptions _memoryCacheEntryOptions;

        public ClaimController(IConfiguration Configuration, MainCoins mc, ILogger<HomeController> logger,
            IMemoryCache memoryCache, IHttpContextAccessor accessor,
            Exchange ex)
        {
            exchange = ex;
            _accessor = accessor;
            _cache = memoryCache;
            configuration = Configuration;
            mainCoins = mc;
            _logger = logger;
            _ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1));
        }

        public IActionResult Index(bool? success, bool? error, string address)
        {
            if (success.HasValue & error.HasValue && success.Value && error.Value)
            {
                success = false;
            }
            var cm = new ClaimModel()
            {
                Error = error,
                Success = success,
                Address = address,
                MainCoinModel = mainCoins.GetCoin()
            };
            return View(cm);
        }

        [HttpPost]
        public IActionResult Claimaddress(string address, string message, string signature, string ownername)
        {
            var coins = mainCoins.GetCoin();
            if (string.IsNullOrEmpty(address) 
                || string.IsNullOrEmpty(signature)
                || string.IsNullOrEmpty(ownername))
            {
                return RedirectToAction("Index", "Claim", new { error = "true", address = address.ToString() });
            }
            bool result = false;
            try
            {
                result = mainCoins.AddAddress(address, message?.ToString() ?? "", signature, ownername, coins.CoinID);
            }
            catch
            {
                result = false;
            }
            if (result)
            {
                return RedirectToAction("Index", "Claim", new { success = "true" });
            }
            else
            {
                return RedirectToAction("Index", "Claim", new { error = "true", address = address.ToString() });
            }
        }
    }
}