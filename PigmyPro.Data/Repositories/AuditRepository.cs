using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;

namespace PigmyPro.Data.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly DapperContext _context;

        public AuditRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLogRow>> GetRecentActivityAsync(int bankId, decimal? branchId, DateTime dateFrom, DateTime dateTo)
        {
            var sql = @"
                SELECT * FROM (
                    -- Agents
                    SELECT 
                        'Agent' AS EntityType,
                        CAST(code AS VARCHAR(50)) AS EntityID,
                        ISNULL(NAME, '') AS EntityName,
                        'Update' AS Action,
                        ChangeBy AS ChangedBy,
                        ChangeIP AS ChangeIP,
                        EntryDate AS ChangeDate,
                        ISNULL(BlockRemark, '') AS Remarks
                    FROM agntmast
                    WHERE BankID = @BankID AND ChangeBy IS NOT NULL
                    " + (branchId.HasValue ? " AND brnc_code = @BranchID " : "") + @"
                    
                    UNION ALL
                    
                    -- Accounts
                    SELECT 
                        'Account' AS EntityType,
                        CAST(CODE2 AS VARCHAR(50)) AS EntityID,
                        ISNULL(name, '') AS EntityName,
                        'Update' AS Action,
                        NULL AS ChangedBy, -- Placeholder, adjust if acmaster has it
                        NULL AS ChangeIP,
                        Entry_Date AS ChangeDate,
                        '' AS Remarks
                    FROM acmaster
                    WHERE BankID = @BankID
                    " + (branchId.HasValue ? " AND brnc_code = @BranchID " : "") + @"
                ) AS Activity
                WHERE CAST(ChangeDate AS DATE) >= @DateFrom 
                  AND CAST(ChangeDate AS DATE) <= @DateTo
                ORDER BY ChangeDate DESC
            ";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AuditLogRow>(sql, new 
            { 
                BankID = bankId, 
                BranchID = branchId, 
                DateFrom = dateFrom.Date, 
                DateTo = dateTo.Date 
            });
        }
    }
}
