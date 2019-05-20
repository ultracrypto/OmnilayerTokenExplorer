using BitcoinLib.Responses.SharedComponents;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinLib
{
    public static class Extensions
    {
        public static BsonArray ToBsonDocumentArray(this IEnumerable<Vin> list)
        {
            var array = new BsonArray();
            foreach (var item in list)
            {
                array.Add(item.ToBson());
            }
            return array;
        }

        public static BsonArray ToBsonDocumentArray(this IEnumerable<Vout> list)
        {
            var array = new BsonArray();
            foreach (var item in list)
            {
                array.Add(item.ToBson());
            }
            return array;
        }
    }
}
