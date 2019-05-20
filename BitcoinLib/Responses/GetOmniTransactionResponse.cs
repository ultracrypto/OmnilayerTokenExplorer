using System.Collections.Generic;
using BitcoinLib.Responses.Bridges;
using BitcoinLib.Responses.SharedComponents;
using MongoDB.Bson.Serialization.Attributes;

namespace BitcoinLib.Responses
{
    /*
     * 
￼
{
  "txid": "3cf466e54a0e10ec9ca8b2ee92a7ba52fc752005a30ef17e0a9a8195eedfb787",
  "fee": "0.00400000",
  "sendingaddress": "131dNpFcE3DQib4jSyFEJL2xaEMGUo1eUE",
  "ismine": false,
  "version": 0,
  "type_int": 50,
  "type": "Create Property - Fixed",
  "propertyid": 701,
  "divisible": true,
  "ecosystem": "main",
  "propertytype": "divisible",
  "category": "Financial and insurance activities",
  "subcategory": "Financial service activities, except insurance and pension funding",
  "propertyname": "OTOCASH (OTO)",
  "data": "OTOCASH Tokens Are For The Future Financial System Currency, A Future Payment System Is With OTOCASH Platform.",
  "url": "https://www.otocash.io/",
  "amount": "38254582.00000000",
  "valid": true,
  "blockhash": "00000000000000000019f63082e7db789e2e8e95a9650825e90a787f5257804f",
  "blocktime": 1548107944,
  "positioninblock": 10,
  "block": 559529,
  "confirmations": 9555
}

    */
    [BsonIgnoreExtraElements]
    public class GetOmniTransactionResponse
    {
        public string txid { get; set; }
        public string txidlower { get; set; }
        public string fee { get; set; }
        public string sendingaddress { get; set; }
        public string sendingaddresslower { get; set; }
        public string referenceaddress { get; set; }
        public string referenceaddresslower { get; set; }
        public bool ismine { get; set; }
        public int version { get; set; }
        public int type_int { get; set; }
        public string type { get; set; }
        public long propertyid { get; set; }
        public bool divisible { get; set; }
        public string ecosystem { get; set; }
        public string amount { get; set; }
        public bool valid { get; set; }
        public string blockhash { get; set; }
        public int blocktime { get; set; }
        public int positioninblock { get; set; }
        public int block { get; set; }
        public int confirmations { get; set; }
    }
}