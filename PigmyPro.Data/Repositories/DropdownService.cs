using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class DropdownService : IDropdownService
    {
        private readonly DapperContext _context;

        public DropdownService(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BankDropdownItem>> GetBankDropdownAsync()
        {
            var sql = "SELECT BankID, Name FROM Banks WHERE ActiveYN = 1 ORDER BY Name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<BankDropdownItem>(sql);
        }

        public async Task<IEnumerable<BranchDropdownItem>> GetBranchDropdownAsync(int bankId)
        {
            var sql = "SELECT BranchID, name AS Name FROM brncmast WHERE BankID = @BankID AND active = 'Y' ORDER BY name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<BranchDropdownItem>(sql, new { BankID = bankId });
        }

        public async Task<IEnumerable<AgentDropdownItem>> GetAgentDropdownAsync(int bankId, int branchId)
        {
            var sql = "SELECT code AS Code, NAME AS Name FROM agntmast WHERE BankID = @BankID AND brnc_code = @BranchID AND Block = 0 ORDER BY NAME";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentDropdownItem>(sql, new { BankID = bankId, BranchID = branchId });
        }

        public async Task<string> GetBankNameAsync(int bankId)
        {
            var sql = "SELECT Name FROM Banks WHERE BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { BankID = bankId }) ?? "";
        }
    }
}
