using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class ReportRepository : DropdownService, IReportRepository
    {
        private readonly DapperContext _context;

        public ReportRepository(DapperContext context) : base(context)
        {
            _context = context;
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
                        -- See PigmyPro.Domain.Enums.AccountType
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


        // ── Reconciliation Report ───────────────────────────────────

        public async Task<IEnumerable<ReconciliationRow>> GetReconciliationReportAsync(
            DateTime dateFrom, DateTime dateTo,
            int? bankId, int? branchId)
        {
            var sql = @"
                WITH MobileData AS (
                    SELECT BankID, Brnc_code, Agent, CAST(Date AS DATE) as Date, SUM(Amount) as MobileAmount
                    FROM MobilePygTrn
                    WHERE CAST(Date AS DATE) >= @DateFrom AND CAST(Date AS DATE) <= @DateTo
                    GROUP BY BankID, Brnc_code, Agent, CAST(Date AS DATE)
                ),
                SystemData AS (
                    SELECT BankID, Brnc_code, Agent, CAST(Date AS DATE) as Date, SUM(Amount) as SystemAmount
                    FROM MobilePygTrn_ALL
                    WHERE CAST(Date AS DATE) >= @DateFrom AND CAST(Date AS DATE) <= @DateTo
                    GROUP BY BankID, Brnc_code, Agent, CAST(Date AS DATE)
                )
                SELECT 
                    ISNULL(m.Date, s.Date) AS Date,
                    ISNULL(br.name, '') AS BranchName,
                    ISNULL(ag.NAME, '') AS AgentName,
                    ISNULL(m.MobileAmount, 0) AS MobileAmount,
                    ISNULL(s.SystemAmount, 0) AS SystemAmount
                FROM MobileData m
                FULL OUTER JOIN SystemData s 
                    ON m.BankID = s.BankID AND m.Brnc_code = s.Brnc_code 
                    AND m.Agent = s.Agent AND m.Date = s.Date
                LEFT JOIN brncmast br 
                    ON br.BankID = ISNULL(m.BankID, s.BankID) 
                    AND CAST(br.BranchID AS DECIMAL(10,0)) = CAST(ISNULL(m.Brnc_code, s.Brnc_code) AS DECIMAL(10,0))
                LEFT JOIN agntmast ag 
                    ON ag.BankID = ISNULL(m.BankID, s.BankID) 
                    AND ag.brnc_code = ISNULL(m.Brnc_code, s.Brnc_code)
                    AND ag.code = CAST(ISNULL(m.Agent, s.Agent) AS DECIMAL(5,0))
                WHERE (@BankID IS NULL OR ISNULL(m.BankID, s.BankID) = @BankID)
                  AND (@BranchID IS NULL OR CAST(ISNULL(m.Brnc_code, s.Brnc_code) AS DECIMAL(10,0)) = @BranchID)
                ORDER BY ISNULL(m.Date, s.Date) DESC, br.name, ag.NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<ReconciliationRow>(sql, new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                BankID = bankId,
                BranchID = branchId
            });
        }
    }
}
