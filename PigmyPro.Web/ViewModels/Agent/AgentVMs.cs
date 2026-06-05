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

        // ── Role flags (set by controller) ──────────────────────────────
        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }

        // ── Agent fields ─────────────────────────────────────────────────
        [Required(ErrorMessage = "Agent Code is required")]
        [Display(Name = "Agent ID/Code")]
        public decimal Code { get; set; }

        [Required(ErrorMessage = "Agent Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Legal Name")]
        public string NAME { get; set; } = string.Empty;

        [StringLength(15)]
        [Display(Name = "Mobile Number")]
        public string? MobileNo { get; set; }

        [Display(Name = "Number of Holidays")]
        public int NoOfHolidays { get; set; }       // int — no 0.00

        public bool Block { get; set; }

        [Display(Name = "Ready to Cash")]
        public bool ReadyToCash { get; set; }

        // ── Bank / Branch selection ───────────────────────────────────────
        public int? SelectedBankID { get; set; }

        [Display(Name = "Assigned Branch")]
        public decimal BranchCode { get; set; }

        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }
    }

    // Used on Index page for filtering
    public class AgentIndexVM
    {
        public IEnumerable<AgentListVM> Agents { get; set; } = new List<AgentListVM>();

        // Filter selections
        public int? FilterBankID { get; set; }
        public decimal? FilterBranchCode { get; set; }

        // Dropdown sources (null means not shown)
        public IEnumerable<SelectListItem>? BankList { get; set; }
        public IEnumerable<SelectListItem>? BranchList { get; set; }

        public bool IsSuperAdmin { get; set; }
        public bool IsBankAdmin { get; set; }
    }
}