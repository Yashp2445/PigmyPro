using System.Collections.Generic;
using System.Data;
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
            var query = @"SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, MobileNo, IsActive, Entry_Date 
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
            var query = @"SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, MobileNo, IsActive, Entry_Date 
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
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, MobileNo, IsActive, Entry_Date FROM UserMast WHERE UserID = @UserID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = id });
        }

        public async Task<User?> GetByIdAndBankIdAsync(int id, int bankId)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, MobileNo, IsActive, Entry_Date FROM UserMast WHERE UserID = @UserID AND BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = id, BankID = bankId });
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var query = "SELECT UserID, BankID, BranchID, Username, PasswordHash, Role, code, name, MobileNo, IsActive, Entry_Date FROM UserMast WHERE Username = @Username";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { Username = username });
        }

        public async Task<int> AddAsync(User user)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "I");
            p.Add("BankID", user.BankID);
            p.Add("BranchID", user.BranchID);
            p.Add("Username", user.Username);
            p.Add("PasswordHash", user.PasswordHash);
            p.Add("Role", user.Role);
            p.Add("Code", user.Code);
            p.Add("Name", user.Name);
            p.Add("MobileNo", user.MobileNo);
            p.Add("IsActive", user.IsActive);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateUser", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> UpdateAsync(User user)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "U");
            p.Add("UserID", user.UserID);
            p.Add("BankID", user.BankID);
            p.Add("BranchID", user.BranchID);
            p.Add("Username", user.Username);
            p.Add("PasswordHash", user.PasswordHash);
            p.Add("Role", user.Role);
            p.Add("Code", user.Code);
            p.Add("Name", user.Name);
            p.Add("MobileNo", user.MobileNo);
            p.Add("IsActive", user.IsActive);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateUser", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> DeleteAsync(int id, int bankId)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "D");
            p.Add("UserID", id);
            p.Add("BankID", bankId);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateUser", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        private static void ThrowIfSpFailed(string? msg)
        {
            if (!string.IsNullOrEmpty(msg) && msg != "1")
                throw new System.Exception($"sp_insertUpdateUser failed: {msg}");
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

        public async Task<(string Msg, string OTP)> GenerateResetOtpAsync(string username)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Username", username);
            parameters.Add("@Msg", dbType: DbType.String, direction: ParameterDirection.Output, size: 80);
            parameters.Add("@OTP", dbType: DbType.String, direction: ParameterDirection.Output, size: 80);

            await connection.ExecuteAsync("SP_GenerateResetOTP_User", parameters, commandType: CommandType.StoredProcedure);

            string msg = parameters.Get<string>("@Msg");
            string otp = parameters.Get<string>("@OTP");

            return (msg, otp);
        }

        public async Task<(string Msg, bool Valid)> VerifyResetOtpAsync(string username, string enteredOtp)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Username", username);
            parameters.Add("@EnteredOTP", enteredOtp);
            parameters.Add("@Msg", dbType: DbType.String, direction: ParameterDirection.Output, size: 80);
            parameters.Add("@Valid", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("SP_VerifyResetOTP_User", parameters, commandType: CommandType.StoredProcedure);

            string msg = parameters.Get<string>("@Msg");
            bool valid = parameters.Get<bool>("@Valid");

            return (msg, valid);
        }
    }
}
