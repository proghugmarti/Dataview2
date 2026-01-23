using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Rut_Processed : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string SurveyId { get; set; }

        [DataMember(Order = 3)]
        [Column(TypeName = "datetime")]
        public DateTime? SurveyDate { get; set; }

        [DataMember(Order = 4)]
        public double Chainage { get; set; }

        [DataMember(Order = 5)]
        public long? LRPNumber { get; set; }

        [DataMember(Order = 6)]
        public double? LRPChainage { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public int RutId { get; set; }

        [DataMember(Order = 9)]
        public double? LeftDepth_mm { get; set; }

        [DataMember(Order = 10)]
        public double? LeftWidth_mm { get; set; }

        [DataMember(Order = 11)]
        public double? LeftCrossSection { get; set; }

        [DataMember(Order = 12)]
        public int? LeftType { get; set; }

        [DataMember(Order = 13)]
        public int? LeftMethod { get; set; }
        [DataMember(Order = 14)]
        public double? LeftPercentDeformation { get; set; }
        [DataMember(Order = 15)]
        public int? LeftValid { get; set; }
        [DataMember(Order = 16)]
        public double? LeftInvalidRatioData { get; set; }
        [DataMember(Order = 17)]
        public double? RightDepth_mm { get; set; }
        [DataMember(Order = 18)]
        public double? RightWidth_mm { get; set; }

        [DataMember(Order = 19)]
        public double? RightCrossSection { get; set; }

        [DataMember(Order = 20)]
        public int? RightType { get; set; }

        [DataMember(Order = 21)]
        public int? RightMethod { get; set; }
        [DataMember(Order = 22)]
        public double? RightPercentDeformation { get; set; }
        [DataMember(Order = 23)]
        public int? RightValid { get; set; }
        [DataMember(Order = 24)]
        public double? RightInvalidRatioData { get; set; }
        [DataMember(Order = 25)]
        public double? LaneDepth_mm { get; set; }
        [DataMember(Order = 26)]
        public double? LaneWidth_mm { get; set; }
        [DataMember(Order = 27)]
        public string ImageFileIndex { get; set; }

        [DataMember(Order = 28)]
        public string GeoJSON { get; set; } //lane Geojson
        [DataMember(Order = 29)]
        public string LwpGeoJSON { get; set; }
        [DataMember(Order = 30)]
        public string RwpGeoJSON { get; set; }

        [DataMember(Order = 31)]
        public double GPSLatitude { get; set; } = 0.0; //Start

        [DataMember(Order = 32)]
        public double GPSLongitude { get; set; } = 0.0; //Start

        [DataMember(Order = 33)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 34)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 35)]
        public double EndGPSLatitude { get; set; }
        [DataMember(Order = 36)]
        public double EndGPSLongitude { get; set; }
        [DataMember(Order = 37)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 38)]
        public double RoundedGPSLongitude { get; set; }
        
        [DataMember(Order = 39)]
        public int SegmentId { get; set; }

        [DataMember(Order = 40)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IRutProcessedService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Rut_Processed request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Rut_Processed>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Rut_Processed> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<LCMS_Rut_Processed> EditValue(LCMS_Rut_Processed request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, 
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Rut_Processed request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Rut_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Rut_Processed>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<LCMS_Rut_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
