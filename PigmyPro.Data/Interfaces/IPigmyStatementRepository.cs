using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    // --- DTO for Pigmy Account Statement Report ---

    public class PigmyStatementRow
    {
        public int ID { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int Code1 { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public long Code2 { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
    }

    public interface IPigmyStatementRepository : IDropdownService
    {
        // Main report query
        Task<IEnumerable<PigmyStatementRow>> GetPigmyStatementAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode, int? code1);
    }
}
