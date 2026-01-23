using DataView2.Core.Models.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using DataView2.Core.Models.Positioning;

namespace DataView2.Core.Helper
{

    public static class CoordinateHelper
    {
        /// <summary>
        /// Transforms a coordinate from WGS84 (EPSG:4326) to another spatial reference using its WKID.
        /// </summary>
        /// <param name="latitude">Latitude in WGS84</param>
        /// <param name="longitude">Longitude in WGS84</param>
        /// <param name="targetWkid">WKID of the target spatial reference (e.g., 3857 for Web Mercator)</param>
        /// <returns>Transformed coordinate as a MapPoint, or null if it fails</returns>
        public static MapPoint TransformCoordinate(double latitude, double longitude, int targetWkid)
        {
            try
            {
                SpatialReference targetSpatialReference;
                // Check if the target WKID is 16370 (Turkish National Grid)
                if (targetWkid == 16370)
                {

                    string wkt16370 = @"
                            PROJCS[""3-degree Gauss-Kruger CM 30E"",
                                GEOGCS[""GCS_TUREF"",
                                    DATUM[""D_Turkish_National_Reference_Frame"",
                                        SPHEROID[""GRS_1980"",6378137.0,298.257222101]],
                                    PRIMEM[""Greenwich"",0.0],
                                    UNIT[""Degree"",0.0174532925199433]],
                                PROJECTION[""Transverse_Mercator""],
                                PARAMETER[""False_Easting"",500000.0],
                                PARAMETER[""False_Northing"",0.0],
                                PARAMETER[""Central_Meridian"",30.0],
                                PARAMETER[""Scale_Factor"",1.0],
                                PARAMETER[""Latitude_Of_Origin"",0.0],
                                UNIT[""Meter"",1.0]]";

                    targetSpatialReference = SpatialReference.Create(wkt16370);

                }
                else {
                    targetSpatialReference = SpatialReference.Create(targetWkid);
                }

                // Define the original spatial reference (WGS84)
                SpatialReference wgs84 = SpatialReferences.Wgs84;
                // Create the original point in WGS84
                MapPoint originalPoint = new MapPoint(longitude, latitude, wgs84); // Note: (x = lon, y = lat)

                // Project the point to the new spatial reference
                MapPoint transformedPoint = (MapPoint)GeometryEngine.Project(originalPoint, targetSpatialReference);

                return transformedPoint;
            }
            catch (Exception ex)
            {
                // Basic error handling
                Console.WriteLine($"Error transforming coordinate: {ex.Message}");
                return null;
            }
        }

        public static double[] TransformFromWkt(string sourceWkt, double x, double y)
        {
            if (sourceWkt.Contains("Mercator_Auxiliary_Sphere"))
            {
                return WebMercatorToWgs84(x,y);
            }
        
            var csFactory = new CoordinateSystemFactory();
            var source = csFactory.CreateFromWkt(sourceWkt);
            var target = GeographicCoordinateSystem.WGS84;

            var transformFactory = new CoordinateTransformationFactory();
            var transform = transformFactory.CreateFromCoordinateSystems(source, target);

            return transform.MathTransform.Transform(new[] { x, y });
        }

        public static double[] WebMercatorToWgs84(double x, double y)
        {
            double lon = x / 6378137.0 * 180.0 / Math.PI;
            double lat = y / 6378137.0;
            lat = (180.0 / Math.PI) * (2.0 * Math.Atan(Math.Exp(lat)) - Math.PI / 2.0);

            return new[] { lon, lat };
        }
        public static double LinearInterpolateSingle(double desiredTime, List<double> baseTimes, List<double> baseValues)
        {
            if (baseTimes.Count != baseValues.Count || baseTimes.Count < 2)
                throw new ArgumentException("Base time and value lists must be the same length and contain at least two points.");

            int j = 1;

            // Find the interval
            while (j < baseTimes.Count && desiredTime > baseTimes[j])
            {
                j++;
            }

            if (j >= baseTimes.Count)
            {
                // Extrapolate using the last interval
                return baseValues[^1];
            }

            if (desiredTime == baseTimes[j])
            {
                return baseValues[j];
            }

            // Linear interpolation
            double t1 = baseTimes[j - 1];
            double t2 = baseTimes[j];
            double v1 = baseValues[j - 1];
            double v2 = baseValues[j];

            double ratio = (desiredTime - t1) / (t2 - t1);
            return v1 + ratio * (v2 - v1);
        }

       
    }
}
