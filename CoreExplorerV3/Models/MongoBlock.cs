using BitcoinLib.Responses;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreExplorerV3.Models
{
    [BsonIgnoreExtraElements]
    public class MongoBlock : ClientBlock
    {
        [BsonId]
        public object Id { get; set; }

        [BsonElement("HashLower")]
        public string HashLower { get; set; }
    }
}
