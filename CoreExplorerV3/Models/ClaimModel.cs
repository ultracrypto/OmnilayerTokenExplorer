using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiMon.Models
{
    public class ClaimModel
    {
        public bool? Success { get; set; }
        public bool? Error { get; set; }
        public string Address { get; set; }
        public MainCoinModel MainCoinModel { get; set; }
    }
}
