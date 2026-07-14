using System;

namespace PigmyPro.Domain.Entities
{
    public class Agent
    {
        public int BankID { get; set; }
        public decimal brnc_code { get; set; } 
        public decimal code { get; set; }     
        public string? NAME { get; set; }
        public string? MobileNo { get; set; }
        public bool? Block { get; set; }      
        public int? NoOfHolidays { get; set; }
        public int ReceiptNoPerAc { get; set; } = 1;
        public string? RadyToCash { get; set; }
        public DateTime EntryDate { get; set; }
    }
}
