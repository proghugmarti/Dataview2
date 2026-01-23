using Dapper;
using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.QC;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Xml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace DataView2.GrpcService.Services
{
    public class QCFilterService : IQCFilterService
    {
        private readonly IRepository<QCFilter> _repository;
        private readonly AppDbContextProjectData _context;

        public QCFilterService(IRepository<QCFilter> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IdReply> Create(QCFilter request, CallContext context = default)
        {
            try
            {
                var entity = await _repository.CreateAsync(request);
                return new IdReply
                {
                    Id = entity.Id,
                    Message = "Filter created Successfully."
                };
            }
            catch
            (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply { };

        }

        public async Task<QCFilter> GetById(IdRequest request, CallContext context = default)
        {
            var id = request.Id;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                // Handle the case where the entity with the specified ID is not found
                throw new KeyNotFoundException($"Entity with ID {id} not found.");
            }

            return entity;
        }

        public async Task<List<QCFilter>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<QCFilter>(entities);
        }

        public async Task<QCFilter> EditValue(QCFilter request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<IdReply> DeleteObject(QCFilter request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Selected filter deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = request.Id,
                Message = "Selecteed record is failed to be deleted."
            };

        }

        public async Task<IEnumerable<QCFilter>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.QCFilter.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<QCFilter>();
            }
        }
    }
}
