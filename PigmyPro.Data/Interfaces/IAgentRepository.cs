using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IAgentRepository
    {
        Task<PagedResult<Agent>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResult<Agent>> GetAllByBankAsync(int bankId, int pageNumber, int pageSize);
        Task<PagedResult<Agent>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode, int pageNumber, int pageSize);
        Task<Agent?> GetByCodeAsync(int bankId, decimal branchCode, decimal agentCode);
        Task<int> AddAsync(Agent agent, string? changedBy = null, string? changeIp = null);

        Task<int> UpdateAsync(
            Agent agent,
            bool resetAgent,
            string? resetRemark,
            bool blockChecked,
            string? blockRemark,
            string? changedBy = null,
            string? changeIp = null);

        Task<int> DeleteAsync(int bankId, decimal branchCode, decimal agentCode, string? changedBy = null, string? changeIp = null);

        Task<IEnumerable<Agent>> GetAgentsAsync(int bankId, decimal branchCode);
        Task<decimal> GetNextAgentCodeAsync(int bankId, decimal branchCode);
        Task<bool> IsMobileNumberInUseAsync(int bankId, string mobileNumber, decimal? excludeAgentCode = null);
    }
}