using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using DataView2.Core.Models.ExportTemplate;


namespace DataView2.Core.Models.DTS
{

    [DataContract]
    public class XMLObject : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string? Parent { get; set; }

        [DataMember(Order = 4)]
        public string Type { get; set; }

        [DataMember(Order = 5)]
        public int Level { get; set; }

        [DataMember(Order = 6)]
        public string GeoJSON{ get; set; } = "DefaultGeoJSON";

        [DataMember(Order = 7)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude

        [DataMember(Order = 8)]
        public double GPSLongitude { get; set;} = 0.0; // Set a default GPSLongitude
        [DataMember(Order = 9)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 10)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 11)]
        public string PavementType { get; set; }

        [DataMember(Order = 12)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 13)]
        public double RoundedGPSLongitude { get; set; }

        [DataMember(Order = 14)]
        public string? SurveyId { get; set; }

        [DataMember(Order = 15)]
        public int SegmentId { get; set; }

        [DataMember(Order = 16)]
        public double Chainage { get; set; }

    }

      

    [ServiceContract]
    public interface IXMLObjectService
    {
        [OperationContract]
        Task<IdReply> Create(XMLObject request,
            CallContext context = default);

        [OperationContract]
        Task<List<XMLObject>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<XMLObject> EditValue(XMLObject request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> ProcessXMLFile(string xmlContent,
        CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(XMLObject request,
            CallContext context = default);
    }

}
