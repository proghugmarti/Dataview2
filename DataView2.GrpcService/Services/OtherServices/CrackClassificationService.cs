using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services.ProcessingServices;
using Esri.ArcGISRuntime.Tasks.Offline;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml;
using static DataView2.Core.Helper.crackClassificationImage;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class CrackClassificationService : ICrackClassification
    {
        double ResolutionX = 1;
        double ResolutionY = 1;
        double ImageWidth;
        double ImageHeight;
        int squareSize = 250;
        int matrixWidth;
        int matrixHeight;

        int segmentsectionId;
        long segmentsurveyId;
        string strSegmentsectionId;
        string strSegmentsurveyId;
        double chainageSegment;

        double squareHorizontalSize = 10;
        double squareVerticalSize = 10;
        int intSquareHorizontalSize = 10;
        int intSquareVerticalSize = 10;

        double DistanceBegin_m = 0;
        double DistanceEnd_m = 0;
        double LaneMarkingModule_Parameters = 0;
        double GeneralParam_WheelPathWidth_mm = 0;
        double GeneralParam_CentralBandWidth_mm = 0;
        double SectionLength_m = 0;
        double LaneMarkLeft = 0;
        double LaneMarkRight = 0;
        double LeftWheelPathLeft = 0;
        double LeftWheelPathRight = 0;
        double RightWheelPathLeft = 0;
        double RightWheelPathRight = 0;

        double cellHorizontalmm = 0;
        double cellVerticalmm = 0;
        double cellAreaM = 0;

        int colLeftWheelPathLeft = 0;
        int colLeftWheelPathRight = 0;
        int colRightWheelPathLeft = 0;
        int colRightWheelPathRight = 0;
        int colLaneMarkRight = 0;
        int colLaneMarkLeft = 0;

        string xmlFile;

        resultType[,]? array;
        resultType[,]? arrayLong;
        resultType[,]? arrayTrans;

        int[,]? arrayId;
        int[,]? crackCounter;

        List<Crack> listCracks = new List<Crack>();
        List<CrackNode> listCrackCrackNodes = new List<CrackNode>();
        List<CrackInMatrix> groupedCells = new List<CrackInMatrix>();
        public List<SummaryCrackClasification> summaries = new List<SummaryCrackClasification>();

        //These variables can be set by the users
        double straightness = 0.7;
        double minDeep = 0;
        int minSizeToStraight = 4;
        int minSizeToAvoidMerge = 6;
        bool ignoreOutLanes = true;

        //Segment Grid:
        double longitude = 0.0;
        double latitude = 0.0;
        double altitude = 0.0;
        double trackAngle = 0.0;
        string severityCalc = "0";
        bool resultRenderWidth = true;  // True: Define if use ResultRenderer_CrackSeverityN_MaxWidth_mm 
        
        double crackSevLow = 0.0;
        double crackSevLowMed = 0.0;
        double crackSevMedHigh = 0.0;
        double crackSevHigh = 0.0;

        bool isLaneMarkedFromData = true;
        string strClassifiedArrayJson = string.Empty;

        public struct cellMatrixSeverity()
        {
            public int posX;
            public int posY;
            public string severity;
        }

        List<cellMatrixSeverity> cellsWithSeverity = new List<cellMatrixSeverity>();
        List<LCMS_Segment_Grid> lCMS_Segment_Grids = new List<LCMS_Segment_Grid>();

        private readonly ICrackClassificationConfiguration crackConfiguration;
        private readonly IRepository<SummaryCrackClasification> _repository;
        private readonly AppDbContextProjectData _context;

        public CrackClassificationService(ICrackClassificationConfiguration crackConfiguration, IRepository<SummaryCrackClasification> repository, AppDbContextProjectData context)
        {
            this.crackConfiguration = crackConfiguration;
            _repository = repository;
            _context = context;
        }

        public async Task<List<SummaryCrackClasification>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<SummaryCrackClasification>(entities);
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<SummaryCrackClasification> EditValue(SummaryCrackClasification request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<IdReply> DeleteObject(SummaryCrackClasification request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Selecteed record deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = request.Id,
                Message = "Selecteed record is failed to be deleted."
            };

        }

        public async Task<IdReply> ReclassifyBatch(XmlFilesRequest request, CallContext context = default)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return new IdReply
                {
                    Id = 0,
                    Message = "Operation Cancelled"
                };
            }

            summaries.Clear();

            //Retrieving variables set by the users
            var classficationSetting = await crackConfiguration.GetClassification(new Empty());
            if (classficationSetting != null)
            {
                straightness = classficationSetting.Straightness;
                minDeep = classficationSetting.MinimumDeep;
                minSizeToStraight = classficationSetting.MinSizeToStraight;
                minSizeToAvoidMerge = classficationSetting.MinSizeToAvoidMerge;
                ignoreOutLanes = classficationSetting.IgnoreOutLanes;
                crackSevLow = classficationSetting.LowThreshold;
                crackSevLowMed = classficationSetting.LowMediumThreshold;
                crackSevMedHigh = classficationSetting.MediumHighThreshold;
                crackSevHigh = classficationSetting.HighThreshold;
            }

            var filePaths = request.XmlFilesPath.Where(file => file.EndsWith(".xml")).ToList();

            const int chunkSize = 10000;
            var segmentSurveyPairs = new HashSet<(int, string)>();
            var gridsToInsert = new List<LCMS_Segment_Grid>();

            foreach (var file in filePaths)
            {
                xmlFile = Path.GetFileNameWithoutExtension(file);
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                getGeneralVariables(doc);  // Load General Information from XML
                getCracks(doc);   // Load Crack Points from XML
                await Reclassify(request.CallFromUI);

                if (!request.CallFromUI)
                {
                    segmentSurveyPairs.Add((Convert.ToInt32(strSegmentsectionId), strSegmentsurveyId));
                    gridsToInsert.AddRange(lCMS_Segment_Grids);
                }

                // When gridsToinsert is holding more than 10000, process and clear
                if (gridsToInsert.Count >= chunkSize)
                {
                    await SaveSegmentGridToDatabase(segmentSurveyPairs.ToList(), gridsToInsert);
                    gridsToInsert.Clear();
                    segmentSurveyPairs.Clear(); // Clear to reclaim memory
                }

                if (context.CancellationToken.IsCancellationRequested)
                {
                    return new IdReply
                    {
                        Id = 0,
                        Message = "Operation Cancelled"
                    };
                }
            }

            if (gridsToInsert.Any())
            {
                await SaveSegmentGridToDatabase(segmentSurveyPairs.ToList(), gridsToInsert);
            }

            if (!request.CallFromUI)
                await SaveToDatabase(summaries);


            return new IdReply
            {
                Id = 0,
                //Message = "Crack classification Saved to the Database"
                Message = strClassifiedArrayJson
            };
        }

        private async Task SaveSegmentGridToDatabase(List<(int segmentId, string surveyId)> segmentSurveyPairs, List<LCMS_Segment_Grid> segmentGrids)
        {
            try
            {
                if (segmentGrids.Count > 0)
                {
                    //await _context.LCMS_Segment_Grid.Where(sg => sg.SegmentId == segmentId && sg.SurveyId == surveyId).ExecuteDeleteAsync();
                    //delete existing segment grid
                    var segmentIds = string.Join(",", segmentSurveyPairs.Select(p => $"({p.segmentId}, '{p.surveyId}')"));
                    string sqlDelete = $"DELETE FROM LCMS_Segment_Grid WHERE (SegmentId, SurveyId) IN ({segmentIds})";
                    await _context.Database.ExecuteSqlRawAsync(sqlDelete);
                    await _context.LCMS_Segment_Grid.AddRangeAsync(segmentGrids);

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Segment Grid: Error saving Data base - Message: {ex.Message}");
            }
        }

        public async Task SaveToDatabase(List<SummaryCrackClasification> entities)
        {
            foreach (var entity in entities)
            {
                await _repository.CreateAsync(entity);
            }
        }

        private async Task Reclassify(bool CallFromUI = false)
        {
            cleanArray();

            if (ignoreOutLanes && isLaneMarkedFromData)
            {
                await addOffRoadInfo();
            }

            await addCracksToMatrix();  //Add the Crack Points to the Matrix as Pending Cells, also complete the lines when is the same Crack

            await ClassifyCracksInMatrix();
            // Join adjancent cells and create objects each object is evaluated according to straightness
            // This is for long and transversal.

            await MergeAdjacentCracks();

            if (!ignoreOutLanes && isLaneMarkedFromData)
            {
                await addOffRoadInfo();
            }

            await addLaneToMatrix();  // Add the Road Information, overwrite offroad cells
                                      //and change "other" cracks that are overlapped with wheelpath as "Alligator"  cracks

            ProcessResults();

            strClassifiedArrayJson = string.Empty;
            if (CallFromUI)
            {
                try
                {
                    strClassifiedArrayJson = Newtonsoft.Json.JsonConvert.SerializeObject(array);
                    strClassifiedArrayJson += "&" + Newtonsoft.Json.JsonConvert.SerializeObject(summaries);
                }
                catch (Exception ex) { Log.Error($"Error in converting json @ Reclassify : {ex.Message}"); }
            }
        }

        public  Task<IdReply> GetSummaries(Empty empty, CallContext context = default)
        {
            ProcessResults();

            IdReply idReply = new IdReply { Id = 0, Message = Newtonsoft.Json.JsonConvert.SerializeObject(summaries) };
            return Task.FromResult(idReply);
        }

        public void ProcessResults() 
        {
            try
            {
                SummaryCrackClasification classification = new SummaryCrackClasification();

                classification.Chstart = DistanceBegin_m;
                classification.Chend = DistanceEnd_m;
                classification.LaneWidth = LaneMarkingModule_Parameters;
                classification.SampleArea = SectionLength_m * (LaneMarkingModule_Parameters / 1000);

                List<int> dicLongCounted = new List<int>();

                List<AreaInMatrix> fatigueAreas = new List<AreaInMatrix>();

                lCMS_Segment_Grids.Clear();
                int squareId = 1, fila = 0;
                double[] previousCoordinate4 = null;
                double[] previousCoordinate2 = null;

                LCMS_Segment_Grid[,] CoordinatesMatrix = new LCMS_Segment_Grid[matrixHeight, matrixWidth];

                for (int i = 0; i < matrixHeight; i++)
                {
                    for (int j = 0; j < matrixWidth; j++)
                    {
                        severityCalc = getCellsSeverityByCoord(cellsWithSeverity, i, j);

                        if (IsInWheelPath(i, j))
                        {
                            classification.WheelpathsArea += cellAreaM;

                            if (array[i, j] != resultType.WheelPath)
                            {
                                classification.crackingWheelpaths += cellAreaM;
                            }
                        }
                        switch (array[i, j])
                        {
                            case resultType.Longitudinal:
                                if (IsInWheelPath(i, j) && !dicLongCounted.Contains(i))
                                {
                                    classification.LongitudinalCracking += squareVerticalSize;
                                    dicLongCounted.Add(i);
                                }

                                if (crackCounter[i, j] <= 1)
                                {
                                    classification.LongCrackVeryLOW = classification.LongCrackVeryLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 2)
                                {
                                    classification.LongCrackLOW = classification.LongCrackLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 3)
                                {
                                    classification.LongCrackMED = classification.LongCrackMED + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 4)
                                {
                                    classification.LongCrackHIGH = classification.LongCrackHIGH + cellAreaM;
                                }
                                else
                                {
                                    classification.LongCrackVeryHIGH = classification.LongCrackVeryHIGH + cellAreaM;
                                }
                                break;
                            case resultType.Transversal:

                                classification.TransverseCracking += squareHorizontalSize;

                                if (crackCounter[i, j] <= 1)
                                {
                                    classification.TransCrackVeryLOW = classification.TransCrackVeryLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 2)
                                {
                                    classification.TransCrackLOW = classification.TransCrackLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 3)
                                {
                                    classification.TransCrackMED = classification.TransCrackMED + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 4)
                                {
                                    classification.TransCrackHIGH = classification.TransCrackHIGH + cellAreaM;
                                }
                                else
                                {
                                    classification.TransCrackVeryHIGH = classification.TransCrackVeryHIGH + cellAreaM;
                                }
                                break;
                            case resultType.Alligator:
                                if (crackCounter[i, j] <= 1)
                                {
                                    classification.AlligatorCrackVeryLOW = classification.AlligatorCrackVeryLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 2)
                                {
                                    classification.AlligatorCrackLOW = classification.AlligatorCrackLOW + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 3)
                                {
                                    classification.AlligatorCrackMED = classification.AlligatorCrackMED + cellAreaM;
                                }
                                else if (crackCounter[i, j] == 4)
                                {
                                    classification.AlligatorCrackHIGH = classification.AlligatorCrackHIGH + cellAreaM;
                                }
                                else
                                {
                                    classification.AlligatorCrackVeryHIGH = classification.AlligatorCrackVeryHIGH + cellAreaM;
                                }

                                var nearFatigue = fatigueAreas.Where(p => p.MinX - 1 <= j && p.MaxX + 1 >= j && p.MinY - 1 <= i && p.MaxY + 1 >= i).FirstOrDefault();

                                if (nearFatigue != null)
                                {
                                    // Add Current cell to Fatige Area
                                    if (j < nearFatigue.MinX)
                                    {
                                        nearFatigue.MinX = j;
                                    }
                                    if (j > nearFatigue.MaxX)
                                    {
                                        nearFatigue.MaxX = j;
                                    }
                                    if (i < nearFatigue.MinY)
                                    {
                                        nearFatigue.MinY = i;
                                    }
                                    if (i > nearFatigue.MaxY)
                                    {
                                        nearFatigue.MaxY = i;
                                    }
                                }
                                else
                                {
                                    // Create new Fatige Area
                                    AreaInMatrix newFatigueArea = new AreaInMatrix()
                                    {
                                        MinX = j,
                                        MaxX = j,
                                        MinY = i,
                                        MaxY = i
                                    };
                                    fatigueAreas.Add(newFatigueArea);
                                }

                                break;
                        }

                        //Segment Grid:
                        double X = 0.0, Y = 0.0;
                        double[] coordinate1 = { 0.0, 0.0 };
                        if (i == 0 && j == 0)
                        {
                            coordinate1 = new double[] { longitude, latitude };
                        }
                        else if (i == 0 && j > 0)
                        {
                            coordinate1 = previousCoordinate4;
                        }
                        else if (i > 0 && j == 0)
                        {
                            coordinate1 = previousCoordinate2;
                        }
                        else if (i > 0 && j > 0)
                        {
                            coordinate1 = previousCoordinate4;
                        }

                        //Calculate Coordinates:
                        Core.Communication.GPSCoordinate segmentGridCoordinate = new Core.Communication.GPSCoordinate
                        {
                            Longitude = coordinate1[0],
                            Latitude = coordinate1[1],
                            Altitude = altitude,
                            TrackAngle = trackAngle
                        };
                        double[] coordinate2 = GeneralService.ConvertToGPSCoordinates(0, 250, segmentGridCoordinate);
                        double[] coordinate3 = GeneralService.ConvertToGPSCoordinates(250, 250, segmentGridCoordinate);
                        double[] coordinate4 = GeneralService.ConvertToGPSCoordinates(250, 0, segmentGridCoordinate);

                        if (j == 0)
                        {
                            previousCoordinate2 = coordinate2;
                        }

                        previousCoordinate4 = coordinate4;

                        var segmentGridJsonData = "";
                        var segmentGridJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                                {
                                            new List<double[]>()
                                            {
                                                coordinate1, coordinate2, coordinate3, coordinate4
                                            }
                            }
                            },
                            properties = new
                            {
                                id = squareId.ToString(),
                                file = xmlFile,
                                type = "Segment Grid", // array[i, j].ToString(), 
                                                       //severity = severityCalc,
                                x = X,
                                y = Y
                            }
                        };

                        try
                        {
                            segmentGridJsonData = JsonSerializer.Serialize(segmentGridJsonDataObject);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Segment Grid: Error in parsing Serialize XML file: {xmlFile} Message: {ex.Message}");
                        }

                        LCMS_Segment_Grid lCMS_Segment_Grid = new LCMS_Segment_Grid();
                        lCMS_Segment_Grid.CrackType = array[i, j].ToString(); // generalizeCrackType(array[i, j].ToString());
                        lCMS_Segment_Grid.Column = j;
                        lCMS_Segment_Grid.Row = i;
                        lCMS_Segment_Grid.SurveyId = strSegmentsurveyId;
                        lCMS_Segment_Grid.SegmentId = segmentsectionId;
                        lCMS_Segment_Grid.GPSLatitude = latitude;
                        lCMS_Segment_Grid.GPSLongitude = longitude;
                        lCMS_Segment_Grid.GPSAltitude = altitude;
                        lCMS_Segment_Grid.RoundedGPSLatitude = Math.Round(latitude, 4);
                        lCMS_Segment_Grid.RoundedGPSLongitude = Math.Round(longitude, 4);
                        lCMS_Segment_Grid.Severity = severityCalc; //Posible values: (None, Very Low, Low, Medium, High, Very High)
                        lCMS_Segment_Grid.GeoJSON = segmentGridJsonData;
                        lCMS_Segment_Grid.GPSTrackAngle = trackAngle;
                        lCMS_Segment_Grid.PavementType = "Asphalt";
                        lCMS_Segment_Grid.Chainage = chainageSegment + Y /1000;

                        if (!lCMS_Segment_Grid.Severity.Equals("None") && !lCMS_Segment_Grid.CrackType.Equals("WheelPath"))
                            lCMS_Segment_Grids.Add(lCMS_Segment_Grid);

                        squareId++;

                        CoordinatesMatrix[i, j] = new LCMS_Segment_Grid
                        {
                            Column = j,
                            Row = i,
                            GeoJSON = segmentGridJsonData,
                            Severity = severityCalc,
                            CrackType = array[i, j].ToString()
                        };
                    }
                }

                // Invert Grid direction:
                for (int col = 0; col < matrixWidth; col++)
                {
                    for (int row = 0; row < matrixHeight / 2; row++)
                    {
                        // Exchange the elements
                        LCMS_Segment_Grid temp = CoordinatesMatrix[row, col];
                        CoordinatesMatrix[row, col] = CoordinatesMatrix[matrixHeight - 1 - row, col];
                        CoordinatesMatrix[matrixHeight - 1 - row, col] = temp;
                    }
                }

                // Asign inverted values to lCMS_Segment_Grids
                if (lCMS_Segment_Grids.Count > 0)
                {
                    for (int col = 0; col < matrixWidth; col++)
                    {
                        for (int row = 0; row < matrixHeight; row++)
                        {
                            if (lCMS_Segment_Grids.FirstOrDefault(segment => segment.Column == col && segment.Row == row) != null)
                                lCMS_Segment_Grids.FirstOrDefault(segment => segment.Column == col && segment.Row == row).GeoJSON = CoordinatesMatrix[row, col].GeoJSON;
                            //lCMS_Segment_Grids.First(segment => segment.Column == col && segment.Row == row).Severity = CoordinatesMatrix[row, col].Severity;
                            //lCMS_Segment_Grids.First(segment => segment.Column == col && segment.Row == row).CrackType = CoordinatesMatrix[row, col].CrackType;
                        }
                    }
                }

                foreach (var elem in fatigueAreas)
                {
                    classification.FatigueArea += (elem.TotalArea() * cellAreaM);
                }
                classification.XmlFileName = xmlFile;

                summaries.Add(classification);
            }
            catch(Exception ex)
            {
                Serilog.Log.Error($"Error in ProcessResult : {ex.Message}");
            }
        }

        public async void getGeneralVariables(XmlDocument doc)
        {
            try
            {
                // those variables should be general by the class

                strSegmentsurveyId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SurveyID").InnerText;
                strSegmentsectionId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SectionID").InnerText;
                chainageSegment = GetDouble(doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/DistanceBegin_m").InnerText);

                if (!String.IsNullOrEmpty(strSegmentsurveyId))
                {
                    segmentsurveyId = long.Parse(strSegmentsurveyId);
                }

                if (!String.IsNullOrEmpty(strSegmentsectionId))
                {
                    segmentsectionId = Int32.Parse(strSegmentsectionId);
                }

                LaneMarkingModule_Parameters = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/LaneMarkingModule_Parameters/LaneMarkingModule_RoadWidth_mm");
                GeneralParam_CentralBandWidth_mm = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_CentralBandWidth_mm");
                GeneralParam_WheelPathWidth_mm = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_WheelPathWidth_mm");
                ImageWidth = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageWidth");
                ImageHeight = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageHeight");

                ResolutionX = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionX");
                ResolutionY = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionY");

                matrixWidth = (int)(ImageWidth * ResolutionX) / squareSize;
                matrixHeight = (int)(ImageHeight * ResolutionY) / squareSize;

                array = new resultType[matrixHeight, matrixWidth];
                arrayLong = new resultType[matrixHeight, matrixWidth];
                arrayTrans = new resultType[matrixHeight, matrixWidth];

                arrayId = new int[matrixHeight, matrixWidth];
                crackCounter = new int[matrixHeight, matrixWidth];

                DistanceBegin_m = await getDoubleFromXML(doc, "/LcmsAnalyserResults/RoadSectionInfo/DistanceBegin_m");
                DistanceEnd_m = await getDoubleFromXML(doc, "/LcmsAnalyserResults/RoadSectionInfo/DistanceEnd_m");
                SectionLength_m = await getDoubleFromXML(doc, "/LcmsAnalyserResults/RoadSectionInfo/SectionLength_m");

                var xmlNodeList = doc.SelectNodes("/LcmsAnalyserResults/LaneMarkInformation/LaneMark");
                isLaneMarkedFromData = true;
                if (xmlNodeList == null)
                {
                    return;
                }

                if (xmlNodeList.Count == 0)
                {
                    LaneMarkLeft = 0;
                    LaneMarkRight = ImageWidth * ResolutionX;
                    isLaneMarkedFromData = false;
                }

                foreach (XmlNode xmlOneLane in xmlNodeList)
                {
                    var xmlSideName = xmlOneLane.SelectSingleNode("LaneSide");
                    var xmlPosition = xmlOneLane.SelectSingleNode("Position");

                    if (xmlSideName == null || xmlPosition == null)
                    {
                        continue;
                    }

                    var strSideName = xmlSideName.InnerText;
                    if (strSideName == "Left")
                    {
                        LaneMarkLeft = Convert.ToDouble(xmlPosition.InnerText);
                    }
                    else
                    {
                        LaneMarkRight = Convert.ToDouble(xmlPosition.InnerText);
                    }
                }

                var middleLine = (LaneMarkRight + LaneMarkLeft) / 2;
                var CentralBandWidthLeft = middleLine - (GeneralParam_CentralBandWidth_mm / 2);
                var CentralBandWidthRight = middleLine + (GeneralParam_CentralBandWidth_mm / 2);

                LeftWheelPathRight = CentralBandWidthLeft;
                LeftWheelPathLeft = CentralBandWidthLeft - GeneralParam_WheelPathWidth_mm;

                RightWheelPathLeft = CentralBandWidthRight;
                RightWheelPathRight = CentralBandWidthRight + GeneralParam_WheelPathWidth_mm;

                squareHorizontalSize = ImageWidth / matrixWidth;
                intSquareHorizontalSize = (int)squareHorizontalSize;

                squareVerticalSize = ImageHeight / matrixHeight;
                intSquareVerticalSize = (int)squareVerticalSize;

                cellHorizontalmm = ImageWidth * ResolutionX / matrixWidth;
                cellVerticalmm = ImageHeight * ResolutionY / matrixHeight;

                cellAreaM = (cellHorizontalmm / 1000) * (cellVerticalmm / 1000);

                if (intSquareVerticalSize < 20 || intSquareHorizontalSize < 20)
                {
                    double scaleTable = (intSquareVerticalSize < intSquareHorizontalSize) ? (20 / squareVerticalSize) : (20 / squareHorizontalSize);
                    intSquareHorizontalSize = (int)(squareHorizontalSize * scaleTable);
                    intSquareVerticalSize = (int)(squareVerticalSize * scaleTable);
                }

                colLeftWheelPathLeft = toXCells(LeftWheelPathLeft);
                colLeftWheelPathRight = toXCells(LeftWheelPathRight);
                colRightWheelPathLeft = toXCells(RightWheelPathLeft);
                colRightWheelPathRight = toXCells(RightWheelPathRight);

                colLaneMarkRight = toXCells(LaneMarkRight);
                colLaneMarkLeft = toXCells(LaneMarkLeft);

                //Segment Grid:

                ////Severity Calculation Units
                //var resultRenderList = doc.SelectNodes("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/ResultRenderer_Parameters");
                //var resultRender = resultRenderList[0];
                //crackSevLow = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity0_MaxWidth_mm");
                //crackSevMed = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity1_MaxWidth_mm");
                //crackSevHigh = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity2_MaxWidth_mm");


                //if (crackSevLow == 0.0)
                //{
                //    resultRenderWidth = false;
                //    crackSevLow = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity0_MaxDepth_mm");
                //    crackSevMed = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity1_MaxDepth_mm");
                //    crackSevHigh = getDoubleFromNode(resultRender, "ResultRenderer_CrackSeverity2_MaxDepth_mm");
                //}

                //Coordinates Definition
                var sectionPos = doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition");
                if (sectionPos != null)
                {
                    //longitude = Convert.ToDouble(doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition/Longitude")?.InnerText);
                    //latitude = Convert.ToDouble(doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition/Latitude")?.InnerText);
                    altitude = Convert.ToDouble(doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition/Altitude")?.InnerText);
                    trackAngle = Convert.ToDouble(doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition/Heading")?.InnerText);
                }
                var coordinatesSegment = _context.LCMS_Segment
                                 .Where(sg => sg.SegmentId == segmentsectionId && sg.SurveyId == strSegmentsurveyId)
                                 .Select(sg => new { sg.GPSLongitude, sg.GPSLatitude, sg.GeoJSON })
                                 .ToList();
                if (coordinatesSegment.Count > 0)
                {

                    longitude = coordinatesSegment[0].GPSLongitude;
                    latitude = coordinatesSegment[0].GPSLatitude;
                    string geoJson = coordinatesSegment[0].GeoJSON;
                    var jsonDoc = JsonDocument.Parse(geoJson);
                    var coordinates = jsonDoc.RootElement
                             .GetProperty("geometry")
                             .GetProperty("coordinates")[0];
                    SurveySections.gpsInfo to = new SurveySections.gpsInfo()
                    {
                        Latitude = coordinates[1][1].GetDouble(),
                        Longitude = coordinates[1][0].GetDouble()
                    };

                    SurveySections.gpsInfo from = new SurveySections.gpsInfo()
                    {
                        Latitude = latitude,
                        Longitude = longitude
                    };
                    trackAngle = GeneralService.CalculateBearing(from, to);
                }
            }
            catch (Exception ex)
            {
                Utils.RegError($"Crack Classification Service - Error when get GeneralVariables: {ex.Message}");
            }

        }

        public void getCracks(XmlDocument doc)
        {
            //List<double> promCracksWidthList = new List<double>();
            //List<double> promCracksDepthList = new List<double>();
            listCracks.Clear();
            listCrackCrackNodes.Clear();

            XmlNode? xmlCracks = doc.SelectSingleNode("LcmsAnalyserResults/CrackInformation/CrackList");
            if (xmlCracks == null)
            {
                return;
            }

            XmlNodeList? xmlCrackList = xmlCracks.SelectNodes("Crack");
            if (xmlCrackList == null)
            {
                return;
            }

            foreach (XmlNode xmlACrack in xmlCrackList)
            {
                Crack crack = new Crack()
                {
                    Id = getIntFromNode(xmlACrack, "CrackID"),
                    WeightedWidth = getDecFromNode(xmlACrack, "WeightedWidth"),
                    WeightedDepth = getDecFromNode(xmlACrack, "WeightedDepth"),
                    CrackType = Crack.CrackTypeEnum.Unknown,
                };

                var xmlNodeList = xmlACrack.SelectNodes("Node");

                if (xmlNodeList == null)
                {
                    return;
                }

                crack.MaxX = crack.MaxY = 0;
                crack.MinX = ImageWidth * ResolutionX;
                crack.MinY = ImageHeight * ResolutionY;

                List<CrackNode> tempCrackNodeList = new List<CrackNode>();

                bool[,] alreadyCounted = new bool[matrixHeight, matrixWidth];

                foreach (XmlNode xmlANode in xmlNodeList)
                {
                    double widthCrack = getDoubleFromNode(xmlANode, "Width");
                    double depthCrack = getDoubleFromNode(xmlANode, "Depth");
                    CrackNode crackNode = new CrackNode()
                    {
                        CrackId = crack.Id,
                        X = getDoubleFromNode(xmlANode, "X"),
                        Y = getDoubleFromNode(xmlANode, "Y"),
                        Depth = getDoubleFromNode(xmlANode, "Depth"),
                        Width = widthCrack
                    };
                    //promCracksWidthList.Add(widthCrack);
                    //promCracksDepthList.Add(depthCrack);

                    var inttoYCells = toYCells(crackNode.Y);
                    var inttoXCells = toXCells(crackNode.X);

                    crackNode.colX = inttoXCells;
                    crackNode.colY = inttoYCells;

                    if (inttoXCells < colLaneMarkLeft || inttoXCells > colLaneMarkRight)
                    {
                        continue;
                    }

                    crack.MaxX = (crack.MaxX > crackNode.X) ? crack.MaxX : crackNode.X;
                    crack.MaxY = (crack.MaxY > crackNode.Y) ? crack.MaxY : crackNode.Y;
                    crack.MinX = (crack.MinX < crackNode.X) ? crack.MinX : crackNode.X;
                    crack.MinY = (crack.MinY < crackNode.Y) ? crack.MinY : crackNode.Y;

                    //Segment Grid:

                    //severityCalc = calcCellSeverity(resultRenderWidth == true ? widthCrack : depthCrack, crackSevLow, crackSevMed, crackSevHigh).Result;
                    severityCalc = calcCellSeverityModified(resultRenderWidth == true ? widthCrack : depthCrack, crackSevLow, crackSevLowMed, crackSevMedHigh, crackSevHigh).Result;

                    cellMatrixSeverity cellMatrix = new cellMatrixSeverity()
                    {
                        posX = inttoXCells,
                        posY = inttoYCells,
                        severity = severityCalc
                    };

                    if (!alreadyCounted[inttoYCells, inttoXCells])
                    {
                        crackCounter[inttoYCells, inttoXCells]++;
                        alreadyCounted[inttoYCells, inttoXCells] = true;
                        cellsWithSeverity.Add(cellMatrix);
                    }
                    else if (severityIsSuperior(severityCalc, getCellsSeverityByCoord(cellsWithSeverity, inttoXCells, inttoYCells)))
                    {
                        int index = cellsWithSeverity.FindIndex(c => c.posX == inttoXCells && c.posY == inttoYCells);
                        cellsWithSeverity[index] = cellMatrix;
                    }


                    tempCrackNodeList.Add(crackNode);
                }

                crack.colMinX = toXCells(crack.MinX);
                crack.colMaxX = toXCells(crack.MaxX);

                crack.colMinY = toYCells(crack.MaxY);  // MaxY is in opposite Direction than column system, so, MaxY is the minimun column Y (down on the image)
                crack.colMaxY = toYCells(crack.MinY);

                //  This Analysis is just for small lines direction
                if (crack.CrackType == Crack.CrackTypeEnum.Unknown)
                {
                    if (crack.MaxX - crack.MinX > crack.MaxY - crack.MinY)
                    {
                        crack.CrackType = Crack.CrackTypeEnum.Transversal;
                    }
                    else
                    {
                        crack.CrackType = Crack.CrackTypeEnum.Longitudinal;
                    }
                }

                listCrackCrackNodes.AddRange(tempCrackNodeList);
                listCracks.Add(crack);
            }
        }

        //Segment Grid - Severity Calculation
        private Task<string> calcCellSeverity(double value, double crackSevLow, double crackSevMed, double crackSevHigh)
        {
            string severityCell = "None";

            if (value < crackSevLow)
            {
                severityCell = "Very Low";
            }
            else if (value >= crackSevLow && value < crackSevMed)
            {
                severityCell = "Low";
            }
            else if (value >= crackSevMed && value <= crackSevHigh)
            {
                severityCell = "Med";
            }
            else if (value > crackSevHigh)
            {
                severityCell = "High";
            }

            return Task.FromResult(severityCell);
        }

        private Task<string> calcCellSeverityModified(double value, double crackSevLow, double crackSevLowMed, double crackSevMedHigh, double crackSevHigh)
        {
            string severityCell = "Very Low";

            if (value < crackSevLow)
            {
                severityCell = "Very Low";
            }
            else if (value >= crackSevLow && value < crackSevLowMed)
            {
                severityCell = "Low";
            }
            else if (value >= crackSevLowMed && value <= crackSevMedHigh)
            {
                severityCell = "Med";
            }
            else if (value >= crackSevMedHigh && value <= crackSevHigh)
            {
                severityCell = "High";
            }
            else if (value > crackSevHigh)
            {
                severityCell = "Very High";
            }

            return Task.FromResult(severityCell);

        }

        private bool severityIsSuperior(string severity, string maxSeverity)
        {
            List<string> severities = new List<string> { "Very Low", "Low", "Medium", "High", "Very High" };
            if (severities.IndexOf(severity) > severities.IndexOf(maxSeverity))
            {
                return true;
            }
            return false;
        }

        private string getCellsSeverityByCoord(List<cellMatrixSeverity> cellsWithSeverity, int xPos, int yPos)
        {
            int index = cellsWithSeverity.FindIndex(c => c.posX == xPos && c.posY == yPos);
            return index != -1 ? cellsWithSeverity[index].severity : "None";
        }
        
        private string generalizeCrackType(string crackType) {
           
            if (crackType == "WheelPath" || crackType == "Offroad") {
                return "None";
            }
            else if (crackType == "Unknown")
            {
                return "Others";
            }
            else if (crackType == "Alligator")
            {
                return "Fatigue";
            }
            else { return crackType; }           
        }

        private Task addCracksToMatrix()
        {
            int previousY, previousX;
            previousY = previousX = -1;
            // when the classification is made later, we just mark the points and not the entire box, so we can analyse in the next step
            foreach (Crack c1 in listCracks)
            {
                int iCrackId = c1.Id;

                List<CrackNode> nodesInThisCrack = listCrackCrackNodes.Where(p => p.CrackId == iCrackId).ToList();
                previousY = previousX = -1;

                if (nodesInThisCrack == null)
                {
                    continue;
                }


                if (c1.WeightedDepth < (decimal)minDeep)
                {
                    continue;
                }

                foreach (var node1 in nodesInThisCrack)
                {

                    if (array[node1.colY, node1.colX] != resultType.Offroad)
                    {
                        array[node1.colY, node1.colX] = resultType.Pending;

                        if (c1.CrackType == Crack.CrackTypeEnum.Longitudinal)
                        {
                            arrayLong[node1.colY, node1.colX] = resultType.Longitudinal;
                        }
                        if (c1.CrackType == Crack.CrackTypeEnum.Transversal)
                        {
                            arrayTrans[node1.colY, node1.colX] = resultType.Transversal;
                        }

                        int verticalDistance = Math.Abs(previousY - node1.colY);
                        int horizontalDistance = Math.Abs(previousX - node1.colX);

                        if (previousX < 0)
                        {
                            verticalDistance = 0;
                            horizontalDistance = 0;
                        }


                        if ((horizontalDistance > 1) || (verticalDistance > 1))
                        {
                            // Vertical Distance is Bigger (Longitudinal Cracks)
                            if (verticalDistance > horizontalDistance)
                            {
                                double dif = (double)horizontalDistance / (double)verticalDistance;
                                double acumJ = Math.Min(previousX, node1.colX);
                                int j = (int)acumJ;
                                for (int i = Math.Min(previousY, node1.colY) + 1; i < Math.Max(previousY, node1.colY); i++)
                                {
                                    array[i, j] = resultType.Pending;
                                    acumJ += dif;
                                    j = (int)Math.Round(acumJ);
                                }
                            }
                            // Horizonta Distance is Bigger (Transversal Cracks)
                            else
                            {
                                double dif = (double)verticalDistance / (double)horizontalDistance;
                                double acumI = Math.Min(previousY, node1.colY);
                                int i = (int)acumI;
                                for (int j = Math.Min(previousX, node1.colX) + 1; j < Math.Max(previousX, node1.colX); j++)
                                {
                                    array[i, j] = resultType.Pending;
                                    acumI += dif;
                                    i = (int)Math.Round(acumI);
                                }
                            }
                        }
                    }

                    previousX = node1.colX;
                    previousY = node1.colY;
                }

            }

            return Task.CompletedTask;
        }

        private async Task MergeAdjacentCracks()
        {
            foreach (var othCrackGroup in groupedCells)
            {
                var listStraightAdjancentGroupedCells = GetAdjacentCracks(othCrackGroup);

                foreach (var adjacentCrack in listStraightAdjancentGroupedCells)
                {
                    if (MergedStraight(othCrackGroup, adjacentCrack) > straightness)
                    {
                        if (othCrackGroup.CrackType != Crack.CrackTypeEnum.Unknown && othCrackGroup.Length() > adjacentCrack.Length())
                        {
                            await MergeGroups(othCrackGroup, adjacentCrack);
                        }
                        else
                        {
                            await MergeGroups(adjacentCrack, othCrackGroup);
                        }
                    }
                };
            }

            foreach (var othCrackGroup in groupedCells.Where(x => x.CrackType == Crack.CrackTypeEnum.Unknown && x.Cells.Count() > 0))
            {
                var listStraightAdjancentGroupedCells = GetAdjacentCracks(othCrackGroup);

                if (listStraightAdjancentGroupedCells.Count == 0)
                {
                    addToArray(othCrackGroup);
                }

                foreach (var adjacentCrack in listStraightAdjancentGroupedCells)
                {
                    if (adjacentCrack.CrackType != Crack.CrackTypeEnum.Unknown && adjacentCrack.Length() >= minSizeToAvoidMerge)
                    {
                        continue;
                    }
                    else
                    {
                        await MergeGroups(othCrackGroup, adjacentCrack);
                    }
                }
            }
        }

        private List<CrackInMatrix> GetAdjacentCracks(CrackInMatrix crack1)
        {
            List<CrackInMatrix> adjacent = new List<CrackInMatrix>();

            //Parallel.ForEach(crack1.Cells,point =>


            foreach (var point in crack1.Cells)
            {

                int i = point.Y;
                int j = point.X;

                int minI = (i > 1) ? i - 1 : i;
                int maxI = (i + 1 >= matrixHeight) ? i : i + 1;
                int minJ = (j > 1) ? j - 1 : j;
                int maxJ = (j + 1 >= matrixWidth) ? j : j + 1;

                for (int subI = minI; subI <= maxI; subI++)
                {
                    for (int subJ = minJ; subJ <= maxJ; subJ++)
                    {
                        if (subI != i || subJ != j)
                        {
                            if (arrayId[subI, subJ] != crack1.Id)
                            {
                                var crackAdj = groupedCells.Where(x => x.Id == arrayId[subI, subJ]).FirstOrDefault();
                                if (crackAdj != null)
                                {
                                    if (!adjacent.Contains(crackAdj))
                                    {
                                        adjacent.Add(crackAdj);
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return adjacent;
        }

        private async Task MergeGroups(CrackInMatrix crackA, CrackInMatrix crackB)
        {

            Parallel.ForEach(crackB.Cells,
                            point =>
                            {
                                array[point.Y, point.X] = (crackA.CrackType == Crack.CrackTypeEnum.Unknown) ? resultType.Other : (resultType)System.Enum.Parse(typeof(resultType), crackA.CrackType.ToString());
                                arrayId[point.Y, point.X] = crackA.Id;
                            });

            crackA.Cells.AddRange(crackB.Cells);
            crackB.Cells.Clear();
            //        groupedCells.Remove(crackB);

        }

        double MergedStraight(CrackInMatrix crackA, CrackInMatrix crackB)
        {
            if (crackA.Cells.Count > 0 && crackB.Cells.Count > 0)
            {
                var mergedXDistance = (double)(1 + Math.Max(crackA.MaxX, crackB.MaxX) - Math.Min(crackA.MinX, crackB.MinX));
                var mergedYDistance = (double)(1 + Math.Max(crackA.MaxY, crackB.MaxY) - Math.Min(crackA.MinY, crackB.MinY));

                double Distance = (double)Math.Max(mergedXDistance, mergedYDistance);

                return Distance / (double)(crackA.Cells.Count() + crackB.Cells.Count());
            }
            else
            {
                return (double)Math.Max(crackA.Straightness(), crackB.Straightness());
            }

        }
        private async Task ClassifyCracksInMatrix()
        {
            groupedCells = new List<CrackInMatrix>();
            int crackCounter = 1;
            #region GetMatrixCellsInGroups
            for (int i = 0; i < matrixHeight; i++)
            {
                for (int j = 0; j < matrixWidth; j++)
                {
                    if (array[i, j] == resultType.Pending)
                    {
                        CrackInMatrix crack = new CrackInMatrix(crackCounter++);
                        await AddAdjacentCells(crack, i, j, (arrayLong[i, j] == resultType.Longitudinal) ? resultType.Longitudinal : arrayTrans[i, j]);
                        groupedCells.Add(crack);
                    }
                }
            }
            #endregion

            #region DefineEachGroupAsCrackType

            foreach (var currentElement in groupedCells)

            //Parallel.ForEach(groupedCells,
            //    currentElement =>
            {


                if (currentElement.XDistance() >= currentElement.YDistance() && currentElement.XDistance() >= minSizeToStraight && currentElement.Straightness() >= straightness)
                {
                    currentElement.CrackType = Crack.CrackTypeEnum.Transversal;
                }
                if (currentElement.YDistance() >= currentElement.XDistance() && currentElement.YDistance() >= minSizeToStraight && currentElement.Straightness() >= straightness)
                {
                    currentElement.CrackType = Crack.CrackTypeEnum.Longitudinal;
                }

                if (currentElement.CrackType != Crack.CrackTypeEnum.Unknown)
                {
                    //foreach (var point in currentElement.Cells)
                    //{
                    //    array[point.Y, point.X] = (ProcessingXMLCrackFilePage.resultType)Enum.Parse(typeof(ProcessingXMLCrackFilePage.resultType), currentElement.CrackType.ToString());
                    //}
                    Parallel.ForEach(currentElement.Cells,
                            point =>
                            {
                                array[point.Y, point.X] = (resultType)System.Enum.Parse(typeof(resultType), currentElement.CrackType.ToString());
                            });
                }
            }
            #endregion
            return;
        }

        private async Task AddAdjacentCells(CrackInMatrix crack, int i, int j, resultType previousType)
        {
            if (array[i, j] == resultType.Pending && (arrayLong[i, j] == previousType || arrayTrans[i, j] == previousType))
            {
                CrackInMatrix.PointInMatrix pointInMatrix = new CrackInMatrix.PointInMatrix(i, j);
                crack.Cells.Add(pointInMatrix);
                array[i, j] = resultType.Other;
                arrayId[i, j] = crack.Id;

                int minI = (i > 1) ? i - 1 : i;
                int maxI = (i + 1 >= matrixHeight) ? i : i + 1;
                int minJ = (j > 1) ? j - 1 : j;
                int maxJ = (j + 1 >= matrixWidth) ? j : j + 1;

                for (int subI = minI; subI <= maxI; subI++)
                {
                    for (int subJ = minJ; subJ <= maxJ; subJ++)
                    {
                        if (subI != i || subJ != j)
                        {
                            await AddAdjacentCells(crack, subI, subJ, previousType);
                        }
                    }
                }
            }

            return;
        }

        public int toXCells(double X)
        {
            return (int)((X / (double)ResolutionX) / (double)squareHorizontalSize);
        }

        public int toYCells(double Y)
        {
            return (matrixHeight - 1) - (int)((Y / ResolutionY) / squareVerticalSize);
        }

        private void addToArray(CrackInMatrix crack)
        {
            Parallel.ForEach(crack.Cells,
                               point =>
                               {
                                   array[point.Y, point.X] = (crack.CrackType == Crack.CrackTypeEnum.Unknown) ? resultType.Other : (resultType)System.Enum.Parse(typeof(resultType), crack.CrackType.ToString());
                                   arrayId[point.Y, point.X] = crack.Id;
                               });

        }

        private Task addOffRoadInfo()
        {
            for (int i = 0; i < matrixHeight; i++)
            {
                for (int j = 0; j < colLaneMarkLeft; j++)
                {
                    array[i, j] = resultType.Offroad;
                }
                for (int j = matrixWidth - 1; j > colLaneMarkRight; j--)
                {
                    array[i, j] = resultType.Offroad;
                }
            }

            return Task.CompletedTask;
        }

        private Task addLaneToMatrix()
        {
            for (int i = 0; i < matrixHeight; i++)
            {
                for (int j = colLeftWheelPathLeft; j <= colLeftWheelPathRight; j++)
                {
                    addWheelPath(i, j);
                }
                for (int j = colRightWheelPathLeft; j <= colRightWheelPathRight; j++)
                {
                    addWheelPath(i, j);
                }
            }
            return Task.CompletedTask;
        }

        private async Task addWheelPath(int i, int j)
        {
            if (array[i, j] == resultType.Unknown)
            {
                array[i, j] = resultType.WheelPath;
            }
            if (array[i, j] == resultType.Other)
            {
                array[i, j] = resultType.Alligator;
                await ChangeCrackTypeToAlligator(i, j);
            }
        }

        private Task ChangeCrackTypeToAlligator(int i, int j)
        {
            var id1 = arrayId[i, j];

            var theCrack = groupedCells.Where(x => x.Id == id1 && x.CrackType == Crack.CrackTypeEnum.Unknown).FirstOrDefault();

            if (theCrack != null)

            {
                theCrack.CrackType = Crack.CrackTypeEnum.Alligator;
                Parallel.ForEach(theCrack.Cells,
                point =>
                {
                    array[point.Y, point.X] = resultType.Alligator;
                });
            }

            return Task.CompletedTask;
        }
      
        private bool IsInWheelPath(int i, int j)
        {
            return ((j >= colLeftWheelPathLeft && j <= colLeftWheelPathRight) || (j >= colRightWheelPathLeft && j <= colRightWheelPathRight));
        }

        public async Task<double> getDoubleFromXML(XmlDocument doc, string fieldName)
        {
            double result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Convert.ToDouble(xmlselNode.InnerText);
            }

            return result;
        }

        public double getDoubleFromNode(XmlNode doc, string fieldName)
        {
            double result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Convert.ToDouble(xmlselNode.InnerText);
            }

            return result;
        }

        public int getIntFromNode(XmlNode doc, string fieldName)
        {
            int result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Convert.ToInt32(xmlselNode.InnerText);
            }

            return result;
        }

        public Decimal getDecFromNode(XmlNode doc, string fieldName)
        {
            Decimal result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Convert.ToDecimal(xmlselNode.InnerText);
            }

            return result;
        }

        public void cleanArray()
        {

            for (int i = 0; i < matrixHeight; i++)
            {
                for (int j = 0; j < matrixWidth; j++)
                {
                    array[i, j] = resultType.Unknown;
                    arrayId[i, j] = 0;
                }
            }
        }

        public async Task<IEnumerable<SummaryCrackClasification>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.SummaryCrackClasifications.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<SummaryCrackClasification>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.SummaryCrackClasifications.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        private double GetDouble(string param)
        {
            try
            {
                if (double.TryParse(param, out double result))
                {
                    // Check if the result is NaN
                    if (double.IsNaN(result))
                    {
                        return -1;  // or another default value of your choice
                    }
                    return Math.Round(result, 2);
                }

                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}
