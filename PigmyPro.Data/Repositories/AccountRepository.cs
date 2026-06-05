using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly DapperContext _context;

        public AccountRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerAccount>> GetAllAsync()
        {
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster ORDER BY CODE2 DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CustomerAccount>(query);
        }

        public async Task<IEnumerable<CustomerAccount>> GetAllByBankAsync(int bankId)
        {
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster WHERE BankID = @BankID ORDER BY CODE2 DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CustomerAccount>(query, new { BankID = bankId });
        }

        public async Task<IEnumerable<CustomerAccount>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode)
        {
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster 
                          WHERE BankID = @BankID AND brnc_code = @brnc_code 
                          ORDER BY CODE2 DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CustomerAccount>(query, new { BankID = bankId, brnc_code = branchCode });
        }

        public async Task<CustomerAccount?> GetByFullCodeAsync(int bankId, decimal code1, decimal branchCode, decimal code2)
        {
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster 
                          WHERE BankID = @BankID AND CODE1 = @CODE1 
                            AND brnc_code = @brnc_code AND CODE2 = @CODE2";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<CustomerAccount>(query,
                new { BankID = bankId, CODE1 = code1, brnc_code = branchCode, CODE2 = code2 });
        }

        public async Task<int> AddAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync("usp_InsertAccount",
                new
                {
                    account.BankID,
                    account.CODE1,
                    account.brnc_code,
                    account.CODE2,
                    account.name,
                    BALANCE = account.BALANCE ?? 0,
                    OPN_DATE = account.OPN_DATE,
                    AgnCode = account.AgnCode ?? 0,
                    account.Mobile_No,
                    ChangeBy = changedBy,
                    ChangeIP = changeIp
                },
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<int> UpdateAsync(CustomerAccount account, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync("usp_UpdateAccount",
                new
                {
                    account.BankID,
                    account.CODE1,
                    account.brnc_code,
                    account.CODE2,
                    account.name,
                    BALANCE = account.BALANCE ?? 0,
                    OPN_DATE = account.OPN_DATE,
                    AgnCode = account.AgnCode ?? 0,
                    account.Mobile_No,
                    ChangeBy = changedBy,
                    ChangeIP = changeIp
                },
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<int> DeleteAsync(int bankId, decimal code1, decimal branchCode, decimal code2)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync("usp_DeleteAccount",
                new { BankID = bankId, CODE1 = code1, brnc_code = branchCode, CODE2 = code2 },
                commandType: System.Data.CommandType.StoredProcedure);
        }
        public async Task<bool> ExistsAsync(int bankId, decimal code1, decimal branchCode, decimal code2)
        {
            var query = @"SELECT COUNT(1) FROM acmaster 
                  WHERE BankID = @BankID AND CODE1 = @CODE1 
                    AND brnc_code = @brnc_code AND CODE2 = @CODE2";
            using var connection = _context.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(query,
                new { BankID = bankId, CODE1 = code1, brnc_code = branchCode, CODE2 = code2 });
            return count > 0;
        }

       
    }
}