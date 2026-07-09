using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    // --- DTOs for Daily Collection Report ---

    public class DailyCollectionRow
    {
        public DateTime Date { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public long Code2 { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Code1 { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // --- DTOs for Agent Summary Report ---

    public class AgentSummaryRow
    {
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = "Unknown";
        public string BranchName { get; set; } = "Unknown";
        public int TotalAccounts { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }



    // --- Dropdown helpers ---

    public class BankDropdownItem
    {
        public int BankID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class BranchDropdownItem
    {
        public int BranchID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AgentDropdownItem
    {
        public long Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IReportRepository : IDropdownService
    {
        // ── Daily Collection Report ─────────────────────────────────
        Task<IEnumerable<DailyCollectionRow>> GetDailyCollectionAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode, int? code1);

        // Agent Summary Report
        Task<IEnumerable<AgentSummaryRow>> GetAgentSummaryAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode);

        Task<IEnumerable<ReconciliationRow>> GetReconciliationReportAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId);
    }

    public class ReconciliationRow
    {
        public DateTime Date { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public decimal MobileAmount { get; set; }
        public decimal SystemAmount { get; set; }
        public decimal Difference => MobileAmount - SystemAmount;
        public string Status => Difference == 0 ? "Matched" : "Discrepancy";
    }
}
