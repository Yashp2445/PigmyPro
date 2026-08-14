using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class ReuploadRepository : IReuploadRepository
    {
        private readonly DapperContext _context;

        public ReuploadRepository(DapperContext context)
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

        public async Task<bool> HasPendingMobileTransactionsAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var sql = @"SELECT COUNT(1) FROM MobilePygTrn 
                        WHERE BankID = @BankID 
                          AND CAST(Brnc_code AS DECIMAL(10,0)) = @BranchCode 
                          AND CAST(Agent AS DECIMAL(18,0)) = @AgentCode";
            using var connection = _context.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql,
                new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode });
            return count > 0;
        }

        public async Task<AgentDetailsRow?> ValidateAgentAsync(int bankId, decimal branchCode, decimal agentCode)
            => await GetAgentDetailsAsync(bankId, branchCode, agentCode);

        public async Task SetAgentStateForReuploadAsync(int bankId, decimal branchCode, decimal agentCode)
        {
            var sql = @"UPDATE agntmast 
                        SET RadyToCash = 'Y', 
                            Down_Load_YN = 'Y'
                        WHERE BankID = @BankID 
                          AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode 
                          AND CAST(code AS DECIMAL(18,0)) = @AgentCode";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, new { BankID = bankId, BranchCode = branchCode, AgentCode = agentCode });
        }

        public async Task CommitImportAsync(int bankId, decimal branchCode, decimal agentCode,
            string userId, string clientIp, int totalRecords, List<ReuploadAccountRow> rows)
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
                        @"IF EXISTS (SELECT 1 FROM acmaster WHERE BankID = @BankID AND CODE1 = 48 AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode AND CAST(CODE2 AS DECIMAL(18,0)) = @Code2)
                          BEGIN
                              UPDATE acmaster 
                              SET name = @Name, ename = @Name, BALANCE = @Balance, OPN_DATE = @OpnDate, AgnCode = @AgentCode, Entry_Date = GETDATE(), Mobile_No = @MobileNumber
                              WHERE BankID = @BankID AND CODE1 = 48 AND CAST(brnc_code AS DECIMAL(10,0)) = @BranchCode AND CAST(CODE2 AS DECIMAL(18,0)) = @Code2
                          END
                          ELSE
                          BEGIN
                              INSERT INTO acmaster 
                                (BankID, CODE1, brnc_code, CODE2, name, ename, 
                                 BALANCE, OPN_DATE, AgnCode, Entry_Date, Mobile_No)
                              VALUES 
                                (@BankID, 48, @BranchCode, @Code2, @Name, @Name,
                                 @Balance, @OpnDate, @AgentCode, GETDATE(), @MobileNumber)
                          END",
                        new
                        {
                            BankID = bankId,
                            BranchCode = branchCode,
                            r.Code2,
                            r.Name,
                            r.Balance,
                            OpnDate = r.OpnDate,
                            AgentCode = agentCode,
                            MobileNumber = r.MobileNumber
                        }, transaction);
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
