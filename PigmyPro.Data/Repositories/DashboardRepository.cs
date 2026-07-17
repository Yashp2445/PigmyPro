using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly DapperContext _context;

        public DashboardRepository(DapperContext context)
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

     
        public async Task<SuperAdminSummary> GetSuperAdminSummaryAsync(int? filterBankId = null)
        {
            var bankFilter = filterBankId.HasValue ? " AND BankID = @FilterBankID" : "";
            var bankFilterBranch = filterBankId.HasValue ? " AND BankID = @FilterBankID" : "";

            var sql = $@"
                SELECT
                    (SELECT COUNT(*) FROM Banks WHERE ActiveYN = 1{(filterBankId.HasValue ? " AND BankID = @FilterBankID" : "")}) AS TotalBanks,
                    (SELECT COUNT(*) FROM brncmast WHERE active = 'Y'{bankFilterBranch}) AS TotalBranches,
                    (SELECT COUNT(*) FROM agntmast WHERE Block = 0{bankFilter}) AS TotalAgents,
                    (SELECT COUNT(*) FROM acmaster WHERE 1=1{bankFilter}) AS TotalAccounts,
                    (SELECT ISNULL(SUM(Amount), 0) FROM MobilePygTrn WHERE 1=1 {bankFilter}) AS TodayCollection";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<SuperAdminSummary>(sql, new
            {
                FilterBankID = filterBankId
            });
        }

        public async Task<IEnumerable<BankWiseSummary>> GetBankWiseSummaryAsync(int? filterBankId = null)
        {
            var bankWhere = filterBankId.HasValue ? " AND b.BankID = @FilterBankID" : "";

            var sql = $@"
                SELECT 
                    b.BankID,
                    b.Name AS BankName,
                    ISNULL(br.BranchCount, 0) AS BranchCount,
                    ISNULL(ag.AgentCount, 0) AS AgentCount,
                    ISNULL(ac.AccountCount, 0) AS AccountCount,
                    ISNULL(tc.TodayCollection, 0) AS TodayCollection
                FROM Banks b
                LEFT JOIN (
                    SELECT BankID, COUNT(*) AS BranchCount 
                    FROM brncmast WHERE active = 'Y' 
                    GROUP BY BankID
                ) br ON br.BankID = b.BankID
                LEFT JOIN (
                    SELECT BankID, COUNT(*) AS AgentCount 
                    FROM agntmast WHERE Block = 0 
                    GROUP BY BankID
                ) ag ON ag.BankID = b.BankID
                LEFT JOIN (
                    SELECT BankID, COUNT(*) AS AccountCount 
                    FROM acmaster 
                    GROUP BY BankID
                ) ac ON ac.BankID = b.BankID
                LEFT JOIN (
                    SELECT BankID, SUM(Amount) AS TodayCollection 
                    FROM MobilePygTrn 
                    WHERE 1=1
                    GROUP BY BankID
                ) tc ON tc.BankID = b.BankID
                WHERE b.ActiveYN = 1{bankWhere}
                ORDER BY b.Name";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<BankWiseSummary>(sql, new
            {
                FilterBankID = filterBankId
            });
        }

        public async Task<IEnumerable<AccountTypeCount>> GetAccountTypeDistributionAsync()
        {
            var sql = @"
                SELECT 
                    CASE CODE1 
                        -- See PigmyPro.Domain.Enums.AccountType
                        WHEN 1 THEN 'Pigmy' 
                        WHEN 2 THEN 'Loan' 
                        WHEN 3 THEN 'Recurring' 
                        ELSE 'Other' 
                    END AS AccountType,
                    COUNT(*) AS [Count]
                FROM acmaster
                GROUP BY CODE1
                ORDER BY CODE1";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AccountTypeCount>(sql);
        }

        public async Task<BankAdminSummary> GetBankAdminSummaryAsync(int bankId)
        {
            var sql = @"
                SELECT
                    (SELECT COUNT(*) FROM brncmast WHERE BankID = @BankID AND active = 'Y') AS TotalBranches,
                    (SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID AND Block = 0) AS TotalAgents,
                    (SELECT COUNT(*) FROM acmaster WHERE BankID = @BankID) AS TotalAccounts,
                    (SELECT ISNULL(SUM(Amount), 0) FROM MobilePygTrn WHERE BankID = @BankID ) AS TodayCollection";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<BankAdminSummary>(sql, new { BankID = bankId, });
        }

        public async Task<IEnumerable<BranchWiseSummary>> GetBranchWiseSummaryAsync(int bankId, int? filterBranchId = null)
        {
            var branchFilter = filterBranchId.HasValue ? " AND br.BranchID = @FilterBranchID" : "";

            var sql = $@"
                SELECT 
                    br.BranchID,
                    br.name AS BranchName,
                    ISNULL(ag.AgentCount, 0) AS AgentCount,
                    ISNULL(ac.AccountCount, 0) AS AccountCount,
                    ISNULL(tc.TodayCollection, 0) AS TodayCollection
                FROM brncmast br
                LEFT JOIN (
                    SELECT BankID, brnc_code, COUNT(*) AS AgentCount 
                    FROM agntmast WHERE BankID = @BankID AND Block = 0 
                    GROUP BY BankID, brnc_code
                ) ag ON ag.BankID = br.BankID AND ag.brnc_code = br.BranchID
                LEFT JOIN (
                    SELECT BankID, brnc_code, COUNT(*) AS AccountCount 
                    FROM acmaster WHERE BankID = @BankID 
                    GROUP BY BankID, brnc_code
                ) ac ON ac.BankID = br.BankID AND ac.brnc_code = br.BranchID
                LEFT JOIN (
                    SELECT BankID, Brnc_code, SUM(Amount) AS TodayCollection 
                    FROM MobilePygTrn 
                    WHERE BankID = @BankID 
                    GROUP BY BankID, Brnc_code
                ) tc ON tc.BankID = br.BankID AND tc.Brnc_code = br.BranchID
                WHERE br.BankID = @BankID AND br.active = 'Y'{branchFilter}
                ORDER BY br.name";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<BranchWiseSummary>(sql, new
            {
                BankID = bankId,
                FilterBranchID = filterBranchId
            });
        }

        public async Task<IEnumerable<TopAgentCollection>> GetTopAgentCollectionsAsync(int bankId, int top, int? filterBranchId = null)
        {
            var branchFilter = filterBranchId.HasValue ? " AND a.brnc_code = @FilterBranchID" : "";
            var branchFilterTrn = filterBranchId.HasValue ? " AND Brnc_code = @FilterBranchID" : "";

            var sql = $@"
                SELECT TOP (@Top)
                    a.NAME AS AgentName,
                    ISNULL(br.name, '') AS BranchName,
                    ISNULL(tc.TodayAmount, 0) AS TodayAmount,
                    ISNULL(tc.AccountsCollected, 0) AS AccountsCollected
                FROM agntmast a
                LEFT JOIN brncmast br ON br.BankID = a.BankID AND br.BranchID = a.brnc_code
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code,
                        SUM(Amount) AS TodayAmount,
                        COUNT(DISTINCT Code2) AS AccountsCollected
                    FROM MobilePygTrn
                    WHERE BankID = @BankID {branchFilterTrn}
                    GROUP BY BankID, Agent, Brnc_code
                ) tc ON tc.BankID = a.BankID AND tc.Agent = a.code AND tc.Brnc_code = a.brnc_code
                WHERE a.BankID = @BankID AND a.Block = 0 AND ISNULL(tc.TodayAmount, 0) > 0{branchFilter}
                ORDER BY tc.TodayAmount DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<TopAgentCollection>(sql, new
            {
                BankID = bankId,
                Top = top,
                FilterBranchID = filterBranchId
            });
        }

   
        public async Task<IEnumerable<AccountTypeCount>> GetAccountTypeDistributionByBankAsync(int bankId)
        {
            var sql = @"
                SELECT 
                    CASE CODE1 
                        -- See PigmyPro.Domain.Enums.AccountType
                        WHEN 1 THEN 'Pigmy' 
                        WHEN 2 THEN 'Loan' 
                        WHEN 3 THEN 'Recurring' 
                        ELSE 'Other' 
                    END AS AccountType,
                    COUNT(*) AS [Count]
                FROM acmaster
                WHERE BankID = @BankID
                GROUP BY CODE1
                ORDER BY CODE1";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AccountTypeCount>(sql, new { BankID = bankId });
        }

        public async Task<BranchAdminSummary> GetBranchAdminSummaryAsync(int bankId, int branchId)
        {
            var sql = @"
                SELECT
                    (SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID AND brnc_code = @BranchID AND Block = 0) AS TotalAgents,
                    (SELECT COUNT(*) FROM acmaster WHERE BankID = @BankID AND brnc_code = @BranchID) AS TotalAccounts,
                    (SELECT ISNULL(SUM(Amount), 0) FROM MobilePygTrn WHERE BankID = @BankID AND Brnc_code = @BranchID ) AS TodayCollection,
                    (SELECT COUNT(DISTINCT Code2) FROM MobilePygTrn WHERE BankID = @BankID AND Brnc_code = @BranchID ) AS AccountsCollectedToday";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<BranchAdminSummary>(sql, new { BankID = bankId, BranchID = branchId, });
        }

        public async Task<IEnumerable<AgentCollectionRow>> GetAgentCollectionsByBranchAsync(int bankId, int branchId)
        {
            var sql = @"
                SELECT 
                    a.NAME AS AgentName,
                    ISNULL(tc.TodayAmount, 0) AS TodayAmount,
                    ISNULL(tc.AccountsCollected, 0) AS AccountsCollected,
                    CAST(a.Block AS BIT) AS IsBlocked
                FROM agntmast a
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code,
                        SUM(Amount) AS TodayAmount,
                        COUNT(DISTINCT Code2) AS AccountsCollected
                    FROM MobilePygTrn
                    WHERE BankID = @BankID AND Brnc_code = @BranchID 
                    GROUP BY BankID, Agent, Brnc_code
                ) tc ON tc.BankID = a.BankID AND tc.Agent = a.code AND tc.Brnc_code = a.brnc_code
                WHERE a.BankID = @BankID AND a.brnc_code = @BranchID
                ORDER BY tc.TodayAmount DESC, a.NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentCollectionRow>(sql, new { BankID = bankId, BranchID = branchId, });
        }

        public async Task<IEnumerable<AgentUploadReadyRow>> GetAgentsReadyForUploadAsync(int bankId, int branchId)
        {
            // Eligibility logic:
            // 1. RadyToCash = 'Y'
            // 2. Has acmaster records for this Bank+Branch+Agent
            // 3. Has NO pending MobilePygTrn records
            var sql = @"
                SELECT 
                    a.code AS AgentCode, 
                    a.NAME AS AgentName
                FROM agntmast a
                WHERE a.BankID = @BankID 
                  AND a.brnc_code = @BranchID
                  AND a.Block = 0
                  AND a.RadyToCash = 'Y'
                  AND EXISTS (
                      SELECT 1 FROM acmaster ac 
                      WHERE ac.BankID = a.BankID 
                        AND CAST(ac.brnc_code AS DECIMAL(10,0)) = a.brnc_code 
                        AND CAST(ac.AgnCode AS DECIMAL(18,0)) = a.code
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM MobilePygTrn m 
                      WHERE m.BankID = a.BankID 
                        AND CAST(m.Brnc_code AS DECIMAL(10,0)) = a.brnc_code 
                        AND CAST(m.Agent AS DECIMAL(18,0)) = a.code
                  )
                ORDER BY a.NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentUploadReadyRow>(sql, new { BankID = bankId, BranchID = branchId });
        }

        public async Task<IEnumerable<AgentOverviewRow>> GetAgentOverviewAsync(int bankId, int? filterBranchId = null)
        {
            var branchFilter = filterBranchId.HasValue ? " AND a.brnc_code = @FilterBranchID" : "";
            var branchFilterTrn = filterBranchId.HasValue ? " AND Brnc_code = @FilterBranchID" : "";

            var sql = $@"
                SELECT 
                    a.code AS AgentCode,
                    a.NAME AS AgentName,
                    ISNULL(br.name, '') AS BranchName,
                    ISNULL(a.MobileNo, '') AS MobileNumber,
                    CAST(a.Block AS BIT) AS IsBlocked,
                    ISNULL(tc.TodayAmount, 0) AS TodayAmount,
                    ISNULL(tc.AccountsCollected, 0) AS AccountsCollected,
                    DATEDIFF(day, max_dt.MaxDate, GETDATE()) AS DaysInactive,
                    a.RadyToCash AS RadyToCash,
                    ISNULL(tc.ReceiptCount, 0) AS ReceiptCount
                FROM agntmast a
                LEFT JOIN brncmast br ON br.BankID = a.BankID AND br.BranchID = a.brnc_code
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code,
                        SUM(Amount) AS TodayAmount,
                        COUNT(DISTINCT Code2) AS AccountsCollected,
                        COUNT(*) AS ReceiptCount
                    FROM MobilePygTrn
                    WHERE BankID = @BankID {branchFilterTrn}
                    GROUP BY BankID, Agent, Brnc_code
                ) tc ON tc.BankID = a.BankID 
                    AND CAST(tc.Agent AS NUMERIC(18,0)) = CAST(a.code AS NUMERIC(18,0)) 
                    AND CAST(tc.Brnc_code AS NUMERIC(18,0)) = CAST(a.brnc_code AS NUMERIC(18,0))
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code, MAX(Date) AS MaxDate
                    FROM MobilePygTrn
                    WHERE BankID = @BankID{branchFilterTrn}
                    GROUP BY BankID, Agent, Brnc_code
                ) max_dt ON max_dt.BankID = a.BankID 
                    AND CAST(max_dt.Agent AS NUMERIC(18,0)) = CAST(a.code AS NUMERIC(18,0)) 
                    AND CAST(max_dt.Brnc_code AS NUMERIC(18,0)) = CAST(a.brnc_code AS NUMERIC(18,0))
                WHERE a.BankID = @BankID{branchFilter}
                ORDER BY ISNULL(DATEDIFF(day, max_dt.MaxDate, GETDATE()), 999999) DESC, a.NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentOverviewRow>(sql, new
            {
                BankID = bankId,
                FilterBranchID = filterBranchId
            });
        }

        public async Task<IEnumerable<AgentOverviewRow>> GetAtRiskAgentsAsync(int bankId, int top, int? branchId = null)
        {
            var branchFilter = branchId.HasValue ? " AND a.brnc_code = @FilterBranchID" : "";
            var branchFilterTrn = branchId.HasValue ? " AND Brnc_code = @FilterBranchID" : "";

            var sql = $@"
                SELECT TOP (@Top)
                    a.code AS AgentCode,
                    a.NAME AS AgentName,
                    ISNULL(br.name, '') AS BranchName,
                    ISNULL(a.MobileNo, '') AS MobileNumber,
                    CAST(a.Block AS BIT) AS IsBlocked,
                    ISNULL(tc.TodayAmount, 0) AS TodayAmount,
                    ISNULL(tc.AccountsCollected, 0) AS AccountsCollected,
                    DATEDIFF(day, max_dt.MaxDate, GETDATE()) AS DaysInactive
                FROM agntmast a
                LEFT JOIN brncmast br ON br.BankID = a.BankID AND br.BranchID = a.brnc_code
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code,
                        SUM(Amount) AS TodayAmount,
                        COUNT(DISTINCT Code2) AS AccountsCollected
                    FROM MobilePygTrn
                    WHERE BankID = @BankID {branchFilterTrn}
                    GROUP BY BankID, Agent, Brnc_code
                ) tc ON tc.BankID = a.BankID 
                    AND CAST(tc.Agent AS NUMERIC(18,0)) = CAST(a.code AS NUMERIC(18,0)) 
                    AND CAST(tc.Brnc_code AS NUMERIC(18,0)) = CAST(a.brnc_code AS NUMERIC(18,0))
                LEFT JOIN (
                    SELECT BankID, Agent, Brnc_code, MAX(Date) AS MaxDate
                    FROM MobilePygTrn
                    WHERE BankID = @BankID{branchFilterTrn}
                    GROUP BY BankID, Agent, Brnc_code
                ) max_dt ON max_dt.BankID = a.BankID 
                    AND CAST(max_dt.Agent AS NUMERIC(18,0)) = CAST(a.code AS NUMERIC(18,0)) 
                    AND CAST(max_dt.Brnc_code AS NUMERIC(18,0)) = CAST(a.brnc_code AS NUMERIC(18,0))
                WHERE a.BankID = @BankID 
                  AND a.Block = 0 
                  AND max_dt.MaxDate IS NOT NULL 
                  AND DATEDIFF(day, max_dt.MaxDate, GETDATE()) > 7
                  {branchFilter}
                ORDER BY DATEDIFF(day, max_dt.MaxDate, GETDATE()) DESC, a.NAME";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentOverviewRow>(sql, new
            {
                BankID = bankId,
                Top = top,
                FilterBranchID = branchId
            });
        }
        public async Task<IEnumerable<DailyTrendPoint>> GetDailyCollectionTrendAsync(int? bankId = null, int? branchId = null)
        {
            var bankFilter = bankId.HasValue ? " AND BankID = @BankID" : "";
            var branchFilter = branchId.HasValue ? " AND Brnc_code = @BranchID" : "";

            var sql = $@"
                SELECT 
                    CAST(Date AS DATE) AS Date,
                    SUM(Amount) AS Amount
                FROM MobilePygTrn
                WHERE 1=1 {bankFilter}
                    {branchFilter}
                GROUP BY CAST(Date AS DATE)
                ORDER BY CAST(Date AS DATE)";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DailyTrendPoint>(sql, new
            {
                BankID = bankId,
                BranchID = branchId,
                });
        }

        public async Task<AcMasterSummary> GetAcMasterSummaryAsync(int? bankId = null, int? branchId = null)
        {
            var bankFilter = bankId.HasValue ? " AND BankID = @BankID" : "";
            var branchFilter = branchId.HasValue ? " AND brnc_code = @BranchID" : "";
            var branchFilterTrn = branchId.HasValue ? " AND Brnc_code = @BranchID" : "";

            var sql = $@"
                SELECT 
                    (SELECT COUNT(*) FROM acmaster WHERE 1=1 {bankFilter} {branchFilter}) AS TotalAccounts,
                    (SELECT ISNULL(SUM(BALANCE), 0) FROM acmaster WHERE 1=1 {bankFilter} {branchFilter}) AS TotalBalance,
                    (SELECT COUNT(DISTINCT Code2) FROM MobilePygTrn WHERE 1=1 {bankFilter} {branchFilterTrn}) AS TotalCollectionAccounts,
                    (SELECT ISNULL(SUM(Amount), 0) FROM MobilePygTrn WHERE 1=1 {bankFilter} {branchFilterTrn}) AS TotalCollectionAmount";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<AcMasterSummary>(sql, new
            {
                BankID = bankId,
                BranchID = branchId,
                });
        }

        public async Task<CollectionHeldSummary> GetCollectionHeldWithAgentsAsync(int bankId, int? branchId = null)
        {
            var branchFilter = branchId.HasValue ? " AND CAST(Brnc_code AS DECIMAL(10,0)) = @BranchID" : "";

            var sql = $@"
                SELECT 
                    COUNT(DISTINCT Agent) AS AgentCount,
                    ISNULL(SUM(Amount), 0) AS TotalAmount
                FROM MobilePygTrn 
                WHERE BankID = @BankID {branchFilter}";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<CollectionHeldSummary>(sql, new
            {
                BankID = bankId,
                BranchID = branchId
            });
        }

        public async Task<CollectionDepositedSummary> GetTodayDepositedCollectionAsync(int bankId, int? branchId = null)
        {
            var branchFilter = branchId.HasValue ? " AND dil.Brnc_Code = @BranchID" : "";

            var sql = $@"
                SELECT 
                    COUNT(DISTINCT dil.Agent_Code) AS AgentCount, 
                    ISNULL(SUM(mpa.Amount), 0) AS TotalAmount
                FROM DataImportLog dil
                JOIN MobilePygTrn_ALL mpa 
                    ON mpa.BankID = dil.BankID
                    AND CAST(mpa.Brnc_code AS DECIMAL(10,0)) = dil.Brnc_Code
                    AND CAST(mpa.Agent AS DECIMAL(18,0)) = dil.Agent_Code
                WHERE dil.BankID = @BankID
                    AND CAST(dil.Import_Date AS DATE) = CAST(GETDATE() AS DATE)
                    {branchFilter}";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstAsync<CollectionDepositedSummary>(sql, new
            {
                BankID = bankId,
                BranchID = branchId
            });
        }
    }
}

