using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Interfaces
{
    public class ReuploadAccountRow
    {
        public long Code2 { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime OpnDate { get; set; }
        public decimal Amount { get; set; }
        public string? MobileNumber { get; set; }
    }

    public interface IReuploadRepository
    {
        Task<IEnumerable<AgentDropdownItem>> GetAgentsByBranchAsync(int bankId, decimal branchCode);
        Task<AgentDetailsRow?> GetAgentDetailsAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<bool> ValidateBranchAsync(int bankId, decimal branchCode);
        Task<bool> HasPendingMobileTransactionsAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<AgentDetailsRow?> ValidateAgentAsync(int bankId, decimal branchCode, decimal agentCode);
        Task SetAgentStateForReuploadAsync(int bankId, decimal branchCode, decimal agentCode);
        Task CommitImportAsync(int bankId, decimal branchCode, decimal agentCode, string userId, string clientIp, int totalRecords, List<ReuploadAccountRow> rows);
    }
}
