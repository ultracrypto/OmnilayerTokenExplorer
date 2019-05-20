using BitcoinLib.Responses;
using CoreExplorerV3.Helpers;
using CoreExplorerV3.Services;
using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;

namespace CoreExplorerV3.Models
{
    public class HashrateViewModel
    {
        public uint LastBlock { get; set; }
        public uint LastAssetBlock { get; set; }
        public List<ClientBlock> LastBlocks { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
        public int ShowBlocks { get; set; }
    }
}