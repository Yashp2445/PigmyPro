using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class MapRepository : IMapRepository
    {
        private readonly DapperContext _context;

        public MapRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RawMapTransaction>> GetMapTransactionsAsync(int bankId, DateTime date, int? branchId = null, long? agentCode = null)
        {
            var branchFilter = branchId.HasValue ? " AND t.Brnc_code = @BranchId" : "";
            var agentFilter = agentCode.HasValue ? " AND t.Agent = @AgentCode" : "";

            var sql = $@"
                SELECT 
                    t.Agent AS AgentCode,
                    ISNULL(a.NAME, 'Unknown') AS AgentName,
                    ISNULL(b.name, 'Unknown') AS BranchName,
                    t.Latitude,
                    t.Longitude,
                    t.Amount,
                    t.Name AS CustomerName,
                    t.Date,
                    t.Entry_Time AS EntryTime,
                    ROW_NUMBER() OVER (ORDER BY t.EntryDate) AS SequenceNo
                FROM MobilePygTrn t
                LEFT JOIN agntmast a ON a.BankID = t.BankID
                    AND CAST(a.brnc_code AS NUMERIC(18,0)) = t.Brnc_code
                    AND CAST(a.code AS NUMERIC(18,0)) = t.Agent
                LEFT JOIN brncmast b ON b.BankID = t.BankID
                    AND CAST(b.BranchID AS NUMERIC(18,0)) = t.Brnc_code
                WHERE t.BankID = @BankId
                  AND CONVERT(date, t.Date) = @Date
                  AND t.Latitude IS NOT NULL AND t.Latitude <> ''
                  AND t.Longitude IS NOT NULL AND t.Longitude <> ''
                  {branchFilter}
                  {agentFilter}
                ORDER BY t.Entry_Time";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<RawMapTransaction>(sql, new { BankId = bankId, Date = date.Date, BranchId = branchId, AgentCode = agentCode });
        }

        public async Task<IEnumerable<BranchDropdownItem>> GetBranchDropdownAsync(int bankId)
        {
            var sql = "SELECT BranchID, name AS Name FROM brncmast WHERE BankID = @BankId AND active = 'Y' ORDER BY name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<BranchDropdownItem>(sql, new { BankId = bankId });
        }

        public async Task<IEnumerable<AgentDropdownItem>> GetAgentDropdownAsync(int bankId, int? branchId = null)
        {
            var branchFilter = branchId.HasValue ? " AND brnc_code = @BranchId" : "";
            var sql = $@"SELECT code AS Code, NAME AS Name FROM agntmast WHERE BankID = @BankId {branchFilter} ORDER BY NAME";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentDropdownItem>(sql, new { BankId = bankId, BranchId = branchId });
        }
    }
}
