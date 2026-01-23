using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Positioning;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.Packages.Lcms;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using Serilog;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;


namespace DataView2.GrpcService.Services.Positioning
{
    public class GPS_RawService : IGPS_RawService
    {
        private readonly IRepository<GPS_Raw> _repository;
        private readonly AppDbContextProjectData _context;

        public GPS_RawService (IRepository<GPS_Raw> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task ProcessGPSrawFile(string filePath, SurveyIdRequest surveyIdRequest)
        {
            // Extract SurveyIdExternal from filename (last 10 digits)
            string surveyIdExternal = Regex.Matches(filePath, @"\d+").Cast<Match>().Last().Value;


            var gpsRawList = new List<GPS_Raw>();


            //foreach (var line in File.ReadLines(filePath))
            List<string> lines = File.ReadLines(filePath).ToList();
            Log.Warning($"Total GPS data lines {lines.Count}");

            int unwantedLines = 0;
            List<(double ,double)> ggaGpsList = new List<(double, double)>();
            double? lastRoll = null;
            double? lastPitch = null;
            double? lastYaw = null;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = JsonSerializer.Deserialize<GpsDataEntry>(line);
                    if (entry?.NmeaLine == null || entry.OdoDataRecord?.Time == null)
                        continue;

                    var parts = entry.NmeaLine.Split(',');
                    if (parts.Length < 6) continue;

                    if (!parts[0].EndsWith("RMC") && !parts[0].EndsWith("GGA") &&!parts[0].EndsWith("PASHR"))
                    {
                        unwantedLines++;
                        continue;
                    }

                    if (parts[0].EndsWith("PASHR"))
                    {
                        // Example: $PASHR,001457.000,227.708,T,-3.785,0.704,,0.044,0.029,0.027,2,3*19
                        if (parts.Length >= 5)
                        {
                            lastYaw = ParseDouble(parts[2]);
                            lastRoll = ParseDouble(parts[3]);
                            lastPitch = ParseDouble(parts[4]);
                        }
                        continue; // Skip adding GPS_Raw for PASHR itself
                    }

                    double systemTime = entry.OdoDataRecord.Time.Value;
                    double chainage = entry.OdoDataRecord.Chainage ?? 0.0; // Default to 0 if null
                    int utcTime = ParseUtcTime(parts[1]);

                    switch (true)
                    {
                        case bool _ when parts[0].EndsWith("RMC"):
                            gpsRawList.Add(new GPS_Raw
                            {
                                Chainage = chainage,
                                Latitude = ParseCoordinate(parts[3], parts[4]),
                                Longitude = ParseCoordinate(parts[5], parts[6]),
                                Heading = parts.Length > 8 ? ParseDouble(parts[8]) : 0.0,
                                SystemTime = systemTime,
                                UTCTime = utcTime,
                                SurveyId = surveyIdRequest.SurveyId,
                                Roll = lastRoll,
                                Pitch = lastPitch,
                                Yaw = lastYaw
                            });
                            break;

                        case bool _ when parts[0].EndsWith("GGA"):
                            //Handle GGA
                            ggaGpsList.Add((ParseCoordinate(parts[2], parts[3]), ParseCoordinate(parts[4], parts[5])));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line: {line}\n{ex.Message}");
                }
            }

            List<double> ggaHeadings = CalculateHeadings(ggaGpsList);
            List<double> rmcHeadings = gpsRawList.Select(g=>g.Heading).ToList();
            if (ggaHeadings.Count > 0 && rmcHeadings.Count > 0 && ggaHeadings.Count == rmcHeadings.Count)
            {
                for (int i = 0; i < ggaHeadings.Count; i++)
                {
                    gpsRawList[i].Heading = ggaHeadings[i];
                }
            }

            Log.Warning($"Unwanted lines = {unwantedLines}");
            Log.Warning($"Useful GPS data lines = {lines.Count - unwantedLines}");

            await _context.GPS_Raw.AddRangeAsync(gpsRawList);
            await _context.SaveChangesAsync();
        }

        #region GGA Heading Calculation
        public List<double> CalculateHeadings(List<(double Lat, double Lon)> coords)
        {
            List<double> headings = new List<double>();

            for (int i = 1; i < coords.Count; i++)
            {
                var (lat1, lon1) = coords[i - 1];
                var (lat2, lon2) = coords[i];

                double heading = GetBearing(lat1, lon1, lat2, lon2);
                headings.Add(heading);
            }

            return headings;
        }

        public double GetBearing(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert to radians
            double φ1 = DegreesToRadians(lat1);
            double φ2 = DegreesToRadians(lat2);
            double Δλ = DegreesToRadians(lon2 - lon1);

            double y = Math.Sin(Δλ) * Math.Cos(φ2);
            double x = Math.Cos(φ1) * Math.Sin(φ2) - Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);
            double θ = Math.Atan2(y, x);

            // Convert to degrees and normalize to 0–360
            double bearing = (RadiansToDegrees(θ) + 360) % 360;
            return bearing;
        }

        private double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
        private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
        #endregion


        private double ParseCoordinate(string value, string direction)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(direction))
                return 0;

            try
            {
                double degrees = Math.Truncate(double.Parse(value) / 100);
                double minutes = double.Parse(value) - (degrees * 100);
                double coordinate = degrees + (minutes / 60);

                coordinate = Math.Round(coordinate * 100000000000000) / 100000000000000;

                return direction.ToUpper() switch
                {
                    "S" or "W" => -coordinate,
                    _ => coordinate
                };
            }
            catch
            {
                return 0;
            }
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0;
        }
        private int ParseUtcTime(string timeStr)
        {

            if (string.IsNullOrEmpty(timeStr)) return 0;

            try
            {
                double raw = double.Parse(timeStr, CultureInfo.InvariantCulture);
                int hours = (int)(raw / 10000);
                int minutes = (int)((raw % 10000) / 100);
                double seconds = raw % 100;

                double totalSeconds = (hours * 3600) + (minutes * 60) + seconds;
                return (int)(totalSeconds * 1000); // convert to milliseconds
            }
            catch
            {
                return 0;
            }
        }



       

        public async Task<List<GPS_Raw>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<GPS_Raw>(entities);
        }

    }
}
