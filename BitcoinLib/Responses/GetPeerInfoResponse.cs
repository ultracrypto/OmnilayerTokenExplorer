// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BitcoinLib.Responses
{
    public class GetPeerInfoResponse
    {
        public uint Id { get; set; }
        public string Addr { get; set; }
        public string AddrLocal { get; set; }
        public string Services { get; set; }
        public long LastSend { get; set; }
        public long LastRecv { get; set; }
        public long BytesSent { get; set; }
        public long BytesRecv { get; set; }
        public long ConnTime { get; set; }
        public double PingTime { get; set; }
        public double Version { get; set; }
        public string SubVer { get; set; }
        public bool Inbound { get; set; }
        public long StartingHeight { get; set; }
        public long BanScore { get; set; }

        [JsonProperty("synced_headers")]
        public int SyncedHeaders { get; set; }

        [JsonProperty("synced_blocks")]
        public int SyncedBlocks { get; set; }

        public IList<int> InFlight { get; set; }
        public bool WhiteListed { get; set; }
    }
}