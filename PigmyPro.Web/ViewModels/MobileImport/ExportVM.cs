// ExportVM.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PigmyPro.Web.ViewModels.MobileImport
{
    public class ExportVM
    {
        public int? BranchCode { get; set; }
        public decimal? AgentCode { get; set; }
        public bool HasSearched { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }

        public List<SelectListItem> Branches { get; set; } = new();
        public List<SelectListItem> Agents { get; set; } = new();
        public List<ExportRowVM> Rows { get; set; } = new();
    }

    public class ExportRowVM
    {
        public long SrNo { get; set; }
        public DateTime CollectionDate { get; set; }
        public int Code1 { get; set; }
        public long Code2 { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}