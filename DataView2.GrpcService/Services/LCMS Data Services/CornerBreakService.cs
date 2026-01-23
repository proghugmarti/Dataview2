using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System;
using System.Text.Json;
using System.Xml;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class CornerBreakService : BaseService<LCMS_Corner_Break, IRepository<LCMS_Corner_Break>>, ICornerBreakService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public CornerBreakService(IRepository<LCMS_Corner_Break> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public double getDoubleFromNode(XmlNode doc, string fieldName)
        {
            double result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Math.Round(Convert.ToDouble(xmlselNode.InnerText), 2);
            }

            return result;
        }

        List<LCMS_Corner_Break> xmlCornerBreakList = new List<LCMS_Corner_Break>();

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}
		public async Task<LCMS_Corner_Break> UpdateQueryAsync(LCMS_Corner_Break_Columns row, CallContext context = default)
        {
            try
            {               
                var entity = new LCMS_Corner_Break
                {
                    SurveyId = null,
                    SurveyDate = DateTime.MinValue,
                    Chainage = 0.0,
                    LRPNumStart = null,
                    LRPChainageStart = null,
                    PavementType = null,
                    CornerId = default,
                    QuarterId = default,
                    AvgDepth_mm = default,
                    Area_mm2 = default,
                    BreakArea_mm2 = default,
                    CNR_SpallingArea_mm2 = default,
                    AreaRatio = default, 
                    ImageFileIndex = null,
                    GPSLatitude = default,
                    GPSLongitude = default,
                    GPSAltitude = default,
                    GPSTrackAngle = default,
                    GeoJSON = null,
                    QCAccepted = false,
                    RoundedGPSLatitude = default,
                    RoundedGPSLongitude = default,
                    SegmentId = default,
                    Id = Convert.ToInt32(row.Key)
                };

                return  await _repository.UpdateSQLAsync(entity, row.FieldsToUpdate, Convert.ToInt32(row.Key));
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when update LCMS_Corner_Break by columns: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<LCMS_Corner_Break>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Corner_Break.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Corner_Break>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Corner_Break.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }
        public async Task<LCMS_Corner_Break> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Corner_Break();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
