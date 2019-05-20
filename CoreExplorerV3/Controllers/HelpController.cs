using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreExplorerV3.Attributes;
using CoreExplorerV3.Models;
using CoreExplorerV3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class HelpController : Controller
    {
        private IConfiguration _configuration;
        public MainCoins mainCoins;
        public HelpController(IConfiguration Configuration, MainCoins mc)
        {
            _configuration = Configuration;
            mainCoins = mc;
        }

        public IActionResult Walletinfo()
        {
            var coinInfo = mainCoins.GetCoin();
            return View(new WalletinfoViewModel()
            {
                MainCoinModel = coinInfo
            });
        }

        public IActionResult Index()
        {
            var coinInfo = mainCoins.GetCoin();
            var peerList = mainCoins.GetPeerList(coinInfo.CoinID);
            StringBuilder sb = new StringBuilder();
            foreach (var peer in peerList)
            {
                sb.AppendLine("addnode=" + peer.Addr);
            }
            return View(new ConnectionViewModel()
            {
                PeerInfo = peerList,
                MainCoinModel = coinInfo,
                AddNode = sb.ToString()
            });
        }
    }
}