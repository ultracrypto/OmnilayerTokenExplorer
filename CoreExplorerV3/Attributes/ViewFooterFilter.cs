using CoreExplorerV3.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreExplorerV3.Attributes
{
    public class ViewFooterAttribute : IActionFilter
    {
        private readonly MainCoins _mc;
        private MemoryCacheEntryOptions _memoryCacheEntryOptions;
        private IMemoryCache _cache;

        public ViewFooterAttribute(MainCoins mc, IMemoryCache memoryCache)
        {
            _mc = mc ?? throw new ArgumentNullException(nameof(mc));
            _cache = memoryCache;
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24));
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var rqf = controller.Request.HttpContext.Features.Get<IRequestCultureFeature>();
                string appendUrl = string.Empty;
                if (rqf.RequestCulture != null)
                {
                    controller.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(rqf.RequestCulture),
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                    );
                    if (rqf.RequestCulture.UICulture.ToString() != "en")
                    {
                        appendUrl = "/" + rqf.RequestCulture.UICulture.ToString();
                    }
                }
                var donationAddr = _mc.GetCoin().DonationAddress;
                
                controller.ViewBag.Favicon = _mc.GetCoin().Favicon;
                controller.ViewBag.AppendUrl = appendUrl;
                controller.ViewBag.CoinName = _mc.GetCoin().CoinName;
                controller.ViewBag.IsCoinDistribution = _mc.GetCoin().IsCoinDistribution;
                controller.ViewBag.IsExchange = _mc.GetCoin().IsExchange;
                controller.ViewBag.DonationAddress = donationAddr;
                controller.ViewBag.CoinSymbol = _mc.GetCoin().CoinSymbol;
                controller.ViewBag.V3Logo = _mc.GetCoin().V3Logo;
                controller.ViewBag.V3ThemeCss = string.Empty;
                controller.ViewBag.GoalPosition = _mc.GetCoin().GoalPosition;
                if (!string.IsNullOrEmpty(_mc.GetCoin().V3Theme))
                {
                    string str;
                    if (!_cache.TryGetValue(_mc.GetCoin().CoinSymbol + "_css_file", out str))
                    {
                        str = File.ReadAllText("wwwroot/" + _mc.GetCoin().V3Theme + ".min.css");
                        _cache.Set(_mc.GetCoin().CoinSymbol + "_css_file", str, _memoryCacheEntryOptions);
                    }
                    controller.ViewBag.V3ThemeCss = str;
                }
                controller.ViewBag.Claims = _mc.GetClaims();
                if (!string.IsNullOrEmpty(_mc.GetCoin().V3Layout))
                {
                    controller.ViewBag.V3Layout = string.Empty;
                }
                else
                {
                    controller.ViewBag.V3Layout = " " + _mc.GetCoin().V3Layout;
                }
                
                if (_mc.GetCoin().CoinImage == "-")
                {
                    controller.ViewBag.CSS = "default.css";
                }
                else
                {
                    controller.ViewBag.CSS = _mc.GetCoin().CoinID + ".css";
                }
                controller.ViewBag.IsClosed = false;
                if (!string.IsNullOrEmpty(_mc.GetCoin().FooterView))
                {
                    controller.ViewBag.FooterFile = "~/Views/Footer/" + _mc.GetCoin().FooterView + ".cshtml";
                }
                else
                {
                    controller.ViewBag.FooterFile = "~/Views/Footer/Default.cshtml";
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}
