using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IBranchRepository
    {
        Task<IEnumerable<Branch>> GetAllAsync();
        Task<IEnumerable<Branch>> GetAllByBankIdAsync(int bankId);
        Task<IEnumerable<Branch>> GetActiveByBankIdAsync(int bankId);
        Task<Branch?> GetByIdAndBankIdAsync(int id, int bankId);
        Task<int> AddAsync(Branch branch);
        Task<int> UpdateAsync(Branch branch);
        Task<int> DeleteAsync(int id, int bankId);
        Task<(int AgentCount, int UserCount, int AccountCount, int TransactionCount)> GetDependentRecordCountsAsync(int bankId, int branchId);
    }
}