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
using System.Collections;
using System.Data.Common;

namespace DataView2.Core.Models
{
    [DataContract]
    public class TablesSetting
    {
        [DataMember(Order = 1)]
        [Key]
        public int RootPage { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string File { get; set; }
    }   

    [DataContract]
    public class TablesSettingReply
    {         
        public bool FileDownloaded { get; set; }
    }
    [DataContract]
    public class TablesSettingsData
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public int SurveyId { get; set; }

        [DataMember(Order = 3)]
        public string SurveyExternalId { get; set; }
    }

        [DataContract]
    public class TablesSettingsDataReply
    {
        [DataMember(Order = 1)]
        public bool RowsExist { get; set; }
    }


    [ServiceContract]
    public interface ISettingTablesService
    {
        [OperationContract]
        Task<IEnumerable<TablesSetting>> QueryAsync(string predicate);
        [OperationContract]
        Task<IEnumerable<TablesSetting>> DBTablesAsync(string folderPath);
        [OperationContract]
        Task<TablesSettingReply> SaveTemplatedCSV(TablesSetting tablesSetting,
            CallContext context = default);
        [OperationContract]
        Task<TablesSettingsDataReply> TableHasDataAsync(TablesSettingsData table);

    }

   
}
