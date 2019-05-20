// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BitcoinLib.Responses
{
    public class GetBudgetResponse
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public string Hash { get; set; }
        public string FeeHash { get; set; }
        public int BlockStart { get; set; }
        public int BlockEnd { get; set; }
        public int TotalPaymentCount { get; set; }
        public int RemainingPaymentCount { get; set; }
        public string PaymentAddress { get; set; }
        public double Ratio { get; set; }
        public int Yeas { get; set; }
        public int Nays { get; set; }
        public int Abstains { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal MonthlyPayment { get; set; }
        public bool IsEstablished { get; set; }
        public bool IsValid { get; set; }
        public string IsValidReason { get; set; }
        public bool fValid { get; set; }
        public decimal Alloted { get; set; }
        public decimal TotalBudgetAlloted { get; set; }
    }

    public class GetBudgetVotesResponse
    {
        public string mnId { get; set; }
        public string nHash { get; set; }
        public string Vote { get; set; }
        public int nTime { get; set; }
        public bool fValid { get; set; }
    }
}