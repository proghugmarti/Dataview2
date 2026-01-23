using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.MapHelpers
{
    public class SegmentSummariesHelper
    {
        public static LCMS_Segment CalculatePickouts(List<LCMS_PickOuts_Raw> pickoutResponse, LCMS_Segment segment)
        {
            segment.PickoutCount = pickoutResponse.Count;
            segment.PickoutAvgPer_m2 = 0;
            if (pickoutResponse.Count > 0)
            {
                segment.PickoutAvgPer_m2 = Math.Round((double)(pickoutResponse.Sum(p => p.Area_mm2) / segment.PickoutCount), 2);
            }
            return segment;
        }

        public static LCMS_Segment CalculateCracks(List<LCMS_Cracking_Raw> crackResponse, LCMS_Segment segment)
        {
            double totalAll = 0, totalLow = 0, totalMed = 0, totalHigh = 0;
            totalAll = Math.Round((double)crackResponse.Sum(c => c.NodeLength_mm), 2);
            totalHigh = Math.Round((double)crackResponse.Where(c => c.Severity == "High").Sum(c => c.NodeLength_mm), 2);
            totalMed = Math.Round((double)crackResponse.Where(c => c.Severity == "Medium").Sum(c => c.NodeLength_mm), 2);
            totalLow = Math.Round((double)crackResponse.Where(c => c.Severity == "Low").Sum(c => c.NodeLength_mm), 2);

            segment.CrackingTotalLengthAllNodes_mm = totalAll;
            segment.CrackingTotalLengthHighSevNodes_mm = totalHigh;
            segment.CrackingTotalLengthLowSevNodes_mm = totalLow;
            segment.CrackingTotalLengthMedSevNodes_mm = totalMed;
            return segment;
        }

        public static LCMS_Segment CalculateSegmentGrids(List<LCMS_Segment_Grid> segmentGridResponse, LCMS_Segment segment)
        {
            double totalLengthLong = 0, totalLengthTrans = 0, totalLengthOther = 0, totalAreaFatigue = 0;
            foreach (var segmentGrid in segmentGridResponse)
            {
                var geoJsonObject = JObject.Parse(segmentGrid.GeoJSON);
                var geometry = geoJsonObject["geometry"];
                var coordinatesArray = geometry["coordinates"];
                var coordinates = coordinatesArray
                         .SelectMany(ring => ((JArray)ring)
                         .Select(coord => new double[] { coord[0].Value<double>(), coord[1].Value<double>() }))
                         .ToList();
                var polyline = CreatePolyline(coordinates);
                double maxDistance = 0;

                for (int i = 1; i < polyline.Parts[0].Points.Count; i++)
                {
                    var point1 = polyline.Parts[0].Points[i - 1];
                    var point2 = polyline.Parts[0].Points[i];

                    var result = point1.DistanceGeodetic(point2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

                    if (result.Distance > maxDistance)
                        maxDistance = result.Distance;
                }


                if (segmentGrid.CrackType == "Longitudinal")
                    totalLengthLong += maxDistance;
                else if (segmentGrid.CrackType == "Transversal")
                    totalLengthTrans += maxDistance;
                else if (segmentGrid.CrackType == "Alligator")
                    totalAreaFatigue += Math.Round(maxDistance / (1000 * 1000), 2);
                else if (segmentGrid.CrackType == "Other")
                    totalLengthOther += maxDistance;
            }

            segment.CrackClassificationTotalLengthLongCracks_mm = Math.Round(totalLengthLong, 2) * 1000;
            segment.CrackClassificationTotalLengthTransCracks_mm = Math.Round(totalLengthTrans, 2) * 1000;
            segment.CrackClassificationTotalLengthOtherCracks_mm = Math.Round(totalLengthOther, 2) * 1000;
            segment.CrackClassificationTotalAreaFatigueCracks_m2 = Math.Round(totalAreaFatigue, 2) * 1000;
            return segment;
        }

        public static LCMS_Segment CalculateRavelling(List<LCMS_Ravelling_Raw> ravellingResponse, LCMS_Segment segment)
        {
            segment.RavellingTotalArea_m2 = ravellingResponse.Count != 0 ? Math.Round((double)(ravellingResponse.Sum(p => p.SquareArea_mm2) / (1000 * 1000)), 2) : 0;//mm2 => m2
            segment.RavellingSeverity = ravellingResponse.Count != 0 ? Math.Round((double)(ravellingResponse.Sum(p => p.ALG1_RavellingIndex) / ravellingResponse.Count), 2) : 0;
            return segment;
        }

        public static LCMS_Segment CalculatePatch(List<LCMS_Patch_Processed> patchResponse, LCMS_Segment segment)
        {
            segment.PatchesArea_m2 = Math.Round(patchResponse.Sum(p => p.Area_m2), 2);
            return segment;
        }

        public static LCMS_Segment CalculateBleeding(List<LCMS_Bleeding> bleedingResponse, LCMS_Segment segment)
        {
            double totalSeverity = 0;
            foreach (var bleeding in bleedingResponse)
            {
                totalSeverity += (double)(DetermineBleedingSeverity(bleeding.LeftSeverity) + DetermineBleedingSeverity(bleeding.RightSeverity));
            }

            segment.BleedingTotalArea_m2 = Math.Round(bleedingResponse.Sum(b => b.Area_m2), 4);
            segment.BleedingSeverity = bleedingResponse.Count != 0 ? Math.Round(totalSeverity / (bleedingResponse.Count * 2)) : 0;
            return segment;
        }

        public static LCMS_Segment CalculatePumping(List<LCMS_Pumping_Processed> pumpingResponse, LCMS_Segment segment)
        {
            segment.PumpingArea_m2 = Math.Round(pumpingResponse.Sum(p => p.Area_m2), 2);
            return segment;
        }

        public static LCMS_Segment CalculateRutting(List<LCMS_Rut_Processed> rutResponse, LCMS_Segment segment)
        {
            double rutAvg = 0;
            foreach (var rut in rutResponse)
            {
                rutAvg += Math.Round((double)((rut.LeftDepth_mm + rut.RightDepth_mm) / 2), 2);
            }
            segment.RutAverage = Math.Round(rutAvg, 2);
            return segment;
        }

        public static LCMS_Segment CalculateRoughness(List<LCMS_Rough_Processed> roughnessResponse, LCMS_Segment segment)
        {
            double iriAvg = 0;
            foreach (var iri in roughnessResponse)
            {
                iriAvg += (iri.LwpIRI + iri.RwpIRI) / 2;
            }

            // Round the final average to 2 decimal places
            segment.IRIAverage = Math.Round(iriAvg, 2);

            return segment;
        }


        public static LCMS_Segment CalculateSealedCracks(List<LCMS_Sealed_Cracks> sealedCrackResponse, LCMS_Segment segment)
        {
            segment.SealedCrackTotalLength_mm = Math.Round((double)sealedCrackResponse.Sum(c => c.Length_mm), 4);
            return segment;
        }

        public static LCMS_Segment CalculateShove(List<LCMS_Shove_Processed> shoveResponse, LCMS_Segment segment)
        {
            double totalShoveLength = 0;
            foreach (var shove in shoveResponse)
            {
                var geoJsonObject = JObject.Parse(shove.GeoJSON);
                var geometry = geoJsonObject["geometry"];
                var coordinatesArray = geometry["coordinates"];
                var coordinates = coordinatesArray.SelectMany(ring => ((JArray)ring).Select(coord => new double[] { coord[0].Value<double>(), coord[1].Value<double>() })).ToList();
                var polyline = CreatePolyline(coordinates);
                double maxDistance = 0;

                for (int i = 1; i < polyline.Parts[0].Points.Count; i++)
                {
                    var point1 = polyline.Parts[0].Points[i - 1];
                    var point2 = polyline.Parts[0].Points[i];

                    var result = point1.DistanceGeodetic(point2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

                    if (result.Distance > maxDistance)
                        maxDistance = result.Distance;
                }

                totalShoveLength += maxDistance;
            }

            segment.ShoveTotalLength_mm = Math.Round(totalShoveLength * 1000, 2);
            return segment;
        }

        public static LCMS_Segment CalculateTexture(List<LCMS_Texture_Processed> macroTextureResponse, LCMS_Segment segment)
        {
            double avgMPD = 0, avgMTD = 0;
            foreach (var mt in macroTextureResponse)
            {
                if (mt.MPDBand1 != null && mt.MPDBand2 != null && mt.MPDBand3 != null && mt.MPDBand4 != null && mt.MPDBand5 != null)
                {
                    avgMPD += (double)(mt.MPDBand1 + mt.MPDBand2 + mt.MPDBand3 + mt.MPDBand4 + mt.MPDBand5) / 5;
                }

                if (mt.MTDBand1 != null && mt.MTDBand2 != null && mt.MTDBand3 != null && mt.MTDBand4 != null && mt.MTDBand5 != null)
                {
                    avgMTD += (double)(mt.MTDBand1 + mt.MTDBand2 + mt.MTDBand3 + mt.MTDBand4 + mt.MTDBand5) / 5;
                }
            }

            segment.AverageMPD_mm = Math.Round(avgMPD, 2);
            segment.AverageMTD_mm = Math.Round(avgMTD, 2);
            return segment;
        }

        public static LCMS_Segment CalculateSagsBumps(List<LCMS_Sags_Bumps> sagsBumpsResponse, LCMS_Segment segment)
        {
            segment.SagsTotalArea_m2 = Math.Round(sagsBumpsResponse.Where(s => s.Type == "Sags").Sum(s => s.Area_m2), 2);
            segment.BumpsTotalArea_m2 = Math.Round(sagsBumpsResponse.Where(s => s.Type == "Bumps").Sum(s => s.Area_m2), 2);
            return segment;
        }

        public static LCMS_Segment CalculateGeometry(List<LCMS_Geometry_Processed> geometryResponse, LCMS_Segment segment)
        {
            double avgGradient = 0, avgHorizontalCurve = 0, avgVerticalCurve = 0;
            segment.GeometryAvgCrossSlope = geometryResponse.Count != 0 ? geometryResponse.Sum(g => g.CrossSlope) / geometryResponse.Count : 0;
            segment.GeometryAvgGradient = avgGradient;
            segment.GeometryAvgHorizontalCurvature = avgHorizontalCurve;
            segment.GeometryAvgVerticalCurvature = avgVerticalCurve;
            return segment;
        }

        public static LCMS_Segment CalculatePotholes(List<LCMS_Potholes_Processed> potholesResponse, LCMS_Segment segment)
        {
            segment.PotholesCount = potholesResponse.Count;
            return segment;
        }

        public static LCMS_Segment CalculateMMO(List<LCMS_MMO_Processed> mmoResponse, LCMS_Segment segment)
        {
            segment.MmoCount = mmoResponse.Count;
            return segment;
        }

        public static LCMS_Segment CalculatePASER(List<LCMS_PASER> paserResponse, LCMS_Segment segment)
        {
            segment.Paser = paserResponse.Count() != 0 ? Math.Round(paserResponse.Sum(s => s.PaserRating), 2) : 0;
            return segment;
        }

        private static Polyline CreatePolyline(List<double[]> paths)
        {
            var pointCollection = new Esri.ArcGISRuntime.Geometry.PointCollection(SpatialReferences.Wgs84);

            foreach (var coordinate in paths)
            {
                pointCollection.Add(new MapPoint(coordinate[0], coordinate[1], SpatialReferences.Wgs84));
            }

            var polyline = new Polyline(pointCollection);
            return polyline;
        }

        private static double DetermineBleedingSeverity(string severity)
        {
            double severityNumber;
            switch (severity)
            {
                case "Low":
                    severityNumber = 1;
                    break;
                case "Medium":
                    severityNumber = 2;
                    break;
                case "High":
                    severityNumber = 3;
                    break;
                case "No Bleeding":
                default:
                    severityNumber = 0;
                    break;
            }
            return severityNumber;
        }
    }
}
