using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
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

namespace DataView2.Core.Models.Positioning
{
    [DataContract]
    public class GPS_Processed 
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }

        [DataMember(Order = 2)]
        public int SurveyId { get; set; }


        // Time References
        [DataMember(Order = 3)]
        public double Time { get; set; }   



        // Physical References
        [DataMember(Order = 4)]
        public int OdoCount { get; set; }     // Interpolated pulse count

        [DataMember(Order = 5)]
        public double Chainage { get; set; }  // Interpolated distance


        // Position Data
        [DataMember(Order = 6)]
        public double Latitude { get; set; }
        [DataMember(Order = 7)]
        public double Longitude { get; set; }
        [DataMember(Order = 8)]
        public double Heading { get; set; }


    }

    [DataContract]
    public class GpsBySurveyAndChainageRequest
    {
        [DataMember(Order = 1)]
        public int SurveyId { get; set; }
        [DataMember(Order = 2)]
        public double StartChainage { get; set; }
        [DataMember(Order = 3)]
        public double EndChainage { get; set; }

    }


    [ServiceContract]
    public interface IGPSProcessedService
    {
        [OperationContract]
        Task<IdReply> Create(GPS_Processed request,
            CallContext context = default);

        [OperationContract]
        Task<List<GPS_Processed>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<GPS_Processed> EditValue(GPS_Processed request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(GPS_Processed request,
            CallContext context = default);

      
        [OperationContract]
        Task<IEnumerable<GPS_Processed>> QueryAsync(string predicate);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);


        [OperationContract]
        Task<CountReply> CreateAll(List<GPS_Processed> request, CallContext context = default);
        [OperationContract]
        Task<GPS_Processed> UpdateGenericData(string fieldsToUpdateSerialized);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasINSGeometryData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<GPS_Processed>> GetBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task DeleteBySurveyID(string surveyID);

        [OperationContract]
        Task<List<GPS_Processed>> GetBySurveyAndChainage(GpsBySurveyAndChainageRequest request, CallContext context = default);
    }
}
