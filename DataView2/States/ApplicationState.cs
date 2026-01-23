using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.MultiInstances;
using DataView2.Engines;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Diagnostics;
using static DataView2.Pages.Map.AddNewTableLayer;


namespace DataView2.States;

public class ApplicationState
{
    public List<string> SelectedTables = new List<string>();
    public List<int> cracksEditIdsForProcessing = new List<int>();
    public ApplicationState()
    {
        appEngine = MauiProgram.AppEngine;
    }

    public ApplicationEngine appEngine;
    public event Action<string> ActivePageChanged;


    public string ActivePage { get; private set; }

    public void SetActivePage(string page)
    {
        Log.Error($"SetActivePage {page}");
        if (ActivePage != page)
        {
            ActivePage = page;
            ActivePageChanged?.Invoke(ActivePage);
        }
    }

    public event Action<string> MenuPageChanged;
    public void SetMenuPage(string page)
    {
        ActivePage = page;
        MenuPageChanged?.Invoke(ActivePage);
    }

    public event Action SurveyTemplateHandled;
    public void HandleSurveyTemplate()
    {
        SurveyTemplateHandled?.Invoke();
    }

    public event Action SetOfflineMapPathHandled;
    public void SetOfflineMapPath()
    {
        SetOfflineMapPathHandled?.Invoke();
    }

    //Dataset & Project event 
    public event Action<Project> OnProjectUpdated;
    public Project SelectedProject { get; private set; }
    public void UpdateProject(Project project)
    {
        SelectedProject = project;
        OnProjectUpdated?.Invoke(project);

        UpdateSessionVars();
    }

    public ProjectRegistry SelectedBaseProject { get; private set; }
    public void UpdateBaseProject(ProjectRegistry project)
    {
        SelectedBaseProject = project;
    }
    public DatabaseRegistryLocal SelectedDataset { get; private set; }
    public event Action OnDatasetSelectedChanged;
    public event Action<DatabaseRegistryLocal> OnDatasetUpdated;
    public bool IsPopupOpen = false;

    public void UpdateDataset(DatabaseRegistryLocal selectedDataset)
    {
        SelectedDataset = selectedDataset;
        OnDatasetUpdated?.Invoke(selectedDataset);
    }
    public void SelectedDatasetChanged(DatabaseRegistryLocal selectedDataset)
    {
        SelectedDataset = selectedDataset;
        OnDatasetSelectedChanged?.Invoke();
        InitializeSurveyAndLayers();
    }

    //Survey event
    public Action OnSurveyIntializeRequested;
    public async void InitializeSurveyAndLayers()
    {
        var response = await appEngine.SurveyService.GetAll(new Empty());
        if (response != null)
        {
            //get all surveys from current dataset once here
            ExistingSurveys = response;
        }
        //get surveys initially
        OnSurveyIntializeRequested?.Invoke();
        //get layers
        RefreshTableNames();
    }
    public IEnumerable<string> SelectedSurveysIds { get; private set; } = new List<string>();
    public IEnumerable<Survey> ExistingSurveys { get; private set; } = new List<Survey>();
    public event Action<IEnumerable<Survey>> OnSurveySelected;
    public event Action<IEnumerable<string>> OnNewSurveySelected;
    public void SurveySelected(IEnumerable<Survey> selectedSurveys)
    {
        var newSurveyIds = selectedSurveys.Select(s => s.SurveyIdExternal).ToList();
        var newlyAddedSurveyIds = newSurveyIds.Except(SelectedSurveysIds).ToList();
        // If there are any newly added surveys, invoke the event passing the new IDs
        if (newlyAddedSurveyIds.Any())
        {
            OnNewSurveySelected?.Invoke(newlyAddedSurveyIds);
        }
        SelectedSurveysIds = newSurveyIds; //selected surveys

        //update maps
        OnSurveySelected?.Invoke(selectedSurveys);
    }

    public event Action<bool> ShowHideSurveyTemplateHandled;
    public void ShowHideSurveyTemplate(bool isChecked)
    {
        ShowHideSurveyTemplateHandled?.Invoke(isChecked);
    }

    public event Action<List<string>> OnTableCheckedChanged;
    public void CheckBoxChanged(List<string> selectedTables)
    {
        SelectedTables = selectedTables;
        OnTableCheckedChanged?.Invoke(selectedTables);
    }

    public event Action<string> OnOverlayVisibilityToggled;
    public void ToggleOverlayVisibility(string overlayId)
    {
        OnOverlayVisibilityToggled?.Invoke(overlayId);
    }

    public event Action<string, bool, string, List<string>> OnLayerSelected;

    public void LayerSelected(string selectedlayer, bool load, string type = null, List<string> newSurveys = null)
    {
        OnLayerSelected?.Invoke(selectedlayer, load, type, newSurveys);
    }

    public event Action<string, bool, List<string>> OnToggleLayers;
    public void ToggleLayers(string tableName, bool state, List<string> newSurveys = null)
    {
        OnToggleLayers?.Invoke(tableName, state, newSurveys);
    }

    public event Action<string, string> OnLayerSelectionCompleted;

    public void LayerSelectionCompleted(string selectedlayername, string passed)
    {
        OnLayerSelectionCompleted?.Invoke(selectedlayername, passed);
    }

    public event Action<string> OnShapefileTableChanged;
    public void ShapefileCheckboxChanged(string selectedShapefile)
    {
        OnShapefileTableChanged?.Invoke(selectedShapefile);
    }

    //Segmentation events
    
    //public event Action OnManualSegmentationSelected;
    //public void ManualSegmentationSelected()
    //{
    //    OnManualSegmentationSelected?.Invoke();
    //}

    public List<SegmentationTableData> tableDatas = new List<SegmentationTableData>();
    public event Action< List<SegmentationTableData>> OnApplyingSegmentation;
    public void ApplySegmentation( List<SegmentationTableData> selectedSurveySegmentations)
    {
        OnApplyingSegmentation?.Invoke( selectedSurveySegmentations);
    }

    // New event for updating coordinates in the map  
    //latitude, longitude
    public event Action<double, double> OnCoordinatesUpdated;
    public void UpdateCoordinates(double latitude, double longitude)
    {
        OnCoordinatesUpdated?.Invoke(latitude, longitude);
    }

    //Show polygon coordinates on UI
    public event Action<List<List<double>>, string> OnPolygonUpdated;
    public void UpdatePolygonCoordinates(List<List<double>> coordinates, string type)
    {
        OnPolygonUpdated?.Invoke(coordinates, type);
    }

    public class MapCoordinateDetails
    {
        public List<List<double>> Coordinates { get; set; }
        public string FileName { get; set; }
        public string GeometryType { get; set; }
        public bool IsFirst { get; set; }
        public bool? SampleUnitStatus { get; set; }
        public string SurveyStatus { get; set; }
        public double StartChainage { get; set; } = 0.0;
    }

    //Pass coordinates to draw graphics on the map
    public event Action<List<MapCoordinateDetails>> OnCoordinatesFetched;
    public void FetchCoordinatesDetails(List<MapCoordinateDetails> mapCoordinateDetails)
    {
        OnCoordinatesFetched?.Invoke(mapCoordinateDetails);
    }

    //Change the surveyset pages
    public event Action<string, Dictionary<string, object>> SurveySetPageChanged;
    public string CurrentPath { get; set; }
    public string EditSurveyCsvObj { get; set; }
    public string SurveyCsvObjFilePath { get; set; }

    public void ChangeSurveySetPage(string page, Dictionary<string, object> param)
    {
        SurveySetPageChanged?.Invoke(page, param);
    }

    public event Action<string, string, string> OnSegmentClicked;
    public void SegmentClicked(string imagePath, string surveyId, string sectionId)
    {
        OnSegmentClicked?.Invoke(imagePath, surveyId, sectionId);
    }

    public event Action<int, int> OnSampleUnitClicked;
    public void SampleUnitClicked(int pciRatingId, int sampleUnitId)
    {
        OnSampleUnitClicked?.Invoke(pciRatingId, sampleUnitId);
    }

    public event Action<string, string, string> OnDefectGraphicClicked;
    public void FindGraphicInfo(string table, string id, string type = null)
    {
        OnDefectGraphicClicked?.Invoke(table, id, type);
    }

    public bool segmentLayer { get; private set; } = true;
    
    public void SegmentLayerChecked(bool isChecked)
    {
        segmentLayer = isChecked;
    }

    public event Action<int> SegmentsLoaded;
    public void SegmentsLoad(int segmentCount)
    {
        SegmentsLoaded?.Invoke(segmentCount);
    }
    public int segmentCount { get; set; } = 0;


    //To hightlight graphics
    public event Action<string, string> PolygonPathPassed;
    public void HighlightGraphic(string path, string status)
    {
        PolygonPathPassed?.Invoke(path, status);
    }

    //To Clear everyhthing on the map
    public event Action SurveySetGraphicCleared;
    public void ClearSurveySetGraphic()
    {
        SurveySetGraphicCleared?.Invoke();
    }

    public event Action GraphicsInMapCleared;
    public void ClearAllGraphics()
    { 
        GraphicsInMapCleared?.Invoke(); 
    }

    //To Close Survey Set Page
    public event Action SurveySetClosed;
    public event Action DisposalRequested;
    public void ExitSurveySet(bool window = false)
    {
        if(window)
        {
            DisposalRequested?.Invoke();
        }
        SurveySetClosed?.Invoke();
    }

    public event Action<bool> RectangleDrawingEnabled;
    public void EnableRectangleDrawing(bool isOfflineOnly = false)
    {
        RectangleDrawingEnabled?.Invoke(isOfflineOnly);
    }

    //Close Offset razor popup
    public event Action OnPopupInMapClosed;
    public void ClosePopupInMap()
    {
        OnPopupInMapClosed?.Invoke();
    }

    public event Action OnSegmentSummaryClosed;
    public void CloseSegmentSummaryMenu()
    {
        OnSegmentSummaryClosed?.Invoke();
    }

    public event Action<string> OnOfflineMapApplied;
    public void applyOfflineMap(string newMapPath)
    {
        OnOfflineMapApplied?.Invoke(newMapPath);
    }

    public bool isUsingOnlineMap;
    public void setUsingOnlineMap(bool newValue)
    {
        isUsingOnlineMap = newValue;
    }

    public event Action<bool> Processing;
    public void NotifyProcessing(bool isprocessing)
    {
        Processing?.Invoke(isprocessing);
    }

    //Reprocess Segments selected
    public bool isReprocessingSegments;
    public bool incorrectReprocessFolder = false;
    public string reprocessFolder = "";
    public string newReprocessFolder = "";
    public bool processStatus = false;
    public List<string> fisFilesForProcessing = new List<string>();

    public string Color;
    public double Thickness;
    public event Action<string> GraphicColorRequested;
    public void GetGraphicColor(string tableName)
    {
        GraphicColorRequested?.Invoke(tableName);
    }
    public event Action<string, string, double, string> BasicGraphicColorSet;
    public void SetBasicGraphicColor(string tableName, string color, double thickness, string label)
    {
        BasicGraphicColorSet?.Invoke(tableName, color, thickness, label);
    }
    public event Action<List<ColorCodeInformation>, string> ColorCodeGraphicSet;
    public void SetColorCodeGraphic(List<ColorCodeInformation> colorCodes, string labelProperty)
    {
        ColorCodeGraphicSet?.Invoke(colorCodes, labelProperty);
    }
    public event Action<List<string>, List<string>, double, double> OnPreviewOffsetButtonClicked;
    public OffsetData offsetData;
    public void PreviewOffsetOnMap(OffsetData offset)
    {
        offsetData = offset;
        OnPreviewOffsetButtonClicked?.Invoke(offset.SurveyIds, offset.Defects, offset.HorizontalOffset, offset.VerticalOffset);
    }

    public List<string> selectableTables { get; set; } = new List<string>();
    public void PassSelectableTables(List<string> tables)
    {
        selectableTables = tables;
    }

    public event Action<double, double, double[]> OnImageZoomInRequested;
    public void ZoomInImages(double xDistance, double yDistance, double[] segmentSize)
    {
        OnImageZoomInRequested?.Invoke(xDistance, yDistance, segmentSize);
    }

    public event Action OnResetImageClicked;
    public void ResetImage()
    {
        OnResetImageClicked?.Invoke();
    }

    public event Action OnColorCodeApplied;
    public void NotifyColorCodeApplied()
    {
        OnColorCodeApplied?.Invoke();
    }

    public List<ColorCodeInformation> ColorCodeInfo = new();

    public event Action<IEnumerable<string>, string, string> OnPreviewDoubleUpsRequested;
    public void PreviewDoubleUps(IEnumerable<string> selectedTable, string survey, string secondSurvey = null)
    {
        OnPreviewDoubleUpsRequested?.Invoke(selectedTable, survey, secondSurvey);
    }

    public event Action<string, IEnumerable<string>, string, string> OnLayerLoadHighlightRequested;

    public void LoadLayersAndHighlight(string mode, IEnumerable<string> selectedTable, string survey, string secondSurvey = null)
    {
        OnLayerLoadHighlightRequested?.Invoke(mode, selectedTable, survey, secondSurvey);
    }

    public event Action<IEnumerable<string>, string, string> OnDeleteDoubleUpRequested;

    public void DeleteDoubleUpRequested(IEnumerable<string> selectedTable, string survey, string secondSurvey = null)
    {
        OnDeleteDoubleUpRequested?.Invoke(selectedTable, survey, secondSurvey);
    }

    public event Action<string> OnDefectsHighlighted;
    public List<Graphic> graphicsToRemove = new();
    public bool IsMultiSelected = false;
    public void NotifyDefectsHighlighted(List<Graphic> graphics, string message)
    {
        graphicsToRemove = graphics;
        OnDefectsHighlighted?.Invoke(message);
    }
    public event Action OnRevertRequested;
    public void RevertGraphicsToRemove()
    {
        OnRevertRequested?.Invoke();
    }
    public event Action<string,string> OnRemoveDefectsRequested;
    public void RemoveHighlightedDefects(string surveyId = null, string surveyName = null)
    {
        OnRemoveDefectsRequested?.Invoke(surveyId, surveyName);
    }
    public event Action OnProcessingCompletedFromMap;
    public void NotifyProcessingCompletedFromMap()
    {
        OnProcessingCompletedFromMap?.Invoke();
    }
    public event Action<List<MapPoint>> OnBoundariesPassed;
    public void DrawBoundaryOnMap(List<MapPoint> boundaryCoordinates)
    {
        OnBoundariesPassed?.Invoke(boundaryCoordinates);
    }
    public event Action<IEnumerable<string>, string> OnPreviewOutsideBoundaryRequested;
    public void PreviewDefectsOutsideBoundary(IEnumerable<string> selectedTable, string survey)
    {
        OnPreviewOutsideBoundaryRequested?.Invoke(selectedTable, survey);
    }
    public event Action<string,string, IEnumerable<string>> OnOutsideBoundaryRemovalRequested;
    public void RemoveDefectsOutsideBoundary(string surveyId, string surveyName, IEnumerable<string> selectedTables)
    {
        OnOutsideBoundaryRemovalRequested?.Invoke(surveyId, surveyName, selectedTables);
    }
    public event Action<bool> TableNamesUpdated;
    public void UpdateTableNames(bool isBoundary = false)
    {
        TableNamesUpdated?.Invoke(isBoundary);
    }

    public event Action<int> PCILayerVisibilityToggled;
    public void UpdatePCIRatingLayerVisibility(int pciRatingId)
    {
        PCILayerVisibilityToggled?.Invoke(pciRatingId);
    }

    public event Action DisableLasButtonRequested;
    public void DisableLasButton()
    {
        DisableLasButtonRequested?.Invoke();
    }

    public event Action OnTableNamesRefreshed;
    public void RefreshTableNames()
    {
        //Fetch layer tables 
        OnTableNamesRefreshed?.Invoke();
    }
    public event Action OnTempBoundaryRemoved;
    public void RemoveTempBoundary()
    {
        OnTempBoundaryRemoved?.Invoke();
    }

    public event Action<object, EventArgs> OnPolygonDrawingRequested;
    public void DrawBoundaryPolygon(object sender, EventArgs e)
    {
        OnPolygonDrawingRequested?.Invoke(sender, e);
    }

    public event Action<object, EventArgs> OnUndoPolygonRequested;
    public void UndoBoundaryPolygon(object sender, EventArgs e)
    {
        OnUndoPolygonRequested?.Invoke(sender, e);
    }

    public event Action OnSavePolygonRequested;
    public void SaveBoundaryPolygon()
    {
        OnSavePolygonRequested?.Invoke();
    }

    public event Action OnGeometryEditorStopped;
    public void NotifyGeometryEditorStopped()
    {
        OnGeometryEditorStopped?.Invoke();
    }

    public event Action GeometryEditorStopRequested;
    public void StopGeometryEditor()
    {
        GeometryEditorStopRequested?.Invoke();
    }

    public event Action FindFisFilesRequested;
    public void FindFisFiles()
    {
        FindFisFilesRequested?.Invoke();
    }

    public event Action<string, string, string> OnNewLayerAdded;
    public void AddNewLayer(string tableName, string geoType, string iconPath = null)
    {
        OnNewLayerAdded?.Invoke(tableName, geoType, iconPath);
    }

    public string CurrentDrawingToolLayer { get; private set; }
    public Guid? CurrentPCIDefectGuid { get; private set; }

    public event Action<string, string, bool> DrawingToolRequested;
    public void SetDrawingToolVisibility(string tableName, string geoType, bool isPCIRating = false, Guid? defectGuid = null)
    {
        CurrentDrawingToolLayer = tableName;
        if(defectGuid != null)
        {
            CurrentPCIDefectGuid = defectGuid;
        }
        DrawingToolRequested?.Invoke(tableName, geoType, isPCIRating);
    }

    public event Action<string, string, string, bool> OnBottomMenuChanged;
    public void SetBottomMenuVisibility(string tableName, string surveyId, string segmentId, bool status)
    {
        OnBottomMenuChanged?.Invoke(tableName, surveyId, segmentId, status);
    }

    public event Action OnCloseDrawingToolInvoked;
    public void InvokeCloseDrawingTool()
    {
        OnCloseDrawingToolInvoked?.Invoke();
    }

    public event Action<string, Dictionary<string, object>, bool> OnDefectFieldsSet;
    public void SetDefectFields(string tableName, Dictionary<string, object> defectFields, bool isLCMSTable)
    {
        OnDefectFieldsSet?.Invoke(tableName, defectFields, isLCMSTable);
    }

    public event Action OnGraphicOutSideSegmentClicked;
    public void GraphicOutSideSegmentClicked()
    {
        OnGraphicOutSideSegmentClicked?.Invoke();
    }

    public event Action<string, string, int> OnMetaTableUpdated;
    public void UpdateMetaTable(string tableName, string iconPath, int iconSize)
    {
        OnMetaTableUpdated?.Invoke(tableName, iconPath, iconSize);
    }

    //Reprocess Segments selected
    public event Action OnReprocessFinished;
    public void NotifySegmentsSelected()
    {
        OnReprocessFinished?.Invoke();
    }

    public event Action<string, string> OnFolderOpenClicked;
    public void FolderOpenClicked(string imagePath, string surveyId)
    {
        OnFolderOpenClicked?.Invoke(imagePath, surveyId);
    }

    public event Action<string> OnVideoPopupClosed;
    public void CloseVideoPopup(string cameraInfo)
    {
        OnVideoPopupClosed?.Invoke(cameraInfo);
    }

    public event Action<string> OnMeasurementClicked;
    public void EnableMeasurement(string type)
    {
        OnMeasurementClicked?.Invoke(type);
    }

    public FieldModel PendingMeasurementField = new FieldModel();
    public event Action OnMeasurementCompleted;
    public void MeasurementCompleted()
    {
        OnMeasurementCompleted?.Invoke(); 
    }

    public event Action<List<DetailLogViewHelper>> OnDetailLogViewHelperReceived;
    public void DetailLogreceived(List<DetailLogViewHelper> detailLogs)
    {
        OnDetailLogViewHelperReceived?.Invoke(detailLogs);
    }

    public event Action<string, List<int>> OnQCFilterApplied;
    public void ApplyQCFilters(string tableName, List<int> ids)
    {
        OnQCFilterApplied?.Invoke(tableName, ids);
    }

    public event Action<string> OnSummariesFetchRequested;
    public void FetchSummaries(string overlayName)
    {
        OnSummariesFetchRequested?.Invoke(overlayName);
    }

    public event Action<bool> OnEditingBooleanChanged;
    public void ChangeEditingBool(bool isEditing)
    {
        OnEditingBooleanChanged?.Invoke(isEditing);
    }

    public void ChangeCrackEdit(string layer)
    {
        CrackEditChanged?.Invoke(layer);
    }
    public event Action<string> CrackEditChanged;

    public event Action OnIRICalculationRequested;
    public string IRISurveyId { get; private set; }
    public int IRIUserDefinedMeter { get; private set; } = 0;
    public void RequestIRIRecalculation(string surveyId, int iriMeter)
    {
        IRISurveyId = surveyId;
        IRIUserDefinedMeter = iriMeter;
        OnIRICalculationRequested?.Invoke();
    }
    public event Action<bool> OnIRIStatusNotified;
    public void NotifyIRIStatus(bool isProcessing)
    {
        OnIRIStatusNotified?.Invoke(isProcessing);
    }

    public event Action<double, JToken> DefectGraphicInfoSent;
    public void SendDefectGraphicInfo(double qty, JToken coordinateToken)
    {
        DefectGraphicInfoSent?.Invoke(qty, coordinateToken);
    }

    public event Action<Guid> PCIDefectRemovalRequested;
    public void RemovePCIDefect(Guid id)
    {
        PCIDefectRemovalRequested?.Invoke(id);
    }

    public event Action<string> PCIRatingModeUpdated;
    public void UpdatePCIRatingMode(string sampleUnitName)
    {
        PCIRatingModeUpdated?.Invoke(sampleUnitName);
    }

    public event Action<string, bool> OnLayerToggleRequestedFromPCI;
    public void AddOrRemoveDefectFromPCIRating(string tableName, bool state)
    {
        OnLayerToggleRequestedFromPCI?.Invoke(tableName, state);
    }

    public event Action OnDefectMultiSelectionRequested;
    public void RequestDefectMultiSelection()
    {
        OnDefectMultiSelectionRequested?.Invoke();
    }


    //Manual Segmentation
    public event Action<Survey, int> OnStartSegmentation;
    public void StartSegmentation(Survey survey, int segmentationId)
    {
        OnStartSegmentation?.Invoke(survey, segmentationId);
    }



    public event Action<List<Survey>, List<SampleUnit>, string, List<SummaryItem>, int> OnSampleUnitSummaryRequested;
    public void CreateSampleUnitSummary(List<Survey> surveys, List<SampleUnit> sampleUnits, string summaryName, List<SummaryItem> summaryItems, int sampleUnitSetId)
    {
        OnSampleUnitSummaryRequested?.Invoke(surveys, sampleUnits, summaryName, summaryItems, sampleUnitSetId);
    }
    public event Action<List<Survey>, int, string, List<SummaryItem>> OnSegmentIntervalSummaryRequested;
    public void CreateSegmentIntervalSummary(List<Survey> surveys, int interval, string summaryName, List<SummaryItem> summaryItems)
    {
        OnSegmentIntervalSummaryRequested?.Invoke(surveys, interval, summaryName, summaryItems);
    }

    public event Action<int, string> OnIntervalSummaryFinished;
    public void NotifyIntervalSummaryProcessed(int progress, string error = null)
    {
        OnIntervalSummaryFinished?.Invoke(progress, error);
    }

    public event Action OnPCISampleUnitsRedrawRequested;
    public void RedrawPCISampleUnits()
    {
        OnPCISampleUnitsRedrawRequested?.Invoke();
    }


    public HashSet<Survey> SelectedSurveysForSegmentation = null;
    public event Action OnImportingSegmentation;
    public void ImportSegmentationCalled()
    {
        OnImportingSegmentation?.Invoke();
    }
    public event Action OnImportSegmentation;
    public void ImportSegmentation()
    {
        OnImportSegmentation?.Invoke();
    }


    //row is clicked in segmentation Table POPUP page  TO HIGHLIGHT ON MAP
     
    public event Action<string, string> OnSurveySegmentationRowClicked;
    public void SurveySegmentationRowClicked(string surveyId, string segmentationName)
    {
        OnSurveySegmentationRowClicked?.Invoke(surveyId, segmentationName);
    }
    //Recalculate LasRutting:
    public event Action OnRecalculateRuttingClick;
    public void RecalculateRuttingSelected( )
    {
        OnRecalculateRuttingClick?.Invoke();
    }

    public event Action<string, Keycode> OnKeycodeDrawing;
    public void SetDrawingKeycode (string keycodeName, Keycode keycode)
    {
        OnKeycodeDrawing?.Invoke(keycodeName, keycode);
    }

    //Multi-Instances Handling:   
    public void UpdateSessionVars()
    {
        var newlyAddedSurveyIds = SelectedSurveysIds.ToList();
        string processId = Process.GetCurrentProcess().Id.ToString();
        string surveyExternalId = newlyAddedSurveyIds?.FirstOrDefault() ?? "-1";
        if (!SharedDVInstanceStore.DVInstances.TrySetSurvey(surveyExternalId, SelectedProject.DBPath, out var error))
        {
            Log.Information($"[DV MultiSession] Survey in use. PID={processId}, Survey={surveyExternalId}");
        }
    }
    public bool IsProjectPathUsedByOtherSession()
    {
        string pathDB = SelectedProject.DBPath;
        return SharedDVInstanceStore.DVInstances.IsProjectPathInUse(pathDB);
    }
    
    //Maps Interfaces
    // Synchronization Action (toggle)
    public Action? OnToggleMapSynchronization { get; set; }
    public string GrpcServiceIP { get; internal set; }

    // Force release map synchronization when closing App
    public event Action? OnForceReleaseMapSync;
    public void ForceReleaseMapSync()
    {
        OnForceReleaseMapSync?.Invoke();
    }
}
