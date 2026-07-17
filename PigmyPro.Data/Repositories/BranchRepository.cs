using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly DapperContext _context;

        public BranchRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Branch>> GetAllAsync()
        {
            var query = "SELECT BranchID, BankID, name, active, EntryDate FROM brncmast ORDER BY BranchID DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Branch>(query);
        }

        public async Task<IEnumerable<Branch>> GetAllByBankIdAsync(int bankId)
        {
            var query = @"SELECT BranchID, BankID, name, active, EntryDate 
                          FROM brncmast 
                          WHERE BankID = @BankID 
                          ORDER BY BranchID DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Branch>(query, new { BankID = bankId });
        }

        public async Task<Branch?> GetByIdAndBankIdAsync(int id, int bankId)
        {
            var query = @"SELECT BranchID, BankID, name, active, EntryDate 
                          FROM brncmast 
                          WHERE BranchID = @BranchID AND BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Branch>(query, new { BranchID = id, BankID = bankId });
        }

        public async Task<int> AddAsync(Branch branch)
        {
            var query = "usp_InsertBranch";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, 
                new { BankID = branch.BankID, Name = branch.Name, Active = branch.Active }, 
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<int> UpdateAsync(Branch branch)
        {
            var query = @"UPDATE brncmast SET 
                          name = @name, 
                          active = @active 
                          WHERE BranchID = @BranchID AND BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, branch);
        }

        public async Task<int> DeleteAsync(int id, int bankId)
        {
            var query = @"DELETE FROM brncmast 
                          WHERE BranchID = @BranchID AND BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { BranchID = id, BankID = bankId });
        }

        public async Task<(int AgentCount, int UserCount, int AccountCount, int TransactionCount)> GetDependentRecordCountsAsync(int bankId, int branchId)
        {
            var sql = @"
                SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID AND brnc_code = @BranchID;
                SELECT COUNT(*) FROM UserMast WHERE BankID = @BankID AND BranchID = @BranchID;
                SELECT COUNT(*) FROM acmaster WHERE BankID = @BankID AND CAST(brnc_code AS INT) = @BranchID;
                SELECT COUNT(*) FROM MobilePygTrn WHERE BankID = @BankID AND CAST(Brnc_code AS INT) = @BranchID;
            ";

            using var connection = _context.CreateConnection();
            using var multi = await connection.QueryMultipleAsync(sql, new { BankID = bankId, BranchID = branchId });

            int agentCount = await multi.ReadFirstAsync<int>();
            int userCount = await multi.ReadFirstAsync<int>();
            int accountCount = await multi.ReadFirstAsync<int>();
            int transactionCount = await multi.ReadFirstAsync<int>();

            return (agentCount, userCount, accountCount, transactionCount);
        }
    }
}