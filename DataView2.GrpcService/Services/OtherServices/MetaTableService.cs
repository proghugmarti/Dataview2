using Azure.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Data.Projects;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProtoBuf.Grpc;
using System.Data.Entity;
using System.Reflection;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class MetaTableService : BaseService<MetaTableValue, IRepository<MetaTableValue>>, IMetaTableService
    {
        //private readonly AppDbContextMetadata _metadataContext;
        private readonly AppDbContextProjectData _projectContext;
        private readonly AppDbContextMetadataLocal _metadataLocalContext;


        public MetaTableService(IRepository<MetaTableValue> repository, AppDbContextMetadata metadataContext, AppDbContextProjectData projectContext, AppDbContextMetadataLocal metadataLocalContext) : base(repository)
        {
            //_metadataContext = metadataContext;
            _projectContext = projectContext;
            _metadataLocalContext = metadataLocalContext;
        }


        public async Task<List<MetaTable>> HasData(Empty empty, CallContext context)
        {
            try
            {
                var tableIds = _projectContext.MetaTableValue.Select(m => m.TableId).Distinct().ToList();

                if (tableIds.Any())
                {
                    var tableNames = _metadataLocalContext.MetaTable
                              .Where(mt => tableIds.Contains(mt.Id))
                              .ToList();

                    return tableNames;
                }
                else
                {
                    return new List<MetaTable>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<MetaTable>();
            }
        }

        public async Task<List<MetaTable>> HasNoData(Empty empty, CallContext context)
        {
            try
            {
                var tableIds = _projectContext.MetaTableValue.Select(m => m.TableId).Distinct().ToList();

                var tableNames = _metadataLocalContext.MetaTable
                            .Where(mt => !tableIds.Contains(mt.Id))
                            .ToList();
                return tableNames;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<MetaTable>();
            }
        }

        public async Task<List<MetaTableResponse>> GetByTable(MetaTable request, CallContext context = default)
        {
            try
            {
                var response = new List<MetaTableResponse>();
                var tableId = request.Id;
                var tableValues = _projectContext.MetaTableValue
                                    .Where(m => m.TableId == tableId)
                                    .ToList();
                if (!tableValues.Any())
                {
                    return new List<MetaTableResponse>();
                }

                var responses = tableValues.Select(tableValue =>
                {
                    var attributes = new List<KeyValueField>();
                    attributes.Add(new KeyValueField { Key = "Id", Type = "int", Value = tableValue.Id.ToString() });
                    for (int i = 1; i <= 25; i++) // Adjust the loop limit based on the maximum number of columns
                    {
                        var headerProperty = typeof(MetaTable).GetProperty($"Column{i}");
                        var typeProperty = typeof(MetaTable).GetProperty($"Column{i}Type");
                        var valueProperty = typeof(MetaTableValue).GetProperty($"StrValue{i}");
                        var decimalValueProperty = typeof(MetaTableValue).GetProperty($"DecValue{i}");

                        if (headerProperty != null && typeProperty != null)
                        {
                            var header = (string)headerProperty.GetValue(request);
                            var headerType = (string)typeProperty.GetValue(request);

                            if (header != null)
                            {
                                string value = null;
                                if (headerType == "Text" || headerType == "Date" || headerType == "Dropdown")
                                {
                                    if (valueProperty != null)
                                    {
                                        value = (string)valueProperty.GetValue(tableValue);
                                    }
                                }
                                else if (headerType == "Number" || headerType == "Measurement")
                                {
                                    if (decimalValueProperty != null)
                                    {
                                        var decimalValue = (decimal?)decimalValueProperty.GetValue(tableValue);
                                        value = decimalValue.HasValue ? decimalValue.Value.ToString() : null;
                                    }
                                }

                                if (value != null)
                                {
                                    attributes.Add(new KeyValueField
                                    {
                                        Key = header,
                                        Value = value,
                                        Type = headerType
                                    });
                                }
                            }
                        }
                    }

                    return new MetaTableResponse
                    {
                        TableId = tableId,
                        TableName = request.TableName,
                        Icon = request.Icon,
                        IconSize = request.IconSize,
                        GeoJSON = tableValue.GeoJSON,
                        SurveyId = tableValue.SurveyId,
                        SegmentId = tableValue.SegmentId,
                        GPSTrackAngle = tableValue.GPSTrackAngle,
                        Attributes = attributes,
                        GPSLatitude = tableValue.GPSLatitude,
                        GPSLongitude = tableValue.GPSLongitude,
                        LRPNumber = tableValue.LRPNumber,
                        Chainage = tableValue.Chainage,
                        ImageFileIndex = tableValue.ImageFileIndex
                    };
                }).ToList();

                return responses;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<MetaTableResponse>();
            }
        }

        public Task<List<MetaTable>> GetAllTables(Empty empty, CallContext context = default)
        {
            var metaTables = _metadataLocalContext.MetaTable.ToList();

            return Task.FromResult(metaTables);
        }

        public async Task<MetaTable> GetByName(string name, CallContext context = default)
        {
            var metaTable = _metadataLocalContext.MetaTable.Where(m => m.TableName == name).FirstOrDefault();
            if (metaTable != null)
            {
                return metaTable;
            }
            else
            {
                return new MetaTable();
            }
        }

        public async Task<IdReply> UpdateMetaTableIconAsync(MetaTable updatedMetaTable, CallContext context = default)
        {
            try
            {
                var existingMetaTable = _metadataLocalContext.MetaTable
                    .Where(m => m.TableName == updatedMetaTable.TableName)
                    .FirstOrDefault();

                if (existingMetaTable != null)
                {
                    existingMetaTable.Icon = updatedMetaTable.Icon;
                    existingMetaTable.IconSize = updatedMetaTable.IconSize;
                    _metadataLocalContext.MetaTable.Update(existingMetaTable);
                    await _metadataLocalContext.SaveChangesAsync();

                    return new IdReply { Id = 0, Message = "Icon updated successfully." };
                }
                else
                {
                    return new IdReply { Id = -1, Message = "no meta table found." };
                }
            }
            catch (Exception ex)
            {
                return new IdReply { Id = -1, Message = "Failed to update the icon. Please try again." };
            }
        }

        public async Task<IdReply> CreateMetaTable(MetaTable metaTable, CallContext context = default)
        {
            try
            {
                _metadataLocalContext.MetaTable.Add(metaTable);
                await _metadataLocalContext.SaveChangesAsync();

                return new IdReply { Id = metaTable.Id, Message = "MetaTable Saved." };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply { Id = -1, Message = "Error in saving metaTable." };
            }
        }

        public async Task<IdReply> EditMetaTable(MetaTable request, CallContext context = default)
        {
            try
            {
                //Find existing metaTable
                var existingMetaTable = _metadataLocalContext.MetaTable
                                   .Where(m => m.Id == request.Id)
                                   .FirstOrDefault();

                if (existingMetaTable != null)
                {
                    foreach (var property in typeof(MetaTable).GetProperties())
                    {
                        if (property.Name.StartsWith("Column")) // Only target Column-related properties
                        {
                            var newValue = property.GetValue(request);
                            property.SetValue(existingMetaTable, newValue);
                        }
                    }
                }

                await _metadataLocalContext.SaveChangesAsync();

                Console.WriteLine("Successfully edited metaTable schema.");
                return new IdReply { Id = 0, Message = "Successfully edited." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error in EditValue from MetaTableService. {ex.Message}");
                return new IdReply { Id = -1, Message = ex.Message };
            }
        }

        public async Task<IdReply> DeleteMetaTable(MetaTable request, CallContext context = default)
        {
            try
            {
                var existingMetaTable = _metadataLocalContext.MetaTable
                                    .Where(m => m.Id == request.Id)
                                    .FirstOrDefault();
                if (existingMetaTable != null)
                {
                    //delete metaTable
                    _metadataLocalContext.Remove(existingMetaTable);
                    await _metadataLocalContext.SaveChangesAsync();

                    //delete associated metaTalbeValues
                    var associatedMetaTables = _projectContext.MetaTableValue.Where(m => m.TableId == existingMetaTable.Id).ToList();
                    _projectContext.RemoveRange(associatedMetaTables);
                    await _projectContext.SaveChangesAsync();

                    return new IdReply { Id = 0, Message = "Successfully deleted" };
                }

                return new IdReply { Id = -1, Message = "No metaTable found" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error in Delete from MetaTableService. {ex.Message}");
                return new IdReply { Id = -1, Message = ex.Message };
            }
        }

        public async Task<List<string>> GetExistingMetaTableNamesBySurvey(string surveyId)
        {
            var metaTableValues = _projectContext.MetaTableValue.Where(data => data.SurveyId == surveyId).ToList();
            if (metaTableValues != null && metaTableValues.Count > 0)
            {
                var tableNames = metaTableValues.Select(m => m.TableName).Distinct().ToList();
                return tableNames;
            }

            return new List<string>();
        }

        public async Task<MetaTableValue> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);

            var entity = _projectContext.MetaTableValue
                                   .Where(m => m.Id == Convert.ToInt32(fieldsToUpdate["Id"]))
                                  .FirstOrDefault();

            var existingMetaTable = _metadataLocalContext.MetaTable
                                   .Where(m => m.TableName == entity.TableName)
                                   .FirstOrDefault();

            foreach (var item in fieldsToUpdate)
            {
                if (item.Key != "Id" && item.Key != "SurveyId" && item.Key != "SegmentId")
                {
                    if (existingMetaTable != null)
                    {
                        string propertyName = GetPropertyNameByValue(existingMetaTable, item.Key);
                        object? dType = GetPropertyValue(existingMetaTable, propertyName + "Type");
                        if (dType != null)
                        {
                            switch (dType.ToString())
                            {
                                case "Text":
                                case "Date":
                                case "Dropdown":
                                    SetPropertyValue(entity, "Str" + propertyName.Replace("Column", "Value"), item.Value);
                                    break;

                                default:
                                    SetPropertyValue(entity, "Dec" + propertyName.Replace("Column", "Value"), item.Value);
                                    break;
                            }
                        }
                    }
                }
            }
            return await _repository.UpdateAsync(entity);
        }

        private string? GetPropertyNameByValue<T>(T obj, object value)
        {
            PropertyInfo? property = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.GetValue(obj)?.Equals(value) == true);

            return property?.Name;
        }

        private object? GetPropertyValue<T>(T obj, string propertyName)
        {
            PropertyInfo? property = typeof(T).GetProperty(propertyName);
            return property?.GetValue(obj);
        }

        private void SetPropertyValue<T>(T obj, string propertyName, object value)
        {
            try
            {
                PropertyInfo? property = typeof(T).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    System.Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    object? convertedValue = value == null ? null : Convert.ChangeType(value, targetType);
                    property.SetValue(obj, convertedValue);
                }
            }
            catch(Exception ex) 
            {
                Serilog.Log.Error($"Error in SetPropertyValue for {propertyName}, value {value}, error {ex.Message}");
            }
        }
    }
}
