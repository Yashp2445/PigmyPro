using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using System.Data;

namespace PigmyPro.Data.Repositories
{
    public class BankRepository : IBankRepository
    {
        private readonly DapperContext _context;

        public BankRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Bank>> GetAllAsync()
        {
            var query = "SELECT BankID, Name, Address, ContactNo, ContactPerson, EmailID, ActiveYN, EntryDateTime, CollectionGLCode, hasCBS, No_of_Holidays, LogoFileName, AppLoginPrefix FROM Banks ORDER BY BankID DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Bank>(query);
        }

        public async Task<Bank?> GetByIdAsync(int id)
        {
            var query = "SELECT * FROM Banks WHERE BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Bank>(query, new { BankID = id });
        }


        public async Task<int> AddAsync(Bank bank)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "I");
            p.Add("Name", bank.Name);
            p.Add("Address", bank.Address);
            p.Add("ContactNo", bank.ContactNo);
            p.Add("ContactPerson", bank.ContactPerson);
            p.Add("EmailID", bank.EmailID);
            p.Add("ActiveYN", bank.ActiveYN);
            p.Add("CollectionGLCode", bank.CollectionGLCode);
            p.Add("hasCBS", bank.hasCBS);
            p.Add("RecieptPrinting", bank.RecieptPrinting);
            p.Add("No_of_Holidays", bank.No_of_Holidays);
            p.Add("Logo", bank.Logo, DbType.Binary);
            p.Add("LogoFileName", bank.LogoFileName);
            p.Add("AppLoginPrefix", bank.AppLoginPrefix);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBank", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> UpdateAsync(Bank bank)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "U");
            p.Add("BankID", bank.BankID);
            p.Add("Name", bank.Name);
            p.Add("Address", bank.Address);
            p.Add("ContactNo", bank.ContactNo);
            p.Add("ContactPerson", bank.ContactPerson);
            p.Add("EmailID", bank.EmailID);
            p.Add("ActiveYN", bank.ActiveYN);
            p.Add("CollectionGLCode", bank.CollectionGLCode);
            p.Add("hasCBS", bank.hasCBS);
            p.Add("RecieptPrinting", bank.RecieptPrinting);
            p.Add("No_of_Holidays", bank.No_of_Holidays);
            p.Add("Logo", bank.Logo, DbType.Binary);
            p.Add("LogoFileName", bank.LogoFileName);
            p.Add("AppLoginPrefix", bank.AppLoginPrefix);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBank", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        public async Task<int> DeleteAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Op", "D");
            p.Add("BankID", id);
            p.Add("Msg", dbType: DbType.String, size: 80, direction: ParameterDirection.Output);

            var rows = await connection.ExecuteAsync("sp_insertUpdateBank", p, commandType: CommandType.StoredProcedure);
            ThrowIfSpFailed(p.Get<string>("Msg"));
            return rows;
        }

        private static void ThrowIfSpFailed(string? msg)
        {
            if (!string.IsNullOrEmpty(msg) && msg != "1")
                throw new System.Exception($"sp_insertUpdateBank failed: {msg}");
        }

        public async Task<int> GetDependentBranchCountAsync(int bankId)
        {
            var query = "SELECT COUNT(*) FROM brncmast WHERE BankID = @BankID";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(query, new { BankID = bankId });
        }
    }
}
