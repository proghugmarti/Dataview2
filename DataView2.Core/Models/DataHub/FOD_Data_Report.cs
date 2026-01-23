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
    
    public class FOD_Data_Report
    {
        [Key]
        [StringLength(50)]
        public string FodID { get; set; }

        [Required]
        public DateTime InitFodDateTime { get; set; }

        [StringLength(50)]
        public string AoaID { get; set; }

        [StringLength(50)]
        public string OperatorID { get; set; }

        [StringLength(100)]
        public string FodImage { get; set; }

        [Required]
        public ulong FodWidth { get; set; } 

        [Required]
        public ulong FodLength { get; set; }
        
        [Required]
        public double FodLat { get; set; }

        [Required]
        public double FodLong { get; set; }

        [StringLength(50)]
        public string UpdateFodDateTime { get; set; }

        [StringLength(50)]
        public string StatusFodAlert { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        [StringLength(50)]
        public string FodCharacter { get; set; }

        [StringLength(50)]
        public string FodSource { get; set; }
    }

    [ServiceContract]
    public interface IFOD_Data_ReportService
    {
      
        [OperationContract]
        Task GetAndSendAllFODDataAsync(Empty empty);
    }
}
