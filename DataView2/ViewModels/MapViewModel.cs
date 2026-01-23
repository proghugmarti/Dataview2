using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Positioning;
using DataView2.Engines;
using DataView2.MapHelpers;
using DataView2.States;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Google.Protobuf.WellKnownTypes;
using Markdig.Helpers;
using MIConvexHull;
using MudBlazor;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
 using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static DataView2.Core.Helper.GeneralHelper;
using static DataView2.Core.Helper.TableNameHelper;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace DataView2.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private List<DatasetBackup> backupList;
        public List<DatasetBackup> BackupList
        {
            get { return backupList; }
            set
            {
                backupList = value;
                OnPropertyChanged(nameof(BackupList));
            }
        }
        int running;
        DateTime? dateTime;

        public MapViewModel()
        {
            appEngine = MauiProgram.AppEngine;
            appState = MauiProgram.AppState;

            appState.OnTableCheckedChanged += HandleTableCheckedChanged;
            appState.OnOverlayVisibilityToggled += HandleOverlayVisibilityChanged;
            appState.OnSurveySelected += ShowSelectedSurveyGraphics;

            //Layer Editor
            appState.GraphicColorRequested += GetGraphicColor;
            appState.BasicGraphicColorSet += SetBasicGraphicColor;
            appState.ColorCodeGraphicSet += SetColorCodeGraphic;

            appState.OnOfflineMapApplied += ApplyofflineMap;
            //appState.OnDatasetSelectedChanged += InitOnClear; it is already clearing from table refresh
            appState.GraphicsInMapCleared += InitOnClear;
            appState.OnBoundariesPassed += DrawBoundariesOnMap;
            appState.OnOutsideBoundaryRemovalRequested += RemoveDefectsOutsideBoundary;
            appState.OnPreviewOutsideBoundaryRequested += HighlightDefectsOutsideBoundary;
            appState.OnPreviewDoubleUpsRequested += HighlightDoubledUpDefects;
            appState.OnRevertRequested += RevertGraphicsToRemove;
            appState.OnTempBoundaryRemoved += RemoveTempBoundary;
            appState.SurveyTemplateHandled += HandleSurveySet;
            appState.OnMetaTableUpdated += UpdateMetaTableIcon;
            appState.OnSummariesFetchRequested += UpdateSummaries;
            appState.OnLayerSelected += LayerSelectionChanged;
            appState.OnDeleteDoubleUpRequested += DeleteDoubleUpsFromDB;
            appState.OnSampleUnitSummaryRequested += CreateSampleUnitSummaries;
            appState.OnSegmentIntervalSummaryRequested += CreateIntervalSummaries;
            
            List<DatasetBackup> options = backups;
            BackupList = options;
            _ = SetupMapAsync(); //initial load

            InitOnClear();
        }

        private void InitOnClear()
        {
            //Clear old data before initializing
            if (this.GraphicsOverlays != null)
            {
                //Clear all the graphics in each overlay
                foreach (var overlay in this.GraphicsOverlays)
                {
                    overlay.Graphics.Clear();
                }
                // Clear the graphics overlay collections
                this.GraphicsOverlays.Clear();
                overlays.Clear();
            }

            //Initializing the graphic layers
            segmentOverlay = new GraphicsOverlay() { Id = LayerNames.Segment };
            pickoutOverlay = new GraphicsOverlay() { Id = LayerNames.Pickout };
            crackingOverlay = new GraphicsOverlay() { Id = LayerNames.Cracking };
            ravellingOverlay = new GraphicsOverlay() { Id = LayerNames.Ravelling };
            potholeOverlay = new GraphicsOverlay() { Id = LayerNames.Potholes };
            patchOverlay = new GraphicsOverlay() { Id = LayerNames.Patch };
            concreteJointOverlay = new GraphicsOverlay() { Id = LayerNames.ConcreteJoint };
            cornerBreakOverlay = new GraphicsOverlay() { Id = LayerNames.CornerBreak };
            spallingOverlay = new GraphicsOverlay() { Id = LayerNames.Spalling };
            boundaryOverlay = new GraphicsOverlay() { Id = "Boundary" };
            fodOverlay = new GraphicsOverlay() { Id = LayerNames.FOD };
            bleedingOverlay = new GraphicsOverlay() { Id = LayerNames.Bleeding };
            curbDropOffOverlay = new GraphicsOverlay() { Id = LayerNames.CurbDropOff };
            pumpingOverlay = new GraphicsOverlay() { Id = LayerNames.Pumping };
            markingContourOverlay = new GraphicsOverlay() { Id = LayerNames.MarkingContour };
            sealedCrackOverlay = new GraphicsOverlay() { Id = LayerNames.SealedCrack };
            mmoOverlay = new GraphicsOverlay() { Id = LayerNames.MMO };
            rumbleStripOverlay = new GraphicsOverlay() { Id = LayerNames.RumbleStrip };

            textureOverlay = new GraphicsOverlay() { Id = MultiLayerName.BandTexture};
            avgTextureOverlay = new GraphicsOverlay() { Id = MultiLayerName.AverageTexture };

            roughnessLaneOverlay = new GraphicsOverlay() { Id = "Lane IRI" };
            roughnessLeftOverlay = new GraphicsOverlay() { Id = "Lwp IRI" };
            roughnessRightOverlay = new GraphicsOverlay() { Id = "Rwp IRI" };

            rutLeftOverlay = new GraphicsOverlay() { Id = "Left Rut" };
            rutRightOverlay = new GraphicsOverlay() { Id = "Right Rut" };
            rutLaneOverlay = new GraphicsOverlay() { Id = "Lane Rut" };

            shoveOverlay = new GraphicsOverlay() { Id = LayerNames.Shove };
            groovesOverlay = new GraphicsOverlay() { Id = LayerNames.Grooves };
            sagsbumpsOverlay = new GraphicsOverlay() { Id = LayerNames.SagsBumps };
            lasPointOverlay = new GraphicsOverlay() { Id = "LasPoints" };
            lasRuttingOverlay = new GraphicsOverlay() { Id = "LasRutting" };
            pciOverlay = new GraphicsOverlay() { Id = LayerNames.PCI };
            paserOverlay = new GraphicsOverlay() { Id = LayerNames.PASER };
            crackSummaryOverlay = new GraphicsOverlay() { Id = LayerNames.CrackSummary };
            geometryOverlay = new GraphicsOverlay() { Id = LayerNames.Geometry };
            surveySetOverlay = new GraphicsOverlay() { Id = "surveySetOverlay" };
            vehiclePathOverlay = new GraphicsOverlay() { Id = "Vehicle Path" };

            iNSGeometryOverlay = new GraphicsOverlay() { Id = "INS Geometry" };
            //Segment Grid:
            segmentGridLongitudinalOverlay = new GraphicsOverlay() { Id = "Longitudinal" };
            segmentGridTransversalOverlay = new GraphicsOverlay() { Id = "Transversal" };
            segmentGridFatigueOverlay = new GraphicsOverlay() { Id = "Fatigue" };
            //segmentGridOthersOverlay = new GraphicsOverlay() { Id = "Others" };

            overlays = new GraphicsOverlayCollection();
            this.GraphicsOverlays = overlays;

            loadedSegments.Clear();
            cameraOverlays?.Clear();

            GetMapGraphicData();
            GetColorCodeInfo();
        }

        private async void LayerSelectionChanged(string tableName, bool load, string type = null, List<string> newSurveys = null)
        {
            string isLoaded = load ? "Unload" : "Load";
            try
            {
                //add-remove graphics(select overlay by the given name) data as per the bool load
                IEnumerable<string> surveysIds = appState.SelectedSurveysIds.ToList();

                if (newSurveys != null && newSurveys.Count > 0)
                {
                    surveysIds = newSurveys;
                }

                if (surveysIds != null && surveysIds.Count() > 0)
                {
                    switch (tableName)
                    {
                        case "All":
                            if (load)
                                await FetchAndDisplayAllTablesBySurvey(surveysIds);
                            else
                                ClearAllGraphicsFromOverlays();
                            break;

                        case LayerNames.Segment:
                            if (load)
                            {
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSegmentForSurveyAsync);
                                }
                                appState.SegmentsLoad(segmentOverlay.Graphics.Where(g => g.IsVisible).Count());
                            }
                            else
                            {
                                segmentOverlay?.Graphics?.Clear();
                                loadedSegments = new List<LCMS_Segment>();
                            }
                            break;

                        case LayerNames.PCI:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPCIForSurveyAsync);
                                }
                            else
                                pciOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.MacroTexture:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMacroTextureForSurveyAsync);
                                }
                            else
                            {
                                textureOverlay?.Graphics?.Clear();
                                avgTextureOverlay?.Graphics?.Clear();
                            }
                            break;

                        case LayerNames.Bleeding:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchBleedingForSurveyAsync);
                                }
                            else
                                bleedingOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.ConcreteJoint:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchConcreteJointsForSurveyAsync);
                                }
                            else
                                concreteJointOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Ravelling:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRavellingForSurveyAsync);
                                }
                            else
                                ravellingOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Cracking:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCracksForSurveyAsync);
                                }
                            else
                                crackingOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Patch:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPatchProcessedForSurveyAsync);
                                }
                            else
                                patchOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Pickout:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPickoutsForSurveyAsync);
                                }
                            else
                                pickoutOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Potholes:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPotholesForSurveyAsync);
                                }
                            else
                                potholeOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Spalling:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSpallingForSurveyAsync);
                                }
                            else
                                spallingOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.CornerBreak:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCornerBreakForSurveyAsync);
                                }
                            else
                                cornerBreakOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.MarkingContour:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMarkingContourForSurveyAsync);
                                }
                            else
                                markingContourOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.SealedCrack:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSealedCrackForSurveyAsync);
                                }
                            else
                                sealedCrackOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Pumping:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPumpingForSurveyAsync);
                                }
                            else
                                pumpingOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.CurbDropOff:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCurbDropOffForSurveyAsync);
                                }
                            else
                                curbDropOffOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.MMO:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMMOForSurveyAsync);
                                }
                            else
                                mmoOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Roughness:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRoughnessForSurveyAsync);
                                }
                            else
                            {
                                roughnessLaneOverlay?.Graphics?.Clear();
                                roughnessRightOverlay?.Graphics?.Clear();
                                roughnessLeftOverlay?.Graphics?.Clear();
                                break;
                            }
                            break;

                        case LayerNames.RumbleStrip:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRumbleStripForSurveyAsync);
                                }
                            else
                                rumbleStripOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Rutting:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRuttingForSurveyAsync);
                                }
                            else
                            {
                                rutLaneOverlay?.Graphics?.Clear();
                                rutRightOverlay?.Graphics?.Clear();
                                rutLeftOverlay?.Graphics?.Clear();
                            }
                            break;

                        case LayerNames.Geometry:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchGeometryForSurveyAsync);
                                }
                            else
                                geometryOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Shove:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchShoveForSurveyAsync);
                                }
                            else
                                shoveOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.Grooves:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchGroovesForSurveyAsync);
                                }
                            else
                                groovesOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.SegmentGrid:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSegmentGridForSurveyAsync);
                                }
                            else
                            {
                                segmentGridOverlay?.Graphics?.Clear();
                                segmentGridFatigueOverlay?.Graphics?.Clear();
                                segmentGridLongitudinalOverlay?.Graphics?.Clear();
                                segmentGridTransversalOverlay?.Graphics?.Clear();
                            }
                            break;

                        case LayerNames.SagsBumps:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSagsBumpsForSurveyAsync);
                                }
                            else
                                sagsbumpsOverlay?.Graphics?.Clear();
                            break;

                        case "Boundary":
                            if (load)
                                await FetchBoundary();
                            else
                                boundaryOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.FOD:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchFODForSurveyAsync);
                                }
                            else
                                fodOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.PASER:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPASERForSurveyAsync);
                                }
                            else
                                paserOverlay?.Graphics?.Clear();
                            break;

                        case LayerNames.CrackSummary:
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCrackSummaryForSurveyAsync);
                                }
                            else
                                crackSummaryOverlay?.Graphics?.Clear();
                            break;

                        case "LasPoints":
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayLasFiles(surveyID);
                                }
                            else
                                lasPointOverlay?.Graphics?.Clear();
                            break;

                        case "LasRutting":
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchLasRuttingForSurveyAsync);
                                }
                            else
                                lasRuttingOverlay?.Graphics?.Clear();
                            break;

                        case "Vehicle Path":
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchVehiclePath(surveyID);
                                }
                            else
                                vehiclePathOverlay?.Graphics?.Clear();
                                break;
                        case "INS Geometry":
                            if (load)
                                foreach (string surveyID in surveysIds)
                                {
                                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchINSGeometryForSurveyAsync);
                                }
                            else
                                iNSGeometryOverlay?.Graphics?.Clear();
                            break;
                        case "Survey Segmentation":
                            await FetchSurveySegmentations();
                            break;

                        //handle by types not tableName
                        default:
                            if (load)
                            {
                                if (type == "Shapefile")
                                {
                                    await FetchShapefileByName(tableName);
                                }
                                else if (type == "MetaTable")
                                {
                                    await FetchMetaTableByName(tableName);
                                }
                                else if (type == "Summary")
                                {
                                    await FetchSummariesByName(tableName);
                                }
                                else if (type == "PCIDefect")
                                {
                                    await FetchPCIDefectsByName(tableName);
                                }
                                else if (type == "Video")
                                {
                                    await FetchAndDisplayVideoByName(tableName, surveysIds.ToList());
                                }
                                else if (type == "Keycode")
                                {
                                    await FetchandDisplayKeycodes(tableName, surveysIds.ToList());
                                }
                            }
                            else
                            {
                                if (type == "Video")
                                {
                                    if (cameraOverlays.ContainsKey(tableName))
                                    {
                                        //close the window if it is open
                                        appState.CloseVideoPopup(tableName);
                                        var matchingCamera = cameraOverlays.FirstOrDefault(x => x.Key == tableName);
                                        matchingCamera.Value?.Graphics.Clear();
                                        cameraOverlays.Remove(matchingCamera.Key);
                                    }
                                }
                                else
                                {
                                    var overlay = overlays.FirstOrDefault(o => o.Id == tableName);
                                    overlay?.Graphics?.Clear();
                                }
                            }

                            break;
                    }
                    ;
                }
            }
            catch (Exception ex)
            {
                isLoaded = load ? "Load" : "Unload";
                Log.Error($"Error in LayerSelectionChanged : {ex.Message}");
            }
            finally
            {
                if (newSurveys == null)
                {
                    appState.LayerSelectionCompleted(tableName, isLoaded);
                }
            }
        }

        List<DatasetBackup> backups = new List<DatasetBackup>
        {
            new DatasetBackup
            {
                Id = 1,
                Name = "Backup1",
                Description = "First backup",
                Timestamp = DateTime.Now,
                Path = "/path/to/backup1"
            },
            new DatasetBackup
            {
                Id = 2,
                Name = "Backup2",
                Description = "Second backup",
                Timestamp = DateTime.Now,
                Path = "/path/to/backup2"
            },
            new DatasetBackup
            {
                Id = 3,
                Name = "Backup3",
                Description = "Third backup",
                Timestamp = DateTime.Now,
                Path = "/path/to/backup3"
            }
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ApplicationState appState;
        public ApplicationEngine appEngine;
        public string activePage;

        private bool _isMapEnabled;
        public bool IsMapEnabled
        {
            get { return _isMapEnabled; }
            set
            {
                if (_isMapEnabled != value)
                {
                    _isMapEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private Map _map;
        public Map Map
        {
            get => _map;
            set
            {
                _map = value;
                OnPropertyChanged(nameof(Map));
            }
        }


        private Option selectedOption;
        public Option SelectedOption
        {
            get { return selectedOption; }
            set
            {
                selectedOption = value;
                OnPropertyChanged(nameof(SelectedOption));
            }
        }

        public ICommand RestoreLastBackupCommand { get; }

        private GraphicsOverlay boundaryOverlay;
        private GraphicsOverlay surveySetOverlay;
        private GraphicsOverlay segmentOverlay;
        private GraphicsOverlay pickoutOverlay;
        private GraphicsOverlay crackingOverlay;
        private GraphicsOverlay ravellingOverlay;
        private GraphicsOverlay potholeOverlay;
        private GraphicsOverlay patchOverlay;
        private GraphicsOverlay concreteJointOverlay;
        private GraphicsOverlay cornerBreakOverlay;
        private GraphicsOverlay spallingOverlay;
        private GraphicsOverlay fodOverlay;
        private GraphicsOverlay bleedingOverlay;
        private GraphicsOverlay curbDropOffOverlay;
        private GraphicsOverlay pumpingOverlay;
        private GraphicsOverlay markingContourOverlay;
        private GraphicsOverlay sealedCrackOverlay;
        private GraphicsOverlay mmoOverlay;
        private GraphicsOverlay textureOverlay;
        private GraphicsOverlay avgTextureOverlay;
        private GraphicsOverlay rumbleStripOverlay;
        private GraphicsOverlay geometryOverlay;
        private GraphicsOverlay shoveOverlay;
        private GraphicsOverlay groovesOverlay;
        private GraphicsOverlay sagsbumpsOverlay;
        private GraphicsOverlay pciOverlay;
        public GraphicsOverlay lasPointOverlay;
        public GraphicsOverlay lasRuttingOverlay;
        private GraphicsOverlay paserOverlay;
        private GraphicsOverlay crackSummaryOverlay;
        private GraphicsOverlay vehiclePathOverlay;
        private GraphicsOverlay iNSGeometryOverlay;

        //Roughness:
        private GraphicsOverlay roughnessLeftOverlay;
        private GraphicsOverlay roughnessRightOverlay;
        private GraphicsOverlay roughnessLaneOverlay;

        //Rutting:
        private GraphicsOverlay rutLeftOverlay;
        private GraphicsOverlay rutRightOverlay;
        private GraphicsOverlay rutLaneOverlay;

        //Segment Grid:
        private GraphicsOverlay segmentGridOverlay;
        private GraphicsOverlay segmentGridLongitudinalOverlay;
        private GraphicsOverlay segmentGridTransversalOverlay;
        private GraphicsOverlay segmentGridFatigueOverlay;
        //private GraphicsOverlay segmentGridOthersOverlay;


        public Dictionary<string, GraphicsOverlay> cameraOverlays = new Dictionary<string, GraphicsOverlay>();
        public Dictionary<string, GraphicsOverlay> keycodesOverlays = new Dictionary<string, GraphicsOverlay>();

        public GraphicsOverlayCollection? overlays;
        public GraphicsOverlayCollection? GraphicsOverlays
        {
            get { return overlays; }
            set
            {
                overlays = value;
                OnPropertyChanged();
            }
        }

        public List<MapGraphicData> mapGraphicData;
        public Symbol segmentSymbol;
        public Symbol highlightedSegmentSymbol;

        public async Task SetupMapAsync()
        {
            var isInternetAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (isInternetAvailable)
            {
                Log.Information($"Internet Connection Found. Use online map");

                //To use Google satellite
                WebTiledLayer webTiledLayer = new WebTiledLayer("https://mt1.google.com/vt/lyrs=y&x={col}&y={row}&z={level}");
                Map = new Map(BasemapStyle.OSMDarkGray);
                Map.OperationalLayers.Add(webTiledLayer);
                //To use arcgis satellite
                //Map = new Map(BasemapStyle.ArcGISImagery);
                appState.setUsingOnlineMap(true);
            }
            else
            {
                Log.Information($"No Internet Connection Found. Use offline map instead");
                //load the latest map
                var offlineMapDirectory = AppPaths.OfflineMapFolder;
                if (Directory.Exists(offlineMapDirectory))
                {
                    var files = Directory.GetFiles(offlineMapDirectory, "*.tpkx");
                    var orderedFiles = files.OrderByDescending(f => File.GetLastAccessTime(f));
                    var latestFile = orderedFiles.FirstOrDefault();
                    ApplyofflineMap(latestFile);
                }
                //no internet and no offline map
                else
                {
                    IsMapEnabled = false;
                }
            }
            if (Map != null)
            {
                IsMapEnabled = true;
            }
        }

        public void ApplyofflineMap(string offlineMapPath)
        {
            if (offlineMapPath == "online")
            {
                appState.setUsingOnlineMap(true);
                WebTiledLayer webTiledLayer = new WebTiledLayer("https://mt1.google.com/vt/lyrs=y&x={col}&y={row}&z={level}");
                Map = new Map(BasemapStyle.OSMDarkGray);
                Map.OperationalLayers.Add(webTiledLayer);
            }
            else
            {
                appState.setUsingOnlineMap(false);

                var tileCacheFiles = Directory.GetFiles(offlineMapPath, "*.tpkx");

                Map = new Map();
                var basemap = new Basemap();

                foreach (var tpkxPath in tileCacheFiles)
                {
                    TileCache cache = new TileCache(tpkxPath);
                    ArcGISTiledLayer tiledLayer = new ArcGISTiledLayer(cache);

                    // Add each layer to basemap
                    basemap.BaseLayers.Add(tiledLayer);
                }

                Map.Basemap = basemap;
            }
        }


        public async Task FetchBoundary()
        {
            boundaryOverlay.Graphics.Clear();
            var boundaries = await appEngine.BoundariesService.GetAll(new Empty());
            var boundariesSymbol = GetGraphicSymbol("Boundaries", GeoType.Polygon.ToString());
            if (boundariesSymbol == null)
            {
                Log.Error("No boundary symbol found. can not show boundary on the map.");
                return;
            }

            if (boundariesSymbol is SimpleFillSymbol fillSymbol)
            {
                boundariesSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Cross, fillSymbol.Color, fillSymbol.Outline);
            }

            foreach (var boundary in boundaries)
            {
                var boundaryCoordinates = boundary.Coordinates;
                var attributes = new Dictionary<string, object>
                {
                    { "Id", boundary.Id },
                    { "SurveyId", boundary.SurveyId },
                    { "SurveyName", boundary.SurveyName },
                    { "Mode", boundary.BoundariesMode },
                    { "Table", "Boundary" }
                };

                var coordinates = JsonSerializer.Deserialize<List<double[]>>(boundaryCoordinates);
                Polygon boundaryPolygon = CreatePolygon(coordinates);
                Graphic boundaryGraphic = new Graphic(boundaryPolygon, attributes, boundariesSymbol);
                boundaryOverlay.Graphics.Add(boundaryGraphic);
            }

            boundaryOverlay.Id = "Boundary";
            //boundaryOverlay.IsVisible = false;
            if (!overlays.Contains(boundaryOverlay))
            {
                overlays.Add(boundaryOverlay);
            }
        }

        public async Task FetchAllShapefiles()
        {
            var shapefiles = await appEngine.ShapefileService.GetAll(new Empty());
            if (shapefiles != null)
            {
                await DisplayShapefiles(shapefiles);
            }
        }

        public async Task FetchShapefileByName(string shapefileName)
        {
            var shapefiles = await appEngine.ShapefileService.GetByName(shapefileName);
            if (shapefiles != null)
            {
                await DisplayShapefiles(shapefiles);
            }
        }
        public async Task DisplayShapefiles(IEnumerable<Shapefile> shapefiles)
        {

            var groupedShapefiles = shapefiles.GroupBy(s => s.ShapefileName);
            foreach (var groupShapefile in groupedShapefiles)
            {
                var tableName = groupShapefile.First().ShapefileName;
                var graphicsOverlay = new GraphicsOverlay { Id = tableName };
                var response = appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = tableName }).Result;

                foreach (var shapefile in groupShapefile)
                {
                    var coordinates = shapefile.Coordinates;
                    var jsonObject = JObject.Parse(coordinates);
                    var coordinatesArray = jsonObject["coordinates"];
                    var attributes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(shapefile.Attributes);
                    var shapeType = shapefile.ShapeType;
                    
                    var graphic = ConvertToGraphic(coordinatesArray, shapeType, response);
                    if (graphic != null)
                    {
                        // Add attributes to the graphic
                        foreach (var attribute in attributes)
                        {
                            graphic.Attributes[attribute.Key] = attribute.Value;
                        }

                        // Add the graphic to the GraphicsOverlay
                        graphicsOverlay.Graphics.Add(graphic);
                    }
                }
                var existingOverlay = overlays.FirstOrDefault(o => o.Id == tableName);
                if (existingOverlay != null)
                {
                    // Option 1: Replace the existing overlay
                    overlays.Remove(existingOverlay);
                }
                overlays.Add(graphicsOverlay);
            }
        }

        public async Task FetchVehiclePath(string surveyId)
        {
            var gpsProcessed = await appEngine.GPSProcessedService.GetBySurvey(surveyId);
            if (gpsProcessed != null && gpsProcessed.Count > 0)
            {
                var response = await appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = "Vehicle Path" });
                Symbol overlaySymbol = null;
                foreach (var gps in gpsProcessed)
                {
                    if (response.Name != null)
                    {
                        overlaySymbol = new SimpleMarkerSymbol
                        {
                            Style = SimpleMarkerSymbolStyle.Triangle,
                            Color = System.Drawing.Color.FromArgb(response.Alpha, response.Red, response.Green, response.Blue),
                            Size = 7.0,
                            Angle = gps.Heading,
                            AngleAlignment = SymbolAngleAlignment.Map
                        };
                    }
                    else
                    {
                        overlaySymbol = new SimpleMarkerSymbol
                        {
                            Style = SimpleMarkerSymbolStyle.Triangle,
                            Color = System.Drawing.Color.FromArgb(150, 255, 0, 0),
                            Size = 7.0,
                            Angle = gps.Heading,
                            AngleAlignment = SymbolAngleAlignment.Map
                        };
                    }
                    var mapPoint = new MapPoint(gps.Longitude, gps.Latitude, SpatialReferences.Wgs84);
                    var graphic = new Graphic(mapPoint, overlaySymbol);
                    vehiclePathOverlay.Graphics.Add(graphic);
                }
            }

            if (!overlays.Contains(vehiclePathOverlay))
            {
                overlays.Add(vehiclePathOverlay);
            }
        }

        private async Task FetchandDisplayKeycodes (string tablename, List<string> surveyIds)
        {

            var keycodes = await appEngine.KeycodeService.GetByDescription(tablename);
            if (keycodes != null && keycodes.Count > 0)
            {
                
                await FetchKeycodes(keycodes, surveyIds);
            }
        }

        private async Task FetchAndDispleayKeycodesAsync(List<string> surveyIds)
        {
            var keycodes = await appEngine.KeycodeService.GetAll(new Empty());
            if (keycodes != null && keycodes.Count > 0)
            {
                await FetchKeycodes(keycodes, surveyIds);
            }
        }

        public async Task FetchKeycodes(List<Keycode> keycodes , List<string> surveyIds)
        {
            if (keycodes != null && keycodes.Count > 0)
            {
                var surveyDataMap = new Dictionary<int, (string VideoFolderPath, string SurveyIdExternal)>();
                var distinctSurveyIds = keycodes
                                        .Select(vf => vf.SurveyId)
                                        .Distinct()
                                        .ToList();
                foreach (var distinctSurveyId in distinctSurveyIds)
                {
                    SurveyIdRequest surveyIdRequest = new SurveyIdRequest
                    {
                        SurveyId = distinctSurveyId,
                    };

                    var matchingSurvey = await appEngine.SurveyService.GetById(surveyIdRequest);
                    if (matchingSurvey.Id != 0)
                    {
                        // Store videoPath and SurveyIdExternal with corresponding DB ID
                        surveyDataMap[distinctSurveyId] = (
                        matchingSurvey.VideoFolderPath ?? string.Empty,  // Ensure it's never null
                        matchingSurvey.SurveyIdExternal
                        );
                    }
                }



                try
                {
                    // List to hold pairs of keycodes 
                    List<KeycodePair> keycodesContinous = new List<KeycodePair>();
                    List<Keycode> keycodesPoints = new List<Keycode>();

                    HashSet<string> overlayNamesSet = new HashSet<string>();


                    foreach (var keycode in keycodes)
                    {
                        // Classify the keycode based on ContinuousStatus
                        if (keycode.ContinuousStatus == "STARTED")
                        {
                            keycodesContinous.Add(new KeycodePair { StartedKeycode = keycode, StartChainage = keycode.Chainage });
                           
                        }
                        else if (keycode.ContinuousStatus == "ENDED")
                        {
                            if (keycodesContinous.Any() && keycodesContinous.Last().StartedKeycode != null)
                            {
                                keycodesContinous.Last().EndedKeycode = keycode; // Set the end point for the last start
                                keycodesContinous.Last().EndChainage = keycode.Chainage;
                            }
                        }
                        else // Create pin Icon for point keycodes
                        {
                            keycodesPoints.Add(keycode);
                        }

                        // Check if the description is in the desired overlays set
                        if (!overlayNamesSet.Contains(keycode.Description))
                        {
                            overlayNamesSet.Add(keycode.Description); // Add the description to the overlay names
                        }
                    }
                    //Create GraphicsOverlay for keycodes
                    foreach(var name in overlayNamesSet)
                    {
                        if (!keycodesOverlays.ContainsKey(name))
                        {
                            var overlay = new GraphicsOverlay { Id = name };
                            keycodesOverlays[name] = overlay;
                            overlays.Add(overlay);
                        }
                    }

                    // Now display keycodes 
                    await DisplayContinuousKeycodes(keycodesContinous, surveyIds);
                    DisplayPointKeycodes(keycodesPoints, surveyIds);

                }
                catch (Exception ex)
                {
                    Log.Error($"Error in FetchKeycodes: {ex.Message}");
                }
               
            }
        }

        private async Task DisplayContinuousKeycodes (List<KeycodePair> continuousKeycodes, List<string> surveyIds)
        {
            // Now handle the keycodePairs to draw lines
            foreach (var pair in continuousKeycodes)
            {
                if (pair.StartedKeycode != null && pair.EndedKeycode != null)
                {

                    // Define attributes for the line graphic
                    var attributes = new Dictionary<string, object>
                        {
                            {"Id", pair.StartedKeycode.Id },
                            {"SurveyId", pair.StartedKeycode.SurveyId },
                            {"Description", pair.StartedKeycode.Description},
                            { "Table", "Keycode" }
                        };

                    // Create points for the polyline
                    var points = new List<MapPoint>
                        {
                            new MapPoint(pair.StartedKeycode.GPSLongitude, pair.StartedKeycode.GPSLatitude, SpatialReferences.Wgs84),
                            new MapPoint(pair.EndedKeycode.GPSLongitude, pair.EndedKeycode.GPSLatitude, SpatialReferences.Wgs84)
                        };
                    GpsBySurveyAndChainageRequest request = new GpsBySurveyAndChainageRequest
                    {
                        SurveyId = pair.StartedKeycode.SurveyId,
                        StartChainage = pair.StartChainage,
                        EndChainage = pair.EndChainage
                    };

                    var middlepoints = await appEngine.GPSProcessedService.GetBySurveyAndChainage(request);
                    foreach (var mid in middlepoints)
                    {
                        var midPoint = new MapPoint(mid.Longitude, mid.Latitude, SpatialReferences.Wgs84);
                        points.Insert(points.Count - 1, midPoint); // Insert before the end point
                    }

                    // Create polyline geometry
                    var geometry = new Polyline(points);
                    var lineSymbol = new SimpleLineSymbol()
                    {
                        Style = SimpleLineSymbolStyle.Solid,
                        Color = System.Drawing.Color.FromArgb(255, 0, 0, 150),
                        Width = 2.0
                    };

                    // Create graphic for the line
                    var lineGraphic = new Graphic(geometry, attributes, lineSymbol);

                    var existingOverlay = overlays.FirstOrDefault(o => o.Id == pair.StartedKeycode.Description);
                    if (!overlays.Contains(existingOverlay))
                    {
                        overlays.Add(existingOverlay);
                    }
                    existingOverlay.Graphics.Add(lineGraphic);
                    

                    // Create and add the start point graphic (green)
                    var startPoint = new MapPoint(pair.StartedKeycode.GPSLongitude, pair.StartedKeycode.GPSLatitude, SpatialReferences.Wgs84);
                    var startPointGraphic = new Graphic(startPoint, new Dictionary<string, object>
                        {
                                {"Type", "Start Point"},
                                {"SurveyId", pair.StartedKeycode.SurveyId },
                                {"Description", pair.StartedKeycode.Description}
                        }, new SimpleMarkerSymbol
                        {
                            Style = SimpleMarkerSymbolStyle.Circle,
                            Color = System.Drawing.Color.FromArgb(255, 0, 255, 0), // Green
                            Size = 5.0 // Adjust the size as needed
                        });

                    existingOverlay.Graphics.Add(startPointGraphic);

                    // Create and add the end point graphic (red)
                    var endPoint = new MapPoint(pair.EndedKeycode.GPSLongitude, pair.EndedKeycode.GPSLatitude, SpatialReferences.Wgs84);
                    var endPointGraphic = new Graphic(endPoint, new Dictionary<string, object>
                          {
                                {"Type", "End Point"},
                                {"SurveyId", pair.EndedKeycode.SurveyId },
                                {"Description", pair.EndedKeycode.Description}
                          }, new SimpleMarkerSymbol
                          {
                              Style = SimpleMarkerSymbolStyle.Circle,
                              Color = System.Drawing.Color.FromArgb(255, 255, 0, 0), // Red
                              Size = 5.0 // Adjust the size as needed
                          });

                    existingOverlay.Graphics.Add(endPointGraphic);
                }
            }
        }
        private void DisplayPointKeycodes(List<Keycode> pointKeycodes, List<string> surveyIds)
        {
            foreach (var keycode in pointKeycodes)
            {
                var attributes = new Dictionary<string, object>
                        {
                            {"Id", keycode.Id },
                            {"SurveyId", keycode.SurveyId },
                            {"Description", keycode.Description},
                            { "Table", "Keycode" }
                        };
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                Stream resourceStream = currentAssembly.GetManifestResourceStream(
                     $"DataView2.Resources.Images.pin.png");
                PictureMarkerSymbol iconSymbol = PictureMarkerSymbol.CreateAsync(resourceStream).Result;
                iconSymbol.Width = 20;
                iconSymbol.Height = 20;

                var mapPoint = new MapPoint(keycode.GPSLongitude, keycode.GPSLatitude, SpatialReferences.Wgs84);
                var graphic = new Graphic(mapPoint, attributes, iconSymbol);


                

                if (!keycodesOverlays.ContainsKey(keycode.Description))
                {
                    var overlay = new GraphicsOverlay { Id = keycode.Description };
                    keycodesOverlays[keycode.Description] = overlay;
                    overlays.Add(overlay);
                }
                var existingOverlay = overlays.FirstOrDefault(o => o.Id == keycode.Description);
           
                existingOverlay.Graphics.Add(graphic);

            }
        }

        public async Task FetchAndDisplayLasFiles(string surveyId)
        {
            // Fetch all LAS points
            var isCoordNull = false;
            var lasFiles = await appEngine.LASfileService.GetAllLASFilesBySurvey(surveyId);

            foreach (var lasFile in lasFiles)
            {
                if (lasFile.Coordinates == null)
                    isCoordNull = true;
            }

            if (isCoordNull)
            {
                await LoadNoCoordLasFiles(lasFiles);
            }
            else
            {
                await LoadCoordLasFiles(lasFiles);
            }
        }

        public async Task LoadNoCoordLasFiles(List<LASfile> lasFiles)
        {

            var lasFileIds = lasFiles.Select(f => f.Id).ToList();

            // Fetch all LAS points
            List<LASPoint> allPoints = await appEngine.LASfileService.GetAllPoints(new Empty());

            // Filter points that belong to the selected LASfiles only
            var filteredPoints = allPoints.Where(p => lasFileIds.Contains(p.LASfileId)).ToList();

            // Group points by LASfileId
            var groupedPointsByFile = filteredPoints.GroupBy(p => p.LASfileId);
            var groupedPointsDictionary = groupedPointsByFile.ToDictionary(g => g.Key, g => g.ToList());

            // Create a dictionary for easy access
            var lasFileDictionary = lasFiles.ToDictionary(lasFile => lasFile.Id, lasFile => lasFile);

            foreach (var group in groupedPointsDictionary)
            {

                var convexHullPoints = ComputeConvexHull(group.Value);
                // Create a polygon from the convex hull points
                var polygon = CreatePolygonFromHull(convexHullPoints);

                // Get the LASfile information from the dictionary
                if (lasFileDictionary.TryGetValue(group.Key, out var lasFile))
                {
                    string name = lasFile.Name;

                    // Display the polygon on the map
                    DisplayLasPolygon(polygon, group.Key, name, lasFiles[0].SurveyId);
                }
            }

            // Ensure the overlay is visible
            if (!overlays.Contains(lasPointOverlay))
            {
                lasPointOverlay.Id = "LasPoints";

                overlays.Add(lasPointOverlay);
            }
            lasPointOverlay.IsVisible = true;
        }

        private void DisplayLasPolygon(Polygon polygon, int lasFileId, string lasFileName, string surveyId)
        {

            var attributes = new Dictionary<string, object>
            {
                {"SurveyId", surveyId},
                {"LASfileId", lasFileId },
                {"LASfileName", lasFileName },
                {"Label", lasFileName}
            };

            var symbol = GetGraphicSymbol("LasPoints", GeoType.Polygon.ToString());
            if (symbol == null)
            {
                symbol = CreateSimpleFillSymbol(255, 0, 0, 100, 1);
            }

            var polygonGraphic = new Graphic(polygon, attributes, symbol);

            lasPointOverlay.Graphics.Add(polygonGraphic);

            if (!overlays.Contains(lasPointOverlay))
            {
                lasPointOverlay.Id = "LasPoints";
                overlays.Add(lasPointOverlay);
            }
            lasPointOverlay.IsVisible = true;

            overlays.ToList();
        }

        private List<LASPoint> ComputeConvexHull(List<LASPoint> points)
        {


            // Step 1: Compute the convex hull
            var vertices = points.Select(p => new LASPointVertex(p.X, p.Y)).ToList();
            var hull = ConvexHull.Create2D(vertices);


            if (hull.ErrorMessage != "")
            {
                Log.Error("Convex hull computation returned error:  " + hull.ErrorMessage);
                return null;
            }
            // Extract the vertices (points) that form the convex hull
            var hullVertices = hull.Result.Select(p => new LASPoint
            {
                X = p.X,
                Y = p.Y
            }).ToList();

            return hullVertices;
        }

        private Polygon CreatePolygonFromHull(List<LASPoint> hullPoints)
        {
            var polygonBuilder = new PolygonBuilder(SpatialReferences.Wgs84);
            foreach (var point in hullPoints)
            {
                polygonBuilder.AddPoint(new MapPoint(point.X, point.Y));
            }
            return polygonBuilder.ToGeometry();
        }

        public async Task LoadCoordLasFiles(List<LASfile> lasFiles)
        {

            var symbol = GetGraphicSymbol("LasPoints", GeoType.Polygon.ToString());
            if (symbol == null)
            {
                symbol = CreateSimpleFillSymbol(255, 0, 0, 100, 1);
            }

            foreach (var lasFile in lasFiles)
            {
                var lasCoordinates = lasFile.Coordinates;
                var attributes = new Dictionary<string, object>
                {
                    {"SurveyId", lasFile.SurveyId },
                    {"LASfileId", lasFile.Id},
                    {"LASfileName", lasFile.Name },
                    { "Table", "LASFile" }
                };

                var coordinates = JsonSerializer.Deserialize<List<double[]>>(lasFile.Coordinates);

                Polygon lasFilePolygon = CreatePolygon(coordinates);
                Graphic lasFileGraphic = new Graphic(lasFilePolygon, attributes, symbol);

                var exists = lasPointOverlay.Graphics.Any(g => (int)g.Attributes["LASfileId"] == lasFile.Id);

                if(!exists)
                    lasPointOverlay.Graphics.Add(lasFileGraphic);

                if (!overlays.Contains(lasPointOverlay))
                {
                    lasPointOverlay.Id = "LasPoints";
                    overlays.Add(lasPointOverlay);
                }
                lasPointOverlay.IsVisible = true;

                overlays.ToList();

            }
        }

        private Graphic ConvertToGraphic(JToken coordinatesArray, string geoType, MapGraphicData graphicInfo, Dictionary<string, object> attributes = null)
        {
            Geometry geometry = null;
            System.Drawing.Color color;
            double thickness = 0.0;
            Symbol symbol = null;

            if (graphicInfo != null && graphicInfo.Name != null)
            {
                color = System.Drawing.Color.FromArgb(graphicInfo.Alpha, graphicInfo.Red, graphicInfo.Green, graphicInfo.Blue);
                thickness = graphicInfo.Thickness;
            }
            else
            {
                //default color
                color = System.Drawing.Color.FromArgb(150, 255, 0, 0);
                thickness = 5.0;
            }

            if (geoType.Equals("point", StringComparison.OrdinalIgnoreCase))
            {
                var coords = coordinatesArray.ToObject<double[]>();
                if (coords.Length == 2)
                {
                    geometry = new MapPoint(coords[0], coords[1], SpatialReferences.Wgs84);
                    symbol = new SimpleMarkerSymbol()
                    {
                        Style = SimpleMarkerSymbolStyle.Circle,
                        Color = color,
                        Size = 10
                    };
                }
            }
            else if (geoType.Equals("line", StringComparison.OrdinalIgnoreCase)
                || geoType.Equals("linestring", StringComparison.OrdinalIgnoreCase)
                || geoType.Equals("polyline", StringComparison.OrdinalIgnoreCase))
            {
                var points = new List<MapPoint>();
                var coords = coordinatesArray.ToObject<double[][]>();
                foreach (var coordPair in coords)
                {
                    if (coordPair.Length == 2)
                    {
                        points.Add(new MapPoint(coordPair[0], coordPair[1], SpatialReferences.Wgs84));
                    }
                }
                geometry = new Polyline(points);
                symbol = new SimpleLineSymbol()
                {
                    Style = SimpleLineSymbolStyle.Solid,
                    Color = color,
                    Width = thickness
                };
            }
            else if (geoType.Equals("polygon", StringComparison.OrdinalIgnoreCase))
            {
                var points = new List<MapPoint>();
                foreach (var ring in coordinatesArray)
                {
                    foreach (var coord in ring)
                    {
                        var pointCoords = coord.ToObject<double[]>();
                        var point = new MapPoint(pointCoords[0], pointCoords[1], SpatialReferences.Wgs84);
                        points.Add(point);
                    }
                }
                geometry = new Polygon(points);
                symbol = new SimpleFillSymbol()
                {
                    Style = SimpleFillSymbolStyle.Solid,
                    Color = color,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, thickness)
                };
            }
            else if (geoType.Equals("multipolygon", StringComparison.OrdinalIgnoreCase))
            {
                var multiPolyCoords = coordinatesArray.ToObject<List<List<List<double>>>>();
                var polygons = new List<Polygon>();
                foreach (var ring in multiPolyCoords)
                {
                    var mapPoints = ring.Select(coord => new MapPoint(coord[0], coord[1], SpatialReferences.Wgs84)).ToList();
                    var polygon = new Polygon(mapPoints);
                    polygons.Add(polygon);
                }

                geometry = GeometryEngine.Union(polygons);
                symbol = new SimpleFillSymbol()
                {
                    Style = SimpleFillSymbolStyle.Solid,
                    Color = color,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, thickness)
                };
            }
            else
            {
                return null;
            }

            if (attributes != null)
            {
                return new Graphic(geometry, attributes, symbol);
            }
            return new Graphic(geometry, symbol);
        }

      

        public void UpdateMetaTableIcon(string tableName, string iconPath, int iconSize)
        {
            var targetOverlay = overlays.FirstOrDefault(o => o.Id == tableName);
            if (targetOverlay != null)
            {
                if (Path.Exists(iconPath))
                {
                    var stream = new MemoryStream();
                    using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(stream);
                    }
                    var newIconSymbol = PictureMarkerSymbol.CreateAsync(stream).Result;
                    newIconSymbol.Height = (double)iconSize;
                    newIconSymbol.Width = (double)iconSize;

                    foreach (var graphic in targetOverlay.Graphics)
                    {
                        graphic.Symbol = newIconSymbol;
                    }
                }
                else
                {
                    Assembly currentAssembly = Assembly.GetExecutingAssembly();
                    Stream resourceStream = currentAssembly.GetManifestResourceStream(
                        $"DataView2.Resources.Images.{iconPath}");
                    PictureMarkerSymbol iconSymbol = PictureMarkerSymbol.CreateAsync(resourceStream).Result;
                    iconSymbol.Width = (double)iconSize;
                    iconSymbol.Height = (double)iconSize;

                    foreach (var graphic in targetOverlay.Graphics)
                    {
                        graphic.Symbol = iconSymbol;
                    }
                }
            }
        }

        public async Task FetchSurveySegmentations()
        {
            var ss = await appEngine.SurveySegmentationService.GetAll(new Empty());
            if (ss != null && ss.Count > 0)
            {
                DisplaySegmentations(ss);
            }
        }

        private void DisplaySegmentations(List<SurveySegmentation> ss)
        {
            try
            {
                var graphicsOverlay = new GraphicsOverlay { Id = "Survey Segmentation" };
                foreach (var segmentation in ss)
                {
                    string[] startData = segmentation.StartPoint.Replace("[", "").Replace("]", "").Split(','), endData = segmentation.EndPoint.Replace("[", "").Replace("]", "").Split(',');
                    double startX = Convert.ToDouble(startData[1]), startY = Convert.ToDouble(startData[0]), endX = Convert.ToDouble(endData[1]), endY = Convert.ToDouble(endData[0]);
                    MapPoint startPoint = new MapPoint(startX, startY, SpatialReferences.Wgs84), endPoint = new MapPoint(endX, endY, SpatialReferences.Wgs84);

                    var pointStartGraphic = new Graphic(startPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.GreenYellow, 15));
                    pointStartGraphic.Attributes.Add("Name", segmentation.Name);
                    pointStartGraphic.Attributes.Add("Start", segmentation.StartPoint);
                    pointStartGraphic.Attributes.Add("Id", segmentation.Id);

                    var pointEndGraphic = new Graphic(endPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Red, 15));
                    pointEndGraphic.Attributes.Add("Name", segmentation.Name);
                    pointEndGraphic.Attributes.Add("End", segmentation.EndPoint);
                    pointEndGraphic.Attributes.Add("Id", segmentation.Id);

                    graphicsOverlay.Graphics.Add(pointStartGraphic);
                    graphicsOverlay.Graphics.Add(pointEndGraphic);
                }

                var existingOverlay = overlays.FirstOrDefault(o => o.Id == "Survey Segmentation");
                if (existingOverlay != null)
                {
                    overlays.Remove(existingOverlay);
                }
                overlays.Add(graphicsOverlay);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in DisplaySegmentations : {ex.Message}");
            }
        }

        public async Task FetchAllMetaTables()
        {
            var metaTables = await appEngine.MetaTableService.GetAllTables(new Google.Protobuf.WellKnownTypes.Empty());
            if (metaTables != null && metaTables.Count > 0)
            {
                await DisplayMetaTables(metaTables);
            }
        }

        public async Task FetchMetaTableByName(string metaTableName)
        {
            var metaTable = await appEngine.MetaTableService.GetByName(metaTableName);
            if (metaTable.TableName != null)
            {
                await DisplayMetaTables(new List<MetaTable> { metaTable });
            }
        }
        public async Task DisplayMetaTables(List<MetaTable> metaTables)
        {
            foreach (var metaTable in metaTables)
            {
                var tableName = metaTable.TableName;

                //create graphic overlay for tableId
                var graphicsOverlay = new GraphicsOverlay { Id = tableName };

                var tableValues = await appEngine.MetaTableService.GetByTable(metaTable);
                if (tableValues != null && tableValues.Any())
                {
                    foreach (var value in tableValues)
                    {
                        //attributes
                        var attributes = ConvertKeyValueFieldsToDictionary(value.Attributes);
                        if (tableName.StartsWith("IRI") && tableName.Contains("Meter Section"))
                        {
                            var keyToUpdate = attributes.Keys.FirstOrDefault(k => k.StartsWith("Average"));

                            if (!string.IsNullOrEmpty(keyToUpdate))
                            {
                                attributes["Average IRI"] = attributes[keyToUpdate]; // Copy value
                                attributes.Remove(keyToUpdate); // Remove old key
                            }
                            attributes.Add("IRISectionId", value.SegmentId);
                        }
                        else
                        {
                            attributes.Add("SegmentId", value.SegmentId);
                        }

                        attributes.Add("SurveyId", value.SurveyId);
                        attributes.Add("TableId", value.TableId);
                        attributes.Add("Table", tableName);
                        attributes.Add("TrackAngle", value.GPSTrackAngle);
                        attributes.Add("Type", "MetaTable");

                        //Get geo info from geoJson
                        var geoJsonObject = JObject.Parse(value.GeoJSON);
                        var geometry = geoJsonObject["geometry"];
                        var geoType = geometry["type"].ToString();
                        var coordinatesArray = geometry["coordinates"];

                        Graphic graphic = null;
                        if (geoType == "Point")
                        {
                            var iconPath = value.Icon;
                            var iconSize = value.IconSize != null ? value.IconSize : 20;
                            var coords = coordinatesArray.ToObject<double[]>();
                            var centerPoint = new MapPoint(coords[0], coords[1], SpatialReferences.Wgs84);
                            if (Path.Exists(iconPath))
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                                var stream = new MemoryStream();
                                using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                                {
                                    fileStream.CopyTo(stream);
                                }
                                PictureMarkerSymbol iconSymbol = PictureMarkerSymbol.CreateAsync(stream).Result;
                                iconSymbol.Height = (double)iconSize;
                                iconSymbol.Width = (double)iconSize;
                                iconSymbol.AngleAlignment = SymbolAngleAlignment.Map;
                                iconSymbol.Angle = value.GPSTrackAngle;

                                graphic = new Graphic(centerPoint, attributes, iconSymbol);
                            }
                            else
                            {
                                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                                Stream resourceStream = currentAssembly.GetManifestResourceStream(
                                    $"DataView2.Resources.Images.{iconPath}");
                                PictureMarkerSymbol iconSymbol = PictureMarkerSymbol.CreateAsync(resourceStream).Result;
                                iconSymbol.Width = (double)iconSize;
                                iconSymbol.Height = (double)iconSize;
                                iconSymbol.AngleAlignment = SymbolAngleAlignment.Map;
                                iconSymbol.Angle = value.GPSTrackAngle;

                                graphic = new Graphic(centerPoint, attributes, iconSymbol);
                            }
                        }
                        else
                        {
                            var colorCodeInfo = appState.ColorCodeInfo.Where(cc => cc.TableName == tableName);
                            if (colorCodeInfo.Any())
                            {
                                var property = colorCodeInfo.First().Property;
                                attributes.TryGetValue(property, out var propertyValue);

                                if (propertyValue != null)
                                {
                                    var points = new List<MapPoint>();

                                    if (geoType == "Polyline")
                                    {
                                        // Parse coordinates for polyline
                                        foreach (var coord in coordinatesArray)
                                        {
                                            var pointCoords = coord.ToObject<double[]>();
                                            var point = new MapPoint(pointCoords[0], pointCoords[1], SpatialReferences.Wgs84);
                                            points.Add(point);
                                        }
                                        var polyline = new Polyline(points);
                                        var symbol = SymbolFromColorList(propertyValue, colorCodeInfo, "Line");

                                        if (symbol != null)
                                            graphic = new Graphic(polyline, attributes, symbol);
                                        else // Accounts for metatable symbols that return null.
                                        {
                                            var response = appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = tableName }).Result;
                                            graphic = ConvertToGraphic(coordinatesArray, geoType, response);
                                        }
                                    }
                                    else if (geoType == "Polygon")
                                    {
                                        foreach (var ring in coordinatesArray)
                                        {
                                            foreach (var coord in ring)
                                            {
                                                var pointCoords = coord.ToObject<double[]>();
                                                var point = new MapPoint(pointCoords[0], pointCoords[1], SpatialReferences.Wgs84);
                                                points.Add(point);
                                            }
                                        }

                                        var polygon = new Polygon(points);
                                        var symbol = SymbolFromColorList(propertyValue, colorCodeInfo, "Fill");
                                        graphic = new Graphic(polygon, attributes, symbol);
                                    }
                                }
                            }
                            else
                            {
                                var response = appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = tableName }).Result;
                                graphic = ConvertToGraphic(coordinatesArray, geoType, response);
                            }
                        }

                        if (graphic != null)
                        {
                            foreach (var attribute in attributes)
                            {
                                graphic.Attributes[attribute.Key] = attribute.Value;
                            }

                            graphicsOverlay.Graphics.Add(graphic);
                        }
                    }
                }
                var existingOverlay = overlays.FirstOrDefault(o => o.Id == tableName);
                if (existingOverlay != null)
                {
                    overlays.Remove(existingOverlay);
                }
                overlays.Add(graphicsOverlay);

                var hasLabel = await OverlayHasLabel(graphicsOverlay.Id);
                //if the color code has label property then set label (table name, string label property ) 
                if (hasLabel != "No Label")
                {
                    SetLabels(graphicsOverlay.Id, hasLabel);
                }
            }
        }

        public async void UpdateSummaries(string overlayName)
        {
            //Clear summaries overlay first
            var summaryOverlay = overlays.FirstOrDefault(x => x.Id == overlayName);
            if (summaryOverlay != null)
            {
                summaryOverlay.Graphics.Clear();

                //Fetch summaries that are already loaded
                await FetchSummariesByName(overlayName);
            }
        }

        public async Task FetchAllSummaries()
        {
            var allSummaries = await appEngine.SummaryService.GetAll(new Empty());
            if (allSummaries != null && allSummaries.Count > 0)
            {
                await DisplaySummaries(allSummaries);
            }
        }

        public async Task FetchSummariesByName(string name)
        {
            var summaries = await appEngine.SummaryService.GetByName(name);
            if (summaries != null && summaries.Count > 0)
            {
                await DisplaySummaries(summaries);
            }
        }

        public async Task DisplaySummaries(List<Summary> summaries)
        {
            foreach (var summary in summaries)
            {
                var sampleUnit = await appEngine.SampleUnitService.GetById(new IdRequest { Id = summary.SampleUnitId.Value });
                if (sampleUnit != null)
                {
                    var polygonCoordinates = sampleUnit.Coordinates;
                    var attributes = new Dictionary<string, object>
                    {
                        { "Id", summary.Id },
                        { "SurveyId", summary.SurveyId },
                        { "Table", summary.Name },
                        { "Type", "Summaries" }
                    };

                    if (summary.SummaryDefects != null)
                    {
                        foreach (var summaryDefect in summary.SummaryDefects)
                        {
                            var table = summaryDefect.TableName;
                            var numericField = summaryDefect.NumericField;
                            var operation = summaryDefect.Operation;
                            var value = summaryDefect.Value;
                            var newField = $"{table} {operation} ({numericField})";

                            if (!attributes.ContainsKey(newField))
                            {
                                attributes.Add(newField, value);
                            }
                        }
                    }

                    var coordinates = JsonSerializer.Deserialize<List<double[]>>(polygonCoordinates);
                    Polygon summaryPolygon = CreatePolygon(coordinates);
                    Graphic graphic = new Graphic(summaryPolygon, attributes);


                    if (graphic != null)
                    {
                        //Color Code
                        var colorCodeInfo = appState.ColorCodeInfo.Where(cc => cc.TableName == summary.Name);
                        if (colorCodeInfo.Any() && summary.SummaryDefects != null)
                        {
                            var property = colorCodeInfo.First().Property;
                            attributes.TryGetValue(property, out var propertyValue);
                            var symbol = SymbolFromColorList(propertyValue, colorCodeInfo, "Fill");
                            if (symbol != null)
                            {
                                graphic.Symbol = symbol;
                            }
                        }
                        else
                        {
                            var response = appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = summary.Name }).Result;
                            if (response != null && response.Name != null)
                            {
                                var color = System.Drawing.Color.FromArgb(response.Alpha, response.Red, response.Green, response.Blue);
                                var thickness = response.Thickness;
                                var symbol = new SimpleFillSymbol()
                                {
                                    Style = SimpleFillSymbolStyle.Solid,
                                    Color = color,
                                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, thickness)
                                };

                                graphic.Symbol = symbol;
                            }
                            else
                            {
                                //default
                                graphic.Symbol = new SimpleFillSymbol()
                                {
                                    Style = SimpleFillSymbolStyle.Solid,
                                    Color = System.Drawing.Color.FromArgb(150, 255, 0, 0),
                                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1)
                                };
                            }
                        }

                        //Add graphic in the overlay
                        var graphicOverlay = overlays.FirstOrDefault(o => o.Id == summary.Name);
                        if (graphicOverlay == null)
                        {
                            graphicOverlay = new GraphicsOverlay { Id = summary.Name };
                            overlays.Add(graphicOverlay);
                        }
                        graphicOverlay.Graphics.Add(graphic);
                    }
                }
            }
        }

        public async Task FetchAllPCIDefects()
        {
            var pciRatings = await appEngine.PCIRatingService.GetAll(new Empty());
            if (pciRatings != null && pciRatings.Count > 0)
            {
                if (pciRatings != null && pciRatings.Count > 0)
                {
                    foreach (var pciRating in pciRatings)
                    {
                        //create defects on the map
                        if (pciRating.PCIDefects != null && pciRating.PCIDefects.Count > 0)
                        {
                            //create rating sample units first
                            await DisplaySampleUnits(pciRating);

                            DisplayPCIDefects(pciRating.PCIDefects.ToList());
                        }
                    }
                }
            }
        }

        public async Task FetchPCIDefectsByName(string tableName)
        {
            if (tableName.Contains("Sample Unit"))
            {
                string ratingName = tableName.Replace("-Sample Unit", "").Trim();
                var rating = await appEngine.PCIRatingService.GetByName(ratingName);
                if (rating != null)
                {
                    await DisplaySampleUnits(rating);
                }
            }
            else
            {
                var defects = await appEngine.PCIDefectsService.GetByTableName(tableName);
                if (defects != null && defects.Count > 0)
                {
                    DisplayPCIDefects(defects);
                }
            }
        }

        public async Task DisplaySampleUnits(PCIRatings pciRating)
        {
            var sampleUnitIds = pciRating.PCIDefects.Select(x => x.SampleUnitId).ToList();
            var sampleUnits = await appEngine.SampleUnitService.GetBySampleUnitSet(new IdRequest { Id = pciRating.SampleUnitSetId });
            if (sampleUnits != null && sampleUnits.Count > 0)
            {
                foreach (var sampleUnit in sampleUnits)
                {
                    if (!sampleUnitIds.Contains(sampleUnit.Id))
                        continue; // skip units not in the list

                    var coordinates = JsonSerializer.Deserialize<List<double[]>>(sampleUnit.Coordinates);
                    Polygon sampleUnitPolygon = CreatePolygon(coordinates);
                    Symbol symbol = segmentSymbol;
                    if (symbol != null)
                    {
                        var attributes = new Dictionary<string, object>
                        {
                            { "PCIRatingId", pciRating.Id },
                            { "SampleUnitId", sampleUnit.Id }
                        };

                        Graphic sampleUnitGraphic = new Graphic(sampleUnitPolygon, attributes, symbol);
                        sampleUnitGraphic.ZIndex = -2;
                        var overlayId = pciRating.RatingName + "-Sample Unit";
                        var targetOverlay = overlays.FirstOrDefault(o => o.Id == overlayId);
                        if (targetOverlay == null)
                        {
                            //create overlay if it doesn't exist
                            var graphicOverlay = new GraphicsOverlay { Id = overlayId };
                            overlays.Add(graphicOverlay);
                            targetOverlay = graphicOverlay;
                        }

                        if (targetOverlay != null)
                        {
                            targetOverlay.Graphics.Add(sampleUnitGraphic);
                        }
                    }
                }
            }
        }

        public void DisplayPCIDefects(List<PCIDefects> pciDefects)
        {
            foreach (var pciDefect in pciDefects)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "Id", pciDefect.Id },
                    { "Qty", pciDefect.Qty },
                    { "Severity", pciDefect.Severity },
                    { "Type", "PCIDefect" }
                };

                var overlayId = pciDefect.PCIRatingName + "-" + pciDefect.DefectName;
                var targetOverlay = overlays.FirstOrDefault(o => o.Id == overlayId);
                if (targetOverlay == null)
                {
                    //create overlay if it doesn't exist
                    var graphicOverlay = new GraphicsOverlay { Id = overlayId };
                    overlays.Add(graphicOverlay);
                    targetOverlay = graphicOverlay;
                }

                if (targetOverlay != null && !string.IsNullOrEmpty(pciDefect.GeoJSON))
                {
                    var graphics = ParseGeoJson(pciDefect.GeoJSON, attributes, pciDefect);
                    targetOverlay.Graphics.AddRange(graphics);
                }
            }
        }

        private async Task FetchAndDisplayVideoAsync(List<string> surveyIds)
        {
            var videoFrames = await appEngine.VideoFrameService.GetAll(new Empty());
            if (videoFrames != null && videoFrames.Count > 0)
            {
                await DisplayVideoGraphics(videoFrames, surveyIds);
            }

            var camera360Frames = await appEngine.Camera360FrameService.GetAll(new Empty());
            if (camera360Frames != null && camera360Frames.Count > 0)
            {
                await Display360CameraGraphics(camera360Frames, surveyIds);
            }
        }

        private async Task FetchAndDisplayVideoByName(string cameraName, List<string> surveyIds)
        {
            if (cameraName == "360 Video")
            {
                var camera360Frames = await appEngine.Camera360FrameService.GetAll(new Empty());
                if (camera360Frames != null && camera360Frames.Count > 0)
                {
                    await Display360CameraGraphics(camera360Frames, surveyIds);
                }
            }
            else
            {
                var videoFrames = await appEngine.VideoFrameService.GetByName(cameraName);
                if (videoFrames != null && videoFrames.Count > 0)
                {
                    await DisplayVideoGraphics(videoFrames, surveyIds);
                }
            }
        }

        private async Task Display360CameraGraphics(List<Camera360Frame> camera360Frames, List<string> surveyIds)
        {
            var surveyIdMap = new Dictionary<int, string>(); // Maps DB ID -> SurveyIdExternal

            var distinctSurveyIds = camera360Frames.Select(x => x.SurveyId).Distinct().ToList();
            foreach (var surveyId in distinctSurveyIds)
            {
                SurveyIdRequest surveyIdRequest = new SurveyIdRequest
                {
                    SurveyId = surveyId
                };

                var matchingSurvey = await appEngine.SurveyService.GetById(surveyIdRequest);
                if (matchingSurvey.Id != 0)
                {
                    surveyIdMap[surveyId] = matchingSurvey.SurveyIdExternal;
                }
            }
            string cameraName = "360 Video";
            var response = await appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = cameraName });

            var relevantFrames = camera360Frames.Where(frame => surveyIdMap.ContainsKey(frame.SurveyId) && surveyIds.Contains(surveyIdMap[frame.SurveyId])).ToList();
            foreach (var frame in relevantFrames)
            {
                var camera360overlay = overlays.FirstOrDefault(x => x.Id == cameraName);
                if (camera360overlay == null)
                {
                    camera360overlay = new GraphicsOverlay { Id = cameraName };
                }

                var attributes = new Dictionary<string, object>
                {
                    ["Camera360FrameId"] = frame.Camera360FrameId,
                    ["SurveyId"] = surveyIdMap.ContainsKey(frame.SurveyId) ? surveyIdMap[frame.SurveyId] : frame.SurveyId, // Fetch SurveyIdExternal,
                    ["ImagePath"] = frame.ImagePath,
                    ["CameraInfo"] = cameraName,
                    ["VideoFrameId"] = frame.Camera360FrameId,
                    ["TrackAngle"] = frame.GPSTrackAngle,
                    ["Chainage"] = frame.Chainage
                };

                Symbol overlaySymbol = null;
                if (response.Name != null)
                {
                    overlaySymbol = new SimpleMarkerSymbol
                    {
                        Style = SimpleMarkerSymbolStyle.Triangle,
                        Color = System.Drawing.Color.FromArgb(response.Alpha, response.Red, response.Green, response.Blue),
                        Size = 7.0,
                        Angle = frame.GPSTrackAngle,
                        AngleAlignment = SymbolAngleAlignment.Map
                    };
                }
                else
                {
                    overlaySymbol = new SimpleMarkerSymbol
                    {
                        Style = SimpleMarkerSymbolStyle.Triangle,
                        Color = System.Drawing.Color.FromArgb(150, 255, 0, 0),
                        Size = 7.0,
                        Angle = frame.GPSTrackAngle,
                        AngleAlignment = SymbolAngleAlignment.Map
                    };
                }
                var centerPoint = new MapPoint(frame.GPSLongitude, frame.GPSLatitude, SpatialReferences.Wgs84);
                //var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, 1.5, LinearUnits.Meters);
                var graphic = new Graphic(centerPoint, attributes, overlaySymbol);

                // Add the graphic to the bag for this overlay
                camera360overlay.Graphics.Add(graphic);
                cameraOverlays[cameraName] = camera360overlay;
                if (!overlays.Contains(camera360overlay))
                {
                    overlays.Add(camera360overlay);
                }
            }
        }

        private async Task DisplayVideoGraphics(List<VideoFrame> videoFrames, List<string> surveyIds)
        {
            var surveyDataMap = new Dictionary<int, (string VideoFolderPath, string SurveyIdExternal)>();
            var distinctSurveyIds = videoFrames
                                    .Select(vf => vf.SurveyId)
                                    .Distinct()
                                    .ToList();
            foreach (var distinctSurveyId in distinctSurveyIds)
            {
                SurveyIdRequest surveyIdRequest = new SurveyIdRequest
                {
                    SurveyId = distinctSurveyId,
                };

                var matchingSurvey = await appEngine.SurveyService.GetById(surveyIdRequest);
                if (matchingSurvey.Id != 0)
                {
                    // Store videoPath and SurveyIdExternal with corresponding DB ID
                    surveyDataMap[distinctSurveyId] = (
                    matchingSurvey.VideoFolderPath ?? string.Empty,  // Ensure it's never null
                    matchingSurvey.SurveyIdExternal
                    );
                }
            }

            var relevantFrames = videoFrames.Where(x => surveyDataMap.ContainsKey(x.SurveyId) && surveyIds.Contains(surveyDataMap[x.SurveyId].SurveyIdExternal));
            
            if (relevantFrames != null && relevantFrames.Count() > 0)
            {
                var grouped = relevantFrames
              .GroupBy(videoObj => videoObj.CameraName)
              .ToList();

                foreach (var group in grouped)
                {
                    string cameraName = group.Key;

                    var cameraNameOverlay = new GraphicsOverlay
                    {
                        Id = cameraName // Using CameraName as the overlay ID
                    };

                    var existingOverlay = overlays.FirstOrDefault(o => o.Id == group.Key);
                    if (existingOverlay != null)
                    {
                        overlays.Remove(existingOverlay);
                    }

                    List<Graphic> graphicsBag = new List<Graphic>();

                    var response = await appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = cameraName });

                    foreach (var item in group)
                    {
                        string videoPath = null;
                        if (surveyDataMap.ContainsKey(item.SurveyId))
                        {
                            var videoFolderPath = surveyDataMap[item.SurveyId].VideoFolderPath;
                            if (!string.IsNullOrEmpty(videoFolderPath))
                            {
                                videoPath = Path.Combine(videoFolderPath, item.ImageFileName);
                            }
                        }

                        if (videoPath == null) continue;

                        var attributes = new Dictionary<string, object>
                        {
                            ["VideoFrameId"] = item.VideoFrameId,
                            ["SurveyId"] = surveyDataMap.ContainsKey(item.SurveyId) ? surveyDataMap[item.SurveyId].SurveyIdExternal : item.SurveyId,
                            ["CameraInfo"] = item.CameraName,
                            ["ImagePath"] = videoPath,
                            ["TrackAngle"] = item.GPSTrackAngle,
                            ["Chainage"] = item.Chainage
                        };

                        Symbol overlaySymbol = null;
                        if (response.Name != null)
                        {
                            overlaySymbol = new SimpleMarkerSymbol
                            {
                                Style = SimpleMarkerSymbolStyle.Triangle,
                                Color = System.Drawing.Color.FromArgb(response.Alpha, response.Red, response.Green, response.Blue),
                                Size = 7.0,
                                Angle = item.GPSTrackAngle,
                                AngleAlignment = SymbolAngleAlignment.Map
                            };
                        }
                        else
                        {
                            overlaySymbol = new SimpleMarkerSymbol
                            {
                                Style = SimpleMarkerSymbolStyle.Triangle,
                                Color = System.Drawing.Color.FromArgb(150, 255, 0, 0),
                                Size = 7.0,
                                Angle = item.GPSTrackAngle,
                                AngleAlignment = SymbolAngleAlignment.Map
                            };
                        }

                        var centerPoint = new MapPoint(item.GPSLongitude, item.GPSLatitude, SpatialReferences.Wgs84);
                        //var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, 1.5, LinearUnits.Meters);
                        var graphic = new Graphic(centerPoint, attributes, overlaySymbol);

                        // Add the graphic to the bag for this overlay
                        cameraNameOverlay.Graphics.Add(graphic);
                    }

                    cameraOverlays[cameraName] = cameraNameOverlay;

                    if (!overlays.Contains(cameraNameOverlay))
                    {
                        overlays.Add(cameraNameOverlay);
                    }
                }
            }
        }
        
        public Dictionary<string, object> ConvertKeyValueFieldsToDictionary(List<KeyValueField> keyValueFields)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var field in keyValueFields)
            {
                if (field.Type == ColumnType.Number.ToString() || field.Type == ColumnType.Measurement.ToString())
                {
                    if (double.TryParse(field.Value, out var decimalValue))
                    {
                        dictionary[field.Key] = decimalValue;
                    }
                }
                else if (field.Type == ColumnType.Text.ToString() || field.Type == ColumnType.Dropdown.ToString() || field.Type == ColumnType.Date.ToString())
                {
                    dictionary[field.Key] = field.Value;
                }
                else if (field.Type == "int" && int.TryParse(field.Value, out var intValue))
                {
                    dictionary[field.Key] = intValue;
                }
            }

            return dictionary;
        }

        private List<LCMS_Segment> loadedSegments = new List<LCMS_Segment>();

        private void DisplaySegments(IEnumerable<LCMS_Segment> segments)
        {
            try
            {
                ConcurrentBag<Graphic> graphicsBag = new ConcurrentBag<Graphic>();
                foreach (var segment in segments)
                {
                    var attributes = new Dictionary<string, object>
                    {
                        { "Id", segment.Id },
                        { "SurveyId", segment.SurveyId },
                        { "SectionId", segment.SegmentId },
                        { "ImageFilePath" ,  segment.ImageFilePath},
                        { "SegmentId", segment.SegmentId },
                        { "Altitude", segment.GPSAltitude },
                        { "TrackAngle", segment.GPSTrackAngle },
                        { "Width", segment.Width },
                        { "Height", segment.Height },
                        { "Chainage", segment.Chainage },
                        { "Table", "Segment"}
                    };
                    var geoJson = segment.GeoJSON;
                    var geoJsonObject = JObject.Parse(geoJson);

                    var coordinates = new List<Double[]>();
                    var geometry = geoJsonObject["geometry"];

                    var coordinatesArray = geometry["coordinates"];
                    if (geoJson != null)
                    {
                        foreach (var ring in coordinatesArray)
                        {
                            foreach (var coord in ring)
                            {
                                var coordinate = new Double[] { coord[0].Value<double>(), coord[1].Value<double>() };
                                coordinates.Add(coordinate);
                            }
                        }
                    }

                    var polygon = CreatePolygon(coordinates);
                    if (segmentSymbol == null)
                        segmentSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(50, 0, 0, 0), new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1.0));
                    Graphic segmentGraphic = new(polygon, attributes, segmentSymbol);
                    segmentGraphic.ZIndex = -1;

                    if (segmentGraphic != null)
                    {
                        graphicsBag.Add(segmentGraphic);
                    }
                }

                foreach (var graphic in graphicsBag)
                {
                    if (!segmentOverlay.Graphics.Contains(graphic))
                        segmentOverlay.Graphics.Add(graphic);
                }

                if (!overlays.Contains(segmentOverlay))
                {
                    segmentOverlay.Id = "Segment";
                    overlays.Add(segmentOverlay);
                    Serilog.Log.Information($"Segments loaded on the map : {segmentOverlay.Graphics.Count}");
                }
                
            }
            catch (Exception ex) { Serilog.Log.Error($"Segments failed to load on the map : {ex.Message}"); }
        }

        //remove overlapped segments
        public void RemoveOverlappingGraphics(GraphicsOverlay graphicsOverlay)
        {
            //var graphics = graphicsOverlay.Graphics.ToList();
            var toRemove = new HashSet<Graphic>();

           var groupedGRaphics = graphicsOverlay.Graphics.Where(g => g.Attributes.ContainsKey("SurveyId")).GroupBy(g => g.Attributes["SurveyId"].ToString()).
                ToDictionary(group => group.Key, group => group.ToList());

            //group by survey 
            foreach (var gg in groupedGRaphics)
            {
                for (int i = 0; i < gg.Value.Count; i++)
                {
                    var g1 = gg.Value[i];
                    for (int j = i + 1; j < gg.Value.Count; j++)
                    {
                        var g2 = gg.Value[j];

                        if (toRemove.Contains(g1) || toRemove.Contains(g2))
                            continue;

                        Geometry geom1 = GeometryEngine.NormalizeCentralMeridian(g1.Geometry);
                        Geometry geom2 = GeometryEngine.NormalizeCentralMeridian(g2.Geometry);

                        var intersection = GeometryEngine.Intersection(geom1, geom2);
                        if (intersection == null || intersection.IsEmpty)
                            continue;

                        double area1 = GeometryEngine.Area(geom1);
                        double area2 = GeometryEngine.Area(geom2);
                        double intersectArea = GeometryEngine.Area(intersection);

                        double overlap1 = (intersectArea / area1) * 100.0;
                        double overlap2 = (intersectArea / area2) * 100.0;

                        // If either overlaps more than 50%, mark one for removal (e.g., remove g2)
                        if (overlap1 > 30 || overlap2 > 30)
                        {
                            toRemove.Add(g2);
                        }
                    }
                }
            }

            // Remove the marked graphics
            foreach (var g in toRemove)
            {
                graphicsOverlay.Graphics.Remove(g);
            }
        }

        public async void CreateGraphicsFromGeoJson<T>(List<T> ObjectOuputs, List<Graphic> graphicsBag) where T : new()
        {
            foreach (var output in ObjectOuputs)
            {
                var outputtype = output.GetType();
                var properties = outputtype.GetProperties();

                var attributes = new Dictionary<string, object>();

                foreach (var property in properties)
                {
                    string propertyName = property.Name;
                    object propertyValue = property.GetValue(output);

                    if (propertyName == "GPSTrackAngle")
                    {
                        attributes["TrackAngle"] = propertyValue;
                        continue;
                    }

                    if (propertyName.Contains("GPS") || propertyName.Contains("QC") || propertyName.Contains("GeoJSON") ||
                        propertyName == "SurveyDate" || propertyName == "PavementType")
                    {
                        continue;
                    }

                    if (propertyValue is DateTime)
                    {
                        continue;
                    }

                    if (propertyValue != null && !string.IsNullOrEmpty(propertyValue.ToString()))
                    {
                        attributes[propertyName] = propertyValue;
                    }
                }

                //Multi Geojson
                if (typeof(T) == typeof(LCMS_Rough_Processed) || typeof(T) == typeof(LCMS_Rut_Processed))
                {
                    var geoJsonFields = new[] { "GeoJSON", "LwpGeoJSON", "RwpGeoJSON", "CwpGeoJSON" };
                    foreach (var field in geoJsonFields)
                    {
                        var graphics =  await HandleMultiGeoJsonField(output, outputtype, field, attributes);
                        if (graphics != null)
                            graphicsBag.AddRange(graphics);
                    }
                }
                else if (typeof(T) == typeof(LCMS_Texture_Processed))
                {
                    var geoJsonFields = new[] { "GeoJSON", "AvgGeoJSON" };
                    foreach (var field in geoJsonFields)
                    {
                        var graphics = await HandleMultiGeoJsonField(output, outputtype, field, attributes);
                        if (graphics != null)
                            graphicsBag.AddRange(graphics);
                    }
                }
                else
                {
                    var geoJsonProperty = outputtype.GetProperty("GeoJSON");
                    if (geoJsonProperty != null)
                    {
                        var geoJson = (string)geoJsonProperty.GetValue(output);
                        var graphics = ParseGeoJson(geoJson, attributes, output);
                        if (graphics.Count == 1)
                        {
                            graphicsBag.Add(graphics[0]); // Directly add the single graphic
                        }
                        else
                        {
                            graphicsBag.AddRange(graphics); // Add all if there are multiple graphics
                        }
                    }
                }
            }
        }

        private async Task<List<Graphic>> HandleMultiGeoJsonField (object output, System.Type outputType, string propertyName, Dictionary<string, object> attributes)
        {
            var property = outputType.GetProperty(propertyName);
            if (property != null)
            {
                var geoJson = (string)property.GetValue(output);
                if (!string.IsNullOrEmpty(geoJson))
                {
                    var newAttributes = new Dictionary<string, object>(attributes);
                    var graphics = ParseGeoJson(geoJson, newAttributes, output);
                    return graphics;
                }
            }
            return null;
        }

        public List<Graphic> ParseGeoJson<T>(string geoJson, Dictionary<string, object> attributes, T output) where T : new()
        {
            try
            {
                List<Graphic> graphicsBag = new List<Graphic>();

                var geoJsonObject = JObject.Parse(geoJson);
                var geometry = geoJsonObject["geometry"];
                var jsonProperties = geoJsonObject["properties"];
                var table = jsonProperties["type"].ToString();

                attributes.Add("Table", table);

                if (jsonProperties["diameter"] != null)
                {
                    var diameter = jsonProperties["diameter"].ToString();
                    attributes.Add("diameter", diameter);
                }

                if (jsonProperties["x"] != null && jsonProperties["y"] != null)
                {
                    var x = jsonProperties["x"].ToString();
                    var y = jsonProperties["y"].ToString();
                    attributes.Add("x", x);
                    attributes.Add("y", y);
                }

                var type = geometry["type"].ToString();
                var coordinates = new List<Double[]>();
                var coordinatesArray = geometry["coordinates"];

                Graphic graphic = null;

                if (type == "Polygon")
                {
                    foreach (var ring in coordinatesArray)
                    {
                        foreach (var coord in ring)
                        {
                            var coordinate = new Double[] { coord[0].Value<double>(), coord[1].Value<double>() };
                            coordinates.Add(coordinate);
                        }
                    }

                    var polygon = CreatePolygon(coordinates);

                    if (output is PCIDefects pciDefect)
                    {
                        var newTableName = pciDefect.PCIRatingName + "-" + pciDefect.DefectName;
                        var pciDefectSymbol = GetGraphicSymbol(newTableName, type, output);
                        if (pciDefectSymbol != null)
                            graphic = new Graphic(polygon, attributes, pciDefectSymbol);
                    }
                    else if (table == LayerNames.PCI && attributes.TryGetValue("RatingScale", out var pciRating))
                    {
                        var symbol = GetSeveritySymbol(table, pciRating.ToString(), type);
                        graphic = new Graphic(polygon, attributes, symbol);
                    }
                    else if (table == LayerNames.PASER && attributes.TryGetValue("PaserRating", out var paser))
                    {
                        var symbol = GetSeveritySymbol(table, paser.ToString(), type);
                        graphic = new Graphic(polygon, attributes, symbol);
                    }
                    else
                    {
                        var graphicSymbol = GetGraphicSymbol(table, GeoType.Polygon.ToString(), output);
                        if (graphicSymbol != null)
                        {
                            graphic = new Graphic(polygon, attributes, graphicSymbol);
                        }
                    }
                }
                else if (type == "Polyline")
                {
                    foreach (var coord in coordinatesArray)
                    {
                        var singleCoordinate = new Double[] { coord[0].Value<double>(), coord[1].Value<double>() };
                        coordinates.Add(singleCoordinate);
                    }
                    var polyline = CreatePolyline(coordinates);

                    if (output is PCIDefects pciDefect)
                    {
                        var newTableName = pciDefect.PCIRatingName + "-" + pciDefect.DefectName;
                        var pciDefectSymbol = GetGraphicSymbol(newTableName, type, output);
                        if (pciDefectSymbol != null)
                        {
                            graphic = new Graphic(polyline, attributes, pciDefectSymbol);
                        }
                    }
                    else
                    {
                        var polylineSymbol = GetGraphicSymbol(table, type, output);
                        graphic = new Graphic(polyline, attributes, polylineSymbol);
                    }
                }
                else if (type == "Point")
                {
                    var singleCoordinate = new Double[] { coordinatesArray[0].Value<double>(), coordinatesArray[1].Value<double>() };
                    var centerPoint = new MapPoint(singleCoordinate[0], singleCoordinate[1], SpatialReferences.Wgs84);
                    attributes.Add("GeoType", "Point");
                    int diameter = 0;

                    if (table == LayerNames.Potholes)
                    {
                        if (double.TryParse(attributes["diameter"].ToString(), out double diameterValue))
                        {
                            diameter = (int)diameterValue / 2;
                        }
                    }
                    else if (table == LayerNames.Pickout)
                    {
                        diameter = 30;
                    }
                    else
                    {
                        diameter = 150;
                    }

                    if (table == LayerNames.FOD)
                    {
                        Assembly currentAssembly = Assembly.GetExecutingAssembly();
                        Stream resourceStream = currentAssembly.GetManifestResourceStream(
                                        "DataView2.Resources.Images.circle_arrow.png");
                        PictureMarkerSymbol arrowSymbol = PictureMarkerSymbol.CreateAsync(resourceStream).Result;
                        arrowSymbol.Height = 20;
                        arrowSymbol.Width = 20;
                        arrowSymbol.AngleAlignment = SymbolAngleAlignment.Map;
                        if (attributes.TryGetValue("TrackAngle", out var trackAngleObj) && trackAngleObj is double trackAngle)
                        {
                            arrowSymbol.Angle = trackAngle;
                        }
                        graphic = new Graphic(centerPoint, attributes, arrowSymbol);
                    }
                    else
                    {
                        if (output is PCIDefects pciDefect)
                        {
                            var newTableName = pciDefect.PCIRatingName + "-" + pciDefect.DefectName;
                            var pciDefectSymbol = GetGraphicSymbol(newTableName, GeoType.Point.ToString(), output);
                            if (pciDefectSymbol != null)
                            {
                                var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, diameter, LinearUnits.Millimeters);
                                graphic = new Graphic(circleGeometry, attributes, pciDefectSymbol);
                            }
                        }
                        else
                        {
                            var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, diameter, LinearUnits.Millimeters);
                            var circleSymbol = GetGraphicSymbol(table, GeoType.Point.ToString(), output);
                            if (circleSymbol != null)
                            {
                                graphic = new Graphic(circleGeometry, attributes, circleSymbol);
                            }
                        }

                    }
                }
                else if (type == "MultiPoint")
                {
                    foreach (var coord in coordinatesArray)
                    {
                        var singleCoordinate = new Double[] { coord[0].Value<double>(), coord[1].Value<double>() };
                        coordinates.Add(singleCoordinate);

                        var centerPoint = new MapPoint(singleCoordinate[0], singleCoordinate[1], SpatialReferences.Wgs84);
                        int diameter = 100;

                        var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, diameter, LinearUnits.Millimeters);
                        var circleSymbol = GetGraphicSymbol(table, GeoType.Point.ToString(), output);
                        if (circleSymbol != null)
                        {
                            graphic = new Graphic(circleGeometry, attributes, circleSymbol);
                        }

                        if (graphic != null)
                        {
                            graphicsBag.Add(graphic);
                            graphic = null;
                        }
                    }
                }
                else if (type == "MultiPolygon")
                {
                    for (int i = 0; i < coordinatesArray.Count(); i++)
                    {
                        var polygon = coordinatesArray[i];
                        var polygonCoordinates = new List<Double[]>();

                        foreach (var coord in polygon)
                        {
                            var coordinate = new Double[] { coord[0].Value<double>(), coord[1].Value<double>() };
                            polygonCoordinates.Add(coordinate);
                        }

                        // Create the polygon geometry for this part of the MultiPolygon
                        var polygonGeometry = CreatePolygon(polygonCoordinates);
                        Dictionary<string, object> newAttributes = new Dictionary<string, object>();
                        Symbol symbol = null;

                        if (attributes.TryGetValue("LeftSeverity", out var leftSeverity) && attributes.TryGetValue("RightSeverity", out var rightSeverity) && table == LayerNames.Bleeding)
                        {
                            var leftSeverityStr = leftSeverity.ToString();
                            var rightSeverityStr = rightSeverity.ToString();
                   
                            Symbol leftSymbol = null;
                            Symbol rightSymbol = null;
                            var colorCodeInfo = appState.ColorCodeInfo.Where(x => x.TableName == table).ToList();
                            if (colorCodeInfo.Any())
                            {
                                var property = colorCodeInfo.First().Property;
                                //Only if color code based on Severity, left and right symbol will be different
                                if (property == "Severity")
                                {
                                    leftSymbol = SymbolFromColorList(leftSeverityStr, colorCodeInfo, "Fill");
                                    rightSymbol = SymbolFromColorList(rightSeverityStr, colorCodeInfo, "Fill");
                                }
                                else
                                {
                                    object propertyValueObj = GetPropertyValue(output, property);
                                    symbol = SymbolFromColorList(propertyValueObj, colorCodeInfo, "Fill");
                                }
                            }
                            else
                            {
                                symbol = GetGraphicSymbol(table, GeoType.MultiPolygon.ToString());
                            }

                            // Apply left or right symbol based on the polygon index
                            if (i == 0 && leftSeverityStr != "No Bleeding") //LEFT
                            {
                                newAttributes = new Dictionary<string, object> (attributes)
                                {
                                    ["Severity"] = leftSeverityStr
                                };
                                newAttributes.Remove("LeftSeverity");
                                newAttributes.Remove("RightSeverity");
                                var symbolToUse = leftSymbol ?? symbol;
                                if (symbolToUse != null)
                                {
                                    graphicsBag.Add(new Graphic(polygonGeometry, newAttributes, symbolToUse));
                                }
                            }
                            else if (i == 1 && rightSeverityStr != "No Bleeding") //RIGHT
                            {
                                newAttributes = new Dictionary<string, object>(attributes)
                                {
                                    ["Severity"] = rightSeverityStr
                                };
                                newAttributes.Remove("LeftSeverity");
                                newAttributes.Remove("RightSeverity");
                                var symbolToUse = rightSymbol ?? symbol;
                                if (symbolToUse != null)
                                {
                                    graphicsBag.Add(new Graphic(polygonGeometry, newAttributes, symbolToUse));
                                }
                            }
                        }
                        else
                        {
                            if (table == MultiLayerName.BandTexture)
                            {
                                foreach (var attribute in attributes)
                                {
                                    string attributeKey = attribute.Key;

                                    if (attributeKey.StartsWith("MTDBand") ||
                                            attributeKey.StartsWith("SMTDBand") ||
                                            attributeKey.StartsWith("MPDBand") ||
                                            attributeKey.StartsWith("RMSBand"))
                                    {
                                        string targetKey = attributeKey.Substring(0, attributeKey.Length - 1);
                                        int bandIndex = int.Parse(attributeKey[^1].ToString()) - 1;

                                        if (bandIndex == i) // If the current band matches the polygon index
                                        {
                                            newAttributes[targetKey] = attribute.Value;
                                        }
                                    }
                                    else
                                    {
                                        newAttributes[attributeKey] = attribute.Value;
                                    }
                                }
                            }
                            else
                            {
                                newAttributes = attributes;
                            }

                            var colorCodeInfo = appState.ColorCodeInfo.Where(cc => cc.TableName == table);
                            if (colorCodeInfo.Any())
                            {
                                var property = colorCodeInfo.First().Property;
                                object propertyValueObj;
                                if (table == MultiLayerName.BandTexture)
                                {
                                    var newProperty = property + (i + 1);
                                    propertyValueObj = GetPropertyValue(output, newProperty);
                                }
                                else
                                {
                                    propertyValueObj = GetPropertyValue(output, property);
                                }
                                symbol = SymbolFromColorList(propertyValueObj, colorCodeInfo, "Fill");
                                if (symbol == null)
                                {
                                    symbol = GetGraphicSymbol(table, GeoType.MultiPolygon.ToString());
                                }
                                var newGraphic = new Graphic(polygonGeometry, newAttributes, symbol);
                                graphicsBag.Add(newGraphic);
                            }
                            else
                            {
                                var polygonFillSymbol = GetGraphicSymbol(table, GeoType.MultiPolygon.ToString());
                                var newGraphic = new Graphic(polygonGeometry, newAttributes, polygonFillSymbol);
                                graphicsBag.Add(newGraphic);
                            }
                        }
                        polygonCoordinates.Clear();
                    }
                }
                
                if (graphic != null)
                {
                    graphicsBag.Add(graphic);
                }

                return graphicsBag;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Parsing Geojson {ex.Message}");
                return new List<Graphic>();
            }
        }


        public void CreateCornerBreakGraphicss(List<LCMS_Corner_Break> corner_Breaks, List<Graphic> graphicsbag)
        {
            var table = LayerNames.CornerBreak;
            var cornerBreakAttribtes = new Dictionary<string, object>();

            // Group corner breaks by coordinates
            var groupedCornerBreaks = corner_Breaks
                .GroupBy(breakObj => new { Longitude = breakObj.GPSLongitude, Latitude = breakObj.GPSLatitude })
                .Select(group =>
                {
                    var cornerBreakAttributes = new Dictionary<string, object>();
                    var idList = new List<string>();

                    double maxAvgDepth = double.MinValue;
                    double maxArea = double.MinValue;
                    double maxBreakArea = double.MinValue;
                    double maxCnrSpallingArea = double.MinValue;
                    double maxAreaRatio = double.MinValue;

                    foreach (var breakObj in group)
                    {
                        var id = breakObj.Id.ToString();
                        idList.Add(id);

                        // Find the maximum values for specified attributes
                        if (breakObj.AvgDepth_mm > maxAvgDepth)
                        {
                            maxAvgDepth = breakObj.AvgDepth_mm;
                        }
                        if (breakObj.Area_mm2 > maxArea)
                        {
                            maxArea = breakObj.Area_mm2;
                        }
                        if (breakObj.BreakArea_mm2 > maxBreakArea)
                        {
                            maxBreakArea = breakObj.BreakArea_mm2;
                        }
                        if (breakObj.CNR_SpallingArea_mm2 > maxCnrSpallingArea)
                        {
                            maxCnrSpallingArea = breakObj.CNR_SpallingArea_mm2;
                        }
                        if (breakObj.AreaRatio > maxAreaRatio)
                        {
                            maxAreaRatio = breakObj.AreaRatio;
                        }
                    }

                    // Add common attributes from the first CoordinateData in the group
                    cornerBreakAttributes.Add("Coordinates", new double[] { group.First().GPSLongitude, group.First().GPSLatitude });
                    cornerBreakAttributes.Add("SurveyId", group.First().SurveyId);
                    cornerBreakAttributes.Add("Table", table);
                    cornerBreakAttributes.Add("SegmentId", group.First().SegmentId);
                    cornerBreakAttributes.Add("TrackAngle", group.First().GPSTrackAngle);
                    string concatenatedString = string.Join(",", idList);
                    cornerBreakAttributes.Add("Id", concatenatedString);

                    // Add maximum values for the specified attributes
                    cornerBreakAttributes.Add("MaxAvgDepth_mm", maxAvgDepth);
                    cornerBreakAttributes.Add("MaxArea_mm2", maxArea);
                    cornerBreakAttributes.Add("MaxBreakArea_mm2", maxBreakArea);
                    cornerBreakAttributes.Add("MaxCNR_SpallingArea_mm2", maxCnrSpallingArea);
                    cornerBreakAttributes.Add("MaxAreaRatio", maxAreaRatio);

                    var geoJson = group.First().GeoJSON;
                    var geoJsonObject = JObject.Parse(geoJson);
                    var jsonProperties = geoJsonObject["properties"];
                    if (jsonProperties["x"] != null && jsonProperties["y"] != null)
                    {
                        var x = jsonProperties["x"].ToString();
                        var y = jsonProperties["y"].ToString();
                        cornerBreakAttributes.Add("x", x);
                        cornerBreakAttributes.Add("y", y);
                    }

                    return cornerBreakAttributes;
                })
                .ToList();

            if (groupedCornerBreaks != null)
            {
                foreach (var cornerBreak in groupedCornerBreaks)
                {
                    if (cornerBreak.ContainsKey("Coordinates"))
                    {
                        var coordinateList = cornerBreak["Coordinates"] as double[];
                        var centerPoint = new MapPoint(coordinateList[0], coordinateList[1], SpatialReferences.Wgs84);
                        var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, 0.25, LinearUnits.Meters);
                        cornerBreak.Remove("Coordinates");
                        var cornerAttributes = cornerBreak;
                        Symbol circleSymbol = null;
                       
                        var colorCodeInfo = appState.ColorCodeInfo.Where(cc => cc.TableName == table);
                        if (colorCodeInfo.Any())
                        {
                            var property = colorCodeInfo.First().Property;
                            var propertyValueObj = cornerBreak["Max" + property];
                            var propertyValue = Convert.ToDouble(propertyValueObj);
                            circleSymbol = SymbolFromColorList(propertyValue, colorCodeInfo, LayerNames.CornerBreak);
                        }
                        else
                        {
                            circleSymbol = GetGraphicSymbol(table, GeoType.Point.ToString());
                        }
                        var graphic = new Graphic(circleGeometry, cornerAttributes, circleSymbol);
                        graphicsbag.Add(graphic);
                    }
                }
            }
        }

        public Symbol GetSeveritySymbol(string tableName, string severityValue, string geoType)
        {
            //Default symbols by severity
            if (tableName == LayerNames.PCI)
            {
                return severityValue switch
                {
                    "Good" => CreateSimpleFillSymbol(0, 255, 0, 180, 1),
                    "Satisfactory" => CreateSimpleFillSymbol(128, 255, 0, 180, 1),
                    "Fair" => CreateSimpleFillSymbol(255, 255, 0, 180, 1),
                    "Poor" => CreateSimpleFillSymbol(255, 192, 0, 180, 1),
                    "Very Poor" => CreateSimpleFillSymbol(255, 128, 0, 180, 1),
                    "Serious" => CreateSimpleFillSymbol(255, 64, 0, 180, 1),
                    "Failed" => CreateSimpleFillSymbol(255, 0, 0, 180, 1),
                    "Unknown" => CreateSimpleFillSymbol(255, 255, 180, 255, 1),
                };
            }
            else if (tableName == LayerNames.PASER)
            {
                var severityInt = Convert.ToInt32(severityValue);
                return severityInt switch
                {
                    10 => CreateSimpleFillSymbol(0, 128, 0, 180, 1),
                    9 => CreateSimpleFillSymbol(50, 205, 50, 180, 1),
                    8 => CreateSimpleFillSymbol(0, 255, 0, 180, 1),
                    7 => CreateSimpleFillSymbol(0, 255, 127, 180, 1),
                    6 => CreateSimpleFillSymbol(173, 255, 47, 180, 1),
                    5 => CreateSimpleFillSymbol(255, 255, 0, 180, 1),
                    4 => CreateSimpleFillSymbol(255, 215, 0, 180, 1),
                    3 => CreateSimpleFillSymbol(255, 140, 0, 180, 1),
                    2 => CreateSimpleFillSymbol(255, 69, 0, 180, 1),
                    1 => CreateSimpleFillSymbol(255, 0, 0, 180, 1)
                };
            }
 
            //if no symbol dict found, return default symbol
            if (geoType == "Polyline")
            {
                return severityValue switch
                {
                    "Very Low" => CreateSimpleLineSymbol(64, 255, 255, 255, 1),
                    "Low" => CreateSimpleLineSymbol(0, 192, 0, 255, 1),
                    "Medium" => CreateSimpleLineSymbol(255, 151, 15, 255, 1),
                    "High" => CreateSimpleLineSymbol(250, 115, 115, 255, 1),
                    "Very High" => CreateSimpleLineSymbol(200, 0, 0, 255, 1),
                    _ => CreateSimpleLineSymbol(0, 0, 0, 0, 1)
                };
            }
            else
            {
                return severityValue switch
                {
                    "Very Low" => CreateSimpleFillSymbol(64, 255, 255, 170, 1),
                    "Low" => CreateSimpleFillSymbol(46, 159, 207, 170, 1),
                    "Medium" => CreateSimpleFillSymbol(53, 105, 176, 170, 1),
                    "High" => CreateSimpleFillSymbol(191, 0, 83, 170, 1),
                    "Very High" => CreateSimpleFillSymbol(200, 0, 0, 255, 1),
                    _ => CreateSimpleFillSymbol(0, 0, 0, 0, 1)
                };
            }
        }

        public async Task AddGraphicsToOverlay(List<Graphic> graphicsBag, GraphicsOverlay graphicsOverlay)
        {
            if (graphicsBag == null || graphicsBag.Count == 0)
            {
                Console.WriteLine("No graphic found in graphicsBag.");
                return;
            }

            //multi geojsons would have more than one graphic overlay
            if (graphicsOverlay == null)
            {
                var overlayGraphicsMap = new Dictionary<string, List<Graphic>>();
                foreach (var graphic in graphicsBag)
                {
                    if (graphic != null && graphic.Attributes.TryGetValue("Table", out var tableNameObj))
                    {
                        var tableName = tableNameObj?.ToString();
                        if (string.IsNullOrEmpty(tableName)) continue;

                        //Segment Grid is actually not multi geojson but due to having separate overlays based on crack type, it should be treated a little differently.
                        if (tableName == LayerNames.SegmentGrid && graphic.Attributes.TryGetValue("CrackType", out var crackType) && crackType != null)
                        {
                            var crackTypeStr = crackType.ToString();
                            if (crackTypeStr == "Alligator")
                            {
                                tableName = "Fatigue";
                            }
                            else
                            {
                                tableName = crackTypeStr;
                            }
                        }
                        
                        var overlay = overlays.FirstOrDefault(x => x.Id == tableName);
                        if (overlay != null)
                        {
                            overlay.Graphics.Add(graphic);
                            if (!overlayGraphicsMap.ContainsKey(tableName))
                            {
                                overlayGraphicsMap[tableName] = new List<Graphic>();
                            }
                            overlayGraphicsMap[tableName].Add(graphic);
                        }
                    }
                }

                // Now batch label setting per overlay
                foreach (var kvp in overlayGraphicsMap)
                {
                    var overlayId = kvp.Key;
                    var hasLabel = await OverlayHasLabel(overlayId);
                    if (hasLabel != "No Label")
                    {
                        SetLabels(overlayId, hasLabel);
                    }
                }
            }
            else
            {
                foreach (var graphic in graphicsBag)
                {
                    if (graphic != null)
                    {
                        graphicsOverlay.Graphics.Add(graphic);
                    }
                }

                var hasLabel = await OverlayHasLabel(graphicsOverlay.Id);
                //if the color code has label property then set label (table name, string label property ) 
                if (hasLabel != "No Label")
                {
                    SetLabels(graphicsOverlay.Id, hasLabel);
                }
            }
        }


    
        public async Task FetchAndDisplayAllTablesBySurvey(IEnumerable<string> surveyIds = null)
        {
            ClearAllGraphicsFromOverlays();

            if (surveyIds == null)
            {
                surveyIds = appState.SelectedSurveysIds.ToList();
            }
            try
            {
                //Add these first
                await FetchAndDisplayVideoAsync(surveyIds.ToList());
                await FetchAndDispleayKeycodesAsync(surveyIds.ToList());
                await FetchBoundary();

                foreach (string surveyID in surveyIds)
                {
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSegmentForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPCIForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMacroTextureForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchBleedingForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchConcreteJointsForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRavellingForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCracksForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPatchProcessedForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPickoutsForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPotholesForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSpallingForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCornerBreakForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMarkingContourForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSealedCrackForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPumpingForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCurbDropOffForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchMMOForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRoughnessForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRumbleStripForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchRuttingForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchGeometryForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchINSGeometryForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchShoveForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSegmentGridForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchGroovesForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchSagsBumpsForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchFODForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchPASERForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchCrackSummaryForSurveyAsync);
                    await FetchAndDisplayDefectsBySurveyAsync(surveyID, FetchLasRuttingForSurveyAsync);
                    await FetchAndDisplayLasFiles(surveyID);
                    await FetchVehiclePath(surveyID);
                }
                await FetchAllSummaries();
                await FetchAllShapefiles();
                await FetchAllMetaTables();
                await FetchAllPCIDefects();
                await FetchSurveySegmentations();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while fetching and displaying all tables by survey.");
                Console.WriteLine(ex.Message);
            }
        }

        // Check if there are zero defects in each layer and hide layer if true.
        public async Task CheckTablesForNoDefects(List<string> deletedLayerNames)
        {
            try
            {
                var matchingOverlays = overlays.Where(o => deletedLayerNames.Contains(o.Id)).ToList();

                if (matchingOverlays != null && matchingOverlays.Count > 0)
                {
                    foreach (var matchingOverlay in matchingOverlays)
                    {
                        if (matchingOverlay.Graphics.Count == 0)
                        {
                            //refresh the layer menu only if the overlay has no graphics at all
                            appState.RefreshTableNames();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while checking for defects by survey.");
                Console.WriteLine(ex.Message);
            }
        }

        private async Task FetchAndDisplayDefectsBySurveyAsync<T>(string surveyID, Func<string, Task<List<T>>> fetchDefectsAsync) where T : new()
        {
            string overlayName = "";

            try
            {
                GraphicsOverlay overlay = DetermineOverlay<T>();

                var defects = await fetchDefectsAsync(surveyID);
                if (defects != null && defects.Any())
                {
                    //should add overlay first to find each from overlays with its Id
                    if (overlay != null && !overlays.Contains(overlay))
                    {
                        overlays.Add(overlay);
                    }

                    if (typeof(T) == typeof(LCMS_Rough_Processed))
                    {
                        //add roughness overlays
                        if (!overlays.Contains(roughnessLaneOverlay))
                        {
                            overlays.Add(roughnessLaneOverlay);
                        }

                        if (!overlays.Contains(roughnessLeftOverlay))
                        {
                            overlays.Add(roughnessLeftOverlay);
                        }

                        if (!overlays.Contains(roughnessRightOverlay))
                        {
                            overlays.Add(roughnessRightOverlay);
                        }
                    }

                    if (typeof(T) == typeof(LCMS_Rut_Processed))
                    {
                        //add rutting overlays
                        if (!overlays.Contains(rutLaneOverlay))
                        {
                            overlays.Add(rutLaneOverlay);
                        }

                        if (!overlays.Contains(rutLeftOverlay))
                        {
                            overlays.Add(rutLeftOverlay);
                        }

                        if (!overlays.Contains(rutRightOverlay))
                        {
                            overlays.Add(rutRightOverlay);
                        }
                    }

                    if (typeof(T) == typeof(LCMS_Segment_Grid))
                    {
                        //add segment grid overlays
                        if (!overlays.Contains(segmentGridLongitudinalOverlay))
                        {
                            overlays.Add(segmentGridLongitudinalOverlay);
                        }

                        if (!overlays.Contains(segmentGridTransversalOverlay))
                        {
                            overlays.Add(segmentGridTransversalOverlay);
                        }

                        if (!overlays.Contains(segmentGridFatigueOverlay))
                        {
                            overlays.Add(segmentGridFatigueOverlay);
                        }

                        //if (!overlays.Contains(segmentGridOthersOverlay))
                        //{
                        //    overlays.Add(segmentGridOthersOverlay);
                        //}
                    }

                    if (typeof(T) == typeof(LCMS_Texture_Processed))
                    {

                        if (!overlays.Contains(textureOverlay))
                        {
                            overlays.Add(textureOverlay);
                        }

                        if (!overlays.Contains(avgTextureOverlay))
                        {
                            overlays.Add(avgTextureOverlay);
                        }
                    }

                    // Determine batch size for processing
                    int batchSize = 200;
                    int defectCount = defects.Count;
                    int processedCount = 0;
                    overlayName = overlay != null ? overlay.Id : null;

                    Log.Information($"Starting to display defects for survey {surveyID} on overlay {overlayName}. Total defects: {defectCount}");

                    // Process defects in batches
                    while (processedCount < defectCount)
                    {
                        int remainingCount = defectCount - processedCount;
                        int currentBatchSize = Math.Min(batchSize, remainingCount);

                        List<Graphic> graphicsBag = new List<Graphic>();

                        // Slice the defects list to current batch
                        var currentBatch = defects.Skip(processedCount).Take(currentBatchSize).ToList();

                        // Create graphics for corner break (example)
                        if (typeof(T) == typeof(LCMS_Corner_Break))
                        {
                            var corners = currentBatch.Cast<LCMS_Corner_Break>().ToList();
                            CreateCornerBreakGraphicss(corners, graphicsBag);
                        }
                        else if (typeof(T) == typeof(LCMS_Segment))
                        {
                            var segments = currentBatch.Cast<LCMS_Segment>().ToList();

                            // Find the new segments that are not in the loadedSegments list
                            var newSegments = segments.Where(segment => !loadedSegments.Any(loaded => loaded.Id == segment.Id));
                            if (newSegments.Count() >= 1)
                            {
                                DisplaySegments(newSegments);

                                // Add the new segments to the loadedSegmentslist
                                loadedSegments.AddRange(newSegments);
                            }
                        }
                        else
                        {
                            // Create graphics for other types
                            CreateGraphicsFromGeoJson(currentBatch, graphicsBag);
                        }

                        // Add graphics to overlay
                        await AddGraphicsToOverlay(graphicsBag, overlay);

                        Log.Information($"Displayed {currentBatchSize} defects for survey {surveyID} on overlay {overlayName}. Processed {processedCount + currentBatchSize}/{defectCount}");

                        // Increment processed count
                        processedCount += currentBatchSize;

                    }

                    //if (typeof(T) == typeof(LCMS_Segment))
                    //    RemoveOverlappingGraphics(segmentOverlay);

                    Log.Information($"Completed displaying all defects for survey {surveyID} on overlay {overlayName}. Total defects displayed: {defectCount}");

                }
            }

            catch (Exception ex)
            {
                Log.Error($"An error occurred while fetching and displaying defects. Layer:{overlayName}");
                Log.Error(ex.Message);
            }
        }

        private async Task<string> OverlayHasLabel(string overlayName)
        {
            MapGraphicData mapGraphicData = await appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = overlayName });
            if (mapGraphicData != null && mapGraphicData.LabelProperty != "No Label")
                return mapGraphicData.LabelProperty;
            else
                return "No Label";
        }
      
        private GraphicsOverlay DetermineOverlay<T>()
        {
            if (typeof(T) == typeof(LCMS_Cracking_Raw))
            {
                return crackingOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Potholes_Processed))
            {
                return potholeOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Concrete_Joints))
            {
                return concreteJointOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Corner_Break))
            {
                return cornerBreakOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Patch_Processed))
            {
                return patchOverlay;
            }
            else if (typeof(T) == typeof(LCMS_PickOuts_Raw))
            {
                return pickoutOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Ravelling_Raw))
            {
                return ravellingOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Spalling_Raw))
            {
                return spallingOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Segment))
            {
                return segmentOverlay;
            }
            else if (typeof(T) == typeof(LCMS_FOD))
            {
                return fodOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Curb_DropOff))
            {
                return curbDropOffOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Marking_Contour))
            {
                return markingContourOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Pumping_Processed))
            {
                return pumpingOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Sealed_Cracks))
            {
                return sealedCrackOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Bleeding))
            {
                return bleedingOverlay;
            }
            else if (typeof(T) == typeof(LCMS_MMO_Processed))
            {
                return mmoOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Rumble_Strip))
            {
                return rumbleStripOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Geometry_Processed))
            {
                return geometryOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Shove_Processed))
            {
                return shoveOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Grooves))
            {
                return groovesOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Sags_Bumps))
            {
                return sagsbumpsOverlay;
            }
            else if (typeof(T) == typeof(LCMS_Segment_Grid))
            {
                return segmentGridOverlay;
            }
            else if (typeof(T) == typeof(LCMS_PCI))
            {
                return pciOverlay;
            }
            else if (typeof(T) == typeof(LCMS_PASER))
            {
                return paserOverlay;
            }
            else if (typeof(T) == typeof(LCMS_CrackSummary))
            {
                return crackSummaryOverlay;
            }
            else if (typeof(T) == typeof(Geometry_Processed))
            {
                return iNSGeometryOverlay;
            }
            else if (typeof(T) == typeof(LAS_Rutting))
            {
                return lasRuttingOverlay;
            }
            else
            {
                return null;
            }
        }
        private async Task<List<LCMS_Segment>> FetchSegmentForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.SegmentService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });

            Log.Information($"Loaded --  {defects.Count} -- segment for survey  -- {surveyID} -- ");


            return defects;
        }
        private async Task<List<LCMS_Cracking_Raw>> FetchCracksForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.CrackingRawService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });

            Log.Information($"Loaded --  {defects.Count} -- cracks for survey  -- {surveyID} -- ");


            return defects;
        }

        private async Task<List<LCMS_Concrete_Joints>> FetchConcreteJointsForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.ConcreteJointService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- CONCRETE JOINTS for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Corner_Break>> FetchCornerBreakForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.CornerBreakService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- CORNER BREAKS for survey  -- {surveyID} -- ");

            return defects;

        }

        private async Task<List<LCMS_FOD>> FetchFODForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.FODService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- FOD for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Patch_Processed>> FetchPatchProcessedForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PatchService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- PATCH for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_PickOuts_Raw>> FetchPickoutsForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PickOutRawService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- PICKOUTS for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Potholes_Processed>> FetchPotholesForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PotholesService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- POTHOLES for survey  -- {surveyID} -- ");


            return defects;
        }

        private async Task<List<LCMS_Ravelling_Raw>> FetchRavellingForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.RavellingService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- RAVELLING for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Spalling_Raw>> FetchSpallingForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.SpallingService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- SPALLING for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Bleeding>> FetchBleedingForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.BleedingService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- BLEEDING for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Marking_Contour>> FetchMarkingContourForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.MarkingContourService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- MARKING CONTOUR for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Pumping_Processed>> FetchPumpingForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PumpingService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- PUMPING for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Sealed_Cracks>> FetchSealedCrackForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.SealedCrackService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- SEALED CRACK for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Curb_DropOff>> FetchCurbDropOffForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.CurbDropOffService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- CURB DROPOFF for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_MMO_Processed>> FetchMMOForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.MMOService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- MMO for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Texture_Processed>> FetchMacroTextureForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.MacroTextureService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- MACRO TEXTURE for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_Rough_Processed>> FetchRoughnessForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.RoughnessService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- ROUGHNESS for survey  -- {surveyID} -- ");

            if (defects.Count > 0)
            {
                var hasCwp = await appEngine.RoughnessService.HasCwpIRI(new Empty());
                if (hasCwp.Id == 1)
                {
                    //Create overlay
                    var roughnessCenterOverlay = new GraphicsOverlay() { Id = "Cwp IRI" };
                    overlays.Add(roughnessCenterOverlay);
                }
            }
            return defects;
        }

        private async Task<List<LCMS_Rumble_Strip>> FetchRumbleStripForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.RumbleStripService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- RUMBLE STRIP for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_Rut_Processed>> FetchRuttingForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.RutProcessedService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- RUT for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_Shove_Processed>> FetchShoveForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.ShoveService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- SHOVE for survey  -- {surveyID} -- ");
            return defects;
        }

        //Segment Grid:
        private async Task<List<LCMS_Segment_Grid>> FetchSegmentGridForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.SegmentGridService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- SEGMENTGRID for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_Geometry_Processed>> FetchGeometryForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.GeometryService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- Geometry for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<Geometry_Processed>> FetchINSGeometryForSurveyAsync(string surveyID)
        {
            var geometries = await appEngine.INSGeometryService.GetBySurvey(surveyID);

            Log.Information($"Loaded -- {geometries.Count} -- INS Geometry for survey -- {surveyID} -- ");

            return geometries;
        }

        private async Task<List<LCMS_Grooves>> FetchGroovesForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.GroovesService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- GROOVES for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_Sags_Bumps>> FetchSagsBumpsForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.SagsBumpsService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- SAGS BUMPS for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_PCI>> FetchPCIForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PCIService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- PCI for survey  -- {surveyID} -- ");

            return defects;
        }
        private async Task<List<LCMS_PASER>> FetchPASERForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.PASERService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- PASER for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LCMS_CrackSummary>> FetchCrackSummaryForSurveyAsync(string surveyID)
        {
            var defects = await appEngine.CrackSummaryService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- CRACK SUMMARY for survey  -- {surveyID} -- ");

            return defects;
        }

        private async Task<List<LAS_Rutting>> FetchLasRuttingForSurveyAsync (string surveyID)
        {
            var defects = await appEngine.LAS_RuttingService.GetBySurvey(new SurveyRequest
            {
                SurveyId = surveyID
            });
            Log.Information($"Loaded --  {defects.Count} -- LAS RUTTING for survey  -- {surveyID} -- ");

            return defects;
        }

        private void ClearAllGraphicsFromOverlays()
        {
            //not removed surveySetOverlay from loaded list once loaded
            foreach (var overlay in overlays.Where(o => o.Id != LayerNames.Segment && o.Id != "surveySetOverlay"))
            {
                overlay.Graphics.Clear();
            }

            foreach (var overlay in cameraOverlays)
            {
                appState.CloseVideoPopup(overlay.Value.Id);
            }
            cameraOverlays.Clear();
        }

        private bool _isGettingColorCodeInfo = false;
        private async void GetColorCodeInfo()
        {
            if (_isGettingColorCodeInfo)
                return;
            _isGettingColorCodeInfo = true;

            appState.ColorCodeInfo.Clear();
            var colorCodeInformation = await appEngine.ColorCodeInformationService.GetAll(new Empty());
            if (colorCodeInformation.Any())
            {
                appState.ColorCodeInfo.AddRange(colorCodeInformation);
            }
            _isGettingColorCodeInfo = false;
        }
        private async void GetMapGraphicData()
        {
            if (mapGraphicData != null && mapGraphicData.Any())
            {
                mapGraphicData.Clear();
            }

            mapGraphicData = new List<MapGraphicData>();

            try
            {
                var response = await appEngine.MapGraphicDataService.GetAll(new Empty());
                if (response.Any())
                {
                    mapGraphicData = response;

                    segmentSymbol = GetGraphicSymbol("Segment", "Segment");
                    highlightedSegmentSymbol = GetGraphicSymbol("HighlightedSegment", "Segment");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Loading Map Graphic Data: {ex.Message}");
            }
        }

        public Symbol GetGraphicSymbol(string tableName, string geoType, object output = null)
        {
            var item = mapGraphicData.FirstOrDefault(x => x.Name == tableName);
            if (item == null)
            {
                item = new MapGraphicData //default red colour
                {
                    Red = 255,
                    Blue = 0,
                    Green = 0,
                    Alpha = 128,
                    Thickness = 5
                };
            }

            //SEGMENT GRAPHICS
            if (geoType == "Segment")
            {
                if (item.Name == "HighlightedSegment")
                {
                    var line = CreateSimpleLineSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                    return CreateSimpleFillSymbol(0, 0, 0, 0, 0, line);
                }
                else
                {
                    var line = CreateSimpleLineSymbol(item.Red, item.Green, item.Blue, 255, item.Thickness);
                    return CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness, line);
                }
            }

            Symbol symbol = null;

            //multi polygon and corner break handle colour code on their own
            if (geoType == GeoType.MultiPolygon.ToString() || tableName == LayerNames.CornerBreak)
            {
                return CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
            }

            //check with color code first
            var colorCodeInfo = appState.ColorCodeInfo.Where(cc => cc.TableName == tableName);
            if (colorCodeInfo.Any())
            {
                var property = colorCodeInfo.First().Property;
                var propertyValueObj = GetPropertyValue(output, property);

                if (geoType == GeoType.Polygon.ToString())
                {
                    string symbolType = string.Empty;
                    if (tableName == LayerNames.MMO || tableName == LayerNames.RumbleStrip || tableName == LayerNames.Pumping)
                    {
                        symbolType = "FillLine";
                    }
                    else
                    {
                        symbolType = "Fill";
                    }
                    var colorCodeSymbol = SymbolFromColorList(propertyValueObj, colorCodeInfo, symbolType);
                    symbol = colorCodeSymbol ?? CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);

                }
                else if (geoType == GeoType.Polyline.ToString())
                {
                    var colorCodeSymbol = SymbolFromColorList(propertyValueObj, colorCodeInfo, "Line");
                    symbol = colorCodeSymbol ?? CreateSimpleLineSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                }
                else if (geoType == GeoType.Point.ToString())
                {
                    var colorCodeSymbol = SymbolFromColorList(propertyValueObj, colorCodeInfo, "Fill");
                    symbol = colorCodeSymbol ?? CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                }
            }
            else
            {
                //use map graphic if no color code
                var symbolType = item.SymbolType;
                if (geoType == GeoType.Polyline.ToString())
                {
                    symbol = CreateSimpleLineSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                }
                else if (geoType == GeoType.Polygon.ToString())
                {
                    if (symbolType == "FillLine")
                    {
                        symbol = CreateFillLineSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                    }
                    else
                    {
                        symbol = CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                    }
                }
                else if (geoType == GeoType.Point.ToString())
                {
                    symbol = CreateSimpleFillSymbol(item.Red, item.Green, item.Blue, item.Alpha, item.Thickness);
                }
            }  

            return symbol;
        }


        public SimpleFillSymbol CreateSimpleFillSymbol(int red, int green, int blue, int alpha, double width, SimpleLineSymbol line = null)
        {
            var color = System.Drawing.Color.FromArgb(alpha, red, green, blue);

            if (line != null)
            {
                return new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, color, line);
            }
            else
            {
                // Create and return the SimpleFillSymbol
                return new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, color, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, width));
            }
        }

        public SimpleFillSymbol CreateFillLineSymbol(int red, int green, int blue, int alpha, double width)
        {
            var color = System.Drawing.Color.FromArgb(alpha, red, green, blue);

            // Create and return the SimpleFillSymbol
            return new SimpleFillSymbol(SimpleFillSymbolStyle.Null, color, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, width));
        }

        public SimpleLineSymbol CreateSimpleLineSymbol(int red, int green, int blue, int alpha, double width)
        {
            var color = System.Drawing.Color.FromArgb(alpha, red, green, blue);

            // Create and return the SimpleLineSymbol
            return new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, width);
        }

        public Polygon CreatePolygon(List<Double[]> rings)
        {
            var polygonBuilder = new PolygonBuilder(SpatialReferences.Wgs84);

            var pointCollection = new Esri.ArcGISRuntime.Geometry.PointCollection(SpatialReferences.Wgs84);

            foreach (var ring in rings)
            {
                pointCollection.Add(new MapPoint(ring[0], ring[1], SpatialReferences.Wgs84));
            }

            polygonBuilder.AddPart(pointCollection);

            return polygonBuilder.ToGeometry();
        }

        public Polyline CreatePolyline(List<Double[]> paths)
        {
            var pointCollection = new Esri.ArcGISRuntime.Geometry.PointCollection(SpatialReferences.Wgs84);

            foreach (var coordinate in paths)
            {
                pointCollection.Add(new MapPoint(coordinate[0], coordinate[1], SpatialReferences.Wgs84));
            }

            var polyline = new Polyline(pointCollection);
            return polyline;
        }

        private void HandleOverlayVisibilityChanged(string overlayId)
        {
            try
            {
                var matchingOverlay = overlays.FirstOrDefault(x => x.Id == overlayId);
                if (matchingOverlay != null)
                {
                    matchingOverlay.IsVisible = !matchingOverlay.IsVisible;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"error in HandleOverlayVisibilityChanged : {ex.Message}");
            }
        }
        private void HandleTableCheckedChanged(List<string> selectedTables)
        {
            try
            {
                foreach (var overlay in overlays)
                {
                    if (selectedTables.Contains(overlay.Id)) //show on the map
                    {
                        overlay.IsVisible = true;
                        
                        if (overlay.Id == "Segment")
                        {
                            foreach (var graphic in overlay.Graphics)
                            {
                                graphic.Symbol = segmentSymbol;
                            }
                            appState.SegmentLayerChecked(true);
                            appState.SegmentsLoad(loadedSegments.Where(s => appState.SelectedSurveysIds.Contains(s.SurveyId)).ToList().Count);
                        }
                        else if (cameraOverlays.ContainsKey(overlay.Id))
                        {
                            foreach (var graphic in overlay.Graphics)
                            {
                                graphic.IsVisible = true;
                            }
                        }
                    }
                    else //hide from the map
                    {
                        if (overlay.Id == "Segment")
                        {
                            foreach (var graphic in overlay.Graphics)
                            {
                                graphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Transparent, 1.0));
                            }
                            appState.SegmentLayerChecked(false);
                            appState.SegmentsLoad(0);
                        }
                        else if (overlay.Id == "drawingDefectOverlay" || overlay.Id == "surveySetOverlay")
                        {
                            overlay.IsVisible = true;
                        }
                        else if (cameraOverlays.ContainsKey(overlay.Id))
                        {
                            foreach (var graphic in overlay.Graphics)
                            {
                                graphic.IsVisible = false;
                            }
                        }
                        else
                        {
                            overlay.IsVisible = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"error in HandleTableCheckedChanged : {ex.Message}");
            }
        }
        private void ShowSelectedSurveyGraphics(IEnumerable<Survey> selectedSurveys)
        {
            HashSet<string> selectedSurveyIds = new HashSet<string>(selectedSurveys.Select(s => s.SurveyIdExternal));
            int visibleSegmentCount = 0;
            Parallel.ForEach(overlays, overlay =>
            {
                try
                {
                    var graphicsToRemove = new List<Graphic>();

                    foreach (var graphic in overlay.Graphics)
                    {
                        var surveyId = graphic.Attributes.ContainsKey("SurveyId") ? graphic.Attributes["SurveyId"]?.ToString() : null;
                        if (selectedSurveyIds.Contains(surveyId))
                        {
                            graphic.IsVisible = true;
                            if (overlay.Id == LayerNames.Segment)
                                visibleSegmentCount++;
                        }
                        else
                        {
                            if (overlay.Id == LayerNames.Segment)
                                graphic.IsVisible = false;
                            else
                                graphicsToRemove.Add(graphic); // Queue graphics for removal                           
                        }
                    }

                    foreach (var graphic in graphicsToRemove)
                    {
                        overlay.Graphics.Remove(graphic);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            appState.SegmentsLoad(visibleSegmentCount);
        }

        #region colorcode
        public void GetGraphicColor(string tableName)
        {
            if (tableName != null)
            {
                var graphicData = mapGraphicData.FirstOrDefault(x => x.Name == tableName);
                if (graphicData != null && graphicData.Id > 0)
                {
                    var color = System.Drawing.Color.FromArgb(graphicData.Alpha, graphicData.Red, graphicData.Green, graphicData.Blue);
                    string hexColor = GeneralHelper.ConvertColorToHex(color);
                    appState.Color = hexColor;
                    appState.Thickness = graphicData.Thickness;
                }
                else
                {  
                    //Default color
                    var color = System.Drawing.Color.FromArgb(150, 255, 0, 0);
                    string hexColor = GeneralHelper.ConvertColorToHex(color);
                    appState.Color = hexColor;
                    appState.Thickness = 5;   
                }
            }
        }

        public async void SetBasicGraphicColor(string tableName, string rgbaHex, double thickness, string label)
        {
            try
            {
                var argbColor = GeneralHelper.ConvertHexToColor(rgbaHex);

                //change the graphic colours on the map
                var overlay = overlays.FirstOrDefault(o => o.Id == tableName);
                if (overlay != null && overlay.Graphics.Count > 0)
                {
                    foreach (Graphic graphic in overlay.Graphics)
                    {
                        if (graphic.Geometry is Polygon)
                        {
                            graphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, argbColor, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, argbColor, thickness));
                        }
                        else if (graphic.Geometry is Polyline)
                        {
                            graphic.Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, argbColor, thickness);
                        }
                        else if (graphic.Geometry is MapPoint)
                        {
                            if (graphic.Symbol is SimpleMarkerSymbol existingSymbol)
                            {
                                existingSymbol.Color = argbColor;
                            }
                        }
                    }
                }

                //Save SymbolDict to the database
                await SaveSymbolDictToDatabase(tableName, argbColor.A, argbColor.R, argbColor.G, argbColor.B, thickness, label);

                //if the label is different than "No Label" then set labels, else  RemoveLabels 
                if (label != "No Label")
                {
                    SetLabels(tableName, label);
                }
                else
                {
                    RemoveLabels(tableName);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                appState.NotifyColorCodeApplied();
            }
        }

        public void SetLabels(string tableName, string label)
        {
            try
            {
                var overlay = overlays.FirstOrDefault(o => o.Id == tableName);
                if (overlay == null)
                {
                    Console.WriteLine($"Overlay with Id '{tableName}' not found.");
                    return;
                }
                // Remove existing label graphics
                RemoveLabels(tableName);
                // Clone the current graphics list to iterate safely
                var currentGraphics = overlay.Graphics.ToList();
                foreach (var graphic in currentGraphics)
                {
                    string labelText = graphic.Attributes.ContainsKey(label)
                        ? graphic.Attributes[label]?.ToString()
                        : "";
                    var textSymbol = new TextSymbol
                        (
                        labelText, System.Drawing.Color.Yellow, 16,
                        Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                        Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);

                    textSymbol.FontWeight = Esri.ArcGISRuntime.Symbology.FontWeight.Bold;
                    textSymbol.HaloColor = System.Drawing.Color.Black;

                    overlay.Graphics.Add(new Graphic(graphic.Geometry, textSymbol));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting labels: {ex.Message}");
            }
        }
        public void RemoveLabels(string tableName)
        {
            try
            {
                var overlay = overlays.FirstOrDefault(o => o.Id == tableName);
                if (overlay == null)
                {
                    Console.WriteLine($"Overlay with Id '{tableName}' not found.");
                    return;
                }
                if (overlay.Graphics.Any(x => x.Symbol is TextSymbol))
                {
                    // Manually remove label graphics by iterating in reverse to avoid skipping elements
                    for (int i = overlay.Graphics.Count - 1; i >= 0; i--)
                    {
                        if (overlay.Graphics[i].Symbol is TextSymbol)
                        {
                            overlay.Graphics.RemoveAt(i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing labels: {ex.Message}");
            }
        }

        public async void SetColorCodeGraphic(List<ColorCodeInformation> colorCodes, string labelProperty)
        {
            try
            {
                var tableName = colorCodes[0].TableName;

                if (tableName == LayerNames.CornerBreak)
                {
                    await SetColorCodeForCornerBreak(colorCodes);
                }
                else
                {
                    var deleteResponse = await appEngine.ColorCodeInformationService.DeleteByTableName(tableName);

                    var createResponse = await appEngine.ColorCodeInformationService.CreateRange(colorCodes);
                    if (createResponse.Id == 1)
                    {
                        Log.Information("Color code information is successfully saved into the database");
                    }
                    else
                    {
                        Log.Error("Error occurred in saving color code information");
                    }

                    //Apply Color Coding
                    var requiredOverlays = new List<GraphicsOverlay>();
                    if (tableName == LayerNames.SegmentGrid)
                    {
                        requiredOverlays.Add(segmentGridFatigueOverlay);
                        requiredOverlays.Add(segmentGridLongitudinalOverlay);
                        requiredOverlays.Add(segmentGridTransversalOverlay);
                        //requiredOverlays.Add(segmentGridOthersOverlay);
                    }
                    else
                    {
                        var matchingOverlay = overlays.FirstOrDefault(o => o.Id == tableName);
                        if (matchingOverlay != null)
                        {
                            requiredOverlays.Add(matchingOverlay);
                        }
                    }

                    if (requiredOverlays.Count > 0)
                    {
                        foreach (var overlay in requiredOverlays)
                        {
                            var processingTasks = new List<Task>();

                            foreach (var colorCode in colorCodes)
                            {
                                var thickness = colorCode.Thickness;
                                var argbColor = GeneralHelper.ConvertHexToColor(colorCode.HexColor);
                                var selectedProperty = colorCode.Property;
                                processingTasks.Add(ApplyColorCodeToGraphics(overlay, colorCode));
                            }

                            // Await all tasks
                            await Task.WhenAll(processingTasks);
                        }
                    }
                }

                if (labelProperty != "No Label")
                {
                    SetLabels(tableName, labelProperty);
                }
                else
                {
                    RemoveLabels(tableName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                GetColorCodeInfo();
                appState.NotifyColorCodeApplied();
            }
        }

        private async Task ApplyColorCodeToGraphics(GraphicsOverlay overlay, ColorCodeInformation colorCode)
        {
            var thickness = colorCode.Thickness;
            var argbColor = GeneralHelper.ConvertHexToColor(colorCode.HexColor);
            var selectedProperty = colorCode.Property;

            foreach (var graphic in overlay.Graphics)
            {
                if (graphic.Attributes.ContainsKey(selectedProperty))
                {
                    var propertyValue = graphic.Attributes[selectedProperty];

                    if (!colorCode.IsStringProperty && propertyValue is IConvertible)
                    {
                        double numericValue = Convert.ToDouble(propertyValue);

                        if (numericValue >= colorCode.MinRange && numericValue <= colorCode.MaxRange)
                        {
                            UpdateGraphicSymbol(graphic, argbColor, thickness);
                        }
                        else if (colorCode.IsAboveFrom && numericValue > colorCode.MinRange)
                        {
                            UpdateGraphicSymbol(graphic, argbColor, thickness);
                        }
                    }
                    else if (colorCode.IsStringProperty && propertyValue.ToString() == colorCode.StringProperty)
                    {
                        UpdateGraphicSymbol(graphic, argbColor, thickness);
                    }
                }
            }
        }

        private void UpdateGraphicSymbol(Graphic graphic, System.Drawing.Color argbColor, double thickness)
        {
            if (graphic.Symbol is SimpleFillSymbol fillSymbol)
            {
                var outlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, argbColor, thickness);
                var newSymbol = new SimpleFillSymbol { Color = argbColor, Style = fillSymbol.Style, Outline = outlineSymbol };
                graphic.Symbol = newSymbol;
            }
            else if (graphic.Symbol is SimpleLineSymbol)
            {
                var newSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, argbColor, thickness);
                graphic.Symbol = newSymbol;
            }
            else if (graphic.Symbol is SimpleMarkerSymbol markerSymbol)
            {
                var newSymbol = new SimpleMarkerSymbol
                {
                    Color = argbColor,
                    Size = markerSymbol.Size, // Keep the existing size
                    Style = markerSymbol.Style, // Keep the existing style
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, argbColor, thickness) // Outline with the given color
                };
                graphic.Symbol = newSymbol;
            }
        }

        public async Task SetColorCodeForCornerBreak(List<ColorCodeInformation> colorCodes)
        {
            try
            {
                var property = colorCodes[0].Property;
                var tableName = colorCodes[0].TableName;
                var deleteResponse = await appEngine.ColorCodeInformationService.DeleteByTableName(tableName);

                await Task.Run(() =>
                {
                    Parallel.ForEach(cornerBreakOverlay.Graphics, graphic =>
                    {
                        double highestValue = Convert.ToDouble(graphic.Attributes["Max" + property]);
                        graphic.Symbol = SymbolFromColorList(highestValue, colorCodes, LayerNames.CornerBreak);
                    });
                });

                var createResponse = await appEngine.ColorCodeInformationService.CreateRange(colorCodes);
                if (createResponse.Id == 1)
                {
                    Log.Information("Color code information is successfully saved into the database");
                }
                else
                {
                    Log.Error("Error occurred in saving color code information");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private object GetPropertyValue(object entity, string propertyName)
        {
            var propInfo = entity.GetType().GetProperty(propertyName);
            return propInfo?.GetValue(entity);
        }
        public Symbol SymbolFromColorList(object value, IEnumerable<ColorCodeInformation> colorCode, string symbolType)
        {
            Symbol symbol = null;

            foreach (var item in colorCode)
            {
                var itemColor = GeneralHelper.ConvertHexToColor(item.HexColor);
                bool isMatch = false;

                if (item.IsStringProperty)
                {
                    var stringValue = value.ToString();
                    if (stringValue == item.StringProperty)
                    {
                        isMatch = true;
                    }
                }
                else
                {
                    var numericValue = Convert.ToDouble(value);
                    if (numericValue >= item.MinRange && numericValue <= item.MaxRange)
                    {
                        isMatch = true;
                    }
                    else if (item.IsAboveFrom && numericValue > item.MinRange)
                    {
                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    if (symbolType == "Line")
                    {
                        symbol = CreateSimpleLineSymbol(itemColor.R, itemColor.G, itemColor.B, itemColor.A, item.Thickness);
                    }
                    else if (symbolType == "Fill" || symbolType == LayerNames.CornerBreak)
                    {
                        symbol = CreateSimpleFillSymbol(itemColor.R, itemColor.G, itemColor.B, itemColor.A, item.Thickness);
                    }
                    else if (symbolType == "FillLine")
                    {
                        symbol = CreateFillLineSymbol(itemColor.R, itemColor.G, itemColor.B, itemColor.A, item.Thickness);
                    }
                    else if (symbolType == "Point")
                    {
                        symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, itemColor, 10);
                    }
                    break;
                }
            }

            return symbol;
        }

        private async Task SaveSymbolDictToDatabase(string tableName, int alpha, int red, int green, int blue, double thickness, string label = null)
        {
            try
            {
                var response = await appEngine.MapGraphicDataService.GetByName(new NameRequest { Name = tableName });
                if (response != null && response.Id > 0)
                {
                    response.Alpha = alpha;
                    response.Red = red;
                    response.Green = green;
                    response.Blue = blue;
                    response.Thickness = thickness;
                    response.LabelProperty = label;

                    var editResponse = await appEngine.MapGraphicDataService.Edit(response);
                    if (editResponse != null && editResponse.Id > 0)
                    {
                        var existing = mapGraphicData.FirstOrDefault(x => x.Id == editResponse.Id);
                        if (existing != null)
                        {
                            existing.Alpha = editResponse.Alpha;
                            existing.Red = editResponse.Red;
                            existing.Green = editResponse.Green;
                            existing.Blue = editResponse.Blue;
                            existing.Thickness = editResponse.Thickness;
                            existing.LabelProperty = editResponse.LabelProperty;
                        }
                    }
                }
                else
                {
                    var request = new MapGraphicData
                    {
                        Name = tableName,
                        Alpha = alpha,
                        Red = red,
                        Green = green,
                        Blue = blue,
                        Thickness = thickness,
                        LabelProperty = label
                    };
                    var createResponse = await appEngine.MapGraphicDataService.Create(request);
                    // Add to in-memory list
                    if (createResponse != null && createResponse.Id > 0)
                    {
                        request.Id = createResponse.Id;
                        mapGraphicData.Add(request);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        #endregion


        private void HandleSurveySet()
        {
            if (!overlays.Contains(surveySetOverlay))
            {
                overlays.Add(surveySetOverlay);
            }
            surveySetOverlay.IsVisible = true;
        }

        private async void DeleteDoubleUpsFromDB(IEnumerable<string> selectedTables, string surveyId, string secondSurveyId)
        {
            try
            {
                var deleteQueries = new List<string>();

                await Task.Run(async () =>
                {
                    var overlappedAreas = await FindSegmentOverlappedArea(surveyId, secondSurveyId);
                    if (!overlappedAreas.Any()) return; // No overlapped areas

                    foreach (var table in selectedTables.Where(x => x != LayerNames.CornerBreak))
                    {
                        var idSet = new ConcurrentBag<string>(); // Thread-safe collection

                        var dbTable = TableNameHelper.GetDBTableName(table);
                        var query = $"SELECT Id, GeoJSON, SurveyId, SegmentId FROM {dbTable} WHERE SurveyId = '{surveyId}' OR SurveyId = '{secondSurveyId}'";
                        var response = await appEngine.SegmentService.ExecuteQueryAndReturnGeoJSON(query);
                        if (response == null || !response.Any()) continue; // Skip if no data

                        // Convert response to graphics
                        var graphicList = response
                            .Select(item =>
                            {
                                var geoJsonObject = JObject.Parse(item.GeoJSON);
                                var geometry = geoJsonObject["geometry"];
                                var type = geometry["type"].ToString();
                                var coordinatesArray = geometry["coordinates"];

                                var attributes = new Dictionary<string, object>
                                {
                                    { "Id", item.Id },
                                    { "SegmentId", item.SegmentId },
                                    { "SurveyId", item.SurveyId }
                                };

                                return ConvertToGraphic(coordinatesArray, type, null, attributes);
                            })
                            .Where(graphic => graphic != null)
                            .ToList();

                        var graphicsBySegmentIdAndSurvey = graphicList
                        .Where(graphic => graphic.Attributes.ContainsKey("SegmentId") && graphic.Attributes.ContainsKey("SurveyId"))
                        .GroupBy(graphic => new { SegmentId = Convert.ToInt32(graphic.Attributes["SegmentId"]), SurveyId = graphic.Attributes["SurveyId"].ToString() })
                        .ToDictionary(group => group.Key, group => group.ToList());


                        Parallel.ForEach(overlappedAreas, area =>
                        {
                            if (graphicsBySegmentIdAndSurvey.TryGetValue(new { SegmentId = area.segmentId, SurveyId = area.surveyId }, out var graphics))
                            {
                                foreach (var graphic in graphics)
                                {
                                    if (GeometryEngine.Contains(area.geometry, graphic.Geometry))
                                    {
                                        if (graphic.Attributes.TryGetValue("Id", out var idObj) && idObj != null)
                                        {
                                            idSet.Add(idObj.ToString());
                                        }
                                    }
                                }
                            }
                        });

                        var uniqueIds = new HashSet<string>(idSet);

                        //Delete the graphics with IdList
                        //if (uniqueIds.Any())
                        //{
                        //    var idString = string.Join(",", uniqueIds.Select(id => $"'{id}'"));
                        //    var deleteQuery = $"DELETE FROM {dbTable} WHERE Id IN ({idString})";
                        //    deleteQueries.Add(deleteQuery);
                        //}
                    }

                    if (selectedTables.Contains(LayerNames.CornerBreak))
                    {
                        //treat corner break differently
                        var cornerBreaks = await FetchCornerBreakForSurveyAsync(surveyId);
                        if (!string.IsNullOrEmpty(secondSurveyId))
                        {
                            var secondSurveyCornerBreaks = await FetchCornerBreakForSurveyAsync(secondSurveyId);
                            if (secondSurveyCornerBreaks != null)
                            {
                                cornerBreaks.AddRange(secondSurveyCornerBreaks);
                            }
                        }

                        if (cornerBreaks != null)
                        {
                            var cornerBreakGraphics = new List<Graphic>();
                            CreateCornerBreakGraphicss(cornerBreaks, cornerBreakGraphics);

                            if (cornerBreakGraphics != null)
                            {
                                var cornerBreakGraphicWithinPolygon = new List<Graphic>();
                                if (overlappedAreas.Count > 0)
                                {
                                    var combinedPolygon = GeometryEngine.Union(overlappedAreas.Select(area => area.geometry)) as Polygon;
                                    if (combinedPolygon != null)
                                    {
                                        cornerBreakGraphics
                                            .AsParallel()
                                            .Where(defect => GeometryEngine.Intersects(combinedPolygon, defect.Geometry))
                                            .ForAll(graphic => cornerBreakGraphicWithinPolygon.Add(graphic));
                                    }
                                }

                                var doubledCornerBreaks = await GetDoubledCornerBreaks(overlappedAreas, cornerBreakGraphicWithinPolygon);
                                if (doubledCornerBreaks.Any())
                                {
                                    var cornerBreakIdSet = new HashSet<string>();

                                    foreach (var graphic in doubledCornerBreaks)
                                    {
                                        if (graphic.Attributes.TryGetValue("Id", out var idObj) && idObj != null)
                                        {
                                            cornerBreakIdSet.Add(idObj.ToString());
                                        }
                                    }

                                    if (cornerBreakIdSet.Any())
                                    {
                                        var idString = string.Join(",", cornerBreakIdSet.Select(id => $"'{id}'"));
                                        var deleteQuery = $"DELETE FROM {TableNameHelper.GetDBTableName(LayerNames.CornerBreak)} WHERE Id IN ({idString})";
                                        deleteQueries.Add(deleteQuery);
                                    }
                                }
                            }
                        }
                    }
                });


                if (deleteQueries.Any())
                {
                    var deleteResponse = await appEngine.SegmentService.ExecuteSQlQueries(deleteQueries);
                    if (deleteResponse.Id != -1)
                    {
                        appState.NotifyProcessingCompletedFromMap();
                    }
                }
                else
                {
                    appState.NotifyDefectsHighlighted(new List<Graphic>(), "No double ups found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<ConcurrentBag<(Geometry geometry, int segmentId, string surveyId)>> FindSegmentOverlappedArea(string surveyId, string secondSurveyId)
        {
            //find overlapped area
            var overlappedAreas = new ConcurrentBag<(Geometry geometry, int segmentId, string surveyId)>();
            if (segmentOverlay != null)
            {
                var segments = segmentOverlay.Graphics.Where(graphic =>
                                graphic.Attributes["SurveyId"].ToString() == surveyId ||
                                (!string.IsNullOrEmpty(secondSurveyId) && graphic.Attributes["SurveyId"].ToString() == secondSurveyId))
                                .ToList();

                // Iterate through each pair of segments to find intersections
                Parallel.For(0, segments.Count - 1, i =>
                {
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        // Get the geometries of the segments
                        var geometry1 = segments[i].Geometry;
                        var geometry2 = segments[j].Geometry;

                        // Check if the geometries intersect
                        var intersection = GeometryEngine.Intersection(geometry1, geometry2);
                        if (intersection != null && !intersection.IsEmpty)
                        {
                            var segmentId1 = Convert.ToInt32(segments[i].Attributes["SegmentId"]);
                            var segmentId2 = Convert.ToInt32(segments[j].Attributes["SegmentId"]);

                            var survey1 = segments[i].Attributes["SurveyId"].ToString();
                            var survey2 = segments[j].Attributes["SurveyId"].ToString();

                            // If survey1 and survey2 are the same, use the larger segmentId and the common surveyId
                            if (survey1 == survey2)
                            {
                                var largerSegmentId = Math.Max(segmentId1, segmentId2);
                                overlappedAreas.Add((intersection, largerSegmentId, survey1));
                            }
                            else
                            {
                                // If the surveyIds are different, get second survey
                                if (survey1 == secondSurveyId)
                                {
                                    overlappedAreas.Add((intersection, segmentId1, secondSurveyId));
                                }
                                else if (survey2 == secondSurveyId)
                                {
                                    overlappedAreas.Add((intersection, segmentId2, secondSurveyId));
                                }
                            }
                        }
                    }
                });
            }
            return overlappedAreas;
        }

        private async Task<List<Graphic>> GetDoubledCornerBreaks(ConcurrentBag<(Geometry geometry, int segmentId, string surveyId)> overlappedAreas, List<Graphic> cornerBreakGraphics)
        {
            var doubledCornerBreaks = new ConcurrentBag<Graphic>();
            var tasks = new List<Task>();

            foreach (var defect1 in cornerBreakGraphics)
            {
                tasks.Add(Task.Run(() =>
                {
                    var bufferGeometry = GeometryEngine.BufferGeodetic(defect1.Geometry, 0.5, LinearUnits.Meters);

                    foreach (var defect2 in cornerBreakGraphics)
                    {
                        if (defect1 != defect2 && (GeometryEngine.Overlaps(defect1.Geometry, defect2.Geometry) || GeometryEngine.Overlaps(bufferGeometry, defect2.Geometry)))
                        {
                            if (defect1.Attributes.TryGetValue("MaxBreakArea_mm2", out var breakArea1) &&
                                defect2.Attributes.TryGetValue("MaxBreakArea_mm2", out var breakArea2))
                            {
                                double breakArea1Value = Convert.ToDouble(breakArea1);
                                double breakArea2Value = Convert.ToDouble(breakArea2);

                                lock (doubledCornerBreaks)
                                {
                                    if (breakArea1Value < breakArea2Value)
                                    {
                                        doubledCornerBreaks.Add(defect1);
                                    }
                                    else if (breakArea1Value > breakArea2Value)
                                    {
                                        doubledCornerBreaks.Add(defect2);
                                    }
                                    else
                                    {
                                        if (!doubledCornerBreaks.Contains(defect1) && !doubledCornerBreaks.Contains(defect2))
                                        {
                                            //Remove the graphic from latter segment if its breakArea is equal
                                            defect1.Attributes.TryGetValue("SegmentId", out var defect1SegmentId);
                                            defect2.Attributes.TryGetValue("SegmentId", out var defect2SegmentId);
                                            int segmentId1 = Convert.ToInt32(defect1SegmentId);
                                            int segmentId2 = Convert.ToInt32(defect2SegmentId);

                                            defect1.Attributes.TryGetValue("SurveyId", out var surveyId1);
                                            defect2.Attributes.TryGetValue("SurveyId", out var surveyId2);
                                            string survey1 = surveyId1?.ToString();
                                            string survey2 = surveyId2?.ToString();

                                            if (survey1 == survey2)
                                            {
                                                // If both surveys are the same, remove the graphic from the latter segment
                                                if (segmentId1 > segmentId2)
                                                {
                                                    doubledCornerBreaks.Add(defect1);
                                                }
                                                else
                                                {
                                                    doubledCornerBreaks.Add(defect2);
                                                }
                                            }
                                            else
                                            {
                                                // If SurveyId is different, always remove the second survey's graphic
                                                doubledCornerBreaks.Add(defect2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);
            return doubledCornerBreaks.ToList();
        }

        private async void HighlightDoubledUpDefects(IEnumerable<string> selectedTables, string surveyId, string secondSurveyId)
        {
            try
            {
                //use Task to update UI
                await Task.Run(async () =>
                {
                    //Get Selected Overlays from selectedTables
                    var selectedOverlays = selectedTables
                        .Select(tableName => overlays.FirstOrDefault(o => o.Id == tableName))
                        .Where(overlay => overlay != null && overlay.Id != LayerNames.CornerBreak)
                        .ToList();

                    var overlappedAreas = await FindSegmentOverlappedArea(surveyId, secondSurveyId);

                    var graphicsBySegmentIdAndSurvey = selectedOverlays
                        .SelectMany(overlay => overlay.Graphics)
                        .Where(graphic => graphic.Attributes.TryGetValue("SegmentId", out _) && graphic.Attributes.TryGetValue("SurveyId", out _))
                        .GroupBy(graphic => new { SegmentId = Convert.ToInt32(graphic.Attributes["SegmentId"]), SurveyId = graphic.Attributes["SurveyId"].ToString() })
                        .ToDictionary(group => group.Key, group => group.ToList());

                    //get all the graphics and highlight the defects inside overlapped area
                    var graphicsToRemove = new ConcurrentBag<Graphic>();

                    Parallel.ForEach(overlappedAreas, area =>
                    {
                        if (graphicsBySegmentIdAndSurvey.TryGetValue(new { SegmentId = area.segmentId, SurveyId = area.surveyId }, out var graphics))
                        {
                            foreach (var graphic in graphics)
                            {
                                if (graphic.Attributes.TryGetValue("GeoType", out var geoType))
                                {
                                    if (geoType.ToString() == "Point")
                                    {
                                        var centerPoint = GeometryEngine.LabelPoint(graphic.Geometry as Polygon) as MapPoint;
                                        if (GeometryEngine.Contains(area.geometry, centerPoint))
                                        {
                                            graphic.IsSelected = true;
                                            graphicsToRemove.Add(graphic);
                                        }
                                    }
                                }
                                else
                                {
                                    if (GeometryEngine.Contains(area.geometry, graphic.Geometry))
                                    {
                                        graphicsToRemove.Add(graphic);
                                        graphic.IsSelected = true;
                                    }
                                }
                            }
                        }
                    });

                    if (selectedTables.Contains(LayerNames.CornerBreak))
                    {
                        //Get ALL Corner Breaks within the overlapped Area
                        var cornerBreakGraphics = new List<Graphic>();

                        if (overlappedAreas.Count > 0)
                        {
                            var combinedPolygon = GeometryEngine.Union(overlappedAreas.Select(area => area.geometry)) as Polygon;
                            if (combinedPolygon != null)
                            {
                                cornerBreakOverlay.Graphics
                                    .AsParallel()
                                    .Where(defect => GeometryEngine.Intersects(combinedPolygon, defect.Geometry))
                                    .ForAll(graphic => cornerBreakGraphics.Add(graphic));
                            }
                        }

                        if (cornerBreakGraphics.Any())
                        {
                            var doubledCornerBreaks = await GetDoubledCornerBreaks(overlappedAreas, cornerBreakGraphics);
                            if (doubledCornerBreaks.Any())
                            {
                                foreach (var graphic in doubledCornerBreaks)
                                {
                                    graphic.IsSelected = true;
                                    graphicsToRemove.Add(graphic);
                                }
                            }
                        }
                    }

                    if (graphicsToRemove.Count > 0)
                    {
                        appState.NotifyDefectsHighlighted(graphicsToRemove.ToList(), null);
                        appState.IsMultiSelected = true;
                    }
                    else
                    {
                        appState.NotifyDefectsHighlighted(new List<Graphic>(), "No double ups found.");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in highlighting double ups : " + ex.Message);
            }
        }

        public void RevertGraphicsToRemove()
        {
            if (appState.graphicsToRemove.Any())
            {
                foreach (var graphic in appState.graphicsToRemove)
                {
                    graphic.IsSelected = false;
                }
            }
            appState.graphicsToRemove.Clear();
            appState.IsMultiSelected = false;
        }

        public void DrawBoundariesOnMap(List<MapPoint> boundaryCoordinates)
        {
            //Remove before drawing a new temp boundary
            RemoveTempBoundary();

            //Draw a new Boundary on map
            var boundaryPolygon = new Polygon(new List<MapPoint>(boundaryCoordinates), SpatialReferences.Wgs84);
            var boundarySymbol = GetGraphicSymbol("Boundaries", GeoType.Polygon.ToString());
            if (boundarySymbol == null)
            {
                Log.Error("No boundary symbol found. can not show boundary on the map.");
                return;
            }

            if (boundarySymbol is SimpleFillSymbol fillSymbol)
            {
                boundarySymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Cross, fillSymbol.Color, fillSymbol.Outline);
            }
            var boundaryGraphic = new Graphic(boundaryPolygon, boundarySymbol);
            var attributes = new Dictionary<string, object>
            {
                { "IsTemp", true },
                { "Mode", BoundariesMode.Include.ToString() },
                { "Table", "Boundary" }
            };

            foreach (var attribute in attributes)
            {
                boundaryGraphic.Attributes.Add(attribute.Key, attribute.Value);
            }

            var tempOverlay = new GraphicsOverlay { Id = "tempOverlay" };
            tempOverlay.Graphics.Add(boundaryGraphic);

            if (!overlays.Contains(tempOverlay))
            {
                overlays.Add(tempOverlay);
            }
        }

        private void RemoveTempBoundary()
        {
            var boundaryOverlay = overlays.FirstOrDefault(o => o.Id == "tempOverlay");
            if (boundaryOverlay != null)
            {
                // Find the boundary graphic using the identifier
                var boundaryGraphic = boundaryOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("IsTemp") && (bool)g.Attributes["IsTemp"]);
                if (boundaryGraphic != null)
                {
                    boundaryOverlay.Graphics.Remove(boundaryGraphic);
                }
                overlays.Remove(boundaryOverlay);
            }
        }

        private void HighlightDefectsOutsideBoundary(IEnumerable<string> selectedTables, string surveyId)
        {
            List<Graphic> graphicsToRemove = new List<Graphic>();
            string message = string.Empty;

            var boundaryTempOverlay = overlays.FirstOrDefault(o => o.Id == "tempOverlay");
            if (boundaryTempOverlay != null)
            {
                if (!boundaryTempOverlay.IsVisible) boundaryTempOverlay.IsVisible = true;

                var boundaryGraphic = boundaryTempOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("IsTemp") && (bool)g.Attributes["IsTemp"]);
                if (boundaryGraphic != null && boundaryGraphic.Geometry is Polygon boundaryPolygon)
                {
                    if (!boundaryGraphic.IsVisible) boundaryGraphic.IsVisible = true;

                    var lcmsOverlays = overlays.Where(x => selectedTables.Contains(x.Id));
                    foreach (var overlay in lcmsOverlays)
                    {
                        if (overlay.Id == "Segment")
                        {
                            continue;
                        }
                        foreach (var graphic in overlay.Graphics)
                        {
                            var survey = graphic.Attributes["SurveyId"]?.ToString();
                            if (survey != null && survey == surveyId)
                            {
                                var geometry = graphic.Geometry;
                                if (geometry != null && IsOutsideBoundary(geometry, boundaryPolygon))
                                {
                                    graphic.IsSelected = true;
                                    graphicsToRemove.Add(graphic);
                                }
                            }
                        }
                    }
                    if (graphicsToRemove.Count == 0)
                    {
                        message = "No defect found outside of the boundary.";
                    }
                    else
                    {
                        appState.IsMultiSelected = true;
                    }
                }
                else
                {
                    message = "Boundary not found. Please check it again.";
                }
            }
            else
            {
                message = "Boundary not found. Please check it again.";
            }
            appState.NotifyDefectsHighlighted(graphicsToRemove, message);
        }
        private async void RemoveDefectsOutsideBoundary(string surveyId, string surveyName, IEnumerable<string> selectedTables)
        {
            string message = null;

            var boundaryTempOverlay = overlays.FirstOrDefault(o => o.Id == "tempOverlay");
            if (boundaryTempOverlay != null)
            {
                var boundaryGraphic = boundaryTempOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("IsTemp") && (bool)g.Attributes["IsTemp"]);
                if (boundaryGraphic != null && boundaryGraphic.Geometry is Polygon boundaryPolygon)
                {
                    var deleteQueries = new List<string>();

                    if (segmentOverlay != null && segmentOverlay.Graphics.Count > 0)
                    {
                        //Get all the segments that are outside of the boundary
                        var segmentIdSet = new HashSet<string>();

                        foreach (var graphic in segmentOverlay.Graphics)
                        {
                            if (GeometryEngine.Contains(boundaryPolygon, graphic.Geometry) &&
                                graphic.Attributes.TryGetValue("SegmentId", out var segmentId))
                            {
                                segmentIdSet.Add(segmentId.ToString());
                            }
                        }

                        var segmentIdListString = string.Join(", ", segmentIdSet);

                        foreach (var selectedTable in selectedTables.Where(x => x != LayerNames.PASER && x != LayerNames.PCI))
                        {
                            var dbTable = TableNameHelper.GetDBTableName(selectedTable);
                            //Get all the defects within the segment that are outside of the boundary
                            var query = $"SELECT Id, GeoJSON FROM {dbTable} WHERE SurveyId = '{surveyId}' AND SegmentId NOT IN ({segmentIdListString}) ";

                            var tableResponse = await appEngine.SegmentService.ExecuteQueryAndReturnGeoJSON(query);
                            if (tableResponse == null || !tableResponse.Any()) continue; // Skip if no data

                            var idSet = new HashSet<int>();

                            foreach (var item in tableResponse)
                            {
                                var geoJsonObject = JObject.Parse(item.GeoJSON);
                                var geometry = geoJsonObject["geometry"];
                                var type = geometry["type"].ToString();
                                var coordinatesArray = geometry["coordinates"];

                                var graphic = ConvertToGraphic(coordinatesArray, type, null, new Dictionary<string, object> { { "Id", item.Id } });

                                if (graphic != null)
                                {
                                    // Check if graphic is outside the boundary
                                    if (IsOutsideBoundary(graphic.Geometry, boundaryPolygon))
                                    {
                                        idSet.Add(item.Id);
                                    }
                                }
                            }

                            //Delete the graphics with IdList
                            if (idSet.Any())
                            {
                                var idString = string.Join(",", idSet.Select(id => $"'{id}'"));
                                var deleteQuery = $"DELETE FROM {dbTable} WHERE Id IN ({idString})";
                                deleteQueries.Add(deleteQuery);
                            }
                        }

                        if (selectedTables.Contains(LayerNames.PASER))
                        {
                            //Update paser value in segment table and delete from paser table
                            var updateQuery = $"UPDATE LCMS_Segment SET PASER = -1.0 WHERE SurveyId = '{surveyId}' AND SegmentId NOT IN ({segmentIdListString})";
                            deleteQueries.Add(updateQuery);

                            var deleteQuery = $"DELETE FROM LCMS_PASER WHERE SurveyId = '{surveyId}' AND SegmentId NOT IN ({segmentIdListString})";
                            deleteQueries.Add(deleteQuery);

                        }

                        if (selectedTables.Contains(LayerNames.PCI))
                        {
                            //Update pci value in segment table and delete from pci table
                            var updateQuery = $"UPDATE LCMS_Segment SET PCI = 0.0 WHERE SurveyId = '{surveyId}' AND SegmentId NOT IN ({segmentIdListString})";
                            deleteQueries.Add(updateQuery);

                            var deleteQuery = $"DELETE FROM LCMS_PCI WHERE SurveyId = '{surveyId}' AND SegmentId NOT IN ({segmentIdListString})";
                            deleteQueries.Add(deleteQuery);
                        }
                    }

                    if (deleteQueries.Count == 0)
                    {
                        message = "No defect found outside of the boundary.";
                    }
                    else
                    {
                        //delete records from db with queries
                        var deleteResponse = await appEngine.SegmentService.ExecuteSQlQueries(deleteQueries);

                        if (deleteResponse.Id != -1) //if sql queries successful save temp boundary to the db
                        {
                            await SaveTempBoundary(boundaryTempOverlay, boundaryGraphic, surveyId, surveyName);
                            appState.NotifyProcessingCompletedFromMap();
                            appState.UpdateTableNames(true);
                        }
                        else
                        {
                            message = "There was an error in deleting the defects outside of boundary";
                        }
                    }
                }
                else
                {
                    message = "Boundary not found. Please check it again.";
                }
            }
            else
            {
                message = "Boundary not found. Please check it again.";
            }

            if (message != null)
            {
                appState.NotifyDefectsHighlighted(new List<Graphic>(), message);
            }
        }

        public async Task SaveTempBoundary(GraphicsOverlay boundaryOverlay, Graphic boundaryGraphic, string surveyId, string surveyName)
        {
            if (boundaryGraphic.Geometry is Polygon polygon)
            {
                var coordinateList = new List<double[]>();
                foreach (var part in polygon.Parts)
                {
                    foreach (var point in part.Points)
                    {
                        coordinateList.Add(new[] { point.X, point.Y });
                    }
                }
                var coordinateJson = Newtonsoft.Json.JsonConvert.SerializeObject(coordinateList);
                var newBoundary = new Core.Models.Other.Boundary
                {
                    SurveyId = surveyId,
                    SurveyName = surveyName,
                    BoundaryName = $"{surveyName}_{DateTime.UtcNow:HHmmss}", //Unique name
                    Coordinates = coordinateJson,
                    BoundariesMode = BoundariesMode.Include.ToString()
                };
                var response = await appEngine.BoundariesService.Create(newBoundary);
            }
            if (overlays.Contains(boundaryOverlay))
            {
                overlays.Remove(boundaryOverlay);
            }
        }

        private bool IsOutsideBoundary(Esri.ArcGISRuntime.Geometry.Geometry geometry, Polygon boundaryPolygon)
        {
            // Handle point geometries
            if (geometry is MapPoint point)
            {
                return !GeometryEngine.Contains(boundaryPolygon, point);
            }

            // Handle polyline geometries
            if (geometry is Polyline polyline)
            {
                foreach (var part in polyline.Parts)
                {
                    foreach (var pt in part.Points)
                    {
                        if (GeometryEngine.Contains(boundaryPolygon, pt))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // Handle polygon geometries
            if (geometry is Polygon polygon)
            {
                foreach (var part in polygon.Parts)
                {
                    foreach (var pt in part.Points)
                    {
                        if (GeometryEngine.Contains(boundaryPolygon, pt))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // If the geometry type is not handled, return false
            return false;
        }
        private async void CreateSampleUnitSummaries(List<Survey> surveyIds, List<SampleUnit> sampleUnits, string summaryName, List<SummaryItem> summaryItems, int sampleUnitSetId)
        {
            try
            {
                var errors = new List<string>();
                int completed = 0;
                int totalOperations = surveyIds.Count() * sampleUnits.Count;
                int progress = 0;
                double currentChainage = 0;
                foreach (var survey in surveyIds)
                {
                    var summaryRequests = new List<SummaryRequest>();

                    foreach (var sampleUnit in sampleUnits)
                    {
                        var summaryRequest = new SummaryRequest
                        {
                            SummaryName = summaryName,
                            CoordinateString = sampleUnit.Coordinates,
                            SelectedSurvey = survey.SurveyIdExternal,
                            SampleUnitId = sampleUnit.Id,
                            SampleUnitSetId = sampleUnitSetId,
                            SummaryItems = summaryItems
                        };

                        var coordinates = JsonSerializer.Deserialize<List<double[]>>(sampleUnit.Coordinates);
                        Polygon sampleUnitPolygon = CreatePolygon(coordinates);
                        var points = sampleUnitPolygon.Parts.SelectMany(part => part.Points).ToList();
                        var minimumYPoint = points.OrderBy(p => p.Y).First();  // Point with minimum Y
                        var maximumYPoint = points.OrderByDescending(p => p.Y).First();  // Point with maximum Y

                        var nearestSegmentToMinimumY = GeneralMapHelper.GetClosestGraphicFromOverlay(segmentOverlay, minimumYPoint, 5, graphic =>
                        {
                            return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                                   surveyName?.ToString() == survey.SurveyIdExternal;
                        });

                        var nearestSegmentToMaximumY = GeneralMapHelper.GetClosestGraphicFromOverlay(segmentOverlay, maximumYPoint, 5, graphic =>
                        {
                            return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                                   surveyName?.ToString() == survey.SurveyIdExternal;
                        });

                        if (nearestSegmentToMinimumY != null && nearestSegmentToMaximumY != null &&
                            nearestSegmentToMinimumY.Attributes.TryGetValue("SegmentId", out var minimumSegmentIdObj) && nearestSegmentToMaximumY.Attributes.TryGetValue("SegmentId", out var maximumSegmentIdObj))
                        {
                            int minimumSegmentId = Convert.ToInt32(minimumSegmentIdObj);
                            int maximumSegmentId = Convert.ToInt32(maximumSegmentIdObj);

                            var firstchainageRequest = new ChainageMapPointRequest
                            {
                                Latitude = minimumYPoint.Y,
                                Longitude = minimumYPoint.X,
                                SegmentId = minimumSegmentId,
                                SurveyId = survey.SurveyIdExternal
                            };
                            var firstChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(firstchainageRequest);

                            var secondChainageRequest = new ChainageMapPointRequest
                            {
                                Latitude = maximumYPoint.Y,
                                Longitude = maximumYPoint.X,
                                SegmentId = maximumSegmentId,
                                SurveyId = survey.SurveyIdExternal
                            };
                            var secondChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(secondChainageRequest);

                            if (firstChainageResponse != null && secondChainageResponse != null)
                            {
                                double chainage1 = firstChainageResponse.Chainage;
                                double chainage2 = secondChainageResponse.Chainage;

                                if (chainage1 != -1 && chainage2 != -1)
                                {
                                    var chainageStart = Math.Min(chainage1, chainage2);
                                    var chainageEnd = Math.Max(chainage1, chainage2);

                                    summaryRequest.ChainageStart = Math.Round(chainageStart, 2);
                                    summaryRequest.ChainageEnd = Math.Round(chainageEnd, 2);
                                }
                            }
                        }

                        var response = await appEngine.SegmentService.GetNumericValueWithinBoundary(summaryRequest);
                        if (response != null && response.Count > 0)
                        {
                            summaryRequest.SummaryItems = response;
                            summaryRequests.Add(summaryRequest);
                        }

                        completed++;
                        progress = (int)((completed / (double)totalOperations) * 100);
                        if (progress < 100) // only send < 100 here
                            appState.NotifyIntervalSummaryProcessed(progress);
                    }

                    if (summaryRequests.Count > 0)
                    {
                        bool hasSummaryItems = summaryRequests
                            .SelectMany(sr => sr.SummaryItems)
                            .Any(item => item.NumericValue > 0);

                        if (!hasSummaryItems)
                        {
                            errors.Add($"Failed to save summary for survey '{survey.SurveyName}' because either no defects were found within the sample unit area, or the survey does not fall within that area.");
                            continue; // Move on to the next survey
                        }

                        // Now save all valid requests for this survey
                        foreach (var request in summaryRequests)
                        {
                            var summaryResponse = await appEngine.SummaryService.CreateSummary(request);
                            if (summaryResponse.Id != 0)
                            {
                                errors.Add($"Failed to save summary for Sample Unit with ID: {request.SampleUnitId}");
                            }
                        }
                    }
                }

                string combinedErrors = null;
                if (errors.Count > 0)
                {
                    combinedErrors = string.Join("\n", errors);
                }
                // All done successfully
                appState.NotifyIntervalSummaryProcessed(100, combinedErrors);
            }
            catch (Exception ex)
            {
                appState.NotifyIntervalSummaryProcessed(-1, "Something went wrong. Please check the logs.");
                Log.Error("Error in CreateSegmentIntervalSummaries" + ex.Message);
            }
        }

        private async void CreateIntervalSummaries(List<Survey> surveys, int interval, string summaryName, List<SummaryItem> summaryItems)
        {
            try
            {
                var surveyPolygonsMap = new Dictionary<string, List<(double chainageStart, double chainageEnd, Polygon summaryPolygon)>>();
                foreach (var survey in surveys)
                {
                    int accumulatedHeight = 0;
                    //var resultPolygon = new List<Polygon>();
                    var currentGroup = new List<Polygon>();
                    var spatialRef = SpatialReferences.Wgs84;
                    int remainingToFill = interval;
                    int? previousSegmentId = null; // Track the previous segment ID
                    double currentChainage = 0;
                    double endChainage = 0;
                    var sameSurveySegmentGraphics = segmentOverlay.Graphics
                        .Where(x => x.Attributes.ContainsKey("SurveyId") && x.Attributes["SurveyId"]?.ToString() == survey.SurveyIdExternal)
                        .ToList();
                    var resultPolygonWithChainage = new List<(double chainage, double chainageEnd, Polygon summaryPolygon)>();

                    if (sameSurveySegmentGraphics != null && sameSurveySegmentGraphics.Any())
                    {
                        var orderedGraphic = sameSurveySegmentGraphics.OrderBy(g => Convert.ToInt32(g.Attributes["SegmentId"]));
                        if (orderedGraphic == null) continue;

                        // Record the start chainage
                        if (orderedGraphic.First().Attributes.TryGetValue("Chainage", out var baseChRaw))
                            currentChainage = Convert.ToDouble(baseChRaw);

                        foreach (var graphic in orderedGraphic)
                        {
                            int currentSegmentId = Convert.ToInt32(graphic.Attributes["SegmentId"]);
                            if (previousSegmentId.HasValue && currentSegmentId - previousSegmentId > 2)
                            {
                                // More than 2 segments are missing, reset the interval
                                if (currentGroup.Count > 0)
                                {
                                    FinalizeCurrentGroup(currentGroup, ref currentChainage, accumulatedHeight, resultPolygonWithChainage);
                                }

                                // Update currentChainage using the first chainage of the next present segment
                                if (graphic.Attributes.TryGetValue("Chainage", out var nextSegmentChainage))
                                {
                                    currentChainage = Convert.ToDouble(nextSegmentChainage);
                                }
                                accumulatedHeight = 0;
                                remainingToFill = interval;
                            }
                            previousSegmentId = currentSegmentId; // Update the previous Segment ID

                            double height = Convert.ToDouble(graphic.Attributes["Height"]);
                            int heightInMeters = (int)(height / 1000);
                            var polygon = graphic.Geometry as Polygon;

                            if (heightInMeters < remainingToFill)
                            {
                                currentGroup.Add(polygon);
                                accumulatedHeight += heightInMeters;
                                remainingToFill -= heightInMeters;
                            }
                            else
                            {
                                var chunks = SplitPolygonIntoIntervalChunks(polygon, remainingToFill, heightInMeters, interval);

                                if (interval > heightInMeters)
                                {
                                    var used = chunks[0];
                                    currentGroup.Add(used);

                                    //ConvexHull does not work well with curved road
                                    //var unioned = GeometryEngine.Union(currentGroup);
                                    //var convexHull = GeometryEngine.ConvexHull(unioned) as Polygon;
                                    //resultPolygon.Add(convexHull);

                                    FinalizeCurrentGroup(currentGroup, ref currentChainage, accumulatedHeight + remainingToFill, resultPolygonWithChainage);
                                    if (chunks.Count > 1)
                                    {
                                        // Save leftover for next round
                                        var leftover = chunks[1];
                                        currentGroup.Add(leftover);
                                        accumulatedHeight = heightInMeters - remainingToFill;
                                        remainingToFill = interval - accumulatedHeight;
                                    }
                                    else
                                    {
                                        accumulatedHeight = 0;
                                        remainingToFill = interval;
                                    }
                                }
                                else
                                {
                                    int currentHeight = 0;
                                    foreach (var chunk in chunks)
                                    {
                                        currentGroup.Add(chunk);
                                        bool isLastChunk = chunk == chunks.Last();

                                        if (isLastChunk)
                                        {
                                            accumulatedHeight += heightInMeters - currentHeight;
                                        }
                                        else
                                        {
                                            accumulatedHeight += remainingToFill;
                                            currentHeight += remainingToFill;
                                        }

                                        if (accumulatedHeight >= interval)
                                        {
                                            FinalizeCurrentGroup(currentGroup, ref currentChainage, accumulatedHeight, resultPolygonWithChainage);

                                            accumulatedHeight = 0;
                                            remainingToFill = interval;
                                        }
                                        else
                                        {
                                            remainingToFill = interval - accumulatedHeight;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        appState.NotifyIntervalSummaryProcessed(-1, "No segments are found. Check if it contains segment in the survey you selected");
                        return;
                    }

                    if (currentGroup.Count > 0)
                    {
                        FinalizeCurrentGroup(currentGroup, ref currentChainage, accumulatedHeight, resultPolygonWithChainage);
                    }

                    //add resultPolygon into dictionary
                    surveyPolygonsMap[survey.SurveyIdExternal] = resultPolygonWithChainage;
                }

                int totalWork = surveyPolygonsMap.Values.Sum(list => list.Count);
                if (totalWork == 0)
                {
                    appState.NotifyIntervalSummaryProcessed(-1, "No interval polygons were generated.");
                    return;
                }
                int processed = 0;

                if (surveyPolygonsMap.Count > 0)
                {
                    foreach (var survey in surveys)
                    {
                        var key = survey.SurveyIdExternal;
                        if (!surveyPolygonsMap.TryGetValue(key, out var polygonsForSurvey) || polygonsForSurvey.Count == 0)
                            continue;

                        var sampleUnitSet = await appEngine.SampleUnitSetService.GetOrCreate(new SampleUnit_Set
                        {
                            Name = $"{survey}_{interval}m",
                            Description = "Auto-generated interval summary set",
                            Type = SampleUnitSetType.Interval
                        });
                        if (sampleUnitSet == null || sampleUnitSet.Id <= 0)
                            continue;
                        
                        int sampleUnitSetId = sampleUnitSet.Id;
                        int total = polygonsForSurvey.Count;
                        int batchSize = 10;
                        if (total > 300) batchSize = 50;
                        else if (total > 100) batchSize = 30;
                        else if (total > 50) batchSize = 15;

                        for (int i = 0; i < total; i += batchSize)
                        {
                            var batch = polygonsForSurvey.Skip(i).Take(batchSize).ToList();
                            var batchRequests = new List<SummaryRequest>();
                            foreach (var (chainageStart, chainageEnd, polygon) in batch)
                            {
                                var coordinatesArray = polygon.Parts
                                  .SelectMany(part => part.Points)
                                  .Select(point => new double[] { point.X, point.Y })
                                  .ToArray();
                                string jsonString = JsonSerializer.Serialize(coordinatesArray);
                                var sampleUnit = await appEngine.SampleUnitService.GetOrCreate(new SampleUnit
                                {
                                    Name = $"{chainageStart}m - {chainageEnd}m",
                                    SampleUnitSetId = sampleUnitSetId,
                                    Coordinates = jsonString,
                                });

                                if (sampleUnit != null && sampleUnit.Id > 0)
                                {
                                    var sampleUnitId = sampleUnit.Id;
                                    var summaryRequest = new SummaryRequest
                                    {
                                        SummaryItems = summaryItems,
                                        SelectedSurvey = survey.SurveyIdExternal,
                                        SummaryName = summaryName,
                                        SampleUnitId = sampleUnitId,
                                        SampleUnitSetId = sampleUnitSetId,
                                        ChainageStart = chainageStart,
                                        ChainageEnd = chainageEnd,
                                        CoordinateString = jsonString
                                    };

                                    batchRequests.Add(summaryRequest);

                                    // increment processed for this one polygon created
                                    processed++;
                                }
                            }

                            var response = await appEngine.SummaryService.BatchCreateSummaries(batchRequests);
                            if (response == null || response.Id == -1)
                            {
                                appState.NotifyIntervalSummaryProcessed(-1, "Error when creating summaries");
                                return;
                            }
                            // Report overall progress
                            int progress = (int)((processed / (double)totalWork) * 100);
                            if (progress < 100)
                                appState.NotifyIntervalSummaryProcessed(progress);
                        }

                    }

                    // All done successfully
                    appState.NotifyIntervalSummaryProcessed(100);
                }
            }
            catch (Exception ex)
            {
                appState.NotifyIntervalSummaryProcessed(-1, "Something went wrong. Please check the logs.");
                Log.Error("Error in CreateSegmentIntervalSummaries" + ex.Message);
            }
        }

        private void FinalizeCurrentGroup(List<Polygon> currentGroup, ref double currentChainage, int accumulatedHeight, List<(double chainageStart, double chainageEnd, Polygon summaryPolygon)> resultPolygonWithChainage)
        {
            var mergedPolygon = GetMergedPoints(currentGroup);
            double endChainage = currentChainage + accumulatedHeight;
            resultPolygonWithChainage.Add((currentChainage, endChainage, mergedPolygon));
            currentChainage = endChainage;
            currentGroup.Clear();
        }

        private Polygon GetMergedPoints(List<Polygon> currentGroup)
        {
            List<MapPoint> mergedPoints = new List<MapPoint>();

            if (currentGroup.Count == 1)
            {
                return currentGroup.FirstOrDefault();
            }

            var lastPolygon = currentGroup.Last();
            foreach (var polygon in currentGroup)
            {
                foreach (var part in polygon.Parts)
                {
                    var insertAt = mergedPoints.Count / 2;
                    var insertPoints = new List<MapPoint>();
                    if (polygon == lastPolygon)
                    {
                        //add top left, top right coordinates for last polygon
                        insertPoints.Add(part.Points[1]);
                        insertPoints.Add(part.Points[2]);
                    }
                    else
                    {
                        //rest polygons add bottom left, bottom right
                        insertPoints.Add(part.Points.First());
                        insertPoints.Add(part.Points.Last());
                    }
                    mergedPoints.InsertRange(insertAt, insertPoints);
                }
            }

            return new Polygon(mergedPoints);
        }

        private List<Polygon> SplitPolygonIntoIntervalChunks(Polygon polygon, double metersToUse, double segmentHeightMeters, double interval)
        {
            //If remaining meter is 0, no need to split the polygon
            if (metersToUse == 0 || segmentHeightMeters == metersToUse)
            {
                return new List<Polygon> { polygon };
            }

            var chunks = new List<Polygon>();
            var sr = polygon.SpatialReference;

            var coords = new List<MapPoint>();
            foreach (var part in polygon.Parts)
            {
                coords.AddRange(part.Points);
            }

            if (coords.Count < 4)
                throw new ArgumentException("Polygon must be a rectangle");

            // Expected order: BottomLeft, TopLeft, TopRight, BottomRight
            var bottomLeft = coords[0];
            var topLeft = coords[1];
            var topRight = coords[2];
            var bottomRight = coords[3];

            // Vector representing height direction (from bottom to top)
            double dxLeft = topLeft.X - bottomLeft.X;
            double dyLeft = topLeft.Y - bottomLeft.Y;

            double currentHeight = 0;

            // Proportion of how much height to take
            double proportion = metersToUse / segmentHeightMeters;

            // Get new points for split height
            var usedTopLeft = new MapPoint(bottomLeft.X + dxLeft * proportion, bottomLeft.Y + dyLeft * proportion, sr);
            var usedTopRight = new MapPoint(bottomRight.X + dxLeft * proportion, bottomRight.Y + dyLeft * proportion, sr);

            // Create the used portion polygon (bottom slice)
            var used = new Polygon(new Esri.ArcGISRuntime.Geometry.PointCollection(sr)
            {
                bottomLeft, usedTopLeft, usedTopRight, bottomRight
            });
            chunks.Add(used);

            // Update bottomLeft/bottomRight for leftover
            bottomLeft = usedTopLeft;
            bottomRight = usedTopRight;
            currentHeight += metersToUse;

            while (currentHeight + interval <= segmentHeightMeters)
            {
                double startProp = currentHeight / segmentHeightMeters;
                double endProp = (currentHeight + interval) / segmentHeightMeters;

                var p1 = new MapPoint(bottomLeft.X + dxLeft * 0, bottomLeft.Y + dyLeft * 0, sr);
                var p2 = new MapPoint(bottomRight.X + dxLeft * 0, bottomRight.Y + dyLeft * 0, sr);
                var p3 = new MapPoint(bottomRight.X + dxLeft * (endProp - startProp), bottomRight.Y + dyLeft * (endProp - startProp), sr);
                var p4 = new MapPoint(bottomLeft.X + dxLeft * (endProp - startProp), bottomLeft.Y + dyLeft * (endProp - startProp), sr);

                var chunk = new Polygon(new Esri.ArcGISRuntime.Geometry.PointCollection(sr) { p1, p2, p3, p4 });
                chunks.Add(chunk);

                // Move bottom up
                bottomLeft = p4;
                bottomRight = p3;
                currentHeight += interval;
            }

            if (currentHeight < segmentHeightMeters)
            {
                var leftover = new Polygon(new Esri.ArcGISRuntime.Geometry.PointCollection(sr)
                {
                    bottomLeft, topLeft, topRight, bottomRight
                });

                chunks.Add(leftover);
            }

            return chunks;
        }
    }
}
