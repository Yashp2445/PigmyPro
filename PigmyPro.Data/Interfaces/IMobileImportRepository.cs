using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    public class ExportRow
    {
        public long SrNo { get; set; }
        public DateTime CollectionDate { get; set; }
        public int Code1 { get; set; }
        public long Code2 { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public DateTime? Entry_Time { get; set; }
    }

    public class AgentDetailsRow
    {
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string RadyToCash { get; set; } = string.Empty;
        public string Down_Load_YN { get; set; } = string.Empty;
    }

    public class ImportAccountRow
    {
        public long Code2 { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime OpnDate { get; set; }
    }

    public interface IMobileImportRepository
    {
        Task<IEnumerable<AgentDropdownItem>> GetAgentsByBranchAsync(int bankId, decimal branchCode);
        Task<IEnumerable<ExportRow>> GetPendingCollectionsAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<AgentDetailsRow?> GetAgentDetailsAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<decimal> GetAccountBalanceAsync(int bankId, decimal branchCode, decimal code2);
        Task LogExportAsync(int bankId, decimal branchCode, decimal agentCode, string userId, int totalRecords, decimal totalAmount);
        Task SetAgentDownloadFlagAsync(int bankId, decimal branchCode, decimal agentCode, string flagValue);
        
        Task<bool> ValidateBranchAsync(int bankId, decimal branchCode);
        Task<bool> HasPendingMobileTransactionsAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<AgentDetailsRow?> ValidateAgentAsync(int bankId, decimal branchCode, decimal agentCode);
        Task CommitImportAsync(int bankId, decimal branchCode, decimal agentCode, string userId, string clientIp, int totalRecords, List<ImportAccountRow> rows);
        Task<int> ReconcileDownloadAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<DateTime?> GetMaxDownloadDateAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<IEnumerable<ExportRow>> GetArchivedCollectionsAsync(int bankId, decimal branchCode, decimal agentCode, DateTime downloadDate);
    }
}
