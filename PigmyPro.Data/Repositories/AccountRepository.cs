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

        public async Task<PagedResult<CustomerAccount>> GetAllAsync(int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM acmaster";
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, ADDR, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster ORDER BY CODE2 DESC
                          OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery);
            var items = await connection.QueryAsync<CustomerAccount>(query, new { PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<CustomerAccount>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<CustomerAccount>> GetAllByBankAsync(int bankId, int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM acmaster WHERE BankID = @BankID";
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, ADDR, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster WHERE BankID = @BankID ORDER BY CODE2 DESC
                          OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { BankID = bankId });
            var items = await connection.QueryAsync<CustomerAccount>(query, new { BankID = bankId, PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<CustomerAccount>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<CustomerAccount>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode, int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM acmaster WHERE BankID = @BankID AND brnc_code = @brnc_code";
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, ADDR, BALANCE, 
                                 OPN_DATE, AgnCode, Mobile_No, Entry_Date 
                          FROM acmaster 
                          WHERE BankID = @BankID AND brnc_code = @brnc_code 
                          ORDER BY CODE2 DESC
                          OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { BankID = bankId, brnc_code = branchCode });
            var items = await connection.QueryAsync<CustomerAccount>(query, new { BankID = bankId, brnc_code = branchCode, PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<CustomerAccount>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CustomerAccount?> GetByFullCodeAsync(int bankId, decimal code1, decimal branchCode, decimal code2)
        {
            var query = @"SELECT BankID, CODE1, brnc_code, CODE2, name, ADDR, BALANCE, 
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
            return await connection.ExecuteAsync("sp_insertUpdateAccount",
                new
                {
                    Flag = "I",
                    account.BankID,
                    account.CODE1,
                    account.brnc_code,
                    account.CODE2,
                    account.name,
                    account.ADDR,
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
            return await connection.ExecuteAsync("sp_insertUpdateAccount",
                new
                {
                    Flag = "U",
                    account.BankID,
                    account.CODE1,
                    account.brnc_code,
                    account.CODE2,
                    account.name,
                    account.ADDR,
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
            return await connection.ExecuteAsync("sp_insertUpdateAccount",
                new { Flag = "D", BankID = bankId, CODE1 = code1, brnc_code = branchCode, CODE2 = code2 },
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

        public async Task<int> GetCollectionGLCodeAsync(int bankId)
        {
            var query = "SELECT CollectionGLCode FROM Banks WHERE BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<int>(query, new { BankID = bankId });
        }
    }
}