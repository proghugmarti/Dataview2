using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Positioning;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DataView2.Core.Helper.TableNameHelper;

namespace DataView2.Core.Helper
{
    public class GeneralHelper
    {
        public static List<string> insertqueries = new List<string>();
        public static List<string> deletequeries = new List<string>();

        //Generic Function for inserting queries
        public static void GetInsertQueries<T>(List<T> dataList, string tableName)
        {
            try
            {
                // Delete existing records
                if (tableName == typeof(Survey).Name)
                {
                    IEnumerable<T> distinctList = dataList.DistinctBy(x => new { SurveyIdExternal = GetPropertyValue(x, "SurveyIdExternal")}).AsEnumerable();
                    foreach (var group in distinctList)
                    {
                        string deleteQuery = $"DELETE FROM {tableName} WHERE SurveyIdExternal='{GetPropertyValue(group, "SurveyIdExternal")}';";
                        if (!deletequeries.Contains(deleteQuery))
                            deletequeries.Add(deleteQuery);
                    }
                }
                else if (tableName == typeof(LCMS_Segment).Name)
                {
                    IEnumerable<T> distinctList = dataList.DistinctBy(x => new { SurveyId = GetPropertyValue(x, "SurveyId"), SegmentId = GetPropertyValue(x, "SectionId") }).AsEnumerable();
                    foreach (var group in distinctList)
                    {
                        string deleteQuery = $"DELETE FROM {tableName} WHERE SurveyId='{GetPropertyValue(group, "SurveyId")}' AND SectionId={GetPropertyValue(group, "SectionId")};";
                        if (!deletequeries.Contains(deleteQuery))
                            deletequeries.Add(deleteQuery);
                    }
                }
                else
                {
                    IEnumerable<T> distinctList = dataList.DistinctBy(x => new { SurveyId = GetPropertyValue(x, "SurveyId"), SegmentId = GetPropertyValue(x, "SegmentId") }).AsEnumerable();
                    foreach (var group in distinctList)
                    {
                        string deleteQuery = $"DELETE FROM {tableName} WHERE SurveyId='{GetPropertyValue(group, "SurveyId")}' AND SegmentId={GetPropertyValue(group, "SegmentId")};";
                        if (!deletequeries.Contains(deleteQuery))
                            deletequeries.Add(deleteQuery);
                    }
                }

                // Get insert query template
                string query = string.Empty;
                string partquery = string.Empty;

                //added to manage query length data overflow exception
                int batchSize = 500;
                for (int i = 0; i < dataList.Count; i += batchSize)
                {
                    var batch = dataList.Skip(i).Take(batchSize).ToList();

                    foreach (var item in batch)
                    {
                        var values = new List<string>();
                        var columns = new List<string>();

                        foreach (var property in typeof(T).GetProperties())
                        {
                            // Skip the 'Id' property
                            if (property.Name == "Id")
                            {
                                continue; // Skip adding 'Id' to the values
                            }

                            columns.Add(property.Name);

                            var value = GetPropertyValue(item, property.Name);

                            if (value == null)
                            {
                                values.Add("@NULL");
                            }
                            else
                            {
                                // Check if the type is string or char and enclose in quotes
                                var type = property.PropertyType;
                                if (value is DateTime dateValue)
                                {
                                    values.Add($"'{dateValue:yyyy-MM-dd HH:mm:ss}'");
                                }
                                else if (type == typeof(string) || type == typeof(char))
                                {
                                    values.Add($"'{value}'");
                                }
                                else
                                {
                                    // For numeric or other types, no quotes
                                    values.Add(value.ToString());
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(query))
                        {
                            query = $"({string.Join(',', columns)})";
                        }

                        partquery = string.Concat(partquery, "(" + string.Join(',', values) + "),");

                        //if (string.Concat("INSERT INTO ", tableName, query, " VALUES ", partquery).Length > (Int16.MaxValue * 4))
                        {
                            insertqueries.Add(string.Concat("INSERT INTO ", tableName, query, " VALUES ", partquery.TrimEnd(',') + ';'));
                            partquery = string.Empty;
                        }
                    }

                    if (!string.IsNullOrEmpty(partquery))
                    {
                        insertqueries.Add(string.Concat("INSERT INTO ", tableName, query, " VALUES ", partquery.TrimEnd(',') + ';'));
                    }

                    //disposing objects
                    query = string.Empty;
                    partquery = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetInsertQueries for {typeof(T).Name} : {ex.Message}");
            }
        }

        public static string ConvertColorToHex(System.Drawing.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
        }

        public static System.Drawing.Color ConvertHexToColor(string rgbaHex)
        {
            string argbHex = "#" + rgbaHex.Substring(7, 2) + rgbaHex.Substring(1, 6);
            return ColorTranslator.FromHtml(argbHex);
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }

        public static double[] ConvertToGPSCoordinates(double x, double y, double longitude, double latitude, double trackAngle)
        {
            if (x == 0 && y == 0)
            {
                double[] coordinate = { longitude, latitude };
                return coordinate;
            }

            double delta_X = x;
            double delta_Y = y;
            double relativeAngle = (180 / Math.PI) * Math.Atan2(delta_X, delta_Y);
            double offset = Math.Sqrt(Math.Pow(delta_X, 2) + Math.Pow(delta_Y, 2)) / 1000;
            double heading = trackAngle + relativeAngle;
            double bearing = heading % 360.0;
            (double convLatitude, double convLongitude) = SurveyUtility.GPSPositionFromBearingAndOffset(latitude, longitude, bearing, offset);

            double[] result = { convLongitude, convLatitude };
            return result;
        }

        public enum GeoType
        {
            Polygon,
            Polyline,
            Point,
            MultiPoint,
            MultiPolygon
        }

        public static string CreateNewGeoJson(GeoType type, JToken coordinates, string id, string file, string tableName, string extraId = null)
        {
            var properties = new JObject(); // Create properties object separately

            if (!string.IsNullOrEmpty(id))
            {
                properties["id"] = id;
            }

            switch (tableName)
            {
                case "Potholes":
                    if (double.TryParse(extraId, out double diameter))
                    {
                        properties["diameter"] = diameter;
                    }
                    break;
                case LayerNames.CurbDropOff:
                    tableName = extraId;
                    break;
            }

            if (!string.IsNullOrEmpty(file))
            {
                properties["file"] = file;
            }

            properties["type"] = tableName;

            var geoJson = new JObject
            {
                ["type"] = "Feature",
                ["geometry"] = new JObject
                {
                    ["type"] = type.ToString(),
                    ["coordinates"] = coordinates
                },
                ["properties"] = properties // Assign properties here
            };

            return geoJson.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
