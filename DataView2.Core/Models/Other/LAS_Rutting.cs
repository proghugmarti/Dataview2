using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class LAS_Rutting
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
        public double RutDepth_mm { get; set; }

        [DataMember(Order = 5)]
        public double RutWidth_m  { get; set; }

        [DataMember(Order = 6)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 7)]
        public double GPSLongitude { get; set; }

        [DataMember(Order = 8)]
        public string GeoJSON { get; set; }

    }

    [DataContract]
    public class LasRuttingRecalculateRequest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public double NewDepthFactor { get; set; }
    }

    public class OldFormatLAS_Rutting
    {
        public int Id { get; set; }
        public double RutDepth_mm { get; set; }
        public double RutWidth_mm { get; set; }
        public string TableName { get; set; }
        public string GeoJSON { get; set; }
        public string SurveyId { get; set; }
        public double GPSTrackAngle { get; set; }
        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double Chainage { get; set; }
        public string ImageFileIndex { get; set; }
        public double GPSLatitude_ITRF96_3 { get; set; }
        public double GPSLongitude_ITRF96_3 { get; set; }
        public string SurveyIdExternal { get; set; }
    }


    [ServiceContract]
    public interface ILAS_RuttingService 
    {
        [OperationContract] 
        Task<IdReply> Create (LAS_Rutting rutting);

        [OperationContract]
        Task<List<LAS_Rutting>> GetAll(Empty empty);

        [OperationContract]
        Task<List<LAS_Rutting>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteById(IdRequest id);

        [OperationContract]
        Task<IdReply> ProcessLASRuttingFiles(List<LASfileRequest> requests);

        [OperationContract]
        Task<IdReply> UpdateRecalculatedById(LasRuttingRecalculateRequest idRequest);
    }
}
