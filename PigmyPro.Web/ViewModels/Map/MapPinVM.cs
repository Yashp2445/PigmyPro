using System;
using System.Collections.Generic;

namespace PigmyPro.Web.ViewModels.Map
{
    public class MapPinVM
    {
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public int SequenceNo { get; set; }
        public string ColorHex { get; set; } = string.Empty;
        public List<MapEntryVM> Entries { get; set; } = new();
    }

    public class MapEntryVM
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string EntryTime { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int SequenceNo { get; set; }
    }
}
