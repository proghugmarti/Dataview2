using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.DataHub
{
    public class AoaData
    {
        public string AoaId { get; set; } = string.Empty;
        public double Lat1 { get; set; }
        public double Long1 { get; set; }
        public double Lat2 { get; set; }
        public double Long2 { get; set; }
        public double Lat3 { get; set; }
        public double Long3 { get; set; }
        public double Lat4 { get; set; }
        public double Long4 { get; set; }
        public List<Block> Blocks { get; set; } = new();
    }

    public class Block
    {
        public string BlockID { get; set; } = string.Empty;
        public double BlockNoSeverity { get; set; }
        public double BlockLat1 { get; set; }
        public double BlockLong1 { get; set; }
        public double BlockLat2 { get; set; }
        public double BlockLong2 { get; set; }
        public double BlockLat3 { get; set; }
        public double BlockLong3 { get; set; }
        public double BlockLat4 { get; set; }
        public double BlockLong4 { get; set; }
    }

    [ServiceContract]
    public interface IAoaDataService
    {

        [OperationContract]
        Task GetAndSendAllAoaDataAsync(Empty empty);
    }
}
