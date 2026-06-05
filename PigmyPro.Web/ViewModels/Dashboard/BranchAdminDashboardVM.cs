using System;
using System.Collections.Generic;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Dashboard
{
    public class BranchAdminDashboardVM
    {
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }
        public int AccountsCollectedToday { get; set; }

        public List<AgentCollectionRow> AgentData { get; set; } = new();
        public List<AgentOverviewRow> AgentOverview { get; set; } = new();

        public DateTime LastUpdated { get; set; }

        // Dashboard filters
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
