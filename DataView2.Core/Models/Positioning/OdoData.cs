using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Positioning
{
     public  class OdoData
     {

        [DataMember(Order = 1)]
        [Key]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public double Chainage { get; set; }
        [DataMember(Order = 3)]
        public int OdoCount { get; set; }
        [DataMember(Order = 4)]
        public int OdoTime { get; set; }
        [DataMember(Order = 5)]
        public double Speed { get; set; }
        [DataMember(Order = 6)]
        public long SystemTime { get; set; }
        [DataMember(Order = 7)]
        public int SurveyId { get; set; }
        [DataMember(Order = 8)]
        public string SurveyName { get; set; }

     }

   

    [ServiceContract]
    public interface IOdoDataService
    {
        [OperationContract]
        Task<IdReply> Create(OdoData request,
            CallContext context = default);

        [OperationContract]
        Task<OdoData> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<List<OdoData>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<OdoData> EditValue(OdoData request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(OdoData request,
            CallContext context = default);

        [OperationContract]
        Task ProcessOdoFile(string filePath, SurveyIdRequest surveyRequestId);


    }

}
