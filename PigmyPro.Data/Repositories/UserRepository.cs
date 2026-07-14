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

        public async Task<PagedResult<User>> GetAllAsync(int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM UserMast";
            var query = @"SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date 
                  FROM UserMast 
                  ORDER BY UserID DESC
                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery);
            var items = await connection.QueryAsync<User>(query, new { PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<User>> GetAllByBankIdAsync(int bankId, int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM UserMast WHERE BankID = @BankID";
            var query = @"SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, IsActive, Entry_Date 
                          FROM UserMast WHERE BankID = @BankID ORDER BY UserID DESC
                          OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { BankID = bankId });
            var items = await connection.QueryAsync<User>(query, new { BankID = bankId, PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            var query = "SELECT COUNT(1) FROM UserMast WHERE Username = @Username";
            if (excludeUserId.HasValue)
            {
                query += " AND UserID != @ExcludeUserId";
            }
            using var connection = _context.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(query, new { Username = username, ExcludeUserId = excludeUserId });
            return count > 0;
        }

        public async Task<bool> HasPendingPasswordResetAsync(string username)
        {
            var query = "SELECT COUNT(1) FROM Change_Password WHERE ini = @Username AND Reason = 'PENDING_RESET'";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(query, new { Username = username }) > 0;
        }

        public async Task<int> CreatePasswordResetRequestAsync(string username)
        {
            var query = @"INSERT INTO Change_Password (ini, Reason, Entry_Date) 
                          VALUES (@Username, 'PENDING_RESET', GETDATE())";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { Username = username });
        }

        public async Task<IEnumerable<dynamic>> GetPendingPasswordResetsAsync(int bankId, bool isSuperAdmin)
        {
            var query = @"
                SELECT c.SRNO, c.ini AS Username, c.Entry_Date AS RequestedDate, u.name AS Name, b.Name AS BankName
                FROM Change_Password c
                INNER JOIN UserMast u ON c.ini = u.Username
                INNER JOIN Banks b ON u.BankID = b.BankID
                WHERE c.Reason = 'PENDING_RESET'";

            if (!isSuperAdmin)
            {
                query += " AND u.BankID = @BankID";
            }

            query += " ORDER BY c.Entry_Date DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<dynamic>(query, new { BankID = bankId });
        }

        public async Task<int> ApprovePasswordResetAsync(string username, string adminUsername, string ipAddress)
        {
            using var connection = _context.CreateConnection();
            var queryUpdateLog = @"
                UPDATE Change_Password 
                SET Reason = @Reason, 
                    Operator_IP = @IpAddress, 
                    Change_Date = GETDATE(), 
                    Working_Date = GETDATE()
                WHERE ini = @Username AND Reason = 'PENDING_RESET'";

            var reason = "APPROVED_RESET_BY_" + adminUsername;
            return await connection.ExecuteAsync(queryUpdateLog, new { Reason = reason, IpAddress = ipAddress, Username = username });
        }

        public async Task<bool> HasApprovedPasswordResetAsync(string username)
        {
            var query = "SELECT COUNT(1) FROM Change_Password WHERE ini = @Username AND Reason LIKE 'APPROVED_RESET_BY_%'";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(query, new { Username = username }) > 0;
        }

        public async Task<int> ConsumeApprovedPasswordResetAsync(string username)
        {
            var query = @"
                UPDATE Change_Password 
                SET Reason = 'RESET_COMPLETED',
                    Change_Date = GETDATE()
                WHERE ini = @Username AND Reason LIKE 'APPROVED_RESET_BY_%'";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { Username = username });
        }

        public async Task<int> LogPasswordChangeAsync(string username, string reason, string ipAddress)
        {
            var query = @"INSERT INTO Change_Password (ini, Reason, Operator_IP, Change_Date, Working_Date, Entry_Date) 
                          VALUES (@Username, @Reason, @IpAddress, GETDATE(), GETDATE(), GETDATE())";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { Username = username, Reason = reason, IpAddress = ipAddress });
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
