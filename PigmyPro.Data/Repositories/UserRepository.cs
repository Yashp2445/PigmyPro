using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _context;

        public UserRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var query = @"SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date 
                  FROM UserMast 
                  ORDER BY UserID DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<User>(query);
        }

        public async Task<IEnumerable<User>> GetAllByBankIdAsync(int bankId)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date FROM UserMast WHERE BankID = @BankID ORDER BY UserID DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<User>(query, new { BankID = bankId });
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date FROM UserMast WHERE UserID = @UserID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = id });
        }

        public async Task<User?> GetByIdAndBankIdAsync(int id, int bankId)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date FROM UserMast WHERE UserID = @UserID AND BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = id, BankID = bankId });
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date FROM UserMast WHERE Username = @Username";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { Username = username });
        }

        public async Task<int> AddAsync(User user)
        {
            var query = @"INSERT INTO UserMast (BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive) 
                        VALUES (@BankID, @BranchID, @Username, @PasswordHash, @Role, @Code, @Name, @IsActive)";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, user);
        }

        public async Task<int> UpdateAsync(User user)
        {
            var query = @"UPDATE UserMast SET 
                        BranchID = @BranchID, 
                        Username = @Username, 
                        PasswordHash = @PasswordHash, 
                        Role = @Role, 
                        code = @Code, 
                        name = @Name, 
                        IsActive = @IsActive 
                        WHERE UserID = @UserID AND BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, user);
        }

        public async Task<int> DeleteAsync(int id, int bankId)
        {
            var query = "DELETE FROM UserMast WHERE UserID = @UserID AND BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { UserID = id, BankID = bankId });
        }

        public async Task<(string Username, string PasswordHash)?> GetAdminCredentialsAsync(string username)
        {
            var query = "SELECT Username, Password FROM AdminCredentials WHERE Username = @Username";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync(query, new { Username = username });

            if (result == null)
                return null;

            return (result.Username, result.Password);
        }
    }
}
