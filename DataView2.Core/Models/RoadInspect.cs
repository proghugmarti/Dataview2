using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DataView2.Core.Models
{
 
    [DataContract]
    public class FisFileRequest
    {
        [DataMember(Order = 1)]
        public string FisFilePath { get; set; }
        [DataMember(Order = 2)]
        public List<string> FisFilesPath { get; set; }  
    }

    [DataContract]
    public class ImageCreationRequest
    {
        [DataMember(Order = 1)]
        public string ImageFilePath { get; set; }
        [DataMember(Order = 2)]
        public string ImageType { get; set; }
    }

    [DataContract]
    public class ProgressResponse
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }

        [DataMember(Order = 2)]
        public int Progress { get; set; }

        [DataMember(Order = 3)]
        public string CurrentFile { get; set; }

        [DataMember(Order = 4)]
        public string Error { get; set; }
    }
 

    [ServiceContract]
    public interface IRoadInspectService
    {
        [OperationContract]
        Task<ProgressResponse> CreateImageFile(ImageCreationRequest request, CallContext context = default);

    }
}
