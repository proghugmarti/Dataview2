using DataView2.GrpcService.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Data.SqlClient;
using DataView2.Core;
using Microsoft.Data.Sqlite;
using Dapper;

namespace DataView2.GrpcService.Interfaces
{
    public interface IRepositoryTables<T> where T : class
    {        
        Task<IEnumerable<T>> QueryAsync(string predicate);
    }

    public class RepositoryTables<T> : IRepositoryTables<T> where T : class
    {
        private readonly AppDbContextProjectData _context;
        private DbSet<T> _entities;

        public RepositoryTables(AppDbContextProjectData context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _entities = context.Set<T>();
        }


        public virtual async Task<IEnumerable<TResult>> QueryAsyncDagger<TResult>(string predicate)
        {
            var connection = _context.Database.GetDbConnection();
            using (connection)
            {
                try
                {
                   // await connection.OpenAsync();
                    return await connection.QueryAsync<TResult>(predicate);
                }
                catch (Exception ex)
                {
                    Utils.RegError($"Error when execute query: {ex.Message}");
                    return new List<TResult>();
                }
            }
        }

        public virtual async Task<IEnumerable<T>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.Set<T>().FromSqlRaw(sqlQuery).ToListAsync();
                //var lstTables = await _context.TableExport.FromSqlRaw(sqlQuery).ToListAsync();
                return lstTables;

            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<T>();
            }
        }
    }

}
