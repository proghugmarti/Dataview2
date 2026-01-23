using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class MetaTableValue : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }
        [DataMember(Order = 3)]
        public int SegmentId { get; set; }
        [DataMember(Order = 4)]
        public long? LRPNumber { get; set; }
        [DataMember(Order = 5)]
        public double Chainage { get; set; } = 0.0;
        [DataMember(Order = 6)]
        public int TableId { get; set; }
        [DataMember(Order = 7)]
        public string TableName { get; set; }
        [DataMember(Order = 8)]
        public string? ImageFileIndex { get; set; }
        [DataMember(Order = 9)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 10)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 11)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 12)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 13)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 14)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 15)]
        public string GeoJSON { get; set; }
        // StrValue columns 1 to 25
        [DataMember(Order = 16)]
        public string? StrValue1 { get; set; }
        [DataMember(Order = 17)]
        public string? StrValue2 { get; set; }
        [DataMember(Order = 18)]
        public string? StrValue3 { get; set; }
        [DataMember(Order = 19)]
        public string? StrValue4 { get; set; }
        [DataMember(Order = 20)]
        public string? StrValue5 { get; set; }
        [DataMember(Order = 21)]
        public string? StrValue6 { get; set; }
        [DataMember(Order = 22)]
        public string? StrValue7 { get; set; }
        [DataMember(Order = 23)]
        public string? StrValue8 { get; set; }
        [DataMember(Order = 24)]
        public string? StrValue9 { get; set; }
        [DataMember(Order = 25)]
        public string? StrValue10 { get; set; }
        [DataMember(Order = 26)]
        public string? StrValue11 { get; set; }
        [DataMember(Order = 27)]
        public string? StrValue12 { get; set; }
        [DataMember(Order = 28)]
        public string? StrValue13 { get; set; }
        [DataMember(Order = 29)]
        public string? StrValue14 { get; set; }
        [DataMember(Order = 30)]
        public string? StrValue15 { get; set; }
        [DataMember(Order = 31)]
        public string? StrValue16 { get; set; }
        [DataMember(Order = 32)]
        public string? StrValue17 { get; set; }
        [DataMember(Order = 33)]
        public string? StrValue18 { get; set; }
        [DataMember(Order = 34)]
        public string? StrValue19 { get; set; }
        [DataMember(Order = 35)]
        public string? StrValue20 { get; set; }
        [DataMember(Order = 36)]
        public string? StrValue21 { get; set; }
        [DataMember(Order = 37)]
        public string? StrValue22 { get; set; }
        [DataMember(Order = 38)]
        public string? StrValue23 { get; set; }
        [DataMember(Order = 39)]
        public string? StrValue24 { get; set; }
        [DataMember(Order = 40)]
        public string? StrValue25 { get; set; }

        // DecValue columns 1 to 25
        [DataMember(Order = 41)]
        public decimal? DecValue1 { get; set; }
        [DataMember(Order = 42)]
        public decimal? DecValue2 { get; set; }
        [DataMember(Order = 43)]
        public decimal? DecValue3 { get; set; }
        [DataMember(Order = 44)]
        public decimal? DecValue4 { get; set; }
        [DataMember(Order = 45)]
        public decimal? DecValue5 { get; set; }
        [DataMember(Order = 46)]
        public decimal? DecValue6 { get; set; }
        [DataMember(Order = 47)]
        public decimal? DecValue7 { get; set; }
        [DataMember(Order = 48)]
        public decimal? DecValue8 { get; set; }
        [DataMember(Order = 49)]
        public decimal? DecValue9 { get; set; }
        [DataMember(Order = 50)]
        public decimal? DecValue10 { get; set; }
        [DataMember(Order = 51)]
        public decimal? DecValue11 { get; set; }
        [DataMember(Order = 52)]
        public decimal? DecValue12 { get; set; }
        [DataMember(Order = 53)]
        public decimal? DecValue13 { get; set; }
        [DataMember(Order = 54)]
        public decimal? DecValue14 { get; set; }
        [DataMember(Order = 55)]
        public decimal? DecValue15 { get; set; }
        [DataMember(Order = 56)]
        public decimal? DecValue16 { get; set; }
        [DataMember(Order = 57)]
        public decimal? DecValue17 { get; set; }
        [DataMember(Order = 58)]
        public decimal? DecValue18 { get; set; }
        [DataMember(Order = 59)]
        public decimal? DecValue19 { get; set; }
        [DataMember(Order = 60)]
        public decimal? DecValue20 { get; set; }
        [DataMember(Order = 61)]
        public decimal? DecValue21 { get; set; }
        [DataMember(Order = 62)]
        public decimal? DecValue22 { get; set; }
        [DataMember(Order = 63)]
        public decimal? DecValue23 { get; set; }
        [DataMember(Order = 64)]
        public decimal? DecValue24 { get; set; }
        [DataMember(Order = 65)]
        public decimal? DecValue25 { get; set; }

        [DataMember(Order = 66)]
        public string? PavementType { get; set; } // Default column from IEntity
    }

    [DataContract]
    public class MetaRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        [DataMember(Order = 2)]
        public string GeoType { get; set; }
        [DataMember(Order = 3)]
        public string? Icon { get; set; }

        [DataMember(Order = 4)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 5)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 6)]
        public double GPSTrackAngle { get; set; }

        [DataMember(Order = 7)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 8)]
        public string SurveyId { get; set; }
        [DataMember(Order = 9)]
        public int SegmentId { get; set; }
        [DataMember(Order = 10)]
        public List<ColumnData> ColumnDatas { get; set; }
    }

    [DataContract]
    public class ColumnData
    {
        [DataMember(Order = 1)]
        public string ColumnName { get; set; }
        [DataMember(Order = 2)]
        public string ColumnType { get; set; }
        [DataMember(Order = 3)]
        public string ColumnValue { get; set; }
    }

    [DataContract]
    public class MetaTableResponse
    {
        [DataMember(Order = 1)]
        public int TableId { get; set; }

        [DataMember(Order = 2)]
        public string TableName { get; set; }

        [DataMember(Order = 3)]
        public string? Icon { get; set; }
        [DataMember(Order = 4)]
        public int? IconSize { get; set; }

        [DataMember (Order = 5)]
        public string GeoJSON { get; set; }
        [DataMember (Order = 6)]
        public string SurveyId { get; set; }
        [DataMember(Order = 7)]
        public int SegmentId { get; set; }
        [DataMember(Order = 8)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 9)]
        public List<KeyValueField> Attributes { get; set; }
        [DataMember(Order = 10)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 11)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 12)]
        public long? LRPNumber { get; set; }
        [DataMember(Order = 13)]
        public double? Chainage { get; set; }
        [DataMember(Order = 14)]
        public string? ImageFileIndex { get; set; }
    
    }

    [ServiceContract]
    public interface IMetaTableService
    {
        [OperationContract]
        Task<IdReply> Create(MetaTableValue request, CallContext context = default);
        [OperationContract]
        Task<MetaTableValue> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<List<MetaTable>> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<MetaTable>> HasNoData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<MetaTableResponse>> GetByTable(MetaTable request, CallContext context = default);
        [OperationContract]
        Task<List<MetaTable>> GetAllTables(Empty empty, CallContext context = default);
        [OperationContract]
        Task<MetaTable> GetByName(string name, CallContext context = default);
        [OperationContract]
        Task<IdReply> UpdateMetaTableIconAsync(MetaTable updatedMetaTable, CallContext context = default);
        [OperationContract]
        Task<IdReply> CreateMetaTable(MetaTable metaTable, CallContext context = default);
        [OperationContract]
        Task<IdReply> EditMetaTable(MetaTable request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteMetaTable(MetaTable request, CallContext context = default);
        [OperationContract]
        Task<List<string>> GetExistingMetaTableNamesBySurvey(string surveyId);
        [OperationContract]
        Task<MetaTableValue> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
