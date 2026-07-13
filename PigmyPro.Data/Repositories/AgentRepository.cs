using System.Collections.Generic;
using System.Data;
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

        public async Task<PagedResult<Agent>> GetAllByBankAndBranchAsync(int bankId, decimal branchCode, int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID AND brnc_code = @brnc_code";
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  WHERE BankID = @BankID AND brnc_code = @brnc_code 
                  ORDER BY code DESC
                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { BankID = bankId, brnc_code = branchCode });
            var items = await connection.QueryAsync<Agent>(query, new { BankID = bankId, brnc_code = branchCode, PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<Agent>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<Agent>> GetAllByBankAsync(int bankId, int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID";
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  WHERE BankID = @BankID 
                  ORDER BY code DESC
                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { BankID = bankId });
            var items = await connection.QueryAsync<Agent>(query, new { BankID = bankId, PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<Agent>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<Agent>> GetAllAsync(int pageNumber, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM agntmast";
            var query = @"SELECT BankID, brnc_code, code, NAME, MobileNo, Block, 
                         NoOfHolidays, RadyToCash, EntryDate 
                  FROM agntmast 
                  ORDER BY BankID, brnc_code, code DESC
                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";
            using var connection = _context.CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery);
            var items = await connection.QueryAsync<Agent>(query, new { PageNumber = pageNumber, PageSize = pageSize });
            
            return new PagedResult<Agent>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

        // ─────────────────────────────────────────────────────────
        // Insert — Block derived from agent.Block (bool -> 'Y'/'N')
        // since the SP now takes @BlockFlag consistently for both
        // Insert and Update.
        // ─────────────────────────────────────────────────────────
        public async Task<int> AddAsync(Agent agent, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "I");
            p.Add("BankID", agent.BankID);
            p.Add("brnc_code", agent.brnc_code);
            p.Add("code", agent.code);
            p.Add("NAME", agent.NAME);
            p.Add("MobileNo", agent.MobileNo);
            p.Add("NoOfHolidays", agent.NoOfHolidays);
            p.Add("RadyToCash", agent.RadyToCash);
            p.Add("OpnBy", changedBy);
            p.Add("OpnIP", changeIp);
            p.Add("BlockFlag", (agent.Block ?? false) ? "Y" : "N");
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("SP_InsertUpdateAgentMast", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        // ─────────────────────────────────────────────────────────
        // Update — base fields always applied; Reset and Block/Unblock
        // are conditionally applied inside the SP based on the flags
        // passed here (driven by the Reset/Block checkboxes on Edit).
        // ─────────────────────────────────────────────────────────
        public async Task<int> UpdateAsync(
            Agent agent,
            bool resetAgent,
            string? resetRemark,
            bool blockChecked,
            string? blockRemark,
            string? changedBy = null,
            string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "U");
            p.Add("BankID", agent.BankID);
            p.Add("brnc_code", agent.brnc_code);
            p.Add("code", agent.code);
            p.Add("NAME", agent.NAME);
            p.Add("MobileNo", agent.MobileNo);
            p.Add("NoOfHolidays", agent.NoOfHolidays);
            p.Add("RadyToCash", agent.RadyToCash);
            p.Add("ChangeBy", changedBy);
            p.Add("ChangeIP", changeIp);
            p.Add("Reset", resetAgent ? "Y" : "N");
            p.Add("ResetRemark", resetRemark ?? string.Empty);
            p.Add("BlockFlag", blockChecked ? "Y" : "N");
            p.Add("BlockRemark", blockRemark ?? string.Empty);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("SP_InsertUpdateAgentMast", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> DeleteAsync(int bankId, decimal branchCode, decimal agentCode, string? changedBy = null, string? changeIp = null)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "D");
            p.Add("BankID", bankId);
            p.Add("brnc_code", branchCode);
            p.Add("code", agentCode);
            p.Add("ChangeBy", changedBy);
            p.Add("ChangeIP", changeIp);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("SP_InsertUpdateAgentMast", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
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

        public async Task<decimal> GetNextAgentCodeAsync(int bankId, decimal branchCode)
        {
            var query = @"SELECT ISNULL(MAX(code), 0) + 1
                          FROM agntmast
                          WHERE BankID = @BankID AND brnc_code = @brnc_code";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(query,
                new { BankID = bankId, brnc_code = branchCode });
        }

        private static void ThrowIfSpFailed(string? msg)
        {
            if (!string.IsNullOrEmpty(msg) && msg != "1")
                throw new System.Exception($"SP_InsertUpdateAgentMast failed: {msg}");
        }
    }
}