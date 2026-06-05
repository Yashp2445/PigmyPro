using System;

namespace PigmyPro.Domain.Entities
{
    public class MobileTransaction
    {
        public int ID { get; set; }
        public int BankID { get; set; }
        public int BranchID { get; set; }
        public decimal AgentCode { get; set; } // Map to agntmast.code
        public decimal AccountNo { get; set; } // Map to acmaster.CODE2
        public decimal Amount { get; set; }
        public string? CollectionStatus { get; set; } // e.g., 'P' for pending, 'C' for completed
        public DateTime CollectionDate { get; set; }
        public string? MobileSerialID { get; set; } // Unique ID from mobile app
        public DateTime EntryDate { get; set; }
        
        // Joined details for UI (optional but helpful)
        public string? AgentName { get; set; }
        public string? CustomerName { get; set; }
    }
}
