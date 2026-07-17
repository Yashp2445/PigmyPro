using System;
using System.Collections.Generic;
using System.Text;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IBankRepository
    {
        Task<IEnumerable<Bank>> GetAllAsync();
        Task<Bank?> GetByIdAsync(int id);
        Task<int> AddAsync(Bank bank);
        Task<int> UpdateAsync(Bank bank);
        Task<int> DeleteAsync(int id);
        Task<int> GetDependentBranchCountAsync(int bankId);
    }
}
