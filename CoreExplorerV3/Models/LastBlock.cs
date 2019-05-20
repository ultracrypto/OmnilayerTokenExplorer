using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    public class LastBlock
    {
        [BsonId]
        public object Id { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("block")]
        public uint Block { get; set; }
    }
}
