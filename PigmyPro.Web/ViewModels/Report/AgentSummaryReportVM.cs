using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Report
{
    public class AgentSummaryReportVM
    {
        // Filters
        public DateTime DateFrom { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime DateTo { get; set; } = DateTime.Today;
        public int? FilterBankID { get; set; }
        public int? FilterBranchID { get; set; }
        public long? FilterAgentCode { get; set; }

        // Dropdown data
        public List<SelectListItem> Banks { get; set; } = new();
        public List<SelectListItem> Branches { get; set; } = new();
        public List<SelectListItem> Agents { get; set; } = new();

        // Report data
        public List<AgentSummaryRow> Rows { get; set; } = new();
        public bool HasSearched { get; set; }

        // Computed summary
        public int GrandTotalAccounts => Rows?.Count > 0 ? Rows.Sum(r => r.TotalAccounts) : 0;
        public decimal GrandTotalAmount => Rows?.Count > 0 ? Rows.Sum(r => r.TotalAmount) : 0;

        // Role context
        public string UserRole { get; set; } = string.Empty;
    }
}
