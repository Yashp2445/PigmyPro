using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IAccountRepository
    {
        Task<PagedResult<CustomerAccount>> GetAllAsync(int pageNumber, int pageSize, decimal? code1 = null, decimal? agentCode = null);
        Task<PagedResult<CustomerAccount>> GetAllByBankAsync(int bankId, int pageNumber, int pageSize, decimal? code1 = null, decimal? agentCode = null);
        Task<PagedResult<CustomerAccount>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode, int pageNumber, int pageSize, decimal? code1 = null, decimal? agentCode = null);
        Task<IEnumerable<decimal>> GetActiveAgentsFromAccountsAsync(int bankId, decimal branchCode, decimal? code1 = null);
        Task<CustomerAccount?> GetByFullCodeAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<int> AddAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null);
        Task<int> UpdateAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null);
        Task<int> DeleteAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<bool> ExistsAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<int> GetCollectionGLCodeAsync(int bankId);
        Task<bool> IsMobileNumberInUseAsync(int bankId, string mobileNo, decimal? excludeCode1 = null, decimal? excludeBranchCode = null, decimal? excludeCode2 = null);
    }
}
