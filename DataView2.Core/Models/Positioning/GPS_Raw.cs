using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Text.Json.Serialization;

namespace DataView2.Core.Models.Positioning
{
    public class GPS_Raw
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public double Chainage { get; set; }

        [DataMember(Order = 3)]
        public double Latitude { get; set; }
        [DataMember(Order = 4)]
        public double Longitude { get; set; }
        [DataMember(Order = 5)]
        public double Heading { get; set; }
        [DataMember(Order = 6)]
        public double SystemTime { get; set; }
        [DataMember(Order = 7)]
        public int UTCTime { get; set; }
        [DataMember(Order = 8)]
        public int SurveyId { get; set; }
        [DataMember(Order = 9)]
        public double? Roll { get; set; }

        [DataMember(Order = 10)]
        public double? Pitch { get; set; }

        [DataMember(Order = 11)]
        public double? Yaw { get; set; }

    }

    public class GPSData
    {
        [JsonPropertyName("OdoDataRecord")]
        public GPSOdoData GPSOdoDataRecord { get; set; }
        public string NmeaLine { get; set; }
    }

     public class GPSOdoData
    {
        public double? Time { get; set; }
        public double? Chainage { get; set; }
        public double? Speed { get; set; }
    }

    public class GPSRMC
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Heading { get; set; }
        public double UtcTime { get; set; }
        public double SystemTime { get; set; }

    }

    public class GPSGGA
    {
        public string Time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Altitude { get; set; }
        public double? SystemTime { get; set; }
    }

    // JSON structure classes
    public class GpsDataEntry
    {
        public required OdoDataa OdoDataRecord { get; set; }
        public required string NmeaLine { get; set; }
    }

    public class OdoDataa
    {
        public double? Time { get; set; }
        public double? Chainage { get; set; }
        public double? Speed { get; set; }
    }

    [ServiceContract]
    public interface IGPS_RawService
    {


        Task<List<GPS_Raw>> GetAll(Empty empty, CallContext context = default);


    }


}
