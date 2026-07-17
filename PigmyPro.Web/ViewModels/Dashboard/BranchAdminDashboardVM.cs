using System;
using System.Collections.Generic;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Dashboard
{
    public class BranchAdminDashboardVM
    {
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public bool HasBankLogo { get; set; }
        public int BankID { get; set; }

        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }
        public int AccountsCollectedToday { get; set; }

        public CollectionHeldSummary? CollectionHeld { get; set; }
        public CollectionDepositedSummary? CollectionDeposited { get; set; }

        public List<AgentCollectionRow> AgentData { get; set; } = new();
        public List<AgentOverviewRow> AgentOverview { get; set; } = new();
        public List<DailyTrendPoint> DailyTrendData { get; set; } = new();
        public List<AgentOverviewRow> AtRiskAgents { get; set; } = new();
        public List<AgentUploadReadyRow> AgentsReadyForUpload { get; set; } = new();

        public DateTime LastUpdated { get; set; }

        // Dashboard filters
    }
}
