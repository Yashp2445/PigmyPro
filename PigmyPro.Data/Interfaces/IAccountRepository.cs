using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IAccountRepository
    {
        Task<IEnumerable<CustomerAccount>> GetAllAsync();
        Task<IEnumerable<CustomerAccount>> GetAllByBankAsync(int bankId);
        Task<IEnumerable<CustomerAccount>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode);
        Task<CustomerAccount?> GetByFullCodeAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<int> AddAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null);
        Task<int> UpdateAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null);
        Task<int> DeleteAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<bool> ExistsAsync(int bankId, decimal code1, decimal branchCode, decimal code2);
        Task<int> GetCollectionGLCodeAsync(int bankId);
    }
}
