using System;
using System.Collections.Generic;
using System.Text;

namespace PigmyPro.Domain.Entities
{
    public class Branch
    {
        public int BranchID { get; set; }
        public int BankID { get; set; }
        public string? Name { get; set; }
        public string? Active { get; set; }
        public DateTime EntryDate { get; set; }
    }
}
