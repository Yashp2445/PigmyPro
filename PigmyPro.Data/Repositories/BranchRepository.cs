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
    }
}