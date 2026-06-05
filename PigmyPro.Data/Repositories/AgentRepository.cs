using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Repositories
{
    public class AgentRepository : IAgentRepository
    {
        private readonly DapperContext _context;

        public AgentRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Agent>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode)
        {
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  WHERE BankID = @BankID AND brnc_code = @brnc_code 
                  ORDER BY code DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Agent>(query, new { BankID = bankId, brnc_code = branchCode });
        }

        
        public async Task<IEnumerable<Agent>> GetAllByBankAsync(int bankId)
        {
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  WHERE BankID = @BankID 
                  ORDER BY code DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Agent>(query, new { BankID = bankId });
        }

       
        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  ORDER BY BankID, brnc_code, code DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Agent>(query);
        }

        public async Task<Agent?> GetByCodeAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  WHERE BankID = @BankID AND brnc_code = @brnc_code AND code = @code";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Agent>(query,
                new { BankID = bankId, brnc_code = branchCode, code = agentCode });
        }

        public async Task<int> AddAsync(Agent agent, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync("usp_InsertAgent",
                new
                {
                    agent.BankID,
                    agent.brnc_code,
                    agent.code,
                    agent.NAME,
                    agent.MobileNo,
                    agent.Block,
                    agent.NoOfHolidays,
                    agent.RadyToCash,
                    ChangeBy = changedBy,
                    ChangeIP = changeIp
                },
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<int> UpdateAsync(Agent agent, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync("usp_UpdateAgent",
                new
                {
                    agent.BankID,
                    agent.brnc_code,
                    agent.code,
                    agent.NAME,
                    agent.MobileNo,
                    agent.Block,
                    agent.NoOfHolidays,
                    agent.RadyToCash,
                    ChangeBy = changedBy,
                    ChangeIP = changeIp
                },
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<int> DeleteAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var query = "DELETE FROM agntmast WHERE BankID = @BankID AND brnc_code = @brnc_code AND code = @code";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { BankID = bankId, brnc_code = branchCode, code = agentCode });
        }

        public async Task<IEnumerable<Agent>> GetAgentsAsync(int bankId, decimal branchCode)
        {
            var query = @"SELECT code, NAME 
                      FROM agntmast
                      WHERE BankID = @BankID 
                        AND brnc_code = @brnc_code
                      ORDER BY NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Agent>(query,
                new { BankID = bankId, brnc_code = branchCode });
        }
    }
}
