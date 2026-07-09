using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.ViewModels.Account
{
    public class AccountListVM
    {
        public int BankID { get; set; }       
        public decimal Code1 { get; set; }
        public decimal BrncCode { get; set; }
        public decimal Code2 { get; set; }
        public string? TypeName { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public decimal Balance { get; set; }
        public DateTime? OpenDate { get; set; }
        public decimal? AgnCode { get; set; }
        public string? MobileNo { get; set; }
    }

    public class AccountIndexVM
    {
        public IEnumerable<AccountListVM> Accounts { get; set; } = new List<AccountListVM>();
        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }
        public int? FilterBankID { get; set; }
        public decimal? FilterBranchCode { get; set; }
        public decimal? FilterCode1 { get; set; }
        public IEnumerable<SelectListItem>? AccountTypeList { get; set; }
        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }
    }

    public class AccountCreateEditVM
    {
        public bool IsEdit { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }
        public int? SelectedBankID { get; set; }
        public decimal? SelectedBranchCode { get; set; }

        [Required(ErrorMessage = "Account Type (GL) is required")]
        [Display(Name = "Account Type")]
        public decimal Code1 { get; set; }

        [Required(ErrorMessage = "Account Serial (Code2) is required")]
        [Display(Name = "Account Number")]
        public decimal Code2 { get; set; }

        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Display(Name = "Address")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Display(Name = "Current Balance")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "Opening Date is required")]
        [Display(Name = "Opening Date")]
        public DateTime? OpenDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Field Agent must be assigned")]
        [Display(Name = "Assigned Agent")]
        public decimal? AgnCode { get; set; }

        [StringLength(15)]
        [Display(Name = "Mobile Number")]
        public string? MobileNo { get; set; }

        public IEnumerable<SelectListItem>? AccountTypeList { get; set; }
        public IEnumerable<SelectListItem>? AgentList { get; set; }
        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }
    }
}
