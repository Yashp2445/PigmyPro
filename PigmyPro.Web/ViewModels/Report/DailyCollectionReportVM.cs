using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Report
{
    public class DailyCollectionReportVM
    {
        // Filters
        public DateTime DateFrom { get; set; } = DateTime.Today;
        public DateTime DateTo { get; set; } = DateTime.Today;
        public int? FilterBankID { get; set; }
        public int? FilterBranchID { get; set; }
        public long? FilterAgentCode { get; set; }
        public int? FilterCode1 { get; set; }

        // Dropdown data
        public List<SelectListItem> Banks { get; set; } = new();
        public List<SelectListItem> Branches { get; set; } = new();
        public List<SelectListItem> Agents { get; set; } = new();

        // Report data
        public List<DailyCollectionRow> Rows { get; set; } = new();
        public bool HasSearched { get; set; }

        // Computed summary
        public decimal TotalAmount => Rows?.Count > 0 ? Rows.Sum(r => r.Amount) : 0;
        public int TotalRecords => Rows?.Count ?? 0;

        // Role context
        public string UserRole { get; set; } = string.Empty;
    }
}
