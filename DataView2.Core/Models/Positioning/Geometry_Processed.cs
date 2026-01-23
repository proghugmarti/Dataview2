using DataView2.Core.Models.LCMS_Data_Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Positioning
{
    [DataContract]
    public class Geometry_Processed : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public double Chainage { get; set; }

        [DataMember(Order = 3)]
        public float Speed { get; set; }

        [DataMember(Order = 4)]
        public float Gradient { get; set; }

        [DataMember(Order = 5)]
        public float HorizontalCurve { get; set; }

        [DataMember(Order = 6)]
        public float CrossSlope { get; set; }

        [DataMember(Order = 7)]
        public float VerticalCurve { get; set; }

        [DataMember(Order = 8)]
        public string SurveyId { get; set; }

        // --- New physical DB columns ---
        [DataMember(Order = 9)]
        public string GeoJSON { get; set; }

        [DataMember(Order = 10)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 11)]
        public double GPSLongitude { get; set; }

        // --- Optional IEntity fields you may want to persist too ---
        [DataMember(Order = 12)]
        public double GPSAltitude { get; set; }

        [DataMember(Order = 13)]
        public double GPSTrackAngle { get; set; }

        [DataMember(Order = 14)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 15)]
        public double RoundedGPSLongitude { get; set; }

        [DataMember(Order = 16)]
        public int SegmentId { get; set; }

        [DataMember(Order = 17)]
        public string PavementType { get; set; }
    }

    [ServiceContract]
    public interface IINSGeometryService
    {
        [OperationContract]
        Task<IEnumerable<Geometry_Processed>> QueryAsync(string sqlQuery);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<List<Geometry_Processed>> GetBySurvey(string surveyId);
    }
}
