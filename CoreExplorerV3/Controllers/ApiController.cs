using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using CoreExplorerV3.Services;
using CoreExplorerV3.Attributes;
using CoreExplorerV3.Models;
using Newtonsoft.Json;
using CoreExplorerV3.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using ExchangesLib.DTO;
using ExchangesLib.Services;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class ApiController : Controller
    {
        private IConfiguration configuration;
        private Exchange exchange;
        private MainCoins mainCoins;
        private readonly ILogger _logger;
        private IMemoryCache _cache;
        private IHttpContextAccessor _accessor;
        private string _ip;
        private MemoryCacheEntryOptions _memoryCacheEntryOptions;

        public ApiController(IConfiguration Configuration, MainCoins mc, ILogger<HomeController> logger, 
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
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMilliseconds(1));
        }

        public IActionResult Index()
        {
            var mc = mainCoins.GetCoin();
            var blockHash = mainCoins.GetBlockByIdMongo(559529);
            var block = mainCoins.GetBlockByHashMongo(blockHash.Hash);
            return View(new ApiViewModel()
            {
                MainCoinModel = mc,
                BlockHash = blockHash.Hash,
                Tx = block.AssetTX.First()
            });
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetDifficulty()
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                str = mainCoins.GetRawQuery("getdifficulty");
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json"); ;
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetConnectionCount()
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                str = mainCoins.GetRawQuery("getconnectioncount");
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json"); ;
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetBlockHash(uint index)
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                var blk = mainCoins.GetBlockByIdMongo(index);
                if (blk == null)
                {
                    throw new Exception();
                }
                str = blk.Hash;
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json"); ;
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetBlock(string hash)
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                var blk = mainCoins.GetBlockByHashMongo(hash);
                if (blk == null)
                {
                    throw new Exception();
                }
                str = JsonConvert.SerializeObject(blk); 
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json");
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetRawTansaction(string txid)
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                var tx = mainCoins.GetRawAssetTransactionMongo(txid);
                if (tx != null)
                {
                    tx.confirmations = (int)mainCoins.GetLastBlockMongo() - tx.Height + 1;
                }
                if (tx == null)
                {
                    throw new Exception();
                }
                str = JsonConvert.SerializeObject(tx);
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json");
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetMoneySupply(string txid, short decrypt)
        {
            var mc = mainCoins.GetCoin();
            if (!mc.IsCoinDistribution)
            {
                var strIPTimeout = "{ \"error\" : \"Feature disabled\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                str = mc.DynamicTotal.ToString();
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json");
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetAddress(string address)
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                //{"address":"REjXTrh64hKd4x6QvXiQzwTM47qXfWv63R","sent":0,"received":1000,"balance":"1000","last_txs":[{"type":"vout","addresses":"7ea8373b87dee6fc8846b0bd86ddfe2f9874e3a1df8bc33564f893525e397266"}]}
                var addressInfo = mainCoins.GetAddressInfo(address);
                AddressinfoApi json;
                if (addressInfo == null)
                {
                    json = new AddressinfoApi()
                    {
                        address = address,
                        balance = 0,
                        received = 0,
                        sent = 0
                    };
                }
                else
                {
                    json = new AddressinfoApi()
                    {
                        address = addressInfo.AddressInfo.Address,
                        balance = addressInfo.AddressInfo.Total,
                        received = addressInfo.AddressInfo.Received,
                        sent = -addressInfo.AddressInfo.Sent
                    };
                    var transactions = mainCoins.GetAddressTransactions(address, 0, 1000);
                    foreach (var tx in transactions)
                    {
                        json.last_txs.Add(new AddressinfoApiTx()
                        {
                            addresses = tx.Transaction,
                            type = tx.Type
                        });
                    }
                }
                str = JsonConvert.SerializeObject(json);
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json");
        }

        [HttpGet]
        [Produces("application/json")]
        public ActionResult GetBalance(string address)
        {
            bool exists;
            if (_cache.TryGetValue(_ip, out exists))
            {
                var strIPTimeout = "{ \"error\" : \"too much requests from your IP, put sleep 1 sec between requests\" }";
                return Content(strIPTimeout, "application/json"); ;
            }
            string str = string.Empty;
            try
            {
                var addressInfo = mainCoins.GetAddressInfo(address);
                if (addressInfo == null)
                {
                    str = "0";
                }
                else
                {
                    str = addressInfo.AddressInfo.Total.ToString();
                }
            }
            catch
            {
                str = "{ \"error\" : \"data error\" }";
            }
            _cache.Set(_ip, true, _memoryCacheEntryOptions);
            return Content(str, "application/json");
        }
    }
}