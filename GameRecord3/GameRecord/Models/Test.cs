using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GameRecord.Models
{
    public class DemoStagingModel
    {
        public string provider { get; set; }
        public JObject providerConfig { get; set; }
        public string currency { get; set; } = "MYR";
        public string playerId1 { get; set; }
        public string playerName1 { get; set; }
        public string password1 { get; set; }
        public string btnRegister { get; set; }
        public string status1 { get; set; }
        public string playerId2 { get; set; }
        public string btnGetBalance { get; set; }
        public string status2 { get; set; }
        public string playerId3 { get; set; }
        public decimal amount3 { get; set; }
        public string btnDeposit { get; set; }
        public string status3 { get; set; }
        public string playerId4 { get; set; }
        public decimal amount4 { get; set; }
        public string btnWithdraw { get; set; }
        public string status4 { get; set; }
        public string playerId5 { get; set; }
        public string language5 { get; set; }
        public string btnOpenGame { get; set; }
        public string status5 { get; set; }
        public string request1 { get; set; }
        public string request2 { get; set; }
        public string request3 { get; set; }
        public string request4 { get; set; }
        public string request5 { get; set; }
        public string response1 { get; set; }
        public string response2 { get; set; }
        public string response3 { get; set; }
        public string response4 { get; set; }
        public string response5 { get; set; }
        public bool success1 { get; set; }
        public bool success2 { get; set; }
        public bool success3 { get; set; }
        public bool success4 { get; set; }
        public bool success5 { get; set; }
        public string anchor { get; set; }
        public string url5 { get; set; }
        public string device5 { get; set; }
        public string btnGetBet { get; set; }
        public string request6 { get; set; }
        public string response6 { get; set; }
        public bool success6 { get; set; }
    }
}