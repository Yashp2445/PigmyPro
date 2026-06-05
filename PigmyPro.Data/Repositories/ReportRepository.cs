using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly DapperContext _context;

        public ReportRepository(DapperContext context)
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

        // ── Daily Collection Report ─────────────────────────────────

        public async Task<IEnumerable<DailyCollectionRow>> GetDailyCollectionAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode, int? code1)
        {
            var sql = @"
                SELECT
                    t.Date,
                    ISNULL(bk.Name, '') AS BankName,
                    ISNULL(br.name, '') AS BranchName,
                    ISNULL(ag.NAME, '') AS AgentName,
                    t.Code2,
                    ISNULL(ac.name, '') AS CustomerName,
                    t.Code1,
                    CASE t.Code1
                        WHEN 1 THEN 'Pigmy'
                        WHEN 2 THEN 'Loan'
                        WHEN 3 THEN 'Recurring'
                        ELSE 'Other'
                    END AS AccountType,
                    t.Amount
                FROM MobilePygTrn_ALL t
                LEFT JOIN Banks bk ON bk.BankID = t.BankID
                LEFT JOIN brncmast br ON br.BankID = t.BankID
                    AND CAST(t.Brnc_code AS DECIMAL(10,0)) = CAST(br.BranchID AS DECIMAL(10,0))
                LEFT JOIN agntmast ag ON ag.BankID = t.BankID
                    AND CAST(t.Agent AS DECIMAL(5,0)) = ag.code
                    AND CAST(t.Brnc_code AS DECIMAL(10,0)) = ag.brnc_code
                LEFT JOIN acmaster ac ON ac.BankID = t.BankID
                    AND CAST(t.Code1 AS DECIMAL(5,0)) = ac.CODE1
                    AND CAST(t.Brnc_code AS DECIMAL(10,0)) = ac.brnc_code
                    AND CAST(t.Code2 AS DECIMAL(18,0)) = ac.CODE2
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

            sql += " ORDER BY t.Date, br.name, ag.NAME, t.Code2";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DailyCollectionRow>(sql, new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                BankID = bankId,
                BranchID = branchId,
                AgentCode = agentCode,
                Code1 = code1
            });
        }

        // ── Agent Summary Report ────────────────────────────────────

        public async Task<IEnumerable<AgentSummaryRow>> GetAgentSummaryAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId, long? agentCode)
        {
            var sql = @"
                SELECT
                    CAST(m.Agent AS DECIMAL(18,0)) AS AgentCode,
                    ISNULL(a.NAME, 'Unknown') AS AgentName,
                    ISNULL(br.name, 'Unknown') AS BranchName,
                    COUNT(DISTINCT m.Code2) AS TotalAccounts,
                    SUM(m.Amount) AS TotalAmount,
                    CASE WHEN NULLIF(COUNT(DISTINCT m.Code2), 0) IS NULL THEN 0
                         ELSE SUM(m.Amount) / COUNT(DISTINCT m.Code2)
                    END AS AverageAmount
                FROM MobilePygTrn_ALL m
                LEFT JOIN agntmast a ON a.BankID = m.BankID
                    AND CAST(m.Agent AS DECIMAL(5,0)) = a.code
                    AND CAST(m.Brnc_code AS DECIMAL(10,0)) = a.brnc_code
                LEFT JOIN brncmast br ON br.BankID = m.BankID
                    AND CAST(m.Brnc_code AS DECIMAL(10,0)) = CAST(br.BranchID AS DECIMAL(10,0))
                WHERE CAST(m.Date AS DATE) >= @DateFrom
                  AND CAST(m.Date AS DATE) <= @DateTo";

            if (bankId.HasValue)
                sql += " AND m.BankID = @BankID";
            if (branchId.HasValue)
                sql += " AND CAST(m.Brnc_code AS DECIMAL(10,0)) = @BranchID";
            if (agentCode.HasValue)
                sql += " AND CAST(m.Agent AS DECIMAL(18,0)) = @AgentCode";

            sql += @"
                GROUP BY CAST(m.Agent AS DECIMAL(18,0)), a.NAME, br.name
                ORDER BY SUM(m.Amount) DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentSummaryRow>(sql, new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                BankID = bankId,
                BranchID = branchId,
                AgentCode = agentCode
            });
        }


    }
}
