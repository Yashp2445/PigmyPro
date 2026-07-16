using System;
using System.Collections.Generic;
using System.Text;

namespace PigmyPro.Domain.Entities
{
    public class User
    {
        public int UserID { get; set; }
        public int BankID { get; set; }
        public int? BranchID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "BranchAdmin"; // SuperAdmin, BankAdmin, BranchAdmin
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? MobileNo { get; set; }
        public bool IsActive { get; set; }
        public DateTime Entry_Date { get; set; }
    }
}
