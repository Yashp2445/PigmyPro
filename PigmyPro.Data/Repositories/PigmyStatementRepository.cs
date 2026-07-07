using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class PigmyStatementRepository : IPigmyStatementRepository
    {
        private readonly DapperContext _context;

        public PigmyStatementRepository(DapperContext context)
        {
            _context = context;
        }

        // ── Dropdown helpers ────────────────────────────────────────

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

        // ── Pigmy Account Statement Report ──────────────────────────

        public async Task<IEnumerable<PigmyStatementRow>> GetPigmyStatementAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode, int? code1)
        {
            var sql = @"
                SELECT
                    t.ID,
                    ISNULL(br.name, '') AS BranchName,
                    t.Date,
                    CAST(t.Agent AS BIGINT) AS AgentCode,
                    ISNULL(ag.NAME, '') AS AgentName,
                    t.Code1,
                    CASE t.Code1
                        WHEN 1 THEN 'Pigmy'
                        WHEN 2 THEN 'Loan'
                        WHEN 3 THEN 'Recurring'
                        ELSE 'Other'
                    END AS AccountType,
                    t.Code2,
                    t.Amount,
                    ISNULL(t.Name, '') AS Name,
                    t.EntryDate
                FROM MobilePygTrn t
                LEFT JOIN brncmast br ON br.BankID = t.BankID
                    AND CAST(t.Brnc_code AS DECIMAL(10,0)) = CAST(br.BranchID AS DECIMAL(10,0))
                LEFT JOIN agntmast ag ON ag.BankID = t.BankID
                    AND CAST(t.Agent AS DECIMAL(5,0)) = ag.code
                    AND CAST(t.Brnc_code AS DECIMAL(10,0)) = ag.brnc_code
                WHERE CAST(t.Date AS DATE) >= @DateFrom
                  AND CAST(t.Date AS DATE) <= @DateTo";

            if (bankId.HasValue)
                sql += " AND t.BankID = @BankID";
            if (branchId.HasValue)
                sql += " AND CAST(t.Brnc_code AS DECIMAL(10,0)) = @BranchID";
            if (agentCode.HasValue)
                sql += " AND CAST(t.Agent AS DECIMAL(18,0)) = @AgentCode";
            if (code1.HasValue)
                sql += " AND t.Code1 = @Code1";

            sql += " ORDER BY t.ID, t.Date, br.name, ag.NAME, t.Code2";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PigmyStatementRow>(sql, new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                BankID = bankId,
                BranchID = branchId,
                AgentCode = agentCode,
                Code1 = code1
            });
        }
    }
}
