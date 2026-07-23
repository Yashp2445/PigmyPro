using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
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

        public async Task<IEnumerable<Branch>> GetActiveByBankIdAsync(int bankId)
        {
            var query = @"SELECT BranchID, BankID, name, active, EntryDate 
                          FROM brncmast 
                          WHERE BankID = @BankID AND active = 'Y' 
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
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "I");
            p.Add("BankID", branch.BankID);
            p.Add("name", branch.Name);
            p.Add("active", branch.Active);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBranch", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> UpdateAsync(Branch branch)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "U");
            p.Add("BranchID", branch.BranchID);
            p.Add("BankID", branch.BankID);
            p.Add("name", branch.Name);
            p.Add("active", branch.Active);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBranch", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> DeleteAsync(int id, int bankId)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "D");
            p.Add("BranchID", id);
            p.Add("BankID", bankId);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBranch", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        private static void ThrowIfSpFailed(string? msg)
        {
            if (!string.IsNullOrEmpty(msg) && msg != "1")
                throw new System.Exception($"sp_insertUpdateBranch failed: {msg}");
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