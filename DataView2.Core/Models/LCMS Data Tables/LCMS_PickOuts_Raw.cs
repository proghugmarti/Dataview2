
using DataView2.Core.Models.Other;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_PickOuts_Raw : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }
        [DataMember(Order = 3)]
        [Column(TypeName = "datetime")]
        public DateTime SurveyDate { get; set; }
        [DataMember(Order = 4)]
        public double Chainage { get; set; }
        [DataMember(Order = 5)]
        public long? LRPNumStart { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainageStart { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public int PickOutId { get; set; }
        [DataMember(Order = 9)]
        public double Area_mm2 { get; set; }
        [DataMember(Order = 10)]
        public double MaxDepth_mm { get; set; }
        [DataMember(Order = 11)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 12)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 13)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 14)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 15)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 16)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 17)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 18)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; } = false;

        [DataMember(Order = 19)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 20)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 21)]
        public int SegmentId { get; set; }
        [DataMember(Order = 22)]
        public double ChainageEnd { get; set; } = 0.0;

    }

    [DataContract]
    public class UpdateCoordinatesRequest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 3)]
        public double GPSLongitude { get; set; }

        [DataMember(Order = 4)]
        public double? Zoom { get; set; }
    }

    [DataContract]
    public class IdRequest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [ServiceContract]
    public interface IPickOutRawService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_PickOuts_Raw request,
            CallContext context = default);

        [OperationContract]
        Task<List<LCMS_PickOuts_Raw>> GetAll(Empty empty,
            CallContext context = default);
        [OperationContract]
        Task<LCMS_PickOuts_Raw> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<LCMS_PickOuts_Raw> EditValue(LCMS_PickOuts_Raw request,
                CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request,
         CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_PickOuts_Raw request,
                CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_PickOuts_Raw>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_PickOuts_Raw>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_PickOuts_Raw>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_PickOuts_Raw>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_PickOuts_Raw> UpdateGenericData(string fieldsToUpdateSerialized);

    }
}


