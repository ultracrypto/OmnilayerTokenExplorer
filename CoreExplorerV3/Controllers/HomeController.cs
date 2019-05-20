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
using CoreMongoDTO.DTO;
using BitcoinLib.Responses;
using CoreExplorerV3.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using ExchangesLib.DTO;
using ExchangesLib.Services;
using Newtonsoft.Json;
using CoreExplorerV3.DTO;
using System.Globalization;

namespace CoreExplorerV3.Controllers
{
    [ServiceFilter(typeof(ViewFooterAttribute))]
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        public const int ON_PAGE = 50;
        private IConfiguration _configuration;
        public MainCoins mainCoins;
        private Exchange exchange;
        private IMemoryCache _cache;
        private IHttpContextAccessor _accessor;
        private MemoryCacheEntryOptions _memoryCacheEntryOptions;
        public HomeController(IConfiguration Configuration, MainCoins mc,
            IMemoryCache memoryCache, IHttpContextAccessor accessor, IStringLocalizer<SharedResources> localizer,
            Exchange ex)
        {
            exchange = ex;
            _localizer = localizer;
            _accessor = accessor;
            _cache = memoryCache;
            _configuration = Configuration;
            mainCoins = mc;
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10));
        }

        public IActionResult Index()
        {
            HashrateViewModel hashRate;
            var coins = mainCoins.GetCoin();
            if (!_cache.TryGetValue(coins.CoinSymbol + "_main_index", out hashRate))
            {
                var lastBlock = mainCoins.GetLastBlockMongo();
                var coinconfirm = lastBlock + coins.CoinConfirms;
                var lastBlockWalletHash = mainCoins.GetBlockByIdMongo(lastBlock);
                int showBlocks = _configuration.GetSection("TotalShow").Get<int>();
                var lastBlocks = mainCoins.GetlastXAssetBlocks(showBlocks);
                var blk = lastBlocks.FirstOrDefault();
                uint blkAsset = 0;
                if (blk != null)
                {
                    blkAsset = checked((uint)blk.Height);
                }
                hashRate = new HashrateViewModel
                {
                    ShowBlocks = showBlocks,
                    LastBlock = lastBlock,
                    LastAssetBlock = blkAsset,
                    LastBlocks = lastBlocks,
                    MainCoinModel = coins
                };
                _cache.Set(coins.CoinSymbol + "_main_index", hashRate, _memoryCacheEntryOptions);
            }
            return View(hashRate);
        }


        public IActionResult Search(string Search)
        {
            if (string.IsNullOrEmpty(Search))
            {
                return View();
            }
            var coins = mainCoins.GetCoin();
            ClientBlock blk;
            ClientBlock blkUnconfirmed;
            String blkHashUnconfirmed;
            GetRawTransactionResponseMongo transaction;
            GetOmniTransactionResponse assetTransactionUnconfirmed;
            GetRawTransactionResponse transactionUnconfirmed;
            object addr;

            uint blockHeight;
            string blockHash = Search;
            if (uint.TryParse(Search, out blockHeight))
            {
                blk = mainCoins.GetBlockByIdMongo(blockHeight);
                if (blk != null)
                {
                    return RedirectToAction("block", new { block = blk.Height });
                }
            }
            else if (blockHash.Length == 64)
            {
                blk = mainCoins.GetAssetBlockMongo(blockHash);
                if (blk != null)
                {
                    return RedirectToAction("block", new { block = blk.Hash });
                }
            }

            if (Search.Length == 64)
            {
                transaction = mainCoins.GetRawTransactionMongo(Search);
                if (transaction != null)
                {
                    return RedirectToAction("transaction", new { transaction = transaction.TxId });
                }
            }
            if (coins.AddrLen.Any(a => a == Search.Length))
            {
                addr = mainCoins.SearchAddressInfo(Search);
                if (addr != null)
                {
                    return RedirectToAction("address", new { address = Search });
                }
            }

            if (blockHeight > 0)
            {
                try
                {
                    blkHashUnconfirmed = mainCoins.GetBlockHashByHeight(blockHeight);
                    if (blkHashUnconfirmed != null)
                    {
                        return RedirectToAction("block", new { block = blockHeight });
                    }
                }
                catch
                {
                    // silent
                }
            }
            else if (blockHash.Length == 64)
            {
                try
                {
                    blkUnconfirmed = mainCoins.GetBlockByHash(blockHash);
                    if (blkUnconfirmed != null)
                    {
                        return RedirectToAction("block", new { block = blkUnconfirmed.Hash });
                    }
                }
                catch
                {
                    //silent
                }
            }
            if (Search.Length == 64)
            {
                try
                {
                    assetTransactionUnconfirmed = mainCoins.GetRawTransactionAsset(Search);
                    if (assetTransactionUnconfirmed != null)
                    {
                        return RedirectToAction("transaction", new { transaction = assetTransactionUnconfirmed.txid });
                    }
                }
                catch
                {
                    // silent
                }
                try
                {
                    transactionUnconfirmed = mainCoins.GetRawTransaction(Search);
                    if (transactionUnconfirmed != null)
                    {
                        return RedirectToAction("transaction", new { transaction = Search });
                    }
                }
                catch
                {
                    // silent
                }
            }

            if (coins.AddrLen.Any(a => a == Search.Length) && mainCoins.IsValidAddres(Search))
            {
                return Redirect("~/address/" + Search);
            }

            return View();
        }

        public IActionResult Block(string block, int? pagenum = null)
        {
            try
            {
                uint blockHeight;
                string blockHash = block;
                ClientBlock blk;

                var lastBlockMongo = mainCoins.GetLastBlockMongo();
                if (uint.TryParse(block, out blockHeight))
                {
                    blk = mainCoins.GetAssetBlockByIdMongo(blockHeight);
                }
                else
                {
                    blk = mainCoins.GetAssetBlockMongo(blockHash);
                }
                if (blk == null)
                {
                    var lastBlockWallet = mainCoins.GetLastBlock();
                    if (blockHeight != 0)
                    {
                        blockHash = mainCoins.GetBlockHashByHeight(blockHeight);
                    }
                    var blkUnconfirmed = mainCoins.GetBlockByHash(blockHash);
                    if (blkUnconfirmed != null && lastBlockMongo > blockHeight)
                    {
                        return View("~/Views/Home/BlockUnindexed.cshtml");
                    }
                    if (blkUnconfirmed != null)
                    {
                        return View("~/Views/Home/BlockUnconfirmed.cshtml");
                    }
                }
                blk.ConfirmationsClient = (int)lastBlockMongo - blk.Height + 1;
                List<ClientTransaction> trlist = new List<ClientTransaction>();
                var pagenr = 1;
                if (pagenum != null)
                {
                    pagenr = pagenum.Value;
                }
                var zPage = pagenr - 1;
                if (zPage < 0)
                {
                    zPage = 0;
                    pagenr = 1;
                }
                var start = (pagenr - 1) * ON_PAGE;
                var txs = mainCoins.GetRawAssetTransactionMongoList(blk.Height);
                var count = blk.Tx.Count;
                var maxPage = count / ON_PAGE + 1;
                if (zPage >= maxPage)
                {
                    zPage = 0;
                    pagenr = 1;
                }
                blk.BTCFee = 0.0m;
                blk.OMNIFee = 0.0m;
                blk.TxCount = blk.Additional != null && blk.Additional.ContainsKey(mainCoins.GetCoin().CoinID + "_assettxcount") ? Convert.ToInt32(blk.Additional[mainCoins.GetCoin().CoinID + "_assettxcount"]) : 0;
                foreach (var tx in txs)
                {
                    var transaction = new ClientTransaction()
                    {
                        Amount = decimal.Parse(tx.amount, CultureInfo.InvariantCulture),
                        block = tx.block,
                        amount = tx.amount,
                        blockhash = tx.blockhash,
                        blocktime = tx.blocktime,
                        confirmations = tx.confirmations,
                        BlockDateTime = MainCoins.UnixTimeStampToDateTime(tx.blocktime),
                        ConfirmationsClient = (int)lastBlockMongo - blk.Height + 1,
                        divisible = tx.divisible,
                        ecosystem = tx.ecosystem,
                        fee = tx.fee,
                        ismine = tx.ismine,
                        positioninblock = tx.positioninblock,
                        propertyid = tx.propertyid,
                        referenceaddress = tx.referenceaddress,
                        sendingaddress = tx.sendingaddress,
                        txid = tx.txid,
                        type = tx.type,
                        type_int = tx.type_int,
                        valid = tx.valid,
                        version = tx.version
                    };
                    blk.BTCFee += decimal.Parse(tx.fee, CultureInfo.InvariantCulture);
                    blk.TotalTransfered += transaction.Amount;
                    trlist.Add(transaction);
                }
                return View(new BlockViewModel
                {
                    Block = blk,
                    ListTransactions = trlist,
                    MainCoinModel = mainCoins.GetCoin(),
                    IsConfirmed = true,
                    Pageing = new Pageing()
                    {
                        MaxCount = count,
                        OnPage = ON_PAGE,
                        Pagenum = pagenr
                    }
                });
            }
            catch
            {
                return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }        

        public IActionResult Transaction(String transaction)
        {
            try
            {
                var lastBlockMongo = mainCoins.GetLastBlockMongo();
                var tx = mainCoins.GetRawAssetTransactionMongo(transaction);
                if (tx == null)
                {
                    var lastBlockWallet = mainCoins.GetLastBlock();
                    try
                    {
                        var unconfirmedTx = mainCoins.GetRawTransactionAsset(transaction);
                        if (unconfirmedTx != null)
                        {
                            return View("~/Views/Home/TransactionUnindexed.cshtml");
                        }
                    }
                    catch
                    {
                    }
                    var unconfirmedTxRaw = mainCoins.GetRawTransaction(transaction);
                    if (unconfirmedTxRaw != null)
                    {
                        return View("~/Views/Home/TransactionUnindexed.cshtml");
                    }
                    throw new Exception("Transaction not found");
                }
                var blk = mainCoins.GetAssetBlockMongo(tx.blockhash);
                var trVM = new TransactionViewModel
                {
                    Trx = tx.txid,
                    Transaction = new ClientTransaction()
                    {
                        Amount = decimal.Parse(tx.amount, CultureInfo.InvariantCulture),
                        block = tx.block,
                        amount = tx.amount,
                        blockhash = tx.blockhash,
                        blocktime = tx.blocktime,
                        confirmations = tx.confirmations,
                        BlockDateTime = MainCoins.UnixTimeStampToDateTime(tx.blocktime),
                        ConfirmationsClient = (int)lastBlockMongo - blk.Height + 1,
                        divisible = tx.divisible,
                        ecosystem = tx.ecosystem,
                        fee = tx.fee,
                        ismine = tx.ismine,
                        positioninblock = tx.positioninblock,
                        propertyid = tx.propertyid,
                        referenceaddress = tx.referenceaddress,
                        sendingaddress = tx.sendingaddress,
                        txid = tx.txid,
                        type = tx.type,
                        type_int = tx.type_int,
                        valid = tx.valid,
                        version = tx.version
                    },
                    MainCoinModel = mainCoins.GetCoin(),
                    IsConfirmed = true
                };
                return View(trVM);
            }
            catch
            {
                return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [Produces("application/json")]
        public ActionResult AddressTransactions(string address, string draw, int start)
        {
            var addressInfo = mainCoins.GetAddressInfo(address);
            var transactions = mainCoins.GetAddressTransactions(address, start, ON_PAGE);
            long count = 0;
            if (addressInfo != null)
            {
                count = Convert.ToInt64(Math.Round(addressInfo.AddressInfo.Transactions));
            }
            else
            {
                count = mainCoins.GetAddressTransactionsCount(address);
            }
            var json = new AddressTransactionJson();
            json.draw = draw;
            json.recordsTotal = count;
            json.recordsFiltered = count;
            json.data = new List<string[]>();
            foreach (var tr in transactions)
            {
                json.data.Add(new string[7]
                {
                    tr.BlockTime.ToString("yyyy-MM-dd HH:mm"),
                    tr.Height.ToString(),
                    tr.Transaction.ToString(),
                    tr.Amount.ToString(),
                    tr.Total.ToString(),
                    tr.Type.ToString(),
                    tr.Valid.ToString()
                });
            }
            var str = JsonConvert.SerializeObject(json);
            return Content(str, "application/json");
        }

        public IActionResult Address(string address, int? pagenum = null, bool? unconfirmed = false)
        {
            try
            {
                var addressInfo = mainCoins.GetAddressInfo(address);
                if (addressInfo == null && mainCoins.IsValidAddres(address))
                {
                    return View(new AddressViewModel()
                    {
                        Address = address,
                        AddressInfo = new AddressInfo()
                        {
                            Address = address,
                            Received = 0.0m,
                            Id = "",
                            Sent = 0.0m,
                            Total = 0.0m,
                            Transactions = 0
                        },
                        MainCoinModel = mainCoins.GetCoin()
                    });
                }
                var mc = mainCoins.GetCoin();
                ExchangePrice ep = exchange.GetMarketprice(mc.CoinSymbol.ToUpper());
                ExchangeFiat fiat = exchange.GetMarketFiat();           
                return View(new AddressViewModel()
                {
                    Address = addressInfo.AddressInfo.Address,
                    AddressInfo = addressInfo.AddressInfo,
                    MainCoinModel = mc,
                    ExchangeFiat = fiat,
                    ExchangePrice = ep,
                    IsUnconfirmed = unconfirmed.GetValueOrDefault()
                });
            }
            catch
            {
                return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
