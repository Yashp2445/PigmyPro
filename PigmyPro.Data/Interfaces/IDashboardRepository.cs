using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    public class SuperAdminSummary
    {
        public int TotalBanks { get; set; }
        public int TotalBranches { get; set; }
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }
    }

    public class BankWiseSummary
    {
        public int BankID { get; set; }
        public string BankName { get; set; } = string.Empty;
        public int BranchCount { get; set; }
        public int AgentCount { get; set; }
        public int AccountCount { get; set; }
        public decimal TodayCollection { get; set; }
    }

    public class AccountTypeCount
    {
        public string AccountType { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AcMasterSummary
    {
        public int TotalAccounts { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AvgBalance { get; set; }
        public int NewAccountsInPeriod { get; set; }
    }

    public class BankAdminSummary
    {
        public int TotalBranches { get; set; }
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }
    }

    public class BranchWiseSummary
    {
        public int BranchID { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int AgentCount { get; set; }
        public int AccountCount { get; set; }
        public decimal TodayCollection { get; set; }
    }

    public class TopAgentCollection
    {
        public string AgentName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal TodayAmount { get; set; }
        public int AccountsCollected { get; set; }
    }

    public class BranchAdminSummary
    {
        public int TotalAgents { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TodayCollection { get; set; }
        public int AccountsCollectedToday { get; set; }
    }

    public class AgentCollectionRow
    {
        public string AgentName { get; set; } = string.Empty;
        public decimal TodayAmount { get; set; }
        public int AccountsCollected { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class AgentOverviewRow
    {
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public decimal TodayAmount { get; set; }
        public int AccountsCollected { get; set; }
        public int? DaysInactive { get; set; }
    }

    public class DailyTrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public interface IDashboardRepository
    {
        // SuperAdmin
        Task<SuperAdminSummary> GetSuperAdminSummaryAsync(DateTime dateFrom, DateTime dateTo, int? filterBankId = null);
        Task<IEnumerable<BankWiseSummary>> GetBankWiseSummaryAsync(DateTime dateFrom, DateTime dateTo, int? filterBankId = null);
        Task<IEnumerable<AccountTypeCount>> GetAccountTypeDistributionAsync();

        // BankAdmin
        Task<BankAdminSummary> GetBankAdminSummaryAsync(int bankId, DateTime dateFrom, DateTime dateTo);
        Task<IEnumerable<BranchWiseSummary>> GetBranchWiseSummaryAsync(int bankId, DateTime dateFrom, DateTime dateTo, int? filterBranchId = null);
        Task<IEnumerable<TopAgentCollection>> GetTopAgentCollectionsAsync(int bankId, int top, DateTime dateFrom, DateTime dateTo, int? filterBranchId = null);
        Task<IEnumerable<AccountTypeCount>> GetAccountTypeDistributionByBankAsync(int bankId);
        Task<IEnumerable<AgentOverviewRow>> GetAgentOverviewAsync(int bankId, DateTime dateFrom, DateTime dateTo, int? filterBranchId = null);

        // BranchAdmin
        Task<BranchAdminSummary> GetBranchAdminSummaryAsync(int bankId, int branchId, DateTime dateFrom, DateTime dateTo);
        Task<IEnumerable<AgentCollectionRow>> GetAgentCollectionsByBranchAsync(int bankId, int branchId, DateTime dateFrom, DateTime dateTo);

        // Trend
        Task<IEnumerable<DailyTrendPoint>> GetDailyCollectionTrendAsync(DateTime dateFrom, DateTime dateTo, int? bankId = null, int? branchId = null);

        // AcMaster Summary
        Task<AcMasterSummary> GetAcMasterSummaryAsync(DateTime dateFrom, DateTime dateTo, int? bankId = null, int? branchId = null);

        // Dropdown helpers for dashboard filters
        Task<IEnumerable<BankDropdownItem>> GetBankDropdownAsync();
        Task<IEnumerable<BranchDropdownItem>> GetBranchDropdownAsync(int bankId);
    }
}
