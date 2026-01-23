using Esri.ArcGISRuntime.Geometry;
using System.Text.Json;

namespace DataView2.GrpcService.Helpers
{
    public class ProcessingHelper
    {
        public static List<double> TryGetBottomMidpoint(string geoJson)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(geoJson);

                // Navigate to the coordinates array
                var coordinatesElement = doc.RootElement
                    .GetProperty("geometry")
                    .GetProperty("coordinates")[0]; // First ring of the polygon

                // Deserialize into List<List<double>>
                var coordinates = JsonSerializer.Deserialize<List<List<double>>>(coordinatesElement.GetRawText());

                // Get first and last
                var first = coordinates.First();
                var last = coordinates.Last();

                // Calculate midpoint
                return new List<double>
                    {
                        (first[0] + last[0]) / 2,
                        (first[1] + last[1]) / 2
                    };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting midpoint: {ex.Message}");
                return null;
            }
        }

        public static bool IsVideoAfterSegment(double segmentLat, double segmentLon, double videoLat, double videoLon, double segmentTrackAngle)
        {
            var segmentPoint = new MapPoint(segmentLon, segmentLat, SpatialReferences.Wgs84);
            var videoPoint = new MapPoint(videoLon, videoLat, SpatialReferences.Wgs84);

            var result = GeometryEngine.DistanceGeodetic(
                segmentPoint, videoPoint,
                LinearUnits.Meters,
                AngularUnits.Degrees,
                GeodeticCurveType.Geodesic);

            double azimuthToVideo = NormalizeAngle(result.Azimuth1);
            double trackAngle = NormalizeAngle(segmentTrackAngle);

            double angleDiff = Math.Abs(trackAngle - azimuthToVideo);
            if (angleDiff > 180) angleDiff = 360 - angleDiff;

            // If the video lies within ±90° of the segment's heading, it's ahead
            return angleDiff < 90;
        }
        public static double NormalizeAngle(double angle) => (angle + 360) % 360;
    }
}
