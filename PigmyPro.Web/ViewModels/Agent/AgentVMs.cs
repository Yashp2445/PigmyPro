using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.ViewModels.Agent
{
    public class AgentListVM
    {
        public int BankID { get; set; }
        public decimal BranchCode { get; set; }
        public decimal Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MobileNo { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public int Holidays { get; set; }
        public bool IsBlocked { get; set; }
        public bool ReadyToCash { get; set; }
        public DateTime? EntryDate { get; set; }
    }

    public class AgentCreateEditVM
    {
        public bool IsEdit { get; set; }

        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }

        [Required(ErrorMessage = "Agent Code is required")]
        [Display(Name = "Agent ID/Code")]
        public decimal? Code { get; set; }

        [Required(ErrorMessage = "Agent Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Legal Name")]
        public string NAME { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required")]
        [ValidIndianMobile(ErrorMessage = "Enter a valid 10-digit mobile number")]
        [Display(Name = "Mobile Number")]
        public string? MobileNo { get; set; }

        [Display(Name = "Number of Holidays")]
        public int NoOfHolidays { get; set; }      

        public bool Block { get; set; }

        [StringLength(150)]
        [Display(Name = "Block Remark")]
        public string? BlockRemark { get; set; }

        [Display(Name = "Reset Agent (clear mobile & password)")]
        public bool ResetAgent { get; set; }

        [StringLength(150)]
        [Display(Name = "Reset Remark")]
        public string? ResetRemark { get; set; }

        [Display(Name = "Ready to Cash")]
        public bool ReadyToCash { get; set; }

        public int? SelectedBankID { get; set; }

        [Display(Name = "Assigned Branch")]
        public decimal BranchCode { get; set; }

        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }
    }

    public class AgentIndexVM
    {
        public IEnumerable<AgentListVM> Agents { get; set; } = new List<AgentListVM>();

        public int? FilterBankID { get; set; }
        public decimal? FilterBranchCode { get; set; }

        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }

        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }
    }

    public class ValidIndianMobileAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var mobile = value as string;

            if (string.IsNullOrWhiteSpace(mobile))
                return new ValidationResult(ErrorMessage ?? "Mobile number is required");

            mobile = mobile.Trim();

            if (mobile.Length != 10 || !IsAllDigits(mobile))
                return new ValidationResult(ErrorMessage ?? "Mobile number must be exactly 10 digits");

            if (mobile[0] < '6' || mobile[0] > '9')
                return new ValidationResult(ErrorMessage ?? "Mobile number must start with 6-9");

            if (IsAllSameDigit(mobile))
                return new ValidationResult("Mobile number cannot be all the same digit");

            if (IsSequential(mobile))
                return new ValidationResult("Mobile number cannot be a sequential pattern");

            return ValidationResult.Success;
        }

        private static bool IsAllDigits(string s)
        {
            foreach (var c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }

        private static bool IsAllSameDigit(string s)
        {
            for (int i = 1; i < s.Length; i++)
                if (s[i] != s[0]) return false;
            return true;
        }

        private static bool IsSequential(string s)
        {
            bool ascending = true, descending = true;
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] - s[i - 1] != 1) ascending = false;
                if (s[i - 1] - s[i] != 1) descending = false;
            }
            return ascending || descending;
        }
    }
}