using System;
namespace CoreExplorerV3.Models
{
    public class Pageing
    {
        public String Addon { get; set; }
        public long MaxCount { get; set; }
        public int OnPage { get; set; }
        public int Pagenum { get; set; }
    }
}
