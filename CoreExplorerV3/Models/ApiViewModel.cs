﻿using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class ApiViewModel
    {
        public string BlockHash { get; set; }
        public string Tx { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
    }
}
