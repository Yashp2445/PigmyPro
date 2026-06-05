using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;

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
            var query = "SELECT * FROM Banks ORDER BY BankID DESC";

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
            var query = @"INSERT INTO Banks 
            (Name, Address, ContactNo, ContactPerson, EmailID, ActiveYN, CollectionGLCode)
            VALUES 
            (@Name, @Address, @ContactNo, @ContactPerson, @EmailID, @ActiveYN, @CollectionGLCode)";

            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, bank);
        }

        public async Task<int> UpdateAsync(Bank bank)
        {
            var query = @"UPDATE Banks SET
            Name = @Name,
            Address = @Address,
            ContactNo = @ContactNo,
            ContactPerson = @ContactPerson,
            EmailID = @EmailID,
            ActiveYN = @ActiveYN,
            CollectionGLCode = @CollectionGLCode
            WHERE BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, bank);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var query = "DELETE FROM Banks WHERE BankID = @BankID";

            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(query, new { BankID = id });
        }
    }
}
