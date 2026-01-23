using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.Database_Tables
{
    [DataContract]
    public class ProjectRegistry 
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public DateTime CreatedAtActionResult { get; set; }

        [DataMember(Order = 4)]
        public DateTime UpdatedAtActionResult { get; set; }

        [DataMember(Order = 5)]
        public string FolderPath { get; set; }
        [DataMember(Order = 6)]
        public string DBPath { get; set; }

        [DataMember(Order = 7)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude

        [DataMember(Order = 8)]
        public double GPSLongitude { get; set; } = 0.0; // Set a default GPSLongitude

        [DataMember(Order = 9)]
        public double RoundedGPSLatitude { get; set; } = 0.0;// Set a default GPSLongitude

        [DataMember(Order = 10)]
        public double RoundedGPSLongitude { get; set; } = 0.0;

        [DataMember(Order = 11)]
        public string IdProject { get; set; } = "";

        [NotMapped]
        public string DisplayName => Name;  //Value shown in List of projects when creating Dataset

        public override string ToString()
        {
            return DisplayName;
        }
    }



    [ServiceContract]
    public interface IProjectRegistryService
    {
        [OperationContract]
        Task<IdReply> Create(ProjectRegistry request, CallContext context = default);

        Task<List<ProjectRegistry>> GetAll(Empty empty,
        CallContext context = default);
       

        [OperationContract]
        Task<ProjectRegistry> GetById(IdRequest request, 
            CallContext context = default);
               
        [OperationContract]
        Task<IdReply> RenameProject(ProjectRegistry request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteProject(IdRequest request, CallContext context = default);

        //[OperationContract]
        //Task<IdReply> DeleteProjectUI(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<IdReply> CheckExistByName(string datasetName);

        [OperationContract]
        Task<string> GetActualDatabasePath(
        CallContext context = default);

        [OperationContract]
        Task<IdReply> Import(ProjectRegistry request, CallContext context = default);

    }

    public class GeoJsonProject
    {
        public string Path { get; set; }
        public string DBPath { get; set; }
    }

    public class ProjectData
    {
        public ProjectRegistry Project { get; set; }
        public string EntredName { get; set; }
        public bool IsEditing = false;
    }
}
