using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Serilog;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DataView2.GrpcService.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> CreateAsync(T entity);
        Task CreateRangeAsync(IEnumerable<T> entities);
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> UpdateAsync(T entity);
        Task<T> UpdateSQLAsync(T entity, Dictionary<string, string> fieldsToUpdate, int key);
        Task<T> UpdateSpecificSQLAsync(T entity, Dictionary<string, string> fieldsToUpdate, int key);
        Task DeleteAsync(int id);
        Task DeleteAllAsync();
        Task<T> Delete(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);
       
        Task<bool> AnyAsync();
        Task<int> GetRecordCountAsync();
        IQueryable<T> Query();
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate);
        Task<(List<T>, int)> GetPagedData(int page, int pageSize);
        List<string> GetColumns();
        Task<T> UpdateEntityAsync(T entityTobeUpdated, Dictionary<string, object> updates, int id);
    }

    public class Repository<T, TContext> : IRepository<T> where T : class where TContext : DbContext
    {
        private readonly IDbContextFactory<TContext> _dbContextFactory;
        private readonly TContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(IDbContextFactory<TContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _context = _dbContextFactory.CreateDbContext();
            _dbSet = _context.Set<T>();
        }

        public async Task<T> CreateAsync(T entity)
        {
            try
            {

                var entityEntry = _context.Entry(entity);

                if (entityEntry.State == EntityState.Detached)
                {
                    // If detached, explicitly attach the entity
                    _context.Attach(entity);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }

            return entity;
        }

        public async Task CreateRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                _context.Set<T>().AddRange(entities);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _context.FindAsync<T>(id);
            if (entity == null)
                throw new Exception($"{typeof(T).Name} not found.");

            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var dbPath = context.Database.GetDbConnection().ConnectionString;

                return await context.Set<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetAllAsync: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateSQLAsync(T entityBorrarla, Dictionary<string, string> fieldsToUpdate, int key)
        {
            T entity = await _context.Set<T>().FindAsync(key);
            try
            {                
                if (entity == null)
                {
                    throw new InvalidOperationException($"The entity with ID {key} was not found.");
                }

                var entry = _context.Entry(entity);
                entry.State = EntityState.Unchanged; 

                foreach (var kvp in fieldsToUpdate)
                {
                    var propertyName = kvp.Key;
                    var propertyValue = kvp.Value;
                    var property = entry.Property(propertyName);

                    if (property != null && !property.Metadata.IsKey()) 
                    {
                        var targetType = property.Metadata.ClrType;
                        var convertedValue = Convert.ChangeType(propertyValue, targetType);
                        property.CurrentValue = convertedValue;
                        property.IsModified = true; 
                    }
                }
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating selected columns: {ex.Message}");
            }

            return entity;
        }

        public async Task<T> UpdateEntityAsync(T entityTobeUpdated, Dictionary<string, object> updates, int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            try
            {
                if (entity == null)
                {
                    Serilog.Log.Error($"The entity with ID {id} was not fund.");
                    return null;
                }

                var entityType = _context.Model.FindEntityType(typeof(T));
                var properties = entityType.GetProperties().ToDictionary(p => p.Name, p => p.PropertyInfo);

                foreach (var (key, value) in updates)
                {
                    if (properties.TryGetValue(key, out var property) && property.CanWrite)
                    {
                        object convertedValue = ConvertToPropertyType(value, property.PropertyType);
                        property.SetValue(entity, convertedValue);
                    }
                }

                await _context.SaveChangesAsync();
                Serilog.Log.Information("Update Entity is successful!");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Error updating Entity : {ex.Message}");
                return null;
            }
            return entity;
        }

        private object ConvertToPropertyType(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null ? Activator.CreateInstance(targetType) : null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return Convert.ChangeType(value, underlyingType);
        }

        public async Task<T> UpdateSpecificSQLAsync(T entityBorrarla, Dictionary<string, string> fieldsToUpdate, int key)
        {
            // Buscar la entidad por clave primaria
            var entity = await _context.Set<T>().FindAsync(key);

            if (entity == null)
            {
                throw new InvalidOperationException($"The entity with ID {key} was not found.");
            }

            try
            {
                
                var entry = _context.Entry(entity);
                
                foreach (var kvp in fieldsToUpdate)
                {
                    var propertyName = kvp.Key;
                    var propertyValue = kvp.Value;

                    var property = entry.Property(propertyName);

                    if (property != null && !property.Metadata.IsKey())
                    {                        
                        var targetType = property.Metadata.ClrType;
                        var convertedValue = Convert.ChangeType(propertyValue, targetType);

                        property.CurrentValue = convertedValue;
                        property.IsModified = true;
                    }
                }                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating selected columns: {ex.Message}");
            }

            return entity;
        }
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<T> Delete(T entity)
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.FindAsync<T>(id);
            if (entity == null)
                throw new Exception($"{typeof(T).Name} not found.");

            _context.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllAsync()
        {
            var entities = await _context.Set<T>().ToListAsync();

            if (entities.Count > 0)
            {
                _context.RemoveRange(entities);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _context.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AnyAsync()
        {
            return await _context.Set<T>().AnyAsync();
        }

        public async Task<int> GetRecordCountAsync()
        {
            return await _context.Set<T>().CountAsync();
        }

        public IQueryable<T> Query()
        {
            return _context.Set<T>().AsQueryable();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public async Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<(List<T>, int)> GetPagedData(int page, int pageSize)
        {
            int totalCount = await _dbSet.CountAsync();
            var data = await _dbSet.Skip(page * pageSize).Take(pageSize).ToListAsync();
            return (data, totalCount);
        }


        public List<string> GetColumns()
        {
            return typeof(T).GetProperties().Select(p => p.Name).ToList();
        }
    }
}
