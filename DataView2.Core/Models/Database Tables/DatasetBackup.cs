using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;

namespace DataView2.Core.Models
{
    [DataContract]
    public class DatasetBackup
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public required string Name { get; set; }

        [DataMember(Order = 3)]
        public string? Description { get; set; }

        [DataMember(Order = 4)]
        public DateTime Timestamp { get; set; }

        [DataMember(Order = 5)]
        public required string Path { get; set; }

        [DataMember(Order = 6)]
        public int DatasetId { get; set; }
    }

    [DataContract]
    public class NewBackupRequest
    {
        [DataMember(Order = 1)]
        public required string Name { get; set; }

        [DataMember(Order = 2)]
        public string? Description { get; set; }

        [DataMember(Order = 3)]
        public required string FilePath { get; set; }
        
    }

    [DataContract]
    public class BackupActionRequest
    {
        [DataMember(Order = 1)]
        public int BackupId { get; set; }
    }

    [DataContract]
    public class DatasetIdRequest
    {
        [DataMember(Order = 1)]
        public int DatasetId { get; set; }
    }

    [DataContract]
    public class ImportBackupRequest
    {
        [DataMember(Order = 1)]
        public string SourceFolder { get; set; }
        [DataMember(Order = 2)]
        public string DestinationFolder { get; set; }
        [DataMember(Order = 3)]
        public bool ImportBackup { get; set; }
    }

    [ServiceContract]
    public interface IDatasetBackupService
    {
        [OperationContract]
        Task<IdReply> CreateBackup(NewBackupRequest request, CallContext context = default);

        [OperationContract]
        Task RestoreBackup(BackupActionRequest request);

        [OperationContract]
        Task<List<DatasetBackup>> GetAllBackups();

        [OperationContract]
        Task<List<DatasetBackup>> GetBackupsByDatasetId(DatasetIdRequest request);

        [OperationContract]
        Task<DatasetBackup> GetBackupByPath(string path);

        [OperationContract]
        Task<DatasetBackup> GetBackupByName(string name);

        [OperationContract]
        Task DeleteBackup(BackupActionRequest request);
        [OperationContract]
        Task<IdReply> HandleBackupImportOption(ImportBackupRequest request);
    }
}
