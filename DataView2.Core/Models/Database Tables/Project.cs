using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Database_Tables;

namespace DataView2.Core.Models
{
    [DataContract]
    public class Project
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
        public double RoundedGPSLatitude { get; set; } = 0.0;

        [DataMember(Order = 10)]
        public double RoundedGPSLongitude { get; set; } = 0.0;

        [DataMember(Order = 11)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdProject { get; set; } = Guid.NewGuid();
    }

    [DataContract]
    public class IdRequestGUID
    {
        [DataMember(Order = 1)]
        public required string Id { get; set; }
    }

    [DataContract]
    public class ImportProjectPath
    {
        [DataMember(Order = 1)]
        public string SourceProjectPath { get; set; }
        [DataMember(Order = 2)]
        public string DestinationProjectPath { get; set; }
    }

    [ServiceContract]
    public interface IProjectService
    {
        [OperationContract]
        Task<IdReply> Create(Project request, CallContext context = default);

        [OperationContract]
        Task<IdReply> Update(Project request, CallContext context = default);
        [OperationContract]
        Task<List<Project>> GetAll(Empty empty,
        CallContext context = default);

        [OperationContract]
        Task<Project> GetById(IdRequest request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> RenameProject(Project request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteProject(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<IdReply> CheckExistByName(string datasetName);

        [OperationContract]
        Task<Project> GetByName(string projectName, CallContext context = default);

    //    [OperationContract]
    //    Task ChangeDatabase(string newDatabasePath,
    //CallContext context = default);
       
        [OperationContract]   
        Task CloseDatabaseConnection(string databasePath);

        [OperationContract]
        Task<Project> GetByIdProject(IdRequestGUID request,
             CallContext context = default);

        [OperationContract]
        Task<string> GetProjectIdFromDb(string dbPath);

        //[OperationContract]
        //Task<IdReply> CopyProject(ImportProjectPath request);
        [OperationContract]
        Task<IdReply> ImportProjectAsync(ImportProjectPath request);

    }



}
