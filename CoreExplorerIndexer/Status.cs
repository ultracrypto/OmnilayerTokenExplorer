using BitcoinLib.Responses;
using BitcoinLib.Services.Coins.Base;
using BitcoinLib.Services.Coins.Bitcoin;
using CoreMongoDTO.DTO;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static CoreExplorerIndexer.Program;

namespace CoreExplorerIndexer
{
    class Status
    {
        private static bool isPortOpen(string host, int port, TimeSpan timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                }

            }
            catch
            {
                return false;
            }
            return true;
        }
        static object locked = new object();
        public static void StatusIndex(object data)
        {
            var obj = data as Mnwork;
            var assets = obj.MAINDB.GetCollection<BsonDocument>("assets");
            var assets2 = obj.MAINDB.GetCollection<MainCoinModel>("assets");
            while (true)
            {
                var uptime = Funcs.DateTimeToUnixTimestamp(DateTime.UtcNow);
                MainCoinModel CoinInfo;
                try
                {
                    CoinInfo = assets2.Find(c => c.CoinSymbol == obj.MainCoinModel.CoinSymbol && c.Version == Program.VERSION).First();
                    if (CoinInfo.NextWalletStatus > uptime)
                    {
                        Thread.Sleep(10000);
                        if (Interrupt)
                        {
                            break;
                        }
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Thread.Sleep(10000);
                    if (Interrupt)
                    {
                        break;
                    }
                    continue;
                }
                var sb = new StringBuilder();
                sb.Append("http://");
                if (!string.IsNullOrEmpty(obj.MainCoinModel.CoinAddressLocal))
                {
                    sb.Append(obj.MainCoinModel.CoinAddressLocal);
                }
                else
                {
                    sb.Append(obj.MainCoinModel.CoinAddress);
                }
                sb.Append(":");
                sb.Append(obj.MainCoinModel.CoinPort);
                GetNetworkInfoResponseLight resp;
                GetInfoResponseLight respinfo;
                string walletVersion = string.Empty;
                string protocolVersion = string.Empty;
                string subversion = string.Empty;
                string connections = string.Empty;
                string walletStatus = string.Empty;
                var cAddress = obj.MainCoinModel.CoinAddress;
                if (!string.IsNullOrEmpty(obj.MainCoinModel.CoinAddressLocal))
                {
                    cAddress = obj.MainCoinModel.CoinAddressLocal;
                }
                if (isPortOpen(cAddress, obj.MainCoinModel.CoinPortInt, TimeSpan.FromSeconds(3)))
                {
                    try
                    {
                        if (obj.MainCoinModel.IsInfoLight)
                        {
                            respinfo = obj.CoinService.GetInfoLight();
                            walletVersion = respinfo.Version.ToString();
                            protocolVersion = respinfo.ProtocolVersion.ToString();
                            subversion = respinfo.WalletVersion.ToString();
                            connections = respinfo.Connections.ToString();
                        } 
                        else
                        {
                            resp = obj.CoinService.GetNetworkInfoLight();
                            walletVersion = resp.Version.ToString();
                            protocolVersion = resp.ProtocolVersion.ToString();
                            subversion = resp.Subversion.ToString();
                            connections = resp.Connections.ToString();
                        }
                        walletStatus = "OK";
                    }
                    catch
                    {
                        walletStatus = "Error";
                    }
                }
                else
                {
                    walletStatus = "Port closed";
                }
                try
                {
                    var document = new BsonDocument
                    {
                        {
                            "$set", new BsonDocument
                            {
                                {"wallet_status", walletStatus},
                                {"wallet_version", walletVersion},
                                {"protocol_version", protocolVersion},
                                {"subversion", subversion},
                                {"connections", connections},
                                {"updated_walletstatus", uptime},
                                {"next_walletstatus", uptime + 36000000}
                            }
                        }
                    };
                    assets.UpdateOne(new BsonDocument() { { "coin_symbol", obj.MainCoinModel.CoinSymbol } }, document, new UpdateOptions { IsUpsert = true });
                }
                catch (Exception e)
                {
                    lock (locked)
                    {
                        File.AppendAllText("peer_error.txt", "[" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + "] " + obj.MainCoinModel.CoinSymbol + " " + e.ToString() + Environment.NewLine);
                    }
                }
                if (Interrupt)
                {
                    break;
                }
            }
        }
    }
}
