using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IAgentRepository
    {
        Task<IEnumerable<Agent>> GetAllAsync();
        Task<IEnumerable<Agent>> GetAllByBankAsync(int bankId);
        Task<IEnumerable<Agent>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode);
        Task<Agent?> GetByCodeAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<int> AddAsync(Agent agent, string? changedBy = null, string? changeIp = null);
        Task<int> UpdateAsync(Agent agent, string? changedBy = null, string? changeIp = null);
        Task<int> DeleteAsync(int bankId, decimal branchCode, decimal agentCode);

        Task<IEnumerable<Agent>> GetAgentsAsync(int bankId, decimal branchCode);
    }
}
