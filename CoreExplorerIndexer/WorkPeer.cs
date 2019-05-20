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
    public class WorkerPeer
    {
        static object locked = new object();
        public static void PeerIndex(object data)
        {
            var obj = data as Mnwork;
            var coinName = obj.MainCoinModel.CoinID;
            var coins = obj.MAINDB.GetCollection<BsonDocument>("assets");
            var coins2 = obj.MAINDB.GetCollection<MainCoinModel>("assets");
            var coin_peers = obj.DB.GetCollection<BsonDocument>(coinName + "_peers");
            while (true)
            {
                var uptime = Funcs.DateTimeToUnixTimestamp(DateTime.UtcNow);
                List<GetPeerInfoResponse> peerlist = new List<GetPeerInfoResponse>();
                try
                {
                    peerlist = obj.CoinService.GetPeerInfo();
                }
                catch
                {
                }
                try
                {
                    foreach (var peer in peerlist)
                    {
                        var document = new BsonDocument
                        {
                            {
                                "$set", new BsonDocument
                                {
                                    {"addr", peer.Addr},
                                    {"version", peer.Version},
                                    {"subver", peer.SubVer?.ToString() ?? ""},
                                    {"pingtime", peer.PingTime},
                                    {"uptime", uptime}
                                }
                            }
                        };
                        coin_peers.UpdateOne(new BsonDocument() { { "addr", peer.Addr } }, document, new UpdateOptions { IsUpsert = true });
                    }
                    coin_peers.DeleteMany(new BsonDocument()
                    {
                        {
                            "uptime", new BsonDocument
                            { { "$lt", uptime } }
                        }
                    });
                }
                catch (Exception e)
                {
                    lock (locked)
                    {
                        File.AppendAllText("peer_error.txt", "[" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + "] " + coinName + " " + e.ToString() + Environment.NewLine);
                    }
                }
                Thread.Sleep(10000);
                if (Interrupt)
                {
                    break;
                }
            }
        }
    }
}
