using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Positioning;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.Text.RegularExpressions;

namespace DataView2.GrpcService.Services.Positioning
{
    public class OdoDataService : IOdoDataService
    {
        private readonly IRepository<OdoData> _repository;
        private readonly AppDbContextProjectData _context;

        public OdoDataService(IRepository<OdoData> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task ProcessOdoFile(string filePath, SurveyIdRequest surveyRequestId)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Odo file not found.", filePath);
            }

            var lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length < 2)
            {
                throw new InvalidDataException("Odo file contains insufficient data.");
            }


            // Extract folder name
            var folderName = new DirectoryInfo(Path.GetDirectoryName(filePath)).Name;

        

            // Extract SurveyName (everything before last underscore and 10 digits)
            int underscoreIndex = folderName.LastIndexOf('_');
            string surveyName = underscoreIndex > 0 ? folderName[..underscoreIndex] : folderName;



            // Remove the first line (Factor line)
            var dataLines = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            var odoDataList = new List<OdoData>();

            foreach (var line in dataLines)
            {
                string cleanLine = line.Substring(1); // Remove first character if needed

                var fields = cleanLine.Split(',');

                if (fields.Length < 5)
                {
                    continue; // Skip invalid lines
                }

                try
                {
                    var odoData = new OdoData
                    {
                        Chainage = double.Parse(fields[0]),
                        OdoCount = int.Parse(fields[1]),
                        OdoTime = int.Parse(fields[2]),
                        Speed = double.Parse(fields[3]),
                        SystemTime = long.Parse(fields[4]),
                        SurveyId = surveyRequestId.SurveyId,
                        SurveyName = surveyName
                    };

                    odoDataList.Add(odoData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line: {line}, Exception: {ex.Message}");
                }
            }

            if (odoDataList.Any())
            {
                await _context.OdoData.AddRangeAsync(odoDataList);
                await _context.SaveChangesAsync();
            }
        } 

        public Task<IdReply> Create(OdoData request, CallContext context = default)
        {
            throw new NotImplementedException();
        }

        public Task<IdReply> DeleteObject(OdoData request, CallContext context = default)
        {
            throw new NotImplementedException();
        }

        public Task<OdoData> EditValue(OdoData request, CallContext context = default)
        {
            throw new NotImplementedException();
        }

        public async Task<List<OdoData>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<OdoData>(entities);
        }

        public Task<OdoData> GetById(IdRequest request, CallContext context = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetOdoDataBySurveyId(string surveyId)
        {
            throw new NotImplementedException();
        }
    }
}
