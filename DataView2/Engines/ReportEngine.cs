using ClosedXML.Excel;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Protos;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DataView2.Engines
{
    public class ReportEngine
    {
        private readonly List<Report> _reports;
        private readonly string _jsonFilePath;
        private GenerateReportService.GenerateReportServiceClient _generateReportService;
        public ReportEngine(GenerateReportService.GenerateReportServiceClient generateReportService)
        {
            var exePath = AppContext.BaseDirectory;
            _jsonFilePath = Path.Combine(exePath, "Report", "reports.json");
            // Load reports from JSON file on initialization
            _reports = LoadReportsFromFile();
            _generateReportService = generateReportService ?? throw new ArgumentNullException(nameof(generateReportService));
        }
        private List<Report> LoadReportsFromFile()
        {
            if (!File.Exists(_jsonFilePath))
                return new List<Report>();
            var json = File.ReadAllText(_jsonFilePath);
            return JsonSerializer.Deserialize<List<Report>>(json);
        }
        public List<Report> GetReports()
        {
            return _reports;
        }
        public string ExecuteReportFunction(string functionName, params object[] parameters)
        {
            var method = this.GetType().GetMethod(functionName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (method == null)
                throw new NotImplementedException($"Report function '{functionName}' not implemented");
            var result = method.Invoke(this, new object[] { parameters });
            if (result != null)
                return result.ToString();
            return string.Empty;
        }
        private string GenerateSeoulCityReport(params object[] parameters)
        {
            // Extract parameters safely
            string paramRoadType = parameters.Length > 0 ? parameters[0] as string : null;
            List<Survey> selectedSurveys = parameters.Length > 1 ? parameters[1] as List<Survey> : new List<Survey>();
            string saveFilepath = parameters.Length > 2 ? parameters[2] as string : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Convert selected survey IDs to comma-separated string
            string selectedSurveyIds = selectedSurveys != null
                ? string.Join(",", selectedSurveys.Select(s => s.SurveyName.ToString()))
                : string.Empty;

            var request = new GenerateReportObjRequest
            {
                SelectedSurveys = selectedSurveyIds,
                SavePathDirectory = saveFilepath,
                FunctionName = "GenerateSeoulCityReport",
                Param1Type = paramRoadType
            };

            GenerateReportObjResponse exporting = _generateReportService.GenerateReportData(request);
            var lblMessage = exporting.Message;
            return lblMessage;
        }
    }
}
