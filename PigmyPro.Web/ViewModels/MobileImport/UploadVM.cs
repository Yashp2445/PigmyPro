// UploadVM.cs
using System;
using System.Collections.Generic;

namespace PigmyPro.Web.ViewModels.MobileImport
{
    public class UploadVM
    {
        public bool HasParsedData { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BranchCode { get; set; }
        public decimal AgentCode { get; set; }
        public string? AgentName { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string ParsedRowsJson { get; set; } = string.Empty;

        public int? SelectedBranchCode { get; set; }
        public decimal? SelectedAgentCode { get; set; }
        
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Branches { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Agents { get; set; } = new();

        public List<ImportAccountRowVM> ParsedRows { get; set; } = new();
    }

    public class ImportAccountRowVM
    {
        public long Code2 { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime OpnDate { get; set; }
        public decimal Amount { get; set; }
    }
}