using CoreExplorerV3.Models;
using CoreMongoDTO.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.DTO
{
    public class AddressTransactionInfo
    {
        public AddressInfo AddressInfo { get; set; }
        public AddressTransaction AddressTransaction { get; set; }
    }
}
