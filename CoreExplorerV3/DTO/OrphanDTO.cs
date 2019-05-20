using CoreExplorerV3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.DTO
{
    public class OrphanDTO
    {
        public List<MongoBlock> Orphans { get; set; }
        public MongoBlock Accepted { get; set; }
        public MongoBlock Origin { get; set; }
    }
}
