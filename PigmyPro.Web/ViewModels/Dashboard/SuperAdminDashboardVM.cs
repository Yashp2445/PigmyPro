using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Web.ViewModels.Dashboard
{
    public class SuperAdminDashboardVM
    {
        public int TotalBanks { get; set; }
        public int TotalBranches { get; set; }
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }

        public List<BankWiseSummary> BankWiseData { get; set; } = new();
        public List<AccountTypeCount> AccountTypeData { get; set; } = new();

        public DateTime LastUpdated { get; set; }

        // Dashboard filters
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int? FilterBankID { get; set; }
        public List<SelectListItem> Banks { get; set; } = new();
    }
}
