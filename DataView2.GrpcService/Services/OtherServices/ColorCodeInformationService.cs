using DataView2.Core.Models;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProtoBuf.Grpc;
using System.Drawing.Imaging;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class ColorCodeInformationService : IColorCodeInformationService
    {
        private readonly IRepository<ColorCodeInformation> _repository;

        public ColorCodeInformationService(IRepository<ColorCodeInformation> repository) 
        {
            _repository = repository;
        }

        public async Task<IdReply> CreateRange(List<ColorCodeInformation> request, CallContext context = default)
        {
            try
            {
                var allEntities = await _repository.GetAllAsync();
                if (allEntities != null && allEntities.Count() > 0)
                {
                    var toCreate = new List<ColorCodeInformation>();

                    foreach (var item in request)
                    {
                        var existing = allEntities.FirstOrDefault(x =>
                           x.TableName == item.TableName &&
                           x.Property == item.Property &&
                           x.StringProperty == item.StringProperty);

                        if (existing == null)
                        {
                            toCreate.Add(item);
                        }
                    }

                    if (toCreate.Count > 0)
                    {
                        await _repository.CreateRangeAsync(toCreate);
                    }
                }
                else
                {
                    await _repository.CreateRangeAsync(request);
                }
                return new IdReply { Id = 1, Message = "Color code added successfully." };
            }
            catch (Exception ex)
            {
                return new IdReply { Id = -1, Message = $"Error in creating color coding: {ex.Message}" };
            }
        }

        public async Task<IdReply> DeleteByTableName(string tableName, CallContext context = default)
        {
            try
            {
                var colorCodes = await _repository.FindAsync(cc => cc.TableName == tableName);
                if (colorCodes != null && colorCodes.Any())
                {
                    await _repository.DeleteRangeAsync(colorCodes);

                    return new IdReply
                    {
                        Id = 1,
                        Message = $"All color codes with tableName '{tableName}' have been deleted."
                    };
                }
                else
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = $"No color codes found with tableName '{tableName}'."
                    };
                }
            }
            catch (Exception ex)
            {
                return new IdReply { Id = -1, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<List<ColorCodeInformation>> GetByName(string tableName, CallContext context = default)
        {
            var colorCodes = await _repository.FindAsync(cc => cc.TableName == tableName);
            if (colorCodes != null && colorCodes.Any())
            {
                return colorCodes.ToList();
            }
            return new List<ColorCodeInformation>();
        }

        public async Task<List<ColorCodeInformation>> GetAll(Empty empty, CallContext context = default)
        {
            var colorCodes = await _repository.GetAllAsync();
            if (colorCodes != null && colorCodes.Any())
            {
                return colorCodes.ToList();
            }
            return new List<ColorCodeInformation>();
        }
    }
}
