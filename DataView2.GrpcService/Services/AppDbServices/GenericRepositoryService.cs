using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace DataView2.GrpcService.Services.AppDbServices
{
    public interface IGenericRepositoryService<T> where T : class
    {
        Task<(List<T>, int)> GetPagedData(int page, int pageSize);
        List<string> GetColumns();
    }

    public class GenericRepositoryService<T, TContext> : IGenericRepositoryService<T> where T : class where TContext : DbContext
    {

        private readonly AppDbContextProjectData _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepositoryService(AppDbContextProjectData context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<(List<T>, int)> GetPagedData(int page, int pageSize)
        {
            int totalCount = await _dbSet.CountAsync();
            var data = await _dbSet.Skip(page * pageSize).Take(pageSize).ToListAsync();
            return (data, totalCount);
        }

        public async Task Update(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public List<string> GetColumns()
        {
            return typeof(T).GetProperties().Select(p => p.Name).ToList();
        }
    }
}
