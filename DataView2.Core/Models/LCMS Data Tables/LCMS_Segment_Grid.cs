using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.LCMS_Data_Tables;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;
using DataView2.Core.Helper;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Segment_Grid : IEntity
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
        public string CrackType { get; set; }// can be only from : longitudinal, transverse. other, fatigue 

        [DataMember(Order = 5)]
        public string Severity { get; set; }

        [DataMember(Order = 6)]
        public int Column { get; set; }

        [DataMember(Order = 7)]
        public int Row { get; set; }

        [DataMember(Order = 8)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 9)]
        public double GPSLongitude { get; set; }

        [DataMember(Order = 10)]
        public double GPSAltitude { get; set; }

        [DataMember(Order = 11)]
        public string GeoJSON { get; set; }

        [DataMember(Order = 12)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 13)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 14)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 15)]
        public string PavementType { get; set; }
        [DataMember(Order = 16)]    
        public double Chainage { get; set; } = 0.0;

    }


    [ServiceContract]
    public interface ISegmentGridService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Segment_Grid request, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Segment_Grid>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Segment_Grid> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<LCMS_Segment_Grid> EditValue(LCMS_Segment_Grid request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Segment_Grid>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<LCMS_Segment_Grid>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Segment_Grid request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Segment_Grid>> GetSegmentGridsBySurveyIDSectionId(Segment_Grid_Params segment_Grid_Params);
        [OperationContract]
        Task ExecuteQueryInDb(List<string> queries);
        [OperationContract]
        Task<LCMS_Segment_Grid> UpdateGenericData(string fieldsToUpdateSerialized);
    }

}