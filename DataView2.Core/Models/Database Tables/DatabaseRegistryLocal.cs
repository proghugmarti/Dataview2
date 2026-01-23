using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using ProtoBuf.Grpc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace DataView2.Core.Models.Database_Tables
{
    [DataContract]
    public class DatabaseRegistryLocal
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string Path { get; set; }

        [DataMember(Order = 4)]
        public int ProjectId { get; set; }

        [DataMember(Order = 5)]
        public DateTime CreatedAtActionResult { get; set; }

        [DataMember(Order = 6)]
        public DateTime UpdatedAtActionResult { get; set; }

        [DataMember(Order = 7)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude

        [DataMember(Order = 8)]
        public double GPSLongitude { get; set; } = 0.0; // Set a default GPSLongitude


    }

    [DataContract]
    public class NewDatasetRequest
    {
        [DataMember(Order = 1)]
        public required string DatasetName { get; set; }

        [DataMember(Order = 2)]
        public required string DatasetLocation { get; set; }

        [DataMember(Order = 3)]
        public required int ProjectId { get; set; }
    }


    [DataContract]
    public class ImportDatasetRequest
    {
        [DataMember(Order = 1)]
        public required string DatasetName { get; set; }

        [DataMember(Order = 2)]
        public required string SourceLocation { get; set; } // Source file path

        [DataMember(Order = 3)]
        public required string DestinationLocation { get; set; } // Where the file should be copied

        [DataMember(Order = 4)]
        public required int ProjectId { get; set; }
    }
    [DataContract]
    public class ExistRequest<T> where T : class
    {
        [DataMember(Order = 1)]
        public T Entity { get; set; }

        [DataMember(Order = 2)]
        public Func<T, bool> Condition { get; set; }
    }

    [DataContract]
    public class DeleteSurveysRequest
    {
        [DataMember(Order = 1)]
        public List<SurveyIdRequest> SelectedSurveys { get; set; }

        [DataMember(Order = 2)]
        public List<string> LcmsTables { get; set; }

        [DataMember(Order = 3)]
        public string DatabasePath { get; set; }
    }

    [DataContract]
    public class QueriesRequest
    {
        [DataMember(Order = 1)]
        public List<string> Queries { get; set; }

        [DataMember(Order = 2)]
        public string DatabasePath { get; set; }
    }

    [DataContract]
    public class DatsetPathRequest
    {
        [DataMember(Order = 1)]
        public List<string> DatsetPaths { get; set; }

        [DataMember(Order = 2)]
        public string folderDataSetToChange { get; set; }

        [DataMember(Order = 3)]
        public string folderDataSetTarget { get; set; }

        [DataMember(Order = 4)]
        public string DatabasePath { get; set; }
    }

    [DataContract]
    public class BackupPathRequest
    {
        [DataMember(Order = 1)]
        public List<string> BackupPaths { get; set; }

        [DataMember(Order = 2)]
        public string folderBackupToChange { get; set; }

        [DataMember(Order = 3)]
        public string folderBackupTarget { get; set; }

        [DataMember(Order = 4)]
        public string DatabasePath { get; set; }
    }


    [DataContract]
    public class ListRequest
    {
        [DataMember(Order = 1)]
        public List<string> ListData { get; set; }
    }

    [DataContract]
    public class NewDatasetRequestLocal
    {
        [DataMember(Order = 1)]
        public required string DatasetName { get; set; }

        [DataMember(Order = 2)]
        public required string DatasetLocation { get; set; }

        [DataMember(Order = 3)]
        public required int ProjectId { get; set; }
        [DataMember(Order = 4)]
        public required string ProjectDbLocation { get; set; }
    }

    [DataContract]
    public class NewDatabaseRequest
    {
        [DataMember(Order = 1)]
        public required string NewDatabasePath { get; set; }

        [DataMember(Order = 2)]
        public required string DbType { get; set; }
    }

    [ServiceContract]
    public interface IDatabaseRegistryLocalService
    {
        [OperationContract]
        Task<List<DatabaseRegistryLocal>> GetAll(Empty empty,
        CallContext context = default);

        [OperationContract]
        Task<List<DatabaseRegistryLocal>> GetAllByProjectId(Project projectId);

        [OperationContract]
        Task<DatabaseRegistryLocal> GetById(IdRequest request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> CreateNewDatasetAsync(NewDatasetRequest request);

        [OperationContract]
        Task<IdReply> ImportDatasetAsync(ImportDatasetRequest request);

        [OperationContract]
        Task<IdReply> Exists(ExistRequest<DatabaseRegistryLocal> request,
            CallContext context = default);

        [OperationContract]
        Task ChangeDatabase(NewDatabaseRequest newDatabasePath, CallContext context = default);

        [OperationContract]
        Task<string> GetActualDatabasePath(
            CallContext context = default);

        [OperationContract]
        Task<IdReply> UpdateGPSCoordinates(UpdateCoordinatesRequest request,
            CallContext context = default);

        [OperationContract]
        Task<ListRequest> GetAllTableColumns(string tableName, 
            CallContext context = default);


        [OperationContract]
        Task<DatabaseRegistryLocal> GetActualDatasetRegistry(IdRequest idRequest,
            CallContext context = default);

        [OperationContract]
        Task<DatabaseRegistryLocal> GetByName(string datasetName);

        [OperationContract]
        Task<IdReply> DeleteDataset(DatabaseRegistryLocal request, CallContext context = default);

        [OperationContract]
        Task<IdReply> RenameDataset(DatabaseRegistryLocal request, CallContext context = default);

        [OperationContract]
        Task CloseDatabaseConnection(string databasePath);

        [OperationContract]
        Task DeleteSurveysAsync(DeleteSurveysRequest request);

        [OperationContract]
        Task<IdReply> CheckExistByName(string datasetName);    

        [OperationContract]
        Task ChangeDataset(string newDataset, CallContext context = default);

        [OperationContract]
        Task<ListRequest> ChangeDatasets(DatsetPathRequest datasets, CallContext context = default);

        [OperationContract]
        Task<ListRequest> ChangeBackups(BackupPathRequest backups, CallContext context = default);

        [OperationContract]
        Task<IdReply> EnsureDatabaseIsUpToDate(string databasePath);

        [OperationContract]
        Task ExecuteQueryInDb(List<string> queries);
    }

}
