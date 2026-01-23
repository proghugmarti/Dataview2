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
    public class LCMS_Rumble_Strip: IEntity
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
     
        public long? LRPNumStart { get; set; }
        [DataMember(Order = 5)]
        public double? LRPChainageStart { get; set; }
        [DataMember(Order = 6)]
        public long? LRPNumEnd { get; set; }
        [DataMember(Order = 7)]
        public double? LRPChainageEnd { get; set; }
        [DataMember(Order = 8)]
        public string PavementType { get; set; }
        [DataMember(Order = 9)]
        public int RumbleStripId { get; set; }
        [DataMember(Order = 10)]
        public string Type { get; set; }
        [DataMember(Order = 11)]
        public double Length_mm { get; set; } 
        [DataMember(Order = 12)]
        public double Area_mm2 { get; set; } 
        [DataMember(Order = 13)]
        public int NumStrip { get; set; }
        [DataMember(Order = 14)]
        public double StripPerMeter { get; set; }
        [DataMember(Order = 15)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 16)]
        public double AvgHeight_mm { get; set; }
        [DataMember(Order = 17)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 18)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 19)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 20)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 21)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 22)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 23)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 24)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 25)]
        public int SegmentId {  get; set; }
        [DataMember(Order = 26)]
        public double Chainage { get; set; }
        [DataMember(Order = 27)]
        public double ChainageEnd { get; set; } = 0.0;

    }

    [ServiceContract]
    public interface IRumbleStripService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Rumble_Strip request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Rumble_Strip>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Rumble_Strip> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Rumble_Strip> EditValue(LCMS_Rumble_Strip request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Rumble_Strip>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IEnumerable<LCMS_Rumble_Strip>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Rumble_Strip request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Rumble_Strip> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
