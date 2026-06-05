using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    public class RawMapTransaction
    {
        public long AgentCode { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? EntryTime { get; set; }
        public int SequenceNo { get; set; }
    }

    public interface IMapRepository
    {
        Task<IEnumerable<RawMapTransaction>> GetMapTransactionsAsync(int bankId, DateTime date, int? branchId = null, long? agentCode = null);
        Task<IEnumerable<BranchDropdownItem>> GetBranchDropdownAsync(int bankId);
        Task<IEnumerable<AgentDropdownItem>> GetAgentDropdownAsync(int bankId, int? branchId = null);

    }
}
