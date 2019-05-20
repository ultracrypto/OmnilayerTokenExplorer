using CoreExplorerV3.Services;
using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoreExplorerV3.Models
{
    public class TransactionViewModel
    {
        public String Trx { get; set; }
        public ClientTransaction Transaction { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
        public bool IsConfirmed { get; set; }
    }
}