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
using DataView2.Core.Models.Other;

namespace DataView2.Core.Models
{
    [DataContract]
    public class GeneralSetting 
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string Description { get; set; } 

        [DataMember(Order = 4)]
        public SettingType Type { get; set; }

        [DataMember(Order = 5)]
        public string Value { get; set; } 

        [DataMember(Order = 6)]
        public string Category { get; set; }

        [DataMember(Order = 7)]
        public string GeoJSON { get; set; } = "DefaultGeoJSON";

        [DataMember(Order = 8)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude

        [DataMember(Order = 9)]
        public double GPSLongitude { get; set; } = 0.0; // Set a default GPSLongitude

        [DataMember(Order = 10)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 11)]
        public double RoundedGPSLongitude { get; set; }

        [DataMember(Order = 12)]
        public string CoordinateSystemType { get; set; } = "";

        [NotMapped]
        public bool IsCoordinateSystemSelected { get; set; } = false;

    }

    [DataContract]
    public class IdReply {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }
    }

    [DataContract]
    public class CountReply
    {
        [DataMember(Order = 1)]
        public int Count { get; set; }
    }

    [DataContract]
    public class SettingName
    {
        [DataMember(Order = 1)]
        public string name { get; set; }
    }

    [DataContract]
    public class LicenseSetting
    {
        [DataMember(Order = 1)]
        public string content { get; set; }
    }

    [ServiceContract]
    public interface ISettingsService
    {
        [OperationContract]
        Task<IdReply> Create(GeneralSetting request,
            CallContext context = default);

        [OperationContract]
        Task<List<GeneralSetting>> GetAllOrCreate(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<GeneralSetting> EditValue(GeneralSetting request,
            CallContext context = default);

        [OperationContract]
        Task<List<GeneralSetting>> GetByName(SettingName request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteByName(SettingName request, CallContext context = default);

        //Get and Set License
        [OperationContract]
        Task<LicenseSetting> GetLicense(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> UpdateLicense(LicenseSetting request, CallContext context = default);

        [OperationContract]
        Task<PavementTypeResponse> ParseSelectedCfgAsync(string fileName);
    }

    public enum SettingType
    {
        Integer,
        Float,
        String
    }
    public enum ServiceState
    {
        Inactive = 0,
        Active = 1
    }
}
