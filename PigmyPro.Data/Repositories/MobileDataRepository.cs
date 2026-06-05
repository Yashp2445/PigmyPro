using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Repositories
{
    public class MobileDataRepository : IMobileDataRepository
    {
        private readonly DapperContext _context;

        public MobileDataRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MobileTransaction>> GetPendingByBankAsync(int bankId, decimal? agentCode = null, DateTime? date = null)
        {
            // MobilePygTrn actual columns: Agent (decimal), Code2 (decimal), Date (datetime),
            // Brnc_code (int), Amount (decimal), BankID (int), Code1 (int)
            // Map to MobileTransaction entity property names via SQL aliases.
            var query = @"
                SELECT 
                    m.BankID,
                    ISNULL(m.Brnc_code, 0) AS BranchID,
                    m.Agent AS AgentCode,
                    m.Code2 AS AccountNo,
                    ISNULL(m.Amount, 0) AS Amount,
                    m.Date AS CollectionDate,
                    m.Date AS EntryDate,
                    ISNULL(a.NAME, '') AS AgentName,
                    ISNULL(c.name, '') AS CustomerName
                FROM MobilePygTrn m
                LEFT JOIN agntmast a ON CAST(m.Agent AS DECIMAL(18,0)) = a.code 
                    AND m.BankID = a.BankID 
                    AND m.Brnc_code = a.brnc_code
                LEFT JOIN acmaster c ON CAST(m.Code2 AS DECIMAL(18,0)) = c.CODE2 
                    AND m.BankID = c.BankID 
                    AND m.Brnc_code = c.brnc_code 
                    AND m.Code1 = c.CODE1
                WHERE 1=1";

            // Apply bank filter only for non-SuperAdmin (bankId > 0)
            if (bankId > 0)
                query += " AND m.BankID = @BankID";

            if (agentCode.HasValue)
                query += " AND m.Agent = @AgentCode";

            if (date.HasValue)
                query += " AND CAST(m.Date AS DATE) = @Date";

            query += " ORDER BY m.Date DESC";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<MobileTransaction>(query, new 
            { 
                BankID = bankId, 
                AgentCode = agentCode, 
                Date = date?.Date 
            });

            return result ?? Enumerable.Empty<MobileTransaction>();
        }
    }
}
