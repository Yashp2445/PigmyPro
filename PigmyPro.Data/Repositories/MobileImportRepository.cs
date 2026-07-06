using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class MobileImportRepository : IMobileImportRepository
    {
        private readonly DapperContext _context;

        public MobileImportRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AgentDropdownItem>> GetAgentsByBranchAsync(int bankId, decimal branchCode)
        {
            var sql = @"SELECT CAST(code AS INT) AS Code, NAME AS Name 
                        FROM agntmast 
                        WHERE BankID = @BankID 
                          AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                          AND Block = 0 
                        ORDER BY NAME";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AgentDropdownItem>(sql,
                new { BankID = bankId, BranchCode = branchCode });
        }

        public async Task<IEnumerable<ExportRow>> GetPendingCollectionsAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var sql = @"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY m.Date, m.ID) AS SrNo,
                    m.Date        AS CollectionDate,
                    CAST(m.Code1 AS INT) AS Code1,
                    CAST(m.Code2 AS BIGINT) AS Code2,
                    ISNULL(ac.name, m.Name) AS CustomerName,
                    m.Amount,
                    ISNULL(ac.BALANCE, 0) AS Balance
                FROM MobilePygTrn m
                LEFT JOIN acmaster ac 
                    ON  ac.BankID = m.BankID 
                    AND CAST(ac.brnc_code AS DECIMAL(10,0)) = CAST(m.Brnc_code AS DECIMAL(10,0))
                    AND CAST(ac.CODE2 AS DECIMAL(18,0)) = CAST(m.Code2 AS DECIMAL(18,0))
                WHERE m.BankID = @BankID 
                  AND CAST(m.Brnc_code AS DECIMAL(10,0)) = @BranchCode 
                  AND CAST(m.Agent AS DECIMAL(18,0)) = @AgentCode
                ORDER BY m.Date, m.ID";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<ExportRow>(sql,
                new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode });
        }

        public async Task<AgentDetailsRow?> GetAgentDetailsAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var sql = @"
                SELECT 
                    CAST(code AS DECIMAL(18,0)) AS AgentCode,
                    NAME          AS AgentName,
                    RadyToCash,
                    Down_Load_YN
                FROM agntmast 
                WHERE BankID = @BankID 
                  AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                  AND CAST(code AS DECIMAL(18,0)) = @AgentCode";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<AgentDetailsRow>(sql,
                new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode });
        }

        public async Task<decimal> GetAccountBalanceAsync(int bankId, decimal branchCode, decimal code2)
        {
            var sql = @"SELECT ISNULL(BALANCE, 0) 
                        FROM acmaster 
                        WHERE BankID = @BankID 
                          AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                          AND CAST(CODE2 AS DECIMAL(18,0)) = @Code2";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<decimal>(sql,
                new { BankID = bankId, BranchCode = branchCode, Code2 = code2 });
        }

        public async Task LogExportAsync(int bankId, decimal branchCode, decimal agentCode,
            string userId, int totalRecords, decimal totalAmount)
        {
            var sql = @"
                INSERT INTO DataExportLog 
                    (BankID, Export_Date, Brnc_Code, Agent_Code, User_ID, 
                     Export_Time, Total_Records, Total_Amount, EntryDate)
                VALUES 
                    (@BankID, @ExportDate, @BranchCode, @AgentCode, @UserID,
                     GETDATE(), @TotalRecords, @TotalAmount, GETDATE())";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                BankID = bankId,
                ExportDate = DateTime.Today,
                BranchCode = branchCode,
                AgentCode = agentCode,
                UserID = userId,
                TotalRecords = totalRecords,
                TotalAmount = (long)totalAmount
            });
        }

        public async Task SetAgentDownloadFlagAsync(int bankId, decimal branchCode,
            decimal agentCode, string flagValue)
        {
            var sql = @"UPDATE agntmast 
                        SET Down_Load_YN = @FlagValue 
                        WHERE BankID = @BankID 
                          AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                          AND CAST(code AS DECIMAL(18,0)) = @AgentCode";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql,
                new { FlagValue = flagValue, BankID = bankId, BranchCode = branchCode, AgentCode = agentCode });
        }


        public async Task<bool> ValidateBranchAsync(int bankId, decimal branchCode)
        {
            var sql = @"SELECT COUNT(1) FROM brncmast 
                        WHERE BankID = @BankID 
                          AND CAST(BranchID AS DECIMAL(10,0)) = @BranchCode";
            using var connection = _context.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql,
                new { BankID = bankId, BranchCode = branchCode });
            return count > 0;
        }

        public async Task<AgentDetailsRow?> ValidateAgentAsync(int bankId, decimal branchCode, decimal agentCode)
            => await GetAgentDetailsAsync(bankId, branchCode, agentCode);

        public async Task CommitImportAsync(int bankId, decimal branchCode, decimal agentCode,
            string userId, string clientIp, int totalRecords, List<ImportAccountRow> rows)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(
                    @"DELETE FROM acmaster 
                      WHERE BankID = @BankID 
                        AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                        AND CAST(AgnCode AS DECIMAL(18,0)) = @AgentCode",
                    new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode },
                    transaction);

                await connection.ExecuteAsync(
                    @"INSERT INTO DataImportLog 
                        (BankID, Import_Date, Brnc_Code, Agent_Code, UserID, 
                         Import_Time, Import_IP, Total_Record_Count, EntryDate)
                      VALUES 
                        (@BankID, @ImportDate, @BranchCode, @AgentCode, @UserID,
                         GETDATE(), @ImportIP, @TotalRecords, GETDATE())",
                    new
                    {
                        BankID = bankId,
                        ImportDate = DateTime.Today,
                        BranchCode = branchCode,
                        AgentCode = agentCode,
                        UserID = userId,
                        ImportIP = clientIp,
                        TotalRecords = totalRecords
                    },
                    transaction);
             
                await connection.ExecuteAsync(
                    @"UPDATE agntmast 
                      SET Down_Load_YN = 'N', RadyToCash = 'N' 
                      WHERE BankID = @BankID 
                        AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                        AND CAST(code AS DECIMAL(18,0)) = @AgentCode",
                    new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode },
                    transaction);

                await connection.ExecuteAsync(
                    @"DELETE FROM MobilePygTrn 
                      WHERE BankID = @BankID 
                        AND CAST(Brnc_code AS DECIMAL(10,0)) = @BranchCode 
                        AND CAST(Agent AS DECIMAL(18,0)) = @AgentCode",
                    new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode },
                    transaction);

                foreach (var r in rows)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO acmaster 
                            (BankID, CODE1, brnc_code, CODE2, name, ename, 
                             BALANCE, OPN_DATE, AgnCode, Entry_Date)
                          VALUES 
                            (@BankID, 48, @BranchCode, @Code2, @Name, @Name,
                             @Balance, @OpnDate, @AgentCode, GETDATE())",
                        new
                        {
                            BankID = bankId,
                            BranchCode = branchCode,
                            r.Code2,
                            r.Name,
                            r.Balance,
                            OpnDate = r.OpnDate,
                            AgentCode = agentCode
                        },
                        transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}