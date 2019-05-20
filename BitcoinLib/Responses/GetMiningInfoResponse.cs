// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using Newtonsoft.Json;
using System.Collections.Generic;

namespace BitcoinLib.Responses
{
    public class GetMiningInfoResponse
    {
        public int Blocks { get; set; }
        public int CurrentBockSize { get; set; }
        public int CurrentBlockTx { get; set; }
        public double Difficulty { get; set; }
        public string Errors { get; set; }
        public int GenProcLimit { get; set; }
        public decimal NetworkHashPS { get; set; }
        public int PooledTx { get; set; }
        public bool Testnet { get; set; }
        public string Chain { get; set; }
        public bool Generate { get; set; }
        public long HashesPerSec { get; set; }
        public double? ProofOfWork { get; set; }
        public double? ProofOfStake { get; set; }
        public double? SearchInterval { get; set; }
        public Dictionary<string, decimal?> DiffList { get; set; }
    }

    public class GetMiningInfoResponseDigi
    {
        public int Blocks { get; set; }
        public int CurrentBockSize { get; set; }
        public int CurrentBlockTx { get; set; }
        public double Difficulty { get; set; }
        public string Errors { get; set; }
        public int GenProcLimit { get; set; }
        public decimal NetworkHashPS { get; set; }
        public int PooledTx { get; set; }
        public bool Testnet { get; set; }
        public string Chain { get; set; }
        public bool Generate { get; set; }
        public long HashesPerSec { get; set; }
        public double? ProofOfWork { get; set; }
        public double? ProofOfStake { get; set; }
        public double? SearchInterval { get; set; }
        public Dictionary<string, decimal?> DiffList { get; set; }
        public decimal difficulty_sha256d { get; set; }
        public decimal difficulty_scrypt { get; set; }
        public decimal difficulty_groestl { get; set; }
        public decimal difficulty_skein { get; set; }
        public decimal difficulty_qubit { get; set; }
    }
    public class GetMiningInfoPosPowResponse
    {
        public int Blocks { get; set; }
        public int CurrentBockSize { get; set; }
        public int CurrentBlockTx { get; set; }
        public double netmhashps { get; set; }
        public PowPoSDifficulty Difficulty { get; set; }
    }

    public class GetMiningInfoPosPowResponse2
    {
        public int Blocks { get; set; }
        public int CurrentBockSize { get; set; }
        public int CurrentBlockTx { get; set; }
        public double netmhashps { get; set; }
        public double Difficulty { get; set; }
    }

    public class GetMiningInfoPosPowResponse3
    {
        [JsonProperty(PropertyName = "Blocks")]
        public int Blocks { get; set; }
        [JsonProperty(PropertyName = "Current Block Size")]
        public int CurrentBockSize { get; set; }
        [JsonProperty(PropertyName = "Current Block Tx")]
        public int CurrentBlockTx { get; set; }
        [JsonProperty(PropertyName = "Net MH/s")]
        public double netmhashps { get; set; }
        public PowPoSDifficulty2 Difficulty { get; set; }
    }


    public class PowPoSDifficulty2
    {
        [JsonProperty(PropertyName = "Proof of Work")]
        public double ProofOfWork { get; set; }
        [JsonProperty(PropertyName = "Proof of Stake")]
        public double ProofOfStake { get; set; }
        [JsonProperty(PropertyName = "Search Interval")]
        public double SearchInterval { get; set; }
    }

    public class PowPoSDifficulty
    {
        [JsonProperty(PropertyName = "proof-of-work")]
        public double ProofOfWork { get; set; }
        [JsonProperty(PropertyName = "proof-of-stake")]
        public double ProofOfStake { get; set; }
        [JsonProperty(PropertyName = "search-interval")]
        public double SearchInterval { get; set; }
    }
}