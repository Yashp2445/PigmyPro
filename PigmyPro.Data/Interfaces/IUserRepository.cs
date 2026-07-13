using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<PagedResult<User>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResult<User>> GetAllByBankIdAsync(int bankId, int pageNumber, int pageSize);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByIdAndBankIdAsync(int id, int bankId);
        Task<User?> GetByUsernameAsync(string username);
        Task<int> AddAsync(User user);
        Task<int> UpdateAsync(User user);
        Task<int> DeleteAsync(int id, int bankId);
        Task<(string Username, string PasswordHash)?> GetAdminCredentialsAsync(string username);
    }
}
