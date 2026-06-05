using System;
using System.Collections.Generic;
using System.Text;

namespace PigmyPro.Domain.Entities
{
    public class CustomerAccount
    {
        public int BankID { get; set; }
        public decimal CODE1 { get; set; } // GL Code (NUMERIC 5,0)
        public decimal brnc_code { get; set; } // Branch ID (NUMERIC 10,0)
        public decimal CODE2 { get; set; } // Account Number (NUMERIC 18,0)
        public string? name { get; set; }
        public decimal? BALANCE { get; set; }
        public DateTime? OPN_DATE { get; set; }
        public decimal? AgnCode { get; set; } // Agent Association (NUMERIC 5,0)
        public string? Mobile_No { get; set; }
        public DateTime? Entry_Date { get; set; }
    }
}
