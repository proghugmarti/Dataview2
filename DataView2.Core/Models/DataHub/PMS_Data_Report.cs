using DataView2.Core.Models.LCMS_Data_Tables;
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
    public class PMS_Data_Report
    {
        [Key]
        [StringLength(50)]
        public string PdID { get; set; }

        [Required]
        public DateTime PdDateTime { get; set; }

        [StringLength(50)]
        public string AoaID { get; set; }

        [StringLength(50)]
        public string OperatorID { get; set; }

        [StringLength(100)]
        public string PdImage { get; set; }

        [Required]
        public Int64 Classification { get; set; }

        [Required]
        public double PdLat { get; set; }

        [Required]
        public double PdLong { get; set; }

        [StringLength(50)]
        public string Note { get; set; }

        [Required]
        public double Width { get; set; }

        [Required]
        public double Length { get; set; }

        public double Depth { get; set; }

        [StringLength(50)]
        public string BlockID { get; set; }

        public Int64 PdSeverity { get; set; }
    }

    [ServiceContract]
    public interface IPMS_Data_ReportService
    {
      
        [OperationContract]
        Task GetAndSendWholeCracksAsync(Empty empty);

    }
}
