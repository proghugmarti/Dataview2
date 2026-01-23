using CsvHelper;
using CsvHelper.Configuration;
using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DotSpatial.Data.MiscUtil;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using ProtoBuf.Grpc;
using Serilog;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class FODService : BaseService<LCMS_FOD, IRepository<LCMS_FOD>>, IFODService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<LCMS_FOD> _repository;

        public FODService(IRepository<LCMS_FOD> repository, AppDbContextProjectData context) : base(repository)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IdReply> ProcessFOD(FODRequest request, CallContext context = default)
        {
            
            var fodList = new List<LCMS_FOD>();
            var surveys = new HashSet<Survey>();

            int processedFilesCount = 0;
            int notProcessedFilesCount = 0;
            bool hasProcessedFiles = false;
            var paths = request.Paths;

            foreach (var path in paths)
            {
                try
                {
                    using (var reader = new StreamReader(path))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        PrepareHeaderForMatch = args => args.Header.ToLower(),
                        MissingFieldFound = null
                    }))
                    {
                        csv.Read();
                        csv.ReadHeader();

                        if (!csv.HeaderRecord.Contains("FOD ID") || !csv.HeaderRecord.Contains("Survey ID"))
                        {
                            notProcessedFilesCount++;
                            continue;
                        }

                        bool surveySaved = false;
                        string surveyIdExternal = string.Empty;

                        while (csv.Read())
                        {
                            var FODID = SafeGetField(csv, "FOD ID");
                            var surveyId = SafeGetField(csv, "Survey ID");

                            var section = SafeGetField(csv, "Section");
                            
                            var segmentId = Regex.Match(section, @"\d+").Value;
                            int.TryParse(segmentId, out int segmentIdInt);

                            var severity = SafeGetField(csv, "Severity");
                            var area = SafeGetDouble(csv, "Area");
                            var volume = SafeGetDouble(csv, "Volume");
                            var maximumHeight = SafeGetDouble(csv, "MaximumHeight");
                            var latitude = SafeGetDouble(csv, "Latitude");
                            var longitude = SafeGetDouble(csv, "Longitude");
                            var altitude = SafeGetDouble(csv, "Altitude");
                            var trackAngle = SafeGetDouble(csv, "Track Angle");

                            var detectionDateString = PreprocessDateString(csv.GetField("Detection Date"));
                            var recoveryDateString = PreprocessDateString(csv.GetField("Recovery Date"));
                            var dateFormats = new[] {
                                "dd/MM/yyyy h:mm:ss tt",  
                                "dd/MM/yyyy HH:mm",    
                            };
                            DateTime detectionDate;
                            DateTime recoveryDate;
                            if (!DateTime.TryParseExact(detectionDateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out detectionDate))
                            {
                                // If parsing fails, use today's date as the fallback
                                detectionDate = DateTime.Today;
                            }

                            // Try to parse Recovery Date
                            if (!DateTime.TryParseExact(recoveryDateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out recoveryDate))
                            {
                                // If parsing fails, use today's date as the fallback
                                recoveryDate = DateTime.Today;
                            }

                            var FODDescription = csv.GetField("FOD Description");
                            if (String.IsNullOrEmpty(FODDescription))
                            {
                                FODDescription = "Not Defined";
                            }

                            var Operator = SafeGetField(csv, "Operator");
                            var imageFile = SafeGetField(csv, "ImageFile"); 
                            var avgHeight = SafeGetDouble(csv, "Average Height");
                            var Status = SafeGetField(csv, "Status");
                            var comments = SafeGetField(csv, "Comments");
                            var reasonnotrecovery = SafeGetField(csv, "Reason Not Recovery");
                            var length = SafeGetDouble(csv, "Length");
                            var width = SafeGetDouble(csv, "Width");

                            var imagePath = SafeGetField(csv, "ImageFile");

                            double[] coordinate = { longitude, latitude };

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Point",
                                    coordinates = coordinate
                                },
                                properties = new
                                {
                                    id = FODID,
                                    file = Path.GetFileName(imagePath),
                                    type = "FOD"
                                }
                            };
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            // Save survey information from the first line
                            if (!surveySaved)
                            {
                                var existingSurvey = await _context.Survey.FirstOrDefaultAsync(s => s.SurveyName == surveyId);
                                if (existingSurvey != null)
                                {
                                    surveyIdExternal = existingSurvey.SurveyIdExternal;
                                }
                                else
                                {
                                    string directoryPath = Path.GetDirectoryName(imagePath);
                                    int index = directoryPath.LastIndexOf("\\ImageFull");
                                    string fullPathBeforeImageFull = directoryPath.Substring(0, index);
                                    surveyIdExternal = detectionDate.ToString("ddMMyyyy");

                                    var survey = new Survey
                                    {
                                        SurveyName = surveyId,
                                        SurveyDate = detectionDate,
                                        ImageFolderPath = fullPathBeforeImageFull,
                                        SurveyIdExternal = detectionDate.ToString("ddMMyyyy"), //temporary surveyId for saving to survey table
                                        DataviewVersion = request.Version,
                                        GPSLatitude = latitude,
                                        GPSLongitude = longitude,
                                        
                                    };

                                    await _context.Survey.AddAsync(survey);
                                    await _context.SaveChangesAsync(); // Ensure changes are saved to the databas
                                }

                                surveySaved = true; // Mark survey as saved
                            }

                            var fod = new LCMS_FOD
                            {
                                FODID = FODID,
                                SurveyId = surveyIdExternal,
                                SurveyName = surveyId,
                                SegmentId = segmentIdInt,
                                Severity = severity,
                                Area = area,
                                Volume = volume,
                                MaximumHeight = maximumHeight,
                                GPSLatitude = latitude,
                                GPSLongitude = longitude,
                                GPSAltitude = altitude,
                                GPSTrackAngle = trackAngle,
                                RoundedGPSLatitude = Math.Round(latitude, 4),
                                RoundedGPSLongitude = Math.Round(longitude, 4),
                                DetectionDate = detectionDate,
                                RecoveryDate = recoveryDate,
                                FODDescription = FODDescription,
                                GeoJSON = jsonData,
                                Operator = Operator,
                                ImageFile = imageFile,
                                AverageHeight = avgHeight,
                                Status = Status,
                                Comments = comments,
                                ReasonNoRecovery = reasonnotrecovery,
                                FODLength_mm = length,
                                FODWidth_mm = width
                            };
                            if (!await ExistsAsync(fod))
                            {
                                fodList.Add(fod);
                            }
                            else
                            {
                                hasProcessedFiles = true;
                            }
                        }
                        processedFilesCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing file {Path.GetFileName(path)}: {ex.Message}");
                }
            }

            if (fodList.Count > 0 || hasProcessedFiles)
            {
                // Save FODList to the database
                await _repository.CreateRangeAsync(fodList);
                return new IdReply
                {
                    Id = 0,
                    Message = notProcessedFilesCount > 0
                     ? $"Imported FODs from {processedFilesCount} file(s). {notProcessedFilesCount} file{(notProcessedFilesCount > 1 ? "s were" : " was")} not processed due to incorrect format."
                :   "All FODs were successfully imported."
                };
            }
            else
            {
                string message = "No FOD detected.";
                if (notProcessedFilesCount > 0)
                {
                    message = $"No FOD detected. {notProcessedFilesCount} file{(notProcessedFilesCount > 1 ? "s were" : " was")} not processed due to incorrect format.";
                }

                return new IdReply
                {
                    Id = -1,
                    Message = message
                };
            }
        }

        private double SafeGetDouble(CsvReader csv, string field)
        {
            var value = csv.GetField(field);
            return double.TryParse(value, out var result) ? result : 0.0;
        }

        private string SafeGetField(CsvReader csv, string field)
        {
            return csv.GetField(field) ?? "";
        }

        private string PreprocessDateString(string dateString)
        {
            // Ensure single-digit days and months have leading zeros
            var datePattern = @"(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4}) (?<hour>\d{1,2}):(?<minute>\d{2}):(?<second>\d{2}) (?<ampm>[APap][Mm])";
            var match = Regex.Match(dateString, datePattern);

            if (match.Success)
            {
                var day = int.Parse(match.Groups["day"].Value).ToString("00");
                var month = int.Parse(match.Groups["month"].Value).ToString("00");
                var year = match.Groups["year"].Value;
                var hour = int.Parse(match.Groups["hour"].Value).ToString("00");
                var minute = match.Groups["minute"].Value;
                var second = match.Groups["second"].Value;
                var ampm = match.Groups["ampm"].Value.ToUpper();

                return $"{day}/{month}/{year} {hour}:{minute}:{second} {ampm}";
            }

            return dateString;
        }

        public async Task<bool> ExistsAsync(LCMS_FOD fod)
        {
            return await _context.LCMS_FOD.AnyAsync(f =>
                f.FODID == fod.FODID &&
                f.SurveyId == fod.SurveyId &&
                f.SegmentId == fod.SegmentId &&
                f.DetectionDate == fod.DetectionDate);
        }

        public async Task<IEnumerable<LCMS_FOD>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_FOD.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_FOD>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_FOD.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<LCMS_FOD> EditValue(LCMS_FOD request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<LCMS_FOD> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_FOD();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
