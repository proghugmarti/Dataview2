
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.ServiceModel;
using ProtoBuf.Grpc;
using Google.Protobuf.WellKnownTypes;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.Other;
using DataView2.Core.Helper;
using System.Text.Json.Serialization;
using Esri.ArcGISRuntime.Geometry;
using Serilog;


namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Segment: IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string SurveyId { get; set; }

        [DataMember(Order = 3)]
        public string SectionId { get; set; }

        [DataMember(Order = 4)]
        public string ImageFilePath { get; set; }

        [DataMember(Order = 5)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 6)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 7)]
        public double GPSAltitude { get; set; }

        [DataMember(Order = 8)]
        public double GPSTrackAngle { get; set;}

        [DataMember(Order = 9)]
        public string PavementType { get; set; }

        [DataMember(Order =10)]
        public string GeoJSON { get; set; }

        [DataMember(Order = 11)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 12)]
        public double RoundedGPSLongitude { get; set; }

        [DataMember(Order = 13)]
        public int SegmentId { get; set; }
        [DataMember(Order = 14)]
        public double Width { get; set; }
        [DataMember(Order = 15)]
        public double Height { get; set; }
        [DataMember(Order = 16)]
        public double? PCI { get; set; }
        [DataMember(Order = 17)]
        public double? Paser { get; set; }
        [DataMember(Order = 18)]
        public int? PickoutCount { get; set; }
        [DataMember(Order = 19)]
        public double? PickoutAvgPer_m2 { get; set; }
        [DataMember(Order = 20)]
        public double? CrackingTotalLengthAllNodes_mm { get; set; }
        [DataMember(Order = 21)]
        public double? CrackingTotalLengthLowSevNodes_mm { get; set; }
        [DataMember(Order = 22)]
        public double? CrackingTotalLengthMedSevNodes_mm { get; set; }
        [DataMember(Order = 23)]
        public double? CrackingTotalLengthHighSevNodes_mm { get; set; }
        [DataMember(Order = 24)]
        public double? CrackClassificationTotalLengthLongCracks_mm { get; set; }
        [DataMember(Order = 25)]
        public double? CrackClassificationTotalLengthTransCracks_mm { get; set; }
        [DataMember(Order = 26)]
        public double? CrackClassificationTotalAreaFatigueCracks_m2 { get; set; }
        [DataMember(Order = 27)]
        public double? CrackClassificationTotalLengthOtherCracks_mm { get; set; }
        [DataMember(Order = 28)]
        public double? RavellingTotalArea_m2 { get; set; }
        [DataMember(Order = 29)]
        public double? RavellingSeverity { get; set; }
        [DataMember(Order = 30)]
        public int? PotholesCount { get; set; }
        [DataMember(Order = 31)]
        public double? PatchesArea_m2 { get; set; }
        [DataMember(Order = 32)]
        public int? MmoCount { get; set; }
        [DataMember(Order = 33)]
        public double? PumpingArea_m2 { get; set; }
        [DataMember(Order = 34)]
        public double? IRIAverage { get; set; }
        [DataMember(Order = 35)]
        public double? RutAverage { get; set; }
        [DataMember(Order = 36)]
        public double? SealedCrackTotalLength_mm { get; set; }
        [DataMember(Order = 37)]
        public double? ShoveTotalLength_mm { get; set; }
        [DataMember(Order = 38)]
        public double? GeometryAvgCrossSlope { get; set; }
        [DataMember(Order = 39)]
        public double? GeometryAvgGradient { get; set; }
        [DataMember(Order = 40)]
        public double? GeometryAvgHorizontalCurvature { get; set; }
        [DataMember(Order = 41)]
        public double? GeometryAvgVerticalCurvature { get; set; }
        [DataMember(Order = 42)]
        public double? BleedingTotalArea_m2 { get; set; }
        [DataMember(Order = 43)]
        public double? BleedingSeverity { get; set; }
        [DataMember(Order = 44)]
        public double? SagsTotalArea_m2 { get; set; }
        [DataMember(Order = 45)]
        public double? BumpsTotalArea_m2 { get; set; }
        [DataMember(Order = 46)]
        public double? AverageMPD_mm { get; set; }
        [DataMember(Order = 47)]
        public double? AverageMTD_mm { get; set; }
        [DataMember(Order = 48)]
        public double Chainage { get; set; } = 0.0;
        [DataMember(Order = 49)]
        public double LaneWidth { get; set; }
        [DataMember(Order = 50)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [DataContract]
    public class SurveyAndSegmentRequest
    {
        [DataMember(Order = 1)]
        public string SurveyId { get; set; }

        [DataMember(Order = 2)]
        public int SegmentId { get; set; }
    }

    [DataContract]
    public class SurveyRequest
    {
        [DataMember(Order = 1)]
        public string SurveyId { get; set; }
    }


    [DataContract]
    public class SegmentMovementRequest
    {
        [DataMember(Order = 1)]
        public List<int> SegmentIds { get; set; }

        [DataMember(Order = 2)]
        public string SurveyId { get; set; }

        [DataMember(Order = 3)]
        public int SegmentId { get; set; }

        [DataMember(Order = 4)]
        public string? FilePath { get; set; }
        [DataMember(Order = 5)]
        public double HorizontalOffset { get; set; }

        [DataMember(Order = 6)]
        public double VerticalOffset { get; set; }

        [DataMember(Order = 7)]
        public string? GraphicType { get; set; }

        [DataMember(Order = 8)]
        public string? PointList { get; set; }
        
    }

    [DataContract]
    public class DefectMovementRequest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Table { get; set; }
        [DataMember(Order = 3)]
        public double HorizontalOffset { get; set; }
        [DataMember(Order = 4)]
        public double VerticalOffset { get; set; }
    }

    [DataContract]
    public class ChainageUpdateRequest
    {
       
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }

        [DataMember(Order = 3)]
        public double ChainageDifference { get; set; }

    }

    [DataContract]
    public class DeletionInfo
    {
        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Table { get; set; }
    }

    [DataContract]
    public class OffsetData
    {
        [DataMember(Order = 1)]
        public List<string> SurveyIds { get; set; }
        [DataMember(Order = 2)]
        public List<string> Defects { get; set; }
        [DataMember(Order = 3)] 
        public double HorizontalOffset { get; set; }
        [DataMember(Order = 4)]
        public double VerticalOffset { get; set; }
    }

    [DataContract]
    public class KeyValueField
    {
        [DataMember(Order = 1)]
        public string Key { get; set; }

        [DataMember(Order = 2)]
        public string Value { get; set; }

        [DataMember(Order = 3)]
        public string Type { get; set; } // Add type information (string, number, date)
    }

    [DataContract]
    public class IdTableRequest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Table { get; set; }
    }

    [DataContract]
    public class QueryResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 3)]
        public string SurveyId { get; set; }
        [DataMember(Order = 4)]
        public int SegmentId { get; set; }
    }

    [DataContract]
    public class PCIAutofillDefectRequest
    {
        [DataMember(Order = 1)]
        public string Coordinates { get; set; }

        [DataMember(Order = 2)]
        public string PavementType { get; set; }
    }

    [DataContract]
    public class ChainageMapPointRequest
    {
        [DataMember(Order = 1)]
        public double Latitude { get; set; }

        [DataMember(Order = 2)]
        public double Longitude { get; set; }
        [DataMember(Order = 3)]
        public int SegmentId { get; set; }

        [DataMember(Order = 4)]
        public string SurveyId { get; set; }
    }

    [DataContract]
    public class ChainageReply
    {
        [DataMember(Order = 1)]
        public double Chainage { get; set; }
    }

    public class SegmentPointInfo
    {
        public int SegmentId { get; set; }
        public MapPoint MapPoint { get; set; }
        public double Chainage { get; set; }  // chainage value for this point
    }

   

    public class DynamicJoinProperties
    {
        public System.Type Table1Type { get; set; }
        public System.Type Table2Type { get; set; }
        public string PropertyName { get; set; }
        public List<string> Table2Columns { get; set; }
        public string SurveyIdColumnName { get; set; }
        public List<string>? SurveyIds { get; set; }
    }

    [ServiceContract]
    public interface ISegmentService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Segment request, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Segment>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Segment> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<LCMS_Segment> EditValue(LCMS_Segment request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Segment request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Segment>> QueryAsync(string predicate);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<CountReply> GetCountAsyncBySurveyId(string surveyId);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Segment>> GetWithinRange(CoordinateRangeRequest coordinateRequest);
        [OperationContract]
        Task<IdReply> UpdateSegmentOffsetInDB(SegmentMovementRequest movementRequest, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteDefectsWithQueryAsync(List<DeletionInfo> deletionInfos);

        [OperationContract]
        Task UpdateDefectsOnly(List<DefectMovementRequest> requests);

        [OperationContract]
        Task<List<LCMS_Segment>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IdReply> UpdateOffsetBySurvey(OffsetData offsetData);
        [OperationContract]
        Task<IdReply> SaveUserDefinedDefect(List<KeyValueField> defectFields, CallContext context = default);
        [OperationContract]
        Task<IdReply> UpdateImagePathInSegmentAndSurvey(List<string> imageFilePaths);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<IdReply> ExecuteSQlQueries(List<string> queries);
        [OperationContract]
        Task<List<int>> ExecuteQueryAndReturnIds(string query);
        [OperationContract]
        Task<List<SummaryItem>> GetNumericValueWithinBoundary(SummaryRequest request);
        [OperationContract]
        Task<List<KeyValueField>> ExtractAttributes(IdTableRequest request, CallContext context = default);
        [OperationContract]
        Task<string> GetDynamicDataAsync(string tableNameWithSurveyId);
        [OperationContract]
        Task<string> GetDynamicJoinAsync(DynamicJoinProperties dynamicJoinProperties);
        [OperationContract]
        Task<LCMS_Segment> UpdateGenericData(string fieldsToUpdateSerialized);
        [OperationContract]
        Task<List<QueryResponse>> ExecuteQueryAndReturnGeoJSON(string query);
        [OperationContract]
        Task<List<PCIDefects>> CalculateAutofillPCIDefects(PCIAutofillDefectRequest request);
        [OperationContract]
        Task<IdReply> UpdateSegmentChainageInDB(ChainageUpdateRequest chainageRequest, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Segment>> CalculateSegmentSummaryFromMap(List<SurveyAndSegmentRequest> request);
        [OperationContract]
        Task<ChainageReply> GetChainageFromSegmentIdToMapPoint(ChainageMapPointRequest chainageMapPointRequest);

       
    }
}
