namespace DataView2.Core.Models
{
    public interface IEntity
    {
        int Id { get; set; }

        string GeoJSON { get; set; }

        double GPSLatitude { get; set; }

        double GPSLongitude { get; set; }

        public double GPSAltitude { get; set; }

        public double GPSTrackAngle { get; set; }
        double RoundedGPSLatitude { get; set; }

        double RoundedGPSLongitude { get; set; }

        string SurveyId { get; set; }

        int SegmentId { get; set; }

        public string PavementType { get; set; }

        public double Chainage { get; set; }
    }
}