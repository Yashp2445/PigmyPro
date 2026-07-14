using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Dashboard
{
    public class BankAdminDashboardVM
    {
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public bool HasBankLogo { get; set; }
        public int BankID { get; set; }

        public int TotalBranches { get; set; }
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }

        public CollectionHeldSummary? CollectionHeld { get; set; }
        public CollectionDepositedSummary? CollectionDeposited { get; set; }

        public List<BranchWiseSummary> BranchWiseData { get; set; } = new();
        public List<TopAgentCollection> TopAgents { get; set; } = new();
        public AcMasterSummary? AcMasterData { get; set; }
        public List<AgentOverviewRow> AgentOverview { get; set; } = new();
        public List<DailyTrendPoint> DailyTrendData { get; set; } = new();
        public List<AgentOverviewRow> AtRiskAgents { get; set; } = new();

        public DateTime LastUpdated { get; set; }

        // Dashboard filters
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int? FilterBranchID { get; set; }
        public List<SelectListItem> Branches { get; set; } = new();
    }
}
