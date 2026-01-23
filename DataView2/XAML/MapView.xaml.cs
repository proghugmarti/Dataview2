using CsvHelper;
using CsvHelper.Configuration;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.MultiInstances;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Engines;
using DataView2.MapHelpers;
using DataView2.Options;
using DataView2.Platforms.Windows;
using DataView2.States;
using DataView2.ViewModels;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Editing;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Handlers;
using MudBlazor;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Windows.System;
using static DataView2.Core.Helper.GeneralHelper;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.MapHelpers.GeneralMapHelper;
using static DataView2.Platforms.Windows.WinAPI;
using static DataView2.States.ApplicationState;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Color = Microsoft.Maui.Graphics.Color;
using Map = Esri.ArcGISRuntime.Mapping.Map;


namespace DataView2.XAML;

public partial class MapView : ContentView, INotifyPropertyChanged
{
    public ApplicationState appState;
    public ApplicationEngine appEngine;
    private MapViewModel viewModel;
    private bool isViewpointChanging = true;
    private GraphicsOverlay newOverlay;
    private GraphicsOverlay lasLinesOverlay;

    private InstanceSyncService? _syncService;
    private bool _syncEnabled = false;
    private bool _syncRemoteActive = false;
    private bool _userIsInteractingWithMap;
    private bool _localUserIsDragging;   


    public MapView()
    {
        InitializeComponent();
        appEngine = MauiProgram.AppEngine;
        appState = MauiProgram.AppState;
        viewModel = Resources["MapViewModel"] as MapViewModel;

        _geometryEditor = new GeometryEditor();

        SetDefaultGeometryEditorStyle();
        MyMapView.GeometryEditor = _geometryEditor;
        MyMapView.GeoViewTapped += OnMapViewTappedAsync;
        MyMapView.ViewpointChanged += MapView_GeoViewVisibleAreaChanged;
        MyMapView.ViewpointChanged += OnLocalViewpointChanged;

        appState.GeometryEditorStopRequested += StopGeometryEditor;
        appState.OnPopupInMapClosed += () => DeactivateAllMapButtons();
        appState.OnDatasetSelectedChanged += CloseAllPopupsAfterDatasetChange;
        appState.OnDefectMultiSelectionRequested += HandleMultiDefectsSelect;
        appState.PolygonPathPassed += HighlightSurveyTemplateGraphics;
        appState.OnVideoPopupClosed += CloseVideoImages;
        appState.OnPolygonDrawingRequested += PolygonButton_Click;
        appState.SurveySetGraphicCleared += ClearSurveySetOverlay;
        appState.SurveySetClosed += ClearSurveySetOverlay;
        appState.OnCoordinatesFetched += DisplayBoundaryGraphicsOnMap;
        appState.OnUndoPolygonRequested += UndoButton_Click;
        appState.DrawingToolRequested += SetDrawingToolVisibility;
        appState.OnKeycodeDrawing += SetDrawingKeycodes;
        appState.OnDefectFieldsSet += SavePropertiesInExistingTable;
        appState.RectangleDrawingEnabled += SelectOfflineMapArea; // Offline map
        appState.OnCoordinatesUpdated += UpdateViewpoint; 
        appState.OnShapefileTableChanged += ChangeViewPointToShapefile; // Shape
        appState.OnMeasurementClicked += EnableMeasurement; // Distance rulers
        appState.OnEditingBooleanChanged += ControlIsEditing; // Sample Units
        appState.OnIRICalculationRequested += StartIRIPoint; // IRI 
        appState.PCIDefectRemovalRequested += RemovePCIDefectGraphic; // PCI
        appState.DisableLasButtonRequested += ClearLasFileRut; // LAS
        appState.OnPreviewOffsetButtonClicked += ApplyOffsetToGraphics;
        appState.OnStartSegmentation += StartSegmentation; //Manual
        appState.OnApplyingSegmentation += CreateSegmentatedSurveysAndSaveSegments; //Apply from table popup autosegmentation
        appState.OnQCFilterApplied += HighlightQCFilterGraphics;
        appState.OnRemoveDefectsRequested += RemoveHighlightedDefects;
        appState.OnSavePolygonRequested += SaveNewBoundaryPolygon;
        appState.OnReprocessFinished += RefreshMapVisibility;
        appState.FindFisFilesRequested += FindFisFiles;
        appState.OnImportingSegmentation += OnImportingSegmentation;
        appState.OnSurveySegmentationRowClicked += HighlightSurveyTemplateGraphics;
        appState.OnRecalculateRuttingClick += RecalculateRutting;
        //appState.OnToggleMapSynchronization += HandleSynchronizationToggle;
        appState.OnForceReleaseMapSync += HandleForceReleaseMapSync;
        MyMapView.NavigationCompleted += OnNavigationCompleted;

        this.BindingContext = this;

        isViewpointChanging = false;
        IsAutoRotationEnabled = true;

        //Reprocess Segments selected
        appState.isReprocessingSegments = false;
        appState.incorrectReprocessFolder = false;
        InitializeMapButtons();
        InitializeSynchronization();
    }

    
    private double GetLatitude(string segmentationPoint)
    {
        return Convert.ToDouble(segmentationPoint.Replace("[", "").Replace("]", "").Split(',', StringSplitOptions.RemoveEmptyEntries)[0]);
    }

    private double GetLongitude(string segmentationPoint)
    {
        return Convert.ToDouble(segmentationPoint.Replace("[", "").Replace("]", "").Split(',', StringSplitOptions.RemoveEmptyEntries)[1]);
    }

    private void ReadyToSegment()
    {
        //close the popup
        RecalculatePage.IsVisible = false;
        appState.IsPopupOpen = false;
        IsSelectingSegmentation = true;
        SegmentationButton.Background = Color.FromArgb("#FFFFFF");

        if (newOverlay != null)
        {
            newOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(newOverlay);
            MyMapView.DismissCallout();
        }

        multiPoints = new List<MapPoint>();
        newOverlay = new GraphicsOverlay { Id = "newOverlay" };
    }

    private void StartSegmentation(Survey selectedSurvey, int surveySegmentId)
    {
        DeactivateAllMapButtons();
        ReadyToSegment();
        segmentedSurvey = selectedSurvey;
        surveySegmentationId = surveySegmentId;
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {        
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public GeometryEditor _geometryEditor;
    private Graphic _selectedGraphic;
    private Graphic _selectedSegment;
    private List<Graphic> selectedGraphics;
    private List<Graphic> selectedVideoGraphics = new List<Graphic>();
    private bool isCreatingGraphics = false;
    private Survey segmentedSurvey;
    private int surveySegmentationId = 0;

    private bool _isEditingDatabase;

    public bool IsEditingDatabase
    {
        get { return _isEditingDatabase; }
        set
        {
            _isEditingDatabase = value;
            OnPropertyChanged();
            if (_isEditingDatabase)
            {
                messageBoxUpdateProgress.IsVisible = true;
            }
            else
            {
                messageBoxUpdateProgress.IsVisible = false;
            }
        }
    }

    private bool _isDeletingDefects;

    private bool isDeletingDefects
    {
        get { return _isDeletingDefects; }
        set
        {
            _isDeletingDefects = value;
            OnPropertyChanged();
            if (_isDeletingDefects)
            {
                messageBoxDelete.IsVisible = true;
            }
            else
            {
                messageBoxDelete.IsVisible = false;
            }
        }
    }

    private bool _isMovingSegments;

    public bool IsMovingSegments
    {
        get { return _isMovingSegments; }
        set
        {
            _isMovingSegments = value;
            OnPropertyChanged();
            if (_isMovingSegments)
            {
                messageBoxPressEnter.IsVisible = true;
                messageBoxSelectSegments.IsVisible = false;
            }
            else
            {
                messageBoxPressEnter.IsVisible = false;
            }
        }
    }

    private bool _isSelectingSegments;

    public bool IsSelectingSegments
    {
        get { return _isSelectingSegments; }
        set
        {
            _isSelectingSegments = value;
            OnPropertyChanged();
            if (_isSelectingSegments)
            {
                messageBoxSelectSegments.IsVisible = true;
            }
            else
            {
                messageBoxSelectSegments.IsVisible = false;
            }
        }
    }


    private bool _isEditing;
    public bool IsEditing
    {
        get { return _isEditing; }
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isOffline;
    public bool IsOffline
    {
        get { return _isOffline; }
        set
        {
            if (_isOffline != value)
            {
                _isOffline = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isSurveyTemplate;
    public bool IsSurveyTemplate
    {
        get { return _isSurveyTemplate; }
        set
        {
            if (_isSurveyTemplate != value)
            {
                _isSurveyTemplate = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLasFilesMode;
    public bool IsLasFilesMode
    {
        get { return _isLasFilesMode; }
        set
        {
            if (_isLasFilesMode != value)
            {
                _isLasFilesMode = value;
                OnPropertyChanged();
            }

            if (_isLasFilesMode)
            {
                messageBoxLas.IsVisible = true;
            }
            else
            {
                messageBoxLas.IsVisible = false;
            }
        }
    }

    private bool _isSelectingLasRutting;
    public bool IsSelectingLasRutting
    {
        get { return _isSelectingLasRutting; }
        set
        {
            _isSelectingLasRutting = value;
            OnPropertyChanged();
            if (_isSelectingLasRutting)
            {
                messageBoxRut.IsVisible = true;
                messageBoxLas.IsVisible = false;
            }
            else
            {
                messageBoxRut.IsVisible = false;
                messageBoxLas.IsVisible = true;
            }
        }
    }

    private bool _isSelectingSegmentation;
    public bool IsSelectingSegmentation
    {
        get { return _isSelectingSegmentation; }
        set
        {
            _isSelectingSegmentation = value;
            OnPropertyChanged();
            if (_isSelectingSegmentation)
            {
                messageBoxSegmentation.IsVisible = true;
            }
            else
            {
                messageBoxSegmentation.IsVisible = false;
            }
        }
    }

    private bool _surveyTemplateVisibility;
    public bool SurveyTemplateVisibility
    {
        get { return _surveyTemplateVisibility; }
        set
        {
            if (_surveyTemplateVisibility != value)
            {
                _surveyTemplateVisibility = value;
                if (_surveyTemplateVisibility)
                {
                    _drawingToolVisibility = false; // Ensure the other value is false
                    OnPropertyChanged(nameof(DrawingToolVisibility));
                    appState.InvokeCloseDrawingTool();
                }
                OnPropertyChanged();
            }
        }
    }
    private bool _isDrawingKeycodePoint; 
    public bool IsDrawingKeycodePoint
    {
        get { return _isDrawingKeycodePoint; }
        set
        {
            if (_isDrawingKeycodePoint != value)
            {
                _isDrawingKeycodePoint = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _isDrawingKeycodeLine;
    public bool IsDrawingKeycodeLine
    {
        get { return _isDrawingKeycodeLine; }
        set
        {
            if (_isDrawingKeycodeLine != value)
            {
                _isDrawingKeycodeLine = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _drawingToolVisibility;
    public bool DrawingToolVisibility
    {
        get { return _drawingToolVisibility; }
        set
        {
            if (_drawingToolVisibility != value)
            {
                _drawingToolVisibility = value;
                if (_drawingToolVisibility)
                {
                    _surveyTemplateVisibility = false; // Ensure the other value is false
                    OnPropertyChanged(nameof(SurveyTemplateVisibility));
                }
                else
                {
                    _geometryEditor.Stop();
                }
                OnPropertyChanged();
            }
        }
    }

    private bool _pciDrawingToolVisibility;
    public bool PCIDrawingToolVisibility
    {
        get { return _pciDrawingToolVisibility; }
        set
        {
            if (_pciDrawingToolVisibility != value)
            {
                _pciDrawingToolVisibility = value;
                if (_drawingToolVisibility)
                {
                    OnPropertyChanged(nameof(SurveyTemplateVisibility));
                }
                else
                {
                    _geometryEditor.Stop();
                }
                OnPropertyChanged();
            }
        }
    }

    private bool _isAutoRotationEnabled;

    public bool IsAutoRotationEnabled
    {
        get => _isAutoRotationEnabled;
        set
        {
            if (_isAutoRotationEnabled != value)
            {
                _isAutoRotationEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isMapSynchronizedEnabled;
    public bool IsMapSynchronizedEnabled
    {
        get => _isMapSynchronizedEnabled;
        set
        {
            if (_isMapSynchronizedEnabled == value)
                return;

            //Change Switch from remote
            if (_ignoreMapSyncToggle)
            {
                _isMapSynchronizedEnabled = value;
                OnPropertyChanged();
                return;
            }

            if (value && SharedDVInstanceStore.DVInstances.IsOnlyInstance())
            {
                Application.Current.MainPage.DisplayAlert(
                "Shared Map message",
                "At least one additional New DataView session must be open to synchronize maps.",
                "OK");
                IsMapSynchronizedEnabled = false;
                OnPropertyChanged();
                return;
            }

            _isMapSynchronizedEnabled = value;
            OnPropertyChanged();
            HandleSynchronizationToggle();
        }
    }
    //Manipulate Switch from remote
    private bool _ignoreMapSyncToggle;

    // Command to reset to the original state
    public ICommand ResetRotationCommand => new Command(async () =>
    {
        if (MyMapView != null)
        {
            // Reset map rotation to 0°
            await MyMapView.SetViewpointRotationAsync(0);
        }
    });

    private bool _isSelectingCracks;

    public bool IsSelectingCracks
    {
        get { return _isSelectingCracks; }
        set
        {
            _isSelectingCracks = value;
            OnPropertyChanged();
            if (_isSelectingCracks)
            {
                messageBoxSelectCracks.IsVisible = true;
            }
            else
            {
                messageBoxSelectCracks.IsVisible = false;
            }
        }
    }

    private bool _isSelectingSummaryCracks;

    public bool IsSelectingSummaryCracks
    {
        get { return _isSelectingSummaryCracks; }
        set
        {
            _isSelectingSummaryCracks = value;
            OnPropertyChanged();
            if (_isSelectingSummaryCracks)
            {
                messageBoxSelectCracks.IsVisible = true;
            }
            else
            {
                messageBoxSelectCracks.IsVisible = false;
            }
        }
    }

    private bool _isSummarizingIRI = false;
    public bool IsSummarizingIRI
    {
        get { return _isSummarizingIRI; }
        set
        {
            _isSummarizingIRI = value;
            OnPropertyChanged();
            if (_isSummarizingIRI)
            {
                messageBoxIRI.IsVisible = true;
            }
            else
            {
                messageBoxIRI.IsVisible = false;
            }
        }
    }

    //no UI binding or change notification required -> please just use simple boolean
    private bool IsChangingSegments = false;
    private bool IsLasRepeatMode = false;
    private bool IsProcessingMultiDefects = false;
    private bool isProcessingLasRutting = false;
    private bool isProcessingCracks = false;
    private bool isProcessingSummaryCracks = false;

    private async void UpdateViewpoint(double latitude, double longitude)
    {
        try
        {
            // Check if latitude and longitude are both non-zero
            if ((latitude != 0 || longitude != 0) && MyMapView.Map.Basemap.BaseLayers.Count > 0)
            {
                Envelope fullExtent = MyMapView.Map.Basemap.BaseLayers[0].FullExtent;
                Envelope currentGeoExtent = (Envelope)fullExtent.Project(SpatialReferences.Wgs84);
                MapPoint mapPoint = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
                if (currentGeoExtent.Contains(mapPoint))
                {
                    // Update the viewpoint based on the new coordinates
                    await UpdateViewpointAsync(latitude, longitude, 2500);
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Alert", "The initial coordinate is not within the current offline map.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in UpdateViewpoint : {ex.Message}");
        }
    }

    // Method to update the viewpoint dynamically
    private async Task UpdateViewpointAsync(double latitude, double longitude, double scale)
    {
        if (!isViewpointChanging)
        {
            try
            {
                isViewpointChanging = true;
                Viewpoint newViewpoint = new Viewpoint(latitude, longitude, scale);
                await MyMapView.SetViewpointAsync(newViewpoint);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                Envelope visibleArea = MyMapView.VisibleArea.Extent;

                Envelope visibleAreaGeographic = (Envelope)GeometryEngine.Project(visibleArea, SpatialReferences.Wgs84);


                List<double> visibleAreaCoordinates = new List<double>
                {

                    visibleAreaGeographic.XMin, visibleAreaGeographic.YMin, visibleAreaGeographic.XMax, visibleAreaGeographic.YMax
                };

                isViewpointChanging = false;
            }
        }

    }

    #region Hotkeys
    private static int m_hHook = 0;
    private HookProc m_HookProcedure;
    private bool keyboardHookActivated = false;
    private bool hasKeyTriggered = false;

    public async void OnDisplayWebChanged(bool e)
    {
        if (!e)
        {
            ActivateKeyboardHook();
            Log.Error("ActivateKeyboardHook");
        }
        else
        {
            DeactivateKeyboardHook();
            Log.Error("ActivateKeyboardHook");
        }
    }

    private void ActivateKeyboardHook()
    {
        if (!keyboardHookActivated)
        {
            m_HookProcedure = new HookProc(HookProcedure);
            m_hHook = SetWindowsHookEx(WH_KEYBOARD, m_HookProcedure, (IntPtr)0, (int)GetCurrentThreadId());
            keyboardHookActivated = true;
        }
    }

    // Method to deactivate the hook when the map is hidden
    private void DeactivateKeyboardHook()
    {
        if (keyboardHookActivated)
        {
            UnhookWindowsHookEx(m_hHook);
            keyboardHookActivated = false;
        }
    }
    private void HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) return;

        bool shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down));
        bool ctrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down));

        string hotkey = GetCharFromKey((uint)wParam, shift);

        if (!string.IsNullOrEmpty(hotkey) && MyMapView.IsLoaded && MyMapView.IsEnabled)
        {
            if (appState.IsPopupOpen && !MyMapView.IsFocused)
            {
                return;
            }
            // handle ctrl+delete key
            if (ctrl && hotkey == "delete")
            {
                var newSelectedGraphics = new List<Graphic>();
                if (_selectedGraphic != null)
                {
                    newSelectedGraphics.Add(_selectedGraphic);
                    HandleDeleteDefects(true, newSelectedGraphics);
                }
                else if (selectedGraphics != null && selectedGraphics.Count > 0)
                {
                    HandleDeleteDefects(true, selectedGraphics);
                }
                else if (_oldGraphics.Count > 0)
                {
                    HandleDeleteSegments(_oldGraphics);
                }
                else if (_selectedSegment != null)
                {
                    HandleDeleteSegments(new List<Graphic> { _selectedSegment });
                }
            }
            else
            {
                HandleHotkey(hotkey.ToLower());
            }
        }
    }

    static string GetCharFromKey(uint key, bool shift)
    {
        if (key == (uint)VirtualKey.Delete)
        {
            return "delete";
        }
        else if (key == (uint)VirtualKey.Enter)
        {
            return "enter";
        }
        else if (key == (uint)VirtualKey.Escape)
        {
            return "escape";
        }
        else if (key == (uint)VirtualKey.Space)
        {
            return "space";
        }
        var buf = new StringBuilder(256);
        var keyboardState = new byte[256];
        if (shift)
            keyboardState[(int)VirtualKey.Shift] = 0xff;

        WinAPI.ToUnicode(key, 0, keyboardState, buf, 256, 0);

        if (buf.Length == 0) return "";
        return buf[0].ToString();
    }

    private async void HandleHotkey(string hotkey)
    {
        try
        {
            //avoid triggering twice
            if (hasKeyTriggered)
            {
                return; // Exit if the hotkey is already processing
            }
            hasKeyTriggered = true; // Set to true to prevent duplicate triggering

            double rotationIncrement = 5.0;

            double currentScale = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetScale ?? 1.0;
            double translationIncrement = CalculateTranslationIncrement(currentScale);
            if (hotkey == "q")
            {
                // Rotate the map view to the left
                await MyMapView.SetViewpointRotationAsync(MyMapView.MapRotation - rotationIncrement);
            }
            else if (hotkey == "e")
            {
                // Rotate the map view to the right
                await MyMapView.SetViewpointRotationAsync(MyMapView.MapRotation + rotationIncrement);
            }
            else if (hotkey == "w")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the translation based on the map's rotation
                double angleInRadians = currentViewpoint.Rotation * Math.PI / 180.0;
                double translationX = Math.Sin(angleInRadians) * translationIncrement;
                double translationY = Math.Cos(angleInRadians) * translationIncrement;

                // Calculate the new center point with the translated offset
                MapPoint newCenter = new MapPoint(currentViewpoint.TargetGeometry.Extent.GetCenter().X + translationX,
                                                  currentViewpoint.TargetGeometry.Extent.GetCenter().Y + translationY);

                // Set the new viewpoint
                await MyMapView.SetViewpointCenterAsync(newCenter);
            }
            else if (hotkey == "a")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the translation based on the map's rotation
                double angleInRadians = (currentViewpoint.Rotation + 90.0) * Math.PI / 180.0;
                double translationX = Math.Sin(angleInRadians) * translationIncrement;
                double translationY = Math.Cos(angleInRadians) * translationIncrement;

                // Calculate the new center point with the translated offset
                MapPoint newCenter = new MapPoint(currentViewpoint.TargetGeometry.Extent.GetCenter().X - translationX,
                                                  currentViewpoint.TargetGeometry.Extent.GetCenter().Y - translationY);

                // Set the new viewpoint
                await MyMapView.SetViewpointCenterAsync(newCenter);
            }
            else if (hotkey == "s")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the translation based on the map's rotation
                double angleInRadians = currentViewpoint.Rotation * Math.PI / 180.0;
                double translationX = Math.Sin(angleInRadians) * translationIncrement;
                double translationY = Math.Cos(angleInRadians) * translationIncrement;

                // Calculate the new center point with the translated offset
                MapPoint newCenter = new MapPoint(currentViewpoint.TargetGeometry.Extent.GetCenter().X - translationX,
                                                  currentViewpoint.TargetGeometry.Extent.GetCenter().Y - translationY);

                // Set the new viewpoint
                await MyMapView.SetViewpointCenterAsync(newCenter);
            }
            else if (hotkey == "d")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the translation based on the map's rotation
                double angleInRadians = (currentViewpoint.Rotation + 90.0) * Math.PI / 180.0;
                double translationX = Math.Sin(angleInRadians) * translationIncrement;
                double translationY = Math.Cos(angleInRadians) * translationIncrement;

                // Calculate the new center point with the translated offset
                MapPoint newCenter = new MapPoint(currentViewpoint.TargetGeometry.Extent.GetCenter().X + translationX,
                                                  currentViewpoint.TargetGeometry.Extent.GetCenter().Y + translationY);

                // Set the new viewpoint
                await MyMapView.SetViewpointCenterAsync(newCenter);
            }
            else if (hotkey == "r")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the new scale
                double newScale = currentViewpoint.TargetScale * 0.9;

                // Set the new viewpoint scale
                await MyMapView.SetViewpointScaleAsync(newScale);
            }
            else if (hotkey == "f")
            {
                Viewpoint currentViewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                // Calculate the new scale
                double newScale = currentViewpoint.TargetScale * 1.1;

                // Set the new viewpoint scale
                await MyMapView.SetViewpointScaleAsync(newScale);
            }
            else if (hotkey == "z")
            {
                if (_selectedSegment != null && !IsChangingSegments)
                {
                    IsChangingSegments = true;
                    string currentSurvey = _selectedSegment.Attributes["SurveyId"].ToString();
                    int currentSectionId = Convert.ToInt32(_selectedSegment.Attributes["SectionId"]);

                    Graphic previousSegment = FindSegmentGraphic(currentSurvey, currentSectionId, -1);
                    if (previousSegment != null)
                    {
                        await HandleSegmentClick(previousSegment);

                        Envelope visibleAreaGeographic = (Envelope)GeometryEngine.Project(MyMapView.VisibleArea.Extent, SpatialReferences.Wgs84);
                        if (!visibleAreaGeographic.Intersects(previousSegment.Geometry.Extent))
                        {
                            MapPoint centerPoint = previousSegment.Geometry.Extent.GetCenter();
                            MyMapView.SetViewpoint(new Viewpoint(centerPoint.Y, centerPoint.X, MyMapView.MapScale));
                        }
                    }
                    await Task.Delay(200);
                    IsChangingSegments = false;

                    if (VideoPlayer.IsVisible)
                    {
                        var centerPoint = GeometryEngine.LabelPoint(_selectedSegment.Geometry as Polygon);
                        await GetClosestVideoGraphic(centerPoint, currentSurvey);
                    }
                }
            }
            else if (hotkey == "x")
            {
                if (_selectedSegment != null && !IsChangingSegments)
                {
                    IsChangingSegments = true;
                    string currentSurvey = _selectedSegment.Attributes["SurveyId"].ToString();
                    int currentSectionId = Convert.ToInt32(_selectedSegment.Attributes["SectionId"]);

                    Graphic nextSegment = FindSegmentGraphic(currentSurvey, currentSectionId, 1);
                    if (nextSegment != null)
                    {
                        await HandleSegmentClick(nextSegment);
                        Geometry nextSegmentGeometry = nextSegment.Geometry;

                        Envelope visibleAreaGeographic = (Envelope)GeometryEngine.Project(MyMapView.VisibleArea.Extent, SpatialReferences.Wgs84);
                        if (!visibleAreaGeographic.Intersects(nextSegmentGeometry.Extent))
                        {
                            MapPoint centerPoint = nextSegmentGeometry.Extent.GetCenter();
                            MyMapView.SetViewpoint(new Viewpoint(centerPoint.Y, centerPoint.X, MyMapView.MapScale));
                        }
                    }
                    await Task.Delay(200);
                    IsChangingSegments = false;

                    if (VideoPlayer.IsVisible)
                    {
                        var centerPoint = GeometryEngine.LabelPoint(_selectedSegment.Geometry as Polygon);
                        await GetClosestVideoGraphic(centerPoint, currentSurvey);
                    }
                }
            }
            else if (hotkey == "delete")
            {
                //Check segments to delete first
                if (_oldGraphics != null && _oldGraphics.Count > 0)
                {
                    HandleDeleteSegments(_oldGraphics);
                    MoveSegmentButton_Clicked(this, EventArgs.Empty);
                }
                else if (_selectedGraphic != null)
                {
                    var selectedGraphics = new List<Graphic>();
                    selectedGraphics.Add(_selectedGraphic);
                    HandleDeleteDefects(false, selectedGraphics);
                }
                else if (selectedGraphics != null && selectedGraphics.Count > 0)
                {
                    HandleDeleteDefects(false, selectedGraphics);
                }
                else if (_selectedSegment != null)
                {
                    HandleDeleteSegments(new List<Graphic> { _selectedSegment });
                }
            }
            else if (hotkey == "h")
            {
                if (_selectedSegment != null)
                {
                    _selectedSegment.IsVisible = false;
                }
            }
            else if (hotkey == "b")
            {
                GraphicsOverlay segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "Segment");
                if (segmentOverlay != null)
                {
                    foreach (var graphic in segmentOverlay.Graphics)
                    {
                        graphic.IsVisible = true;
                    }
                }
            }
            else if (hotkey == "enter")
            {
                if (IsProcessingMultiDefects && selectedGraphics != null && selectedGraphics.Count > 0)
                {
                    MoveAndSaveMultiDefects();
                    IsProcessingMultiDefects = false;
                }

                else if (IsSelectingLasRutting && !isProcessingLasRutting)
                {
                    isProcessingLasRutting = true;
                    DeactivateKeyboardHook(); // Disables hotkey inputs when typing

                    while (isProcessingLasRutting)
                    {
                        string input = await App.Current.MainPage.DisplayPromptAsync(
                            "Auto Repeat Mode",
                            "Please enter the spacing meters:",
                            "OK",
                            "Cancel",
                            keyboard: Microsoft.Maui.Keyboard.Numeric
                        );

                        if (input == null)
                        {
                            isProcessingLasRutting = false;
                            ActivateKeyboardHook();
                            return; // Exit if user clicks cancel
                        }

                        if (int.TryParse(input, out int spacingMeters) && spacingMeters > 0)
                        {
                            if (newOverlay != null)
                            {
                                MyMapView.GraphicsOverlays.Remove(newOverlay);
                                newOverlay = null;
                            }
                            AutoRepeatRutting(spacingMeters);
                            break;
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Invalid Input", "Please enter a valid positive number.", "OK");
                        }
                    }

                    isProcessingLasRutting = false;
                    ActivateKeyboardHook();
                    DeactivateAllMapButtons();
                    appState.UpdateTableNames();
                }

                else
                {
                    //moving segment
                    if (IsMovingSegments)
                    {
                        MoveAndSaveMultiSegments();
                        IsMovingSegments = false;

                        ClearMultiSelectSegments();
                    }
                    ////saving new table graphics
                    //else if (DrawingToolVisibility && isCreatingGraphics)
                    //{
                    //    if (!_geometryEditor.Geometry.IsEmpty)
                    //    {
                    //        SaveAddedDefect_Click(this, new EventArgs());
                    //    }
                    //}
                }
            }
            else if (hotkey == "m" && !IsProcessingMultiDefects)
            {
                DeactivateAllMapButtons();
                IsProcessingMultiDefects = true;
                if (!IsMovingSegments && !_geometryEditor.IsStarted)
                {
                    if (selectedGraphics != null)
                    {
                        selectedGraphics.Clear();
                    }
                    if (_selectedGraphic != null)
                    {
                        _selectedGraphic.IsSelected = false;
                        _selectedGraphic = null;
                        appState.FindGraphicInfo(null, null);
                    }
                    HandleMultiDefectsSelect();
                }

            }
            else if (hotkey == "escape")
            {
                DeactivateAllMapButtons();
            }
            else if (hotkey == "p")
            {
                ReprocessSegment();
            }
            else if (hotkey == "g")
            {
                if (IsSelectingCracks && !isProcessingCracks)
                {
                    isProcessingCracks = true;
                    await HandleCrackEdit(LayerNames.CrackClassification);
                    isProcessingCracks = false;
                }
                else if (IsSelectingSummaryCracks && !isProcessingSummaryCracks)
                {
                    isProcessingSummaryCracks = true;
                    await HandleCrackEdit(LayerNames.CrackSummary);
                    isProcessingSummaryCracks = false;
                }
            }
            else if (hotkey == "c" && VideoPlayer.IsVisible)
            {
                PlayButton_Clicked(this, new EventArgs());
            }

        }
        catch (Exception ex)
        {
            Log.Error($"Exception in HandleHotkey: {ex.Message}");
        }
        finally
        {
            hasKeyTriggered = false; // Ensure this boolean is reset to false
        }
    }

    private async Task HandleCrackEdit(string editingLayer)
    {
        if (_oldGraphics != null && _oldGraphics.Any())
        {
            foreach (var graphic in _oldGraphics)
            {
                if (!appState.cracksEditIdsForProcessing.Contains(Convert.ToInt32(graphic.Attributes["Id"])))
                    appState.cracksEditIdsForProcessing.Add(Convert.ToInt32(graphic.Attributes["Id"]));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                EditCracksContainerPage.IsVisible = true;
                appState.ChangeCrackEdit(editingLayer);
            });
            appState.IsPopupOpen = true;
        }
        else
            await Application.Current.MainPage.DisplayAlert("Alert", $"Please select a {editingLayer.ToLower()} first.", "OK");
    }

    private async void MoveAndSaveMultiDefects()
    {
        IsProcessingMultiDefects = true;
        messageBoxDefects.IsVisible = false;
        IsEditingDatabase = true;
        await Task.Delay(100);

        //move defects on the map
        var updatedGeometry = _geometryEditor.Geometry;
        if (updatedGeometry == null || updatedGeometry.IsEmpty || !(updatedGeometry is Polygon))
        {
            Log.Error("Updated geometry is invalid");
            return;
        }

        var parts = (updatedGeometry as Polygon).Parts;
        var DefectMovementRequests = new List<DefectMovementRequest>(selectedGraphics.Count());
        int i = 0;
        var existingDefects = new HashSet<string>();

        foreach (var updatedPart in parts)
        {
            if (i >= selectedGraphics.Count)
            {
                Log.Error("More parts than selected graphics.");
                break;
            }
            var originalGraphic = selectedGraphics[i++];
            var originalGeometry = originalGraphic.Geometry;

            var table = originalGraphic.Attributes.ContainsKey("Table") ? originalGraphic.Attributes["Table"]?.ToString() : null;
            var idObj = originalGraphic.Attributes.ContainsKey("Id") ? originalGraphic.Attributes["Id"] : null;
            if (table == null || idObj == null) continue;

            var tableIdObj = originalGraphic.Attributes.ContainsKey("TableId") ? originalGraphic.Attributes["TableId"] : null;
            var type = originalGraphic.Attributes.ContainsKey("Type") ? originalGraphic.Attributes["Type"].ToString() : null;
            if (type != null)
            {
                switch (type)
                {
                    case "MetaTable":
                        table = "MetaTableValue";
                        break;
                    case "PCIDefect":
                        table = "PCIDefects";
                        break;
                }
            }
            else
            {
                if (table.Contains("IRI"))
                {
                    table = LayerNames.Roughness;
                }
                else if (table.Contains("Rut"))
                {
                    table = LayerNames.Rutting;
                }
            }

            if (originalGeometry is Polygon originalPolygon)
            {
                double[] offset = CalculateOffsetWithGeometry(originalGraphic.Geometry, new Polygon(updatedPart));
                if (offset[0] == 0 && offset[1] == 0) continue;

                // Create a new polygon geometry for the graphic
                var newPolygon = new Polygon(updatedPart.Points, updatedGeometry.SpatialReference);
                originalGraphic.Geometry = newPolygon;

                if (table == LayerNames.CornerBreak)
                {
                    var idList = idObj.ToString().Split(',').Select(int.Parse);
                    foreach (var id in idList)
                    {
                        DefectMovementRequests.Add(new DefectMovementRequest { Id = id, Table = table, HorizontalOffset = offset[0], VerticalOffset = offset[1] });
                    }
                }
                else
                {
                    var id = (int)idObj;
                    DefectMovementRequests.Add(new DefectMovementRequest { Id = id, Table = table, HorizontalOffset = offset[0], VerticalOffset = offset[1] });
                }
            }
            else if (originalGeometry is Polyline originalPolyline)
            {
                double[] offset = CalculateOffsetWithGeometry(originalPolyline, new Polyline(updatedPart));
                if (offset[0] == 0 && offset[1] == 0) continue;

                var newPolyline = new Polyline(updatedPart.Points, updatedGeometry.SpatialReference);
                originalGraphic.Geometry = newPolyline;

                var id = (int)idObj;
                string key = $"{id}-{table}";

                if (!existingDefects.Contains(key))
                {
                    existingDefects.Add(key);
                    DefectMovementRequests.Add(new DefectMovementRequest { Id = id, Table = table, HorizontalOffset = offset[0], VerticalOffset = offset[1] });
                }
            }
            else if (originalGeometry is MapPoint originalPoint)
            {
                double[] offset = CalculateOffsetWithGeometry(originalPoint, updatedPart.Points.First());
                if (offset[0] == 0 && offset[1] == 0) continue;

                var newPoint = updatedPart.Points.First();
                originalGraphic.Geometry = newPoint;

                var id = (int)idObj;
                DefectMovementRequests.Add(new DefectMovementRequest { Id = id, Table = table, HorizontalOffset = offset[0], VerticalOffset = offset[1] });
            }
        }
        //update database
        if (DefectMovementRequests.Count > 0)
        {
            await appEngine.SegmentService.UpdateDefectsOnly(DefectMovementRequests);
        }

        // Force UI update
        await Task.Delay(50); // Small delay to ensure UI updates

        _geometryEditor.Stop();
        IsProcessingMultiDefects = false;
        IsEditingDatabase = false;
    }

    private double[] CalculateOffsetWithGeometry(Geometry oldGeometry, Geometry newGeometry)
    {
        MapPoint oldCentroid;
        MapPoint newCentroid;

        if (oldGeometry is Polygon && newGeometry is Polygon)
        {
            oldCentroid = GeometryEngine.LabelPoint(oldGeometry as Polygon) as MapPoint;
            newCentroid = GeometryEngine.LabelPoint(newGeometry as Polygon) as MapPoint;
        }
        else if (oldGeometry is Polyline oldPolyline && newGeometry is Polyline newPolyline)
        {
            oldCentroid = GetFirstPoint(oldPolyline);
            newCentroid = GetFirstPoint(newPolyline);
        }
        else if (oldGeometry is MapPoint oldMapPoint && newGeometry is MapPoint newMapPoint)
        {
            oldCentroid = oldMapPoint;
            newCentroid = newMapPoint;
        }
        else
        {
            throw new ArgumentException("Geometry types do not match or are not supported.");
        }

        double offsetX = newCentroid.X - oldCentroid.X;
        double offsetY = newCentroid.Y - oldCentroid.Y;

        return new double[] { offsetX, offsetY };
    }

    private MapPoint GetFirstPoint(Polyline polyline)
    {
        var points = polyline.Parts.SelectMany(part => part.Points).ToList();
        if (points.Count == 0)
        {
            throw new ArgumentException("Polyline has no points.");
        }

        return points.First();
    }

    private double CalculateTranslationIncrement(double scale)
    {
        return scale / 80;
    }

    private Graphic FindSegmentGraphic(string currentSurvey, int currentSegmentId, int increment)
    {
        GraphicsOverlay segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == LayerNames.Segment);
        if (segmentOverlay != null)
        {
            int segmentId = currentSegmentId + increment;
            var matchingGraphic = segmentOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("SurveyId") && g.Attributes["SurveyId"].Equals(currentSurvey) &&
               g.Attributes.ContainsKey("SectionId") && g.Attributes["SectionId"].Equals(segmentId));
            if (matchingGraphic != null)
            {
                return matchingGraphic;
            }
        }
        return null;
    }

    private void HandleMultiDefectsSelect()
    {
        messageBoxMultiSelect.IsVisible = true;
        var freeHandTool = new FreehandTool();
        freeHandTool.Style = new GeometryEditorStyle
        {
            LineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 1),
            FillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(80, System.Drawing.Color.Cyan), new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Gray, 1)),
            VertexSymbol = null,
            SelectedVertexSymbol = null,
            FeedbackLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Cyan, 2),
            FeedbackVertexSymbol = null,
        };
        _geometryEditor.Tool = freeHandTool;

        if (!_geometryEditor.IsStarted)
        {
            _geometryEditor.Start(GeometryType.Polygon);
            selectedGraphics?.Clear();
            selectedGraphics = new List<Graphic>();
            _geometryEditor.PropertyChanged += MultiSelectDefects;
        }
    }

    private async void MultiSelectDefects(object sender, EventArgs e)
    {
        Geometry rectangleGeometry = _geometryEditor.Geometry;
        if (rectangleGeometry == null || rectangleGeometry.IsEmpty || !(rectangleGeometry is Polygon))
        {
            return;
        }
        var selectableTables = appState.selectableTables;

        List<Part> parts = new List<Part>();
        List<GraphicsOverlay> selectableOverlays = GetSelectableOverlays(selectableTables);
        var graphicsCopy = new ConcurrentBag<Graphic>();

        // Gather all graphics from the selected overlays
        foreach (var overlay in selectableOverlays)
        {
            if (overlay.IsVisible)
            {
                foreach (var graphic in overlay.Graphics)
                {
                    graphicsCopy.Add(graphic);
                }
            }
        }

        var spatialReference = MyMapView.SpatialReference;

        if (spatialReference != null)
        {
            var projectedRectangleGeometry = GeometryEngine.Project(rectangleGeometry, spatialReference);
            try
            {
                var projectedGraphics = graphicsCopy.Select(graphic =>
                {
                    var projectedGeometry = GeometryEngine.Project(graphic.Geometry, spatialReference);
                    return new { Graphic = graphic, ProjectedGeometry = projectedGeometry };
                });

                // Use a spatial query to find graphics within the rectangle area
                var graphicsInRectangle = projectedGraphics.Where(item =>
                    GeometryEngine.Intersects(projectedRectangleGeometry, item.ProjectedGeometry))
                    .Select(item => item.Graphic).ToList();

                if (graphicsInRectangle != null && graphicsInRectangle.Count > 0)
                {
                    // Store the selected graphics
                    selectedGraphics.AddRange(graphicsInRectangle);
                    UpdateGeometryEditor(selectedGraphics);

                    messageBoxMultiSelect.IsVisible = false;
                    messageBoxDefects.IsVisible = true;
                }
                else
                {
                    _geometryEditor.PropertyChanged -= MultiSelectDefects;
                    _geometryEditor.Stop();
                    messageBoxMultiSelect.IsVisible = false;
                    IsProcessingMultiDefects = false;
                }
            }
            catch (Exception ex)
            {
                selectedGraphics?.Clear();
                _geometryEditor.PropertyChanged -= MultiSelectDefects;
                _geometryEditor.Stop();
                Log.Error($"error in selecting graphics: {ex.Message}");
            }
        }
    }

    private void UpdateGeometryEditor(List<Graphic> selectedGraphics)
    {
        List<Part> parts = new List<Part>();
        var processedRoughnessIds = new HashSet<object>();
        var processedRutIds = new HashSet<object>();
        var overlayDict = MyMapView.GraphicsOverlays.ToDictionary(o => o.Id, o => o);
        var additionalGraphics = new List<Graphic>();
        var tables = selectedGraphics
        .Where(g => g.Attributes.TryGetValue("Table", out var t) && t is string)
        .GroupBy(g => g.Attributes["Table"] as string);

        foreach (var graphics in tables) // gets grouped graphics belonging to each table
        {
            var tableName = graphics.Key;
            if (!overlayDict.TryGetValue(tableName, out var overlay)) continue;

            foreach (var graphic in graphics)
            {
                var relatedGraphics = new List<Graphic>();
                graphic.Attributes.TryGetValue("SurveyId", out var surveyId);
                graphic.Attributes.TryGetValue("SegmentId", out var segmentId);

                // Macro Texture
                if (tableName == MultiLayerName.BandTexture && graphic.Attributes.TryGetValue("TextureId", out var textureId))
                {
                    relatedGraphics = AddMatchingGraphics(additionalGraphics, overlay.Graphics, "TextureId", textureId, surveyId, segmentId);
                }
                // Bleeding
                else if (tableName == LayerNames.Bleeding && graphic.Attributes.TryGetValue("BleedingId", out var bleedingId))
                {
                    relatedGraphics = AddMatchingGraphics(additionalGraphics, overlay.Graphics, "BleedingId", bleedingId, surveyId, segmentId);
                }
                // Multilayer IRI
                else if (tableName.Contains("IRI") && graphic.Attributes.TryGetValue("RoughnessId", out var roughnessId))
                {
                    if (processedRoughnessIds.Add(roughnessId))
                    {
                        foreach (var iriOverlay in overlayDict.Values.Where(o => o.Id.Contains("IRI")))
                        {
                            var matches = AddMatchingGraphics(additionalGraphics, iriOverlay.Graphics, "RoughnessId", roughnessId, surveyId, segmentId);
                            relatedGraphics.AddRange(matches);
                        }
                    }
                }
                // Multilayer Rut
                else if (tableName.Contains("Rut") && graphic.Attributes.TryGetValue("RutId", out var rutId))
                {
                    if (processedRutIds.Add(rutId))
                    {
                        foreach (var rutOverlay in overlayDict.Values.Where(o => o.Id.Contains("Rut")))
                        {
                            var matches = AddMatchingGraphics(additionalGraphics, rutOverlay.Graphics, "RutId", rutId, surveyId, segmentId);
                            relatedGraphics.AddRange(matches);
                        }
                    }
                }
                // Adds graphics to graphic list
                if (relatedGraphics != null && relatedGraphics.Any())
                {
                    additionalGraphics.AddRange(relatedGraphics);
                }
            }
        }

        var selectedSet = new HashSet<Graphic>(selectedGraphics);

        foreach (var graphic in additionalGraphics)
        {
            if (selectedSet.Add(graphic))
            {
                selectedGraphics.Add(graphic);
            }
        }

        // Adds parts to list.
        parts = AddParts(parts, selectedGraphics);
        _geometryEditor.PropertyChanged -= MultiSelectDefects;
        _geometryEditor.Stop();

        if (parts.Count > 0)
        {
            SetMoveOnlyGeometryEditorStyle();
            var combinedPolygon = new Polygon(parts);
            _geometryEditor.Start(combinedPolygon);
            _geometryEditor.SelectGeometry();
        }
    }

    // Adds multi selected defect parts to a part list
    private List<Part> AddParts(List<Part> parts, List<Graphic> selectedGraphics)
    {
        foreach (var graphic in selectedGraphics)
        {
            var spatialRef = graphic.Geometry?.SpatialReference;
            if (spatialRef == null) continue;

            if (graphic.Geometry is Polygon polygon)
            {
                foreach (var part in polygon.Parts)
                {
                    var newPart = new Part(spatialRef);
                    foreach (var point in part.Points)
                    {
                        newPart.AddPoint(point);
                    }
                    parts.Add(newPart);
                }
            }
            else if (graphic.Geometry is Polyline polyline)
            {
                foreach (var part in polyline.Parts)
                {
                    var newPart = new Part(spatialRef);
                    foreach (var point in part.Points)
                    {
                        newPart.AddPoint(point);
                    }
                    parts.Add(newPart);
                }
            }
            else if (graphic.Geometry is MapPoint mapPoint)
            {
                var newPart = new Part(spatialRef);
                newPart.AddPoint(mapPoint);
                parts.Add(newPart);
            }
        }
        return parts;
    }
    private List<Graphic> AddMatchingGraphics(List<Graphic> graphics, IEnumerable<Graphic> overlayGraphics, string key, object id, object surveyId, object segmentId)
    {
        var relatedGraphics = overlayGraphics.Where(g =>
                                 g.Attributes.TryGetValue(key, out var k) && Equals(k, id) &&
                                 g.Attributes.TryGetValue("SurveyId", out var otherSurveyId) && Equals(otherSurveyId, surveyId) &&
                                 g.Attributes.TryGetValue("SegmentId", out var otherSegmentId) && Equals(otherSegmentId, segmentId)).ToList();

        return relatedGraphics;
    }

    private List<GraphicsOverlay> GetSelectableOverlays(List<string> selectableTables)
    {
        if (selectableTables == null || selectableTables.Count == 0)
        {
            return MyMapView.GraphicsOverlays
                .Where(overlay => overlay.Id != "Segment" && overlay.Id != "Boundaries" && !overlay.Id.EndsWith("Sample Unit") && overlay.Id != "surveySetOverlay")
                .ToList();
        }
        var selectedTables = new List<string>(selectableTables);

        foreach (var mapping in TableNameHelper.MultiLayerNameMappings)
        {
            if (selectedTables.Contains(mapping.Key))
            {
                selectedTables.Remove(mapping.Key);
                selectedTables.AddRange(mapping.Value); // Add the corresponding layers
            }
        }

        return selectedTables
            .Select(name => MyMapView.GraphicsOverlays.FirstOrDefault(overlay => overlay.Id == name))
            .Where(graphicsOverlay => graphicsOverlay != null)
            .ToList();
    }

    private async void HandleDeleteDefects(bool ctrl, List<Graphic> selectedGraphics)
    {
        try
        {
            if (isDeletingDefects)
            {
                return;
            }

            IsProcessingMultiDefects = false;
            isDeletingDefects = true;

            // Create a copy of the selectedGraphics list
            var graphicsToDelete = new List<Graphic>(selectedGraphics);

            if (graphicsToDelete.Count > 0)
            {
                bool result = false;

                if (!ctrl)
                {
                    result = await App.Current.MainPage.DisplayAlert("Confirmation", $"Are you sure you want to remove {graphicsToDelete.Count} selected item(s) from the map?", "Yes", "No");
                }
                else
                {
                    result = true;
                }

                if (result)
                {
                    var deletedGraphicsOverlay = graphicsToDelete.Select(g => g.GraphicsOverlay?.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

                    await DeleteDefectsWithQuery(graphicsToDelete);

                    await viewModel.CheckTablesForNoDefects(deletedGraphicsOverlay);
                }

                if (_selectedGraphic != null)
                {
                    _selectedGraphic.IsSelected = false;
                    _selectedGraphic = null;
                    appState.FindGraphicInfo(null, null);
                }
                else if (selectedGraphics.Count > 0)
                {
                    selectedGraphics.Clear();
                }

                if (_geometryEditor.IsStarted)
                {
                    _geometryEditor.Stop();
                }
                messageBoxDefects.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        finally
        {
            isDeletingDefects = false;
        }
    }

    #endregion

    #region MapTapped
    List<MapPoint> multiPoints;
    MapPoint lasStartPoint;
    MapPoint lasEndPoint;
    private bool IsPolylineActive = false;
    private bool IsPolygonActive = false;

    public event EventHandler<QCMapData> MapAreaChangedEvent;
    public Graphic previousGraphic;
    public Graphic previousSurveySetGraphic;
    public List<Graphic> previousSegmentsSurveySetGraphics = new List<Graphic>();
    private void SetDefaultGeometryEditorStyle()
    {
        var cyanOutline = new SolidStrokeSymbolLayer(6, System.Drawing.Color.Cyan);
        var grayStroke = new SolidStrokeSymbolLayer(2, System.Drawing.Color.Gray);

        //Geometry Editor style
        GeometryEditorStyle geometryEditorStyle = new GeometryEditorStyle
        {
            MidVertexSymbol = null,
            SelectedMidVertexSymbol = null,
            LineSymbol = new MultilayerPolylineSymbol(new List<SymbolLayer> { cyanOutline, grayStroke }),
            FillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(180, System.Drawing.Color.Cyan), new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Aqua, 1)),
            VertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.OrangeRed, 15),
            SelectedVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.OrangeRed, 15),
            FeedbackLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.DarkOrange, 2),
            FeedbackVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.FromArgb(255, 79, 0), 10)
        };
        _geometryEditor.Tool = new VertexTool();
        _geometryEditor.Tool.Style = geometryEditorStyle;
    }

    public void HideMeasurementCallout()
    {
        if (MyMapView.IsCalloutVisible)
        {
            MyMapView.DismissCallout();
        }

        GraphicsOverlay pinOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "pinOverlay");
        if (pinOverlay != null)
        {
            pinOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(pinOverlay);
            //    MySearchBar.Text = string.Empty;
        }

        //remove text layouts from map SurveySegmentation
        GraphicsOverlay ssOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "Survey Segmentation");
        if (ssOverlay != null)
        {
            var textGraphics = ssOverlay.Graphics.Where(g => g.Symbol is TextSymbol).ToList();
            foreach (var graphic in textGraphics)
            {
                ssOverlay.Graphics.Remove(graphic);
            }
        }
    }

    private void LasFileButton_Clicked(Object sender, EventArgs e)
    {
        DeactivateAllMapButtons();

        if (!IsLasFilesMode)
        {
            IsLasFilesMode = true;

            LasFileButton.Background = Color.FromArgb("#FFFFFF");  // Active button style
            lasLinesOverlay = new GraphicsOverlay
            {
                Id = "lasLinesOverlay"
            };
            MyMapView.GraphicsOverlays.Add(lasLinesOverlay);

            GraphicsOverlay lasRutOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "LasRutting");
            if (lasRutOverlay != null)
                AutoRepeatLasButton.IsVisible = true;
        }
    }

    private void AutoRepeatLasButton_Clicked(Object sender, EventArgs e)
    {
        if (!IsSelectingLasRutting)
        {
            DeactivateAllMapButtons(true);
            IsSelectingLasRutting = true;
            SelectMapArea();
            AutoRepeatLasButton.Background = Color.FromArgb("#FFFFFF");

        }
        else
        {
            ClearAutoRepeatRut();
        }
    }

    private void ClearAutoRepeatRut()
    {
        AutoRepeatLasButton.Background = Color.FromArgb("#90D3D3D3");
        IsSelectingLasRutting = false;
        _geometryEditor.Stop();
        _geometryEditor.PropertyChanged -= MultiSelectLasRutting;
        _oldGraphics.Clear();
        previousGraphics.Clear();

        if (newOverlay != null)
        {
            MyMapView.GraphicsOverlays.Remove(newOverlay);
            newOverlay = null;
        }
    }
    private MapPoint CheckPointInsidePolygon(MapPoint point, string groupId, bool autoRut = false)
    {
        point = (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);

        if (viewModel.lasPointOverlay.Graphics.Any())
        {
            bool isInside = viewModel.lasPointOverlay.Graphics.Any(g =>
                g.Geometry is Polygon poly && GeometryEngine.Contains(poly, point)); // Check if point is inside a polygon

            if (!isInside)
            {
                return AdjustPointToNearbyPolygon(point, groupId, autoRut);
            }
        }
        return point;
    }

    private MapPoint AdjustPointToNearbyPolygon(MapPoint point, string groupId, bool autoRut)
    {
        double searchRadius = 0.2;
        var buffer = GeometryEngine.BufferGeodetic(point, searchRadius, LinearUnits.Meters);
        var nearbyPolygons = viewModel.lasPointOverlay.Graphics.Where(g => g.Geometry is Polygon poly && GeometryEngine.Intersects(buffer, poly)).ToList();
        Graphic closest = null;

        if (nearbyPolygons.Any())
        {
            // Finds closest polygon relative to the point.
            closest = nearbyPolygons.OrderBy(g => GeometryEngine.Distance(point, g.Geometry.Extent.GetCenter())).First();
            point = GeometryEngine.NearestCoordinate(closest.Geometry, point).Coordinate;
        }

        if (closest != null && GeometryEngine.Intersects(buffer, closest.Geometry))
        {
            var polygonMaxMinX = closest.Geometry.Extent;
            double newX = point.X;

            if (point.X > polygonMaxMinX.XMax)
                newX = polygonMaxMinX.XMax;
            else if (point.X < polygonMaxMinX.XMin)
                newX = polygonMaxMinX.XMin;

            point = new MapPoint(newX, point.Y, point.SpatialReference);
        }

        if (!autoRut)
            AdjustLasOverlayGraphic(point, groupId);

        return point;
    }

    private void AdjustLasOverlayGraphic(MapPoint point, string groupId)
    {
        // Searches and deletes old graphic from overlay
        var originalGraphic = lasLinesOverlay.Graphics.FirstOrDefault(g =>
            g.Attributes.ContainsKey("GroupId") && g.Attributes["GroupId"].ToString() == groupId);

        if (originalGraphic != null)
            lasLinesOverlay.Graphics.Remove(originalGraphic);

        // Adds adjusted graphic to overlay
        var adjustedGraphic = new Graphic(point, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 10));
        adjustedGraphic.Attributes["GroupId"] = groupId;
        lasLinesOverlay.Graphics.Add(adjustedGraphic);
    }

    private async Task HandleLasModeClick(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        try
        {
            MapPoint clickedPoint = MyMapView.ScreenToLocation(e.Position);
            var groupId = Guid.NewGuid().ToString(); // Generate a unique ID for the group

            MapPoint projectedStartPoint;
            MapPoint projectedEndPoint;

            if (lasStartPoint == null)
            {
                // Start Point
                lasStartPoint = clickedPoint;
                var startPointGraphic = new Graphic(lasStartPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 10));
                startPointGraphic.Attributes["GroupId"] = groupId;
                lasLinesOverlay.Graphics.Add(startPointGraphic);
            }
            else if (lasEndPoint == null && clickedPoint != lasStartPoint)
            {
                // End Point
                lasEndPoint = clickedPoint;
                var endPointGraphic = new Graphic(lasEndPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 10));
                endPointGraphic.Attributes["GroupId"] = groupId;
                lasLinesOverlay.Graphics.Add(endPointGraphic);
            }

            // Checks if las points are in between the las file polygon gaps, move graphic to closest polygon if true.
            projectedStartPoint = CheckPointInsidePolygon(lasStartPoint, groupId);
            projectedEndPoint = CheckPointInsidePolygon(lasEndPoint, groupId);

            // Polyline
            var initialPolyline = new Polyline(new List<MapPoint> { projectedStartPoint, projectedEndPoint });
            var initialPolylineGraphic = new Graphic(initialPolyline, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Blue, 1.0));
            lasLinesOverlay.Graphics.Add(initialPolylineGraphic);

            var intersectingPolygons = new List<Graphic>();
            var projectedLine = (Polyline)GeometryEngine.Project(initialPolyline, viewModel.lasPointOverlay.Graphics.FirstOrDefault()?.Geometry?.SpatialReference);


            // Find the las polygons for faster filtering  
            foreach (var graphic in viewModel.lasPointOverlay.Graphics)
            {
                if (graphic.Geometry is Polygon polygon && GeometryEngine.Intersects(projectedLine, polygon))
                {
                    intersectingPolygons.Add(graphic);
                }
            }

            // Extract LASfileId from the attributes of intersecting polygons
            var lasFileIds = intersectingPolygons
                .Select(graphic => graphic.Attributes.ContainsKey("LASfileId") ? graphic.Attributes["LASfileId"] : null)
                .Where(id => id != null)
                .Cast<int>()
                .ToList();


            double straightEdgeLength = GeometryEngine.DistanceGeodetic(projectedStartPoint, projectedEndPoint,
             LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;

            var requestpoints = new Core.Models.Other.PointsRequest
            {
                X1 = projectedStartPoint.X,
                Y1 = projectedStartPoint.Y,
                X2 = projectedEndPoint.X,
                Y2 = projectedEndPoint.Y,
                LASFileIds = lasFileIds  //filtering by id the polygons
            };


            // Fetch LAS points along the line
            var points = await appEngine.LASfileService.GetPointsAlongLineAsync(requestpoints);
            if (points.Count == 0)
            {
                // Clear points
                lasStartPoint = null;
                lasEndPoint = null;
                return;
            }

            // Draw the final polyline and points
            var firstPoint = new MapPoint(points.First().X, points.First().Y, SpatialReferences.Wgs84);
            var lastPoint = new MapPoint(points.Last().X, points.Last().Y, SpatialReferences.Wgs84);
            var finalPolyline = new Polyline(new List<MapPoint> { firstPoint, lastPoint });
            var finalPolylineGraphic = new Graphic(finalPolyline, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.PeachPuff, 1.0));
            finalPolylineGraphic.Attributes["GroupId"] = groupId;
            lasLinesOverlay.Graphics.Add(finalPolylineGraphic);
            foreach (var point in points)
            {
                var lasmapPoint = new MapPoint(point.X, point.Y, SpatialReferences.Wgs84);
                var pointGraphic = new Graphic(lasmapPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.PeachPuff, 5));
                lasLinesOverlay.Graphics.Add(pointGraphic);
            }

            DisplayLASPointsChart(points, "");

            // Clear points
            lasStartPoint = null;
            lasEndPoint = null;
            intersectingPolygons = new List<Graphic>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task HandleKeycodePointClick(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        try
        {
            MapPoint clickedPoint = MyMapView.ScreenToLocation(e.Position);
            var startPointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 10));
            var newOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");
            if (newOverlay == null)
            {
                newOverlay = new GraphicsOverlay { Id = "drawingDefectOverlay" };
                MyMapView.GraphicsOverlays.Add(newOverlay);
            }

            newOverlay.Graphics.Add(startPointGraphic);
            newOverlay.IsVisible = true; 

        }

        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }


    private List<MapPoint> clickedPoints = new List<MapPoint>();

    private async Task HandleKeycodeLineClick(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {

        try
        {
            // Convert screen position to map location
            MapPoint clickedPoint = MyMapView.ScreenToLocation(e.Position);

            var newOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");

            // If the overlay does not exist, create it
            if (newOverlay == null)
            {
                newOverlay = new GraphicsOverlay { Id = "drawingDefectOverlay" };
                MyMapView.GraphicsOverlays.Add(newOverlay);
            }

            // Determine color based on the number of clicks
            System.Drawing.Color pointColor;
            if (clickedPoints.Count == 0)
            {
                pointColor = System.Drawing.Color.FromArgb(255, 0, 255, 0); // First click: green
                                                                            // Create the graphic for the clicked point
                var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, pointColor, 10));
                newOverlay.Graphics.Add(pointGraphic);
                // Add the clicked point to the list
                clickedPoints.Add(clickedPoint);


            }
            else if (clickedPoints.Count == 1)
            {
                pointColor = System.Drawing.Color.FromArgb(255, 255, 0, 0); // Second click: red
                var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, pointColor, 10));
                newOverlay.Graphics.Add(pointGraphic);
                // Add the clicked point to the list
                clickedPoints.Add(clickedPoint);

                // Create polyline geometry
                var geometry = new Polyline(clickedPoints);
                var lineSymbol = new SimpleLineSymbol()
                {
                    Style = SimpleLineSymbolStyle.Solid,
                    Color = System.Drawing.Color.FromArgb(255, 0, 0, 150),
                    Width = 2.0
                };

                // Create graphic for the line
                var lineGraphic = new Graphic(geometry, lineSymbol);

                newOverlay.Graphics.Add(lineGraphic);


            }
            else
            {
                return; // More than two points; exit the method
            }

           
            // Add the point graphic to the overlay
            newOverlay.IsVisible = true;


            // Clear the list if it reaches more than two points
            if (clickedPoints.Count >= 2)
            {
                clickedPoints.Clear();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }


    private async void MultiSelectLasRutting(object sender, EventArgs e)
    {
        _geometryEditor.PropertyChanged -= MultiSelectLasRutting;
        var rectangleGeometry = _geometryEditor.Geometry;
        GraphicsOverlay lasRutOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "LasRutting");

        if (rectangleGeometry == null || rectangleGeometry.IsEmpty || !(rectangleGeometry is Polygon))
        {
            return;
        }

        if (rectangleGeometry != null && rectangleGeometry is Polygon && lasRutOverlay != null)
        {
            List<Graphic> selectedGraphics = new List<Graphic>(); // List to store selected graphics
            List<Geometry> selectedGeometries = new List<Geometry>(); // List to store geometries of selected graphics
            List<Part> parts = new List<Part>(); // List to store parts of selected graphics

            // Step 1: Get Selected graphics with the rectangle 
            try
            {
                // Obtain the spatial reference from the MapView
                var spatialReference = MyMapView.SpatialReference;
                if (spatialReference != null)
                {
                    var projectedRectangleGeometry = GeometryEngine.Project(rectangleGeometry, spatialReference);

                    var projectedGraphics = lasRutOverlay.Graphics
                         .Where(graphic => graphic.IsVisible)
                         .Select(graphic =>
                         {
                             var projectedGeometry = GeometryEngine.Project(graphic.Geometry, spatialReference);
                             return new { Graphic = graphic, ProjectedGeometry = projectedGeometry };
                         });

                    // Use a spatial query to find graphics within the rectangle area
                    var graphicsInRectangle = projectedGraphics.Where(item =>
                        GeometryEngine.Intersects(projectedRectangleGeometry, item.ProjectedGeometry))
                        .Select(item => item.Graphic);

                    if (!graphicsInRectangle.Any())
                    {
                        IsMovingSegments = false;
                        MoveSegmentsButton.Background = Color.FromArgb("#90D3D3D3");
                        _geometryEditor.Stop();
                        await Task.Delay(100);
                        SelectMapArea();
                        return;
                    }

                    HandleMultipleSegmentSelection(graphicsInRectangle, previousGraphics);//highlights segments 
                                                                                          // Store the selected graphics
                    selectedGraphics.AddRange(graphicsInRectangle);
                    selectedGeometries.AddRange(graphicsInRectangle.Select(graphic => graphic.Geometry));

                    Log.Information("Starting selection of segments inside free hand tool");
                    // Create parts for each selected graphic
                    foreach (var graphic in graphicsInRectangle)
                    {
                        if (graphic.Geometry is Polyline polyline)
                        {

                            foreach (var part in polyline.Parts)
                            {
                                // Convert ReadOnlyPart to Part
                                var newPart = new Part(graphic.Geometry.SpatialReference);
                                foreach (var point in part.Points)
                                {
                                    newPart.AddPoint(point);
                                }
                                parts.Add(newPart);
                            }
                        }
                    }
                    Log.Information("Finished selection of segments inside free hand tool");
                }
                else
                {
                    Console.WriteLine("Spatial Reference is null.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the spatial query: {ex.Message}");
            }

            Log.Information("GeometryEditor Removing ProcessGraphicsInRectanle ");
            _geometryEditor.PropertyChanged -= MultiSelectLasRutting;
            _geometryEditor.Stop();
            _oldGraphics = selectedGraphics;

            Log.Information("GeometryEditor oldGraphics Selected Graphics -- Combined Polygon");
            //Step 2: Get the new location for the lines by joining them into one polygon to start graphic editor 
            var combinedPolygon = new Polyline(parts);

            Log.Information("Before SetMoveOnlyGeometryEditorStyle()");

            SetMoveOnlyGeometryEditorStyle();

            Log.Information("After SetMoveOnlyGeometryEditorStyle()");
            _geometryEditor.Start(combinedPolygon);

            //Including movement indicator:
            _geometryEditor.PropertyChanged += GeometryEditor_PropertyChanged;

            //IsMovingSegments = true;
            if (_geometryEditor?.Geometry != null && !_geometryEditor.Geometry.IsEmpty)
            {
                _geometryEditor.SelectGeometry();
            }

        }
    }
    //HashSet<Part> adjustedParts = new HashSet<Part>();
    private async void AutoRepeatRutting(double spacingMeters)
    {
        if (_geometryEditor.Geometry == null)
        {
            _geometryEditor.Stop();
            return;
        }

        // Get updated geometry
        Geometry updatedGeometry = _geometryEditor.Geometry;
        List<Part> updatedParts = new List<Part>();
        List<List<Part>> intermediateParts = new List<List<Part>>(); // Holds all intermediate lines

        if (updatedGeometry is Polyline polyline && polyline.Parts.Any())
        {
            foreach (var part in polyline.Parts)
            {
                var newPart = new Part(updatedGeometry.SpatialReference);
                newPart.AddPoints(part.Points); // Copy existing points
                updatedParts.Add(newPart);
            }
        }
        else
        {
            _geometryEditor.Stop();
            return; // Exit if no valid parts exist
        }

        _geometryEditor.Stop();

        // Find the graphics overlay
        GraphicsOverlay lasRutOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "LasRutting");

        if (lasRutOverlay == null)
        {
            await Application.Current.MainPage.DisplayAlert("Alert", "Please calculate a rut first.", "OK");
            return;
        }

        // Convert _oldGraphics to polylines
        List<Polyline> oldPolylines = _oldGraphics
            .Where(g => g.Geometry is Polyline)
            .Select(g => (Polyline)g.Geometry)
            .ToList();

        // Add original, intermediate, and duplicated parts as graphics
        for (int i = 0; i < updatedParts.Count; i++)
        {
            Polyline newPolyline = new Polyline(new List<Part> { updatedParts[i] }, updatedGeometry.SpatialReference);
            var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 2);
            var lineGraphic = new Graphic(newPolyline, lineSymbol);
            //lasRutOverlay.Graphics.Add(lineGraphic);

            // Check if a corresponding old polyline exists
            if (i < oldPolylines.Count)
            {
                var originalPart = oldPolylines[i].Parts.FirstOrDefault();
                var copiedPart = updatedParts[i];

                if (originalPart != null && copiedPart != null && originalPart.Points.Count == copiedPart.Points.Count)
                {
                    Part originalMutablePart = new Part(originalPart.SpatialReference);
                    originalMutablePart.AddPoints(originalPart.Points);
                    int numLines = (int)(CalculateDistance(originalMutablePart, copiedPart) / spacingMeters);

                    for (int k = 1; k <= numLines + 1; k++)
                    {
                        double ratio = (double)k / (numLines + 1);
                        var middlePart = new Part(updatedGeometry.SpatialReference);

                        for (int j = 0; j < originalPart.Points.Count; j++)
                        {
                            MapPoint originalPoint = originalPart.Points[j];
                            MapPoint copiedPoint = copiedPart.Points[j];

                            double adjustX = 0;

                            //if (!adjustedParts.Contains(copiedPart))
                            //  adjustX = (j == 0) ? -0.00001 : 0.00001;

                            // Compute an interpolated point at `ratio` of the way
                            MapPoint intermediatePoint = new MapPoint(
                                    (originalPoint.X + (copiedPoint.X - originalPoint.X) * ratio) + adjustX,
                                    originalPoint.Y + (copiedPoint.Y - originalPoint.Y) * ratio,
                                    updatedGeometry.SpatialReference
                                );

                            middlePart.AddPoint(intermediatePoint);
                        }

                        // Store intermediate parts
                        if (intermediateParts.Count < k) intermediateParts.Add(new List<Part>());
                        intermediateParts[k - 1].Add(middlePart);
                    }
                    //if (!adjustedParts.Contains(copiedPart))
                    //  adjustedParts.Add(copiedPart);
                }
            }
        }

        // Add intermediate lines to overlay
        for (int k = 0; k < intermediateParts.Count; k++)
        {
            foreach (var part in intermediateParts[k])
            {
                Polyline middlePolyline = new Polyline(new List<Part> { part }, updatedGeometry.SpatialReference);
                var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 2);
                var middleGraphic = new Graphic(middlePolyline, lineSymbol);
                //lasRutOverlay.Graphics.Add(middleGraphic);
                try
                {
                    await SaveAutoRepeatRutting(middleGraphic, 0); //at this point id is 0 because is new
                }
                catch (Exception ex)
                {
                    // Log the error (optional)
                    Console.WriteLine($"Failed to save rutting for part: {ex.Message}");
                    // Continue with the next part
                }
            }
        }

        foreach (var polylineGraphic in polylineGraphics)
        {
            lasLinesOverlay.Graphics.Remove(polylineGraphic);
        }

        appState.ToggleLayers("LasRutting", true);
    }

    private List<Graphic> polylineGraphics = new List<Graphic>();

    private async Task SaveAutoRepeatRutting(Graphic RutLineGaphic , int rutId)
    {
        var rutGeom = RutLineGaphic.Geometry;
        if (rutGeom is Polyline polyline)
        {
            // Get the start and end points
            var linepart = polyline.Parts.FirstOrDefault();


            var groupId1 = Guid.NewGuid().ToString();
            var groupId2 = Guid.NewGuid().ToString();

            // Project points to WGS84
            var projectedStartPoint = CheckPointInsidePolygon(linepart.StartPoint, groupId1, true);
            var projectedEndPoint = CheckPointInsidePolygon(linepart.EndPoint, groupId2, true);

            // Polyline
            var initialPolyline = new Polyline(new List<MapPoint> { projectedStartPoint, projectedEndPoint });
            var polylineGraphic = new Graphic(initialPolyline, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Blue, 1.0));
            lasLinesOverlay.Graphics.Add(polylineGraphic);
            polylineGraphics.Add(polylineGraphic);

            var intersectingPolygons = new List<Graphic>();
            var projectedLine = (Polyline)GeometryEngine.Project(initialPolyline, viewModel.lasPointOverlay.Graphics.FirstOrDefault()?.Geometry?.SpatialReference);


            // Find the las polygons for faster filtering  
            foreach (var graphic in viewModel.lasPointOverlay.Graphics)
            {
                if (graphic.Geometry is Polygon polygon && GeometryEngine.Intersects(projectedLine, polygon))
                {
                    intersectingPolygons.Add(graphic);
                }
            }

            // Extract LASfileId from the attributes of intersecting polygons
            var lasFileIds = intersectingPolygons
                .Select(graphic => graphic.Attributes.ContainsKey("LASfileId") ? graphic.Attributes["LASfileId"] : null)
                .Where(id => id != null)
                .Cast<int>()
                .ToList();

            var requestpoints = new Core.Models.Other.PointsRequest
            {
                X1 = projectedStartPoint.X,
                Y1 = projectedStartPoint.Y,
                X2 = projectedEndPoint.X,
                Y2 = projectedEndPoint.Y,
                LASFileIds = lasFileIds, //filtering by id the polygons
                LasRuttingId = rutId
            };

            var result = await Task.Run(() => appEngine.LASfileService.GetPointsAndCalculateRutFromLine(requestpoints));
        }
    }


    private async void RecalculateRutting()
    {

        messageBoxRecalculateRutting.IsVisible = true;
        IsEditingDatabase = true;
        messageBoxUpdateProgress.IsVisible = false;
        await Task.Delay(100);
        lasLinesOverlay = new GraphicsOverlay
        {
            Id = "lasLinesOverlay"
        };
        MyMapView.GraphicsOverlays.Add(lasLinesOverlay);

        GraphicsOverlay lasRutOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "LasRutting");


        foreach (var lasrutGraphic in lasRutOverlay.Graphics.ToList())
        {
            var rutGeom = lasrutGraphic.Geometry;

            var lasRutId = lasrutGraphic.Attributes.ContainsKey("Id") ? (int)lasrutGraphic.Attributes["Id"] : 0;
            await SaveAutoRepeatRutting(lasrutGraphic, lasRutId);

        }

        await Application.Current.MainPage.DisplayAlert(
                   "ℹ️ Information",
                   "LAS Rutting recalculated successfully.",
                   "OK");

        appState.ToggleLayers("LAS_Rutting", true);
        IsEditingDatabase = false;
        messageBoxRecalculateRutting.IsVisible = false;

    }




    // Function to compute the average distance between two polylines
    private double CalculateDistance(Part original, Part copied)
    {
        double totalDistance = 0;
        int count = Math.Min(original.Points.Count, copied.Points.Count); // Ensure same number of points

        for (int i = 0; i < count; i++)
        {
            MapPoint originalPoint = original.Points[i];
            MapPoint copiedPoint = copied.Points[i];

            // Compute geodetic distance between corresponding points
            var result = GeometryEngine.DistanceGeodetic(originalPoint, copiedPoint,
                LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

            totalDistance += result.Distance;
        }

        return totalDistance / count; // Return average geodetic distance
    }


    private void MeasurementButton_Clicked(object sender, EventArgs e)
    {
        var clickedButton = sender as Microsoft.Maui.Controls.Button;
        if (clickedButton == null) return;

        bool isPolyline = clickedButton == PolylineMeasurementButton;
        bool isPolygon = clickedButton == PolygonMeasurementButton;
        bool status = false;
        if (isPolyline)
        {
            status = IsPolylineActive;
            DeactivateAllMapButtons();
            IsPolylineActive = !status;
        }
        else if (isPolygon)
        {
            status = IsPolygonActive;
            DeactivateAllMapButtons();
            IsPolygonActive = !status;
        }

        if (newOverlay != null)
        {
            newOverlay.Graphics.Clear();
            distanceLabelGraphic = null;
            polygonGraphic = null;
            measurementDistance = 0;
            MyMapView.GraphicsOverlays.Remove(newOverlay);
            MyMapView.DismissCallout();
        }

        PolylineMeasurementButton.Background = IsPolylineActive ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#90D3D3D3");
        PolygonMeasurementButton.Background = IsPolygonActive ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#90D3D3D3");

        if (IsPolylineActive || IsPolygonActive)
        {
            multiPoints = new List<MapPoint>();
            newOverlay = new GraphicsOverlay { Id = "newOverlay" };
        }
        else
        {
            multiPoints = null;
            messageBoxMeasurement.IsVisible = false;
            appState.PendingMeasurementField = null;
        }
    }

    private double measurementDistance = 0;
    private void PolylineMeasurement(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        MapPoint clickedPoint = MyMapView.ScreenToLocation(e.Position);
        if (clickedPoint != null)
        {
            if (multiPoints.Count == 0)
            {
                // Capture the start point
                multiPoints.Add(clickedPoint);

                var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 10));
                newOverlay.Graphics.Add(pointGraphic);
                MyMapView.GraphicsOverlays.Add(newOverlay);

                var startPoint = multiPoints[0];

                // Create temporary line
                var line = new Polyline(new List<MapPoint> { startPoint, startPoint });
                tempLineGraphic = new Graphic(line,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Yellow, 2));
                newOverlay.Graphics.Add(tempLineGraphic);

                // Create temporary cursor point graphic
                tempPointGraphic = new Graphic(clickedPoint,
                    new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 8));
                newOverlay.Graphics.Add(tempPointGraphic);

            }
            else
            {
                // Capture the end point
                multiPoints.Add(clickedPoint);
                var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 10));
                newOverlay.Graphics.Add(pointGraphic);

                // Draw a polyline between start and end points
                var polyline = new Polyline(multiPoints);
                var polylineGraphic = new Graphic(polyline, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Yellow, 1.0));
                newOverlay.Graphics.Add(polylineGraphic);

                measurementDistance = 0.0;
                // Calculate distance between points
                for (int i = 0; i < multiPoints.Count - 1; i++)
                {
                    var current = multiPoints[i];
                    var next = multiPoints[i + 1];
                    measurementDistance += GeometryEngine.DistanceGeodetic(current, next, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;
                }
                string distanceString = string.Format("{0:F2} m", measurementDistance);

                CalloutDefinition callout = new CalloutDefinition(distanceString);
                MyMapView.ShowCalloutAt(e.Location, callout);

                if (appState.PendingMeasurementField != null && appState.PendingMeasurementField.FieldName != null)
                {
                    appState.PendingMeasurementField.NewValue = Math.Round(measurementDistance, 2).ToString();
                }
            }
        }
    }

    private Graphic startTempLineGraphic;
    private void PolygonMeasurement(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        MapPoint clickedPoint = MyMapView.ScreenToLocation(e.Position);
        if (clickedPoint != null)
        {
            // Capture the start point
            multiPoints.Add(clickedPoint);

            var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 10));
            newOverlay.Graphics.Add(pointGraphic);

            if (!MyMapView.GraphicsOverlays.Contains(newOverlay))
            {
                MyMapView.GraphicsOverlays.Add(newOverlay);
            }

            if (multiPoints.Count == 1)
            {
                CreateTempGraphics(clickedPoint);

            }

            if (multiPoints.Count == 2)
            {
                // Create temporary dashed line between first and second point
                var baseLine = new Polyline(new List<MapPoint> { multiPoints[0], multiPoints[1] });
                startTempLineGraphic = new Graphic(baseLine,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Yellow, 2));
                newOverlay.Graphics.Add(startTempLineGraphic);
            }

            if (multiPoints.Count > 2)
            {
                UpdatePolygon(e, clickedPoint);
            }
        }
    }

    // Creates temporary dash graphics when using area tool.
    private void CreateTempGraphics(MapPoint clickedPoint)
    {
        // Create temporary line
        var line = new Polyline(new List<MapPoint> { multiPoints[0], multiPoints[0] });
        tempLineGraphic = new Graphic(line,
            new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Yellow, 2));
        newOverlay.Graphics.Add(tempLineGraphic);

        // Create temporary line
        tempLineTwoGraphic = new Graphic(line,
            new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Yellow, 2));
        newOverlay.Graphics.Add(tempLineTwoGraphic);

        // Create temporary cursor point graphic
        tempPointGraphic = new Graphic(clickedPoint,
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 8));
        newOverlay.Graphics.Add(tempPointGraphic);
    }

    private Graphic polygonGraphic;
    // Creates area polygon and label.
    private void UpdatePolygon(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e, MapPoint clickedPoint)
    {
        var polygon = new Polygon(multiPoints);

        // Remove baseline once polygon is made
        if (startTempLineGraphic != null)
        {
            newOverlay.Graphics.Remove(startTempLineGraphic);
            startTempLineGraphic = null;
        }

        if (polygonGraphic == null)
        {
            polygonGraphic = new Graphic(polygon,
                new SimpleFillSymbol(SimpleFillSymbolStyle.Solid,
                System.Drawing.Color.FromArgb(128, 255, 255, 0), null));
            newOverlay.Graphics.Add(polygonGraphic);
        }
        else
        {
            polygonGraphic.Geometry = polygon;
        }

        // Calculate area and display
        measurementDistance = GeometryEngine.AreaGeodetic(polygon, AreaUnits.SquareMeters, GeodeticCurveType.Geodesic);

        if (measurementDistance > 0)
        {
            string areaString = string.Format("{0:F2} m²", measurementDistance);
            CalloutDefinition callout = new CalloutDefinition(areaString);
            MyMapView.ShowCalloutAt(e.Location, callout);

            if (appState.PendingMeasurementField != null && appState.PendingMeasurementField.FieldName != null)
            {
                appState.PendingMeasurementField.NewValue = Math.Round(measurementDistance, 2).ToString();
            }
        }
    }
    private void EnableMeasurement(string type)
    {
        if (type == GeoType.Polyline.ToString())
        {
            if (IsPolylineActive && !messageBoxMeasurement.IsVisible)
            {
                IsPolylineActive = false;
            }
            MeasurementButton_Clicked((Microsoft.Maui.Controls.Button)PolylineMeasurementButton, new EventArgs());
            messageBoxMeasurement.IsVisible = IsPolylineActive;
        }
        else if (type == GeoType.Polygon.ToString())
        {
            if (IsPolygonActive && !messageBoxMeasurement.IsVisible)
            {
                IsPolygonActive = false;
            }
            MeasurementButton_Clicked((Microsoft.Maui.Controls.Button)PolygonMeasurementButton, new EventArgs());
            messageBoxMeasurement.IsVisible = IsPolygonActive;
        }
    }
    private Graphic tempLineGraphic;
    private Graphic tempLineTwoGraphic;
    private Graphic distanceLabelGraphic;
    private Graphic tempPointGraphic;
    private MapPoint currentPointerMapPoint;

    // Triggers when using measurement tools.
    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        // Get current map point from cursor
        var screenPoint = (Point)e.GetPosition(MyMapView);
        currentPointerMapPoint = MyMapView.ScreenToLocation(screenPoint);

        // Updates the live distance tracker for measurement tools.
        if (IsPolylineActive && multiPoints.Count > 0)
        {
            UpdatePolylineMeasurement(e);
        }
        else if (IsPolygonActive && multiPoints.Count > 0)
        {
            UpdatePolygonMeasurement(e);
        }

        if (!_isApplyingRemoteView)
        {
            _userIsInteractingWithMap = true;
        }
    }

    // Updates the live distance tracker for distance
    private void UpdatePolylineMeasurement(PointerEventArgs e)
    {
        // Update temporary line
        var lastPoint = multiPoints[^1];
        tempLineGraphic.Geometry = new Polyline(new List<MapPoint> { lastPoint, currentPointerMapPoint });

        // Update cursor following point
        tempPointGraphic.Geometry = currentPointerMapPoint;

        // Calculate distance
        double distance = GeometryEngine.DistanceGeodetic(multiPoints[multiPoints.Count() - 1], currentPointerMapPoint, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;
        double totalDistance = distance + measurementDistance;

        UpdateLabel($"{totalDistance:F2} m");
    }

    // Updates the live distance tracker for area
    private void UpdatePolygonMeasurement(PointerEventArgs e)
    {
        // Update temporary line
        var lastPoint = multiPoints[^1];
        tempLineGraphic.Geometry = new Polyline(new List<MapPoint> { lastPoint, currentPointerMapPoint });

        if (multiPoints.Count > 1)
        {
            // Update temporary line
            var startPoint = multiPoints[0];
            tempLineTwoGraphic.Geometry = new Polyline(new List<MapPoint> { startPoint, currentPointerMapPoint });

            // Update cursor following point
            tempPointGraphic.Geometry = currentPointerMapPoint;

            var previewPoints = new List<MapPoint>(multiPoints) { currentPointerMapPoint };
            var polygon = new Polygon(previewPoints);
            double area = GeometryEngine.AreaGeodetic(polygon, AreaUnits.SquareMeters, GeodeticCurveType.Geodesic);

            UpdateLabel($"{area:F2} m²");
        }
    }

    // Creates and updates the label with current distance from cursor
    private void UpdateLabel(string text)
    {
        // Offset label above cursor
        var screenPointF = MyMapView.LocationToScreen(currentPointerMapPoint);
        var offsetScreenPoint = new Point(screenPointF.X, screenPointF.Y - 10);
        var offsetMapPoint = MyMapView.ScreenToLocation(offsetScreenPoint);

        if (distanceLabelGraphic == null)
        {
            var textSymbol = new TextSymbol(
                text,
                System.Drawing.Color.White,
                11,
                Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom)
            {
                BackgroundColor = System.Drawing.Color.FromArgb(160, 0, 0, 0),
            };

            distanceLabelGraphic = new Graphic(offsetMapPoint, textSymbol);
            newOverlay.Graphics.Add(distanceLabelGraphic);
        }
        else
        {
            var textSymbol = (TextSymbol)distanceLabelGraphic.Symbol;
            textSymbol.Text = text;

            distanceLabelGraphic.Geometry = offsetMapPoint;
        }
    }

    //Triggers when right-clicking on the map
    private void OnMapViewRightTapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        GraphicsOverlay targetOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "newOverlay");
        if (targetOverlay != null)
        {
            targetOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(targetOverlay);
            if (multiPoints != null)
            {
                multiPoints.Clear();
                distanceLabelGraphic = null;
                polygonGraphic = null;
                measurementDistance = 0;
            }
            HideMeasurementCallout();
        }

        if (appState.PendingMeasurementField != null && appState.PendingMeasurementField.FieldName != null)
        {
            messageBoxMeasurement.IsVisible = false;
            appState.PendingMeasurementField.DefaultValue = appState.PendingMeasurementField.NewValue;
            appState.MeasurementCompleted();
            appState.PendingMeasurementField = null;
            IsPolygonActive = false;
            IsPolylineActive = false;
            PolygonMeasurementButton.Background = Color.FromArgb("#90D3D3D3");
            PolylineMeasurementButton.Background = Color.FromArgb("#90D3D3D3");
        }
    }

    //Triggers when left-clicking on the map
    private async void OnMapViewTappedAsync(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        try
        {
            bool ctrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down));

            var mapPoint = e.Position;

            //Disable to add more vertex in polyline or polygon
            if (_geometryEditor.IsStarted && _geometryEditor.Tool != null && isCreatingGraphics == false)
            {
                if (_geometryEditor.Tool is VertexTool)
                {
                    e.Handled = true;
                }
            }

            HideMeasurementCallout();

            if (_geometryEditor.IsStarted && !ctrl)
            {
                if (_geometryEditor.Geometry.GeometryType == GeometryType.Multipoint)
                {
                    var multipoint = (Multipoint)_geometryEditor.Geometry;

                    // Check if there are more than 2 points
                    if (multipoint.Points.Count >= 2)
                    {
                        // Get the last point (third point added)
                        var newStartingPoint = multipoint.Points.Last();

                        // Clear the GeometryEditor to reset
                        _geometryEditor.ClearGeometry();

                        // Start a new multipoint geometry with the last clicked point
                        _geometryEditor.Start(GeometryType.Multipoint);
                        _geometryEditor.InsertVertex(newStartingPoint);
                    }
                }
                return;
            }

            //Ruler
            if (IsPolylineActive)
            {
                PolylineMeasurement(e);
            }
            else if (IsPolygonActive)
            {
                PolygonMeasurement(e);
            }
            //No Ruller

            //Las file modes
            else if (IsLasFilesMode)
            {
                await HandleLasModeClick(e);
            }
            //End las file modes

            //Keycodes
            else if (IsDrawingKeycodePoint)
            {
                await HandleKeycodePointClick(e);
            }

            else if (IsDrawingKeycodeLine)
            {
                await HandleKeycodeLineClick(e);
            }
            //End keycode 


            else if (IsSelectingSegmentation)//manage survey segmentation
            {
                SetPinsOnTheMap(e);
            }
            else if (IsSummarizingIRI)
            {
                SetPinsOnTheMapForIRI(e);
            }
            else
            {
                if (_selectedGraphic != null)
                {
                    if (!appState.graphicsToRemove.Contains(_selectedGraphic) && _selectedGraphic.GraphicsOverlay != null)
                    {
                        _selectedGraphic.IsSelected = false;
                        _selectedGraphic = null;
                        appState.FindGraphicInfo(null, null);
                    }
                }

                // Call the function to process graphics at the tapped map point
                ProcessGraphicsAtMapPoint(mapPoint, e.Location, ctrl);

                if (!IsPlaying && viewModel.cameraOverlays.Count > 0)
                {
                    await GetClosestVideoGraphic(e.Location);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in OnMapViewTappedAsync" + ex.Message);
        }
    }

    //List<int> selectedSegmentsForSurvey = new List<int>(); //not used

    List<SegmentPointInfo> selectedSegmentPoints = new List<SegmentPointInfo>();
    private class MapPointData
    {
        public int SegmentId { get; set; } = 0;
        public bool IsStartPointSelected { get; set; } = false;
        public MapPoint StartMapPoint { get; set; }
        public double StartChainage { get; set; } = 0.0;
    }

    private MapPointData mapPointData = new MapPointData();

    //Manual segmentation
    private async void SetPinsOnTheMap(Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
    {
        try
        {
            Microsoft.Maui.Graphics.Point mapPoint = e.Position;
            IReadOnlyList<IdentifyGraphicsOverlayResult> results = MyMapView.IdentifyGraphicsOverlaysAsync(mapPoint, 10, false).Result;
            bool isSegmentClicked = results.Any(r => r.GraphicsOverlay.Id == LayerNames.Segment);
            if (isSegmentClicked)
            {
                var segmentResult = results.FirstOrDefault(r => r.GraphicsOverlay.Id == LayerNames.Segment);
                if (segmentResult != null)
                {
                    segmentResult.Graphics[0].Attributes.TryGetValue("Id", out var segId);
                    segmentResult.Graphics[0].Attributes.TryGetValue("SegmentId", out var selectedSegmentId);
                    segmentResult.Graphics[0].Attributes.TryGetValue("SurveyId", out var surveyId);

                    if (!mapPointData.IsStartPointSelected)
                    {
                        mapPointData.SegmentId = Convert.ToInt32(selectedSegmentId);
                        mapPointData.IsStartPointSelected = true;
                        mapPointData.StartMapPoint = (MapPoint)GeometryEngine.Project(e.Location, SpatialReferences.Wgs84);
                        AddPointOnTheMap(mapPointData.StartMapPoint);

                        // Get chainage for this start segment and point
                        var startChainageRequest = new ChainageMapPointRequest
                        {
                            Latitude = mapPointData.StartMapPoint.Y,
                            Longitude = mapPointData.StartMapPoint.X,
                            SegmentId = mapPointData.SegmentId,
                            SurveyId = surveyId.ToString()
                        };
                        var startChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(startChainageRequest);
                        selectedSegmentPoints.Add(new SegmentPointInfo
                        {
                            SegmentId = mapPointData.SegmentId,
                            MapPoint = mapPointData.StartMapPoint,
                            Chainage = startChainageResponse.Chainage
                        });

                    }
                    else
                    {
                        //manage the start point lat long
                        if (mapPointData != null && mapPointData.SegmentId >= 0)
                        {
                            MapPoint startPoint = null, endPoint = null;

                            startPoint = mapPointData.StartMapPoint;
                            endPoint = (MapPoint)GeometryEngine.Project(e.Location, SpatialReferences.Wgs84);

                            //multiPoints.Add(startPoint);
                            //selectedSegmentsForSurvey.Add(mapPointData.SegmentId);
                            AddPointOnTheMap(startPoint);

                            //clear map point data
                            mapPointData = new MapPointData();


                            SurveySegmentation ss1 = await appEngine.SurveySegmentationService.GetById(new IdRequest { Id = surveySegmentationId });
                            if (ss1 != null)
                            {
                                ss1.StartPoint = $"[{startPoint.Y},{startPoint.X}]";
                                await appEngine.SurveySegmentationService.UpdateSegmentation(ss1);
                            }


                            //MapPoint clickedPoint = new MapPoint(longs.Last(), lats.Last(), SpatialReferences.Wgs84);
                            // Capture the end point
                            //multiPoints.Add(endPoint);
                            //selectedSegmentsForSurvey.Add(Convert.ToInt32(selectedSegmentId));
                            AddPointOnTheMap(endPoint);

                            SurveySegmentation ss = await appEngine.SurveySegmentationService.GetById(new IdRequest { Id = surveySegmentationId });
                            if (ss != null)
                            {
                                ss.EndPoint = $"[{endPoint.Y},{endPoint.X}]";
                                await appEngine.SurveySegmentationService.UpdateSegmentation(ss);
                            }



                            bool result = false;
                            Survey oldSurvey = await appEngine.SurveyService.GetSurveyEntityByExternalId(segmentedSurvey.SurveyIdExternal);

                            mapPointData.SegmentId = Convert.ToInt32(selectedSegmentId);
                            // Get chainage for this end segment and point
                            var endChainageRequest = new ChainageMapPointRequest
                            {
                                Longitude = endPoint.X,
                                Latitude = endPoint.Y,
                                SegmentId = mapPointData.SegmentId,
                                SurveyId = surveyId.ToString()
                            };
                            var endChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(endChainageRequest);
                            selectedSegmentPoints.Add(new SegmentPointInfo
                            {
                                SegmentId = mapPointData.SegmentId,
                                MapPoint = endPoint,
                                Chainage = endChainageResponse.Chainage
                            });
                            if (oldSurvey != null)
                            {
                                result = await ApplySegmentationToDefects(oldSurvey, ss, selectedSegmentPoints);
                                //multiPoints = null;
                                selectedSegmentPoints.Clear();

                            }

                            if (result)
                            {
                                //show msg that segmentation has been applied
                                await Application.Current.MainPage.DisplayAlert("Information", $"Survey Segmentation has been applied successfully.", "OK");

                            }
                            else
                            {
                                ClearSegmentation();
                                await appEngine.SurveySegmentationService.DeleteObject(ss);
                                await Application.Current.MainPage.DisplayAlert("Error", $"Survey Segmentation has failed to be applied.", "OK");
                                return;
                            }

                            await RefreshMapAfterSegmentation();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            mapPointData = new MapPointData();
            Log.Error($"Erorr in SetPinsOnTheMap : {ex.Message}");
            ClearSegmentation();
            await Application.Current.MainPage.DisplayAlert("Error", $"Survey Segmentation has failed to be applied.", "OK");
        }
    }

    private async void SetPinsOnTheMapForIRI(GeoViewInputEventArgs e)
    {
        try
        {
            Microsoft.Maui.Graphics.Point mapPoint = e.Position;
            IReadOnlyList<IdentifyGraphicsOverlayResult> results = MyMapView.IdentifyGraphicsOverlaysAsync(mapPoint, 10, false).Result;
            bool isSegmentClicked = results.Any(r => r.GraphicsOverlay.Id == LayerNames.Segment);
            if (isSegmentClicked)
            {
                var segmentResult = results.FirstOrDefault(r => r.GraphicsOverlay.Id == LayerNames.Segment);
                if (segmentResult != null)
                {
                    segmentResult.Graphics[0].Attributes.TryGetValue("SegmentId", out var selectedSegmentId);
                    segmentResult.Graphics[0].Attributes.TryGetValue("SurveyId", out var surveyId);
                    var segmentId = Convert.ToInt32(selectedSegmentId);

                    if (!mapPointData.IsStartPointSelected)
                    {
                        mapPointData.SegmentId = segmentId;
                        mapPointData.IsStartPointSelected = true;
                        mapPointData.StartMapPoint = (MapPoint)GeometryEngine.Project(e.Location, SpatialReferences.Wgs84);
                        AddPointOnTheMap(mapPointData.StartMapPoint);

                        // Get chainage for this start segment and point
                        var startChainageRequest = new ChainageMapPointRequest
                        {
                            Longitude = mapPointData.StartMapPoint.X,
                            Latitude = mapPointData.StartMapPoint.Y,
                            SegmentId = mapPointData.SegmentId,
                            SurveyId = surveyId.ToString()
                        };
                        var startChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(startChainageRequest);
                        if (startChainageResponse != null)
                            mapPointData.StartChainage = startChainageResponse.Chainage;
                    }
                    else
                    {
                        //manage the start point lat long
                        if (mapPointData != null && mapPointData.SegmentId >= 0 && mapPointData.StartChainage != 0)
                        {
                            MapPoint endPoint = (MapPoint)GeometryEngine.Project(e.Location, SpatialReferences.Wgs84);
                            AddPointOnTheMap(endPoint);

                            // Get chainage for this end segment and point
                            var endChainageRequest = new ChainageMapPointRequest
                            {
                                Latitude = endPoint.Y,
                                Longitude = endPoint.X,
                                SegmentId = segmentId,
                                SurveyId = surveyId.ToString()
                            };
                            var endChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(endChainageRequest);
                            if (endChainageResponse != null)
                            {
                                appState.NotifyIRIStatus(true);
                                await ProcessIRIByUserDefinedMeter(mapPointData.StartChainage, endChainageResponse.Chainage);
                            }
                            ClearIRISummary();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Erorr in SetPinsOnTheMapForIRI : {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"", "OK");
        }
    }

    private async Task RefreshMapAfterSegmentation()
    {
        try
        {
            //reload all data on the map
            appState.InitializeSurveyAndLayers();
            //appState.SegmentsLoad(0);

            //reset all data from the map
            segmentedSurvey = null;
            surveySegmentationId = 0;
            SegmentationButton.Background = Color.FromArgb("#90D3D3D3");
            IsSelectingSegmentation = false;

            //multiPoints = null;
            //selectedSegmentsForSurvey.Clear();
            newOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(newOverlay);
            MyMapView.DismissCallout();

            LoaderOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            Log.Error($"Erorr in RefreshMapAfterSegmentation : {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"Survey Segmentation has failed to be applied.", "OK");
        }
    }

    private async Task<bool> ApplySegmentationToDefects(Survey refSurvey, SurveySegmentation ss, List<SegmentPointInfo> segmentsPointsInfos)
    {
        try
        {
            LoaderOverlay.IsVisible = true;

            segmentsPointsInfos = segmentsPointsInfos.OrderBy(spi => spi.SegmentId).ToList();
            double firstChainage = segmentsPointsInfos.First().Chainage;
            double lastChainage = segmentsPointsInfos.Last().Chainage;

            // Create a new survey
            var newSurvey = await CreateManualSegmentationSurvey(refSurvey, ss, firstChainage, lastChainage);
            if (newSurvey == null) return false;


            List<SegmentationData> segmentationSegmentsRequest = new List<SegmentationData>();
            foreach (var segment in segmentsPointsInfos)
            {

                //Create a new SegmentationData for each segmentsPointsInfos
                SegmentationData segmentationda = new SegmentationData
                {
                    SurveyId = refSurvey.SurveyIdExternal,
                    SectionId = segment.SegmentId,
                    NewSurveyId = newSurvey.SurveyIdExternal,
                    Chainage = segment.Chainage
                };

                segmentationSegmentsRequest.Add(segmentationda);

            }
            await appEngine.SurveySegmentationService.ProcessSegmentationSegments(segmentationSegmentsRequest);

            Survey newSurveyEntity = await appEngine.SurveyService.GetSurveyEntityByName(ss.Name);

            //chainage recaulcualte 

            // Now safely access .Value
            double oldChainage = firstChainage; // Use .Value to get the double
            double chainageDifference = ss.StartChainage - oldChainage;

            ChainageUpdateRequest chainageRequest = new ChainageUpdateRequest
            {
                SurveyId = newSurveyEntity.SurveyIdExternal,
                ChainageDifference = chainageDifference
            };

            await appEngine.SegmentService.UpdateSegmentChainageInDB(chainageRequest);

            //Update survey chainage 
            newSurveyEntity.StartChainage = ss.StartChainage;
            newSurveyEntity.EndChainage = newSurveyEntity.EndChainage + chainageDifference;

            await appEngine.SurveyService.EditValue(newSurveyEntity);


            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in ApplySegmentationToDefects: {ex.Message}");
            return false;
        }
    }


    private async Task<Survey> CreateManualSegmentationSurvey(Survey refSurvey, SurveySegmentation ss, double firstChainage, double lastChainage)
    {
        string segmentedSurveyExtId = DateTime.Now.ToString("ddHHmmss");

        Survey segmentedSurvey = JsonSerializer.Deserialize<Survey>(JsonSerializer.Serialize(refSurvey));
        segmentedSurvey.Id = 0;
        segmentedSurvey.SurveyIdExternal = segmentedSurveyExtId;
        segmentedSurvey.SurveyName = ss.Name;

        segmentedSurvey.GPSLatitude = GetLatitude(ss.StartPoint);
        segmentedSurvey.GPSLongitude = GetLongitude(ss.StartPoint);
        segmentedSurvey.StartChainage = firstChainage;
        segmentedSurvey.EndChainage = lastChainage;
        segmentedSurvey.Direction = ss.Direction.ToString();
        segmentedSurvey.Lane = ss.Lane;


        if (!string.IsNullOrEmpty(await appEngine.SurveyService.GetSurveyIdBySurveyName(segmentedSurvey.SurveyName)))
        {
            segmentedSurvey.SurveyName = string.Concat(ss.Name, "_", segmentedSurveyExtId);
        }

        IdReply idReply = await appEngine.SurveyService.Create(segmentedSurvey);


        if (idReply != null && idReply.Id > 0)
        {
            Log.Information($"Segmented survey {segmentedSurvey.SurveyName} has been created");
            return segmentedSurvey;
        }

        return null;
    }


    private async void AddPointOnTheMap(MapPoint clickedPoint)
    {
        try
        {
            var pointGraphic = new Graphic(clickedPoint, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 15));
            if (newOverlay == null)
                newOverlay = new GraphicsOverlay { Id = "newOverlay" };

            newOverlay.Graphics.Add(pointGraphic);

            if (!MyMapView.GraphicsOverlays.Contains(newOverlay))
                MyMapView.GraphicsOverlays.Add(newOverlay);
        }
        catch (Exception ex)
        {
            Log.Error($"Erorr in AddPointOnTheMap : {ex.Message}");
        }
    }

    private void DisplayLASPointsChart(List<LASPoint> lasPoints, string title)
    {
        var chartPage = new ChartPage(lasPoints, appEngine, appState);

        var chartWindow = new Microsoft.Maui.Controls.Window(chartPage)
        {

            Title = title
        };
        App.Current?.OpenWindow(chartWindow);
        (App.Current as App)?.OpenWindows.Add(chartWindow);
    }

    private async void ProcessGraphicsAtMapPoint(Microsoft.Maui.Graphics.Point mapPoint, MapPoint e, bool ctrl)
    {
        IReadOnlyList<IdentifyGraphicsOverlayResult> results = await MyMapView.IdentifyGraphicsOverlaysAsync(mapPoint, 10, false);
        if (results == null) return;

        var relevantOverlays = results.Where(r => r.GraphicsOverlay.Id == "Boundray" || r.GraphicsOverlay.Id.EndsWith("shp") ||
                                            r.GraphicsOverlay.Id == "LasPoints" || r.GraphicsOverlay.Id == "Survey Segmentation").ToList();
        foreach (var result in relevantOverlays)
        {
            ProcessCallOutOverlay(result, e);
        }
        var filteredResults = results
           .Where(result => !IsIgnoredOverlay(result.GraphicsOverlay.Id))
           .ToList();

        bool isSegmentClicked = results.Any(r => r.GraphicsOverlay.Id == LayerNames.Segment);
        if (isSegmentClicked)
        {
            HandleSegmentClickFlow(filteredResults, ctrl);
        }
        else
        {
            HandleNonSegmentClickFlow(filteredResults, ctrl);
        }
    }

    private void ProcessCallOutOverlay(IdentifyGraphicsOverlayResult result, MapPoint e)
    {
        if (result.GraphicsOverlay.Id == "Survey Segmentation")
        {
            foreach (var graphic in result.Graphics)
            {
                Graphic startGraphic, endGraphic;
                MapPoint startMapPoint = null, endMapPoint = null;

                var Id = graphic.Attributes.FirstOrDefault(pair => pair.Key == "Id").Value;
                if (graphic.Attributes.ContainsKey("Start"))
                {
                    startGraphic = graphic;
                    endGraphic = result.GraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("End") && g.Attributes["Id"].ToString() == Id.ToString());
                    startMapPoint = e;
                }
                else
                {
                    endGraphic = graphic;
                    startGraphic = result.GraphicsOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("Start") && g.Attributes["Id"].ToString() == Id.ToString());
                    endMapPoint = e;
                }

                string[] startData = startGraphic.Attributes.FirstOrDefault(pair => pair.Key == "Start").Value?.ToString().Replace("[", "").Replace("]", "").Split(','),
                    endData = endGraphic.Attributes.FirstOrDefault(pair => pair.Key == "End").Value?.ToString().Replace("[", "").Replace("]", "").Split(',');
                string strName = graphic.Attributes.FirstOrDefault(pair => pair.Key == "Name").Value.ToString();

                List<string> attributeStringsStart = new List<string>();
                List<string> attributeStringsEnd = new List<string>();
                if (startData?.Length > 1)
                {
                    double startX = Convert.ToDouble(startData[1]), startY = Convert.ToDouble(startData[0]);
                    attributeStringsStart.Add($"{graphic.GraphicsOverlay.Id} : {strName}");
                    attributeStringsStart.Add($"Start Longitude : {startX}");
                    attributeStringsStart.Add($"Start Latitude : {startY}");
                    if (startMapPoint == null)
                    {
                        startMapPoint = new MapPoint(startX, startY, SpatialReferences.Wgs84);
                    }
                }

                if (endData?.Length > 1)
                {
                    double endX = Convert.ToDouble(endData[1]), endY = Convert.ToDouble(endData[0]);
                    attributeStringsEnd.Add($"{graphic.GraphicsOverlay.Id} : {strName}");
                    attributeStringsEnd.Add($"End Longitude : {endX}");
                    attributeStringsEnd.Add($"End Latitude : {endY}");
                    if (endMapPoint == null)
                    {
                        endMapPoint = new MapPoint(endX, endY, SpatialReferences.Wgs84);
                    }
                }

                string attributeStringStart = string.Join(Environment.NewLine, attributeStringsStart);
                string attributeStringEnd = string.Join(Environment.NewLine, attributeStringsEnd);

                var textStart = new TextSymbol(attributeStringStart, System.Drawing.Color.Yellow, 15,
                    Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);

                var textEnd = new TextSymbol(attributeStringEnd, System.Drawing.Color.Yellow, 15,
                    Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center,
                    Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);

                result.GraphicsOverlay.Graphics.Add(new Graphic(startMapPoint, textStart));
                result.GraphicsOverlay.Graphics.Add(new Graphic(endMapPoint, textEnd));
            }
            return;
        }
        else
        {
            foreach (var graphic in result.Graphics)
            {
                var attributes = graphic.Attributes.Where(pair => pair.Key != "IsTemp");
                var attributeStrings = attributes.Select(pair => $"{pair.Key} : {pair.Value}");
                string attributeString = string.Join(Environment.NewLine, attributeStrings);

                CalloutDefinition myCalloutDefinition = new CalloutDefinition(graphic.GraphicsOverlay.Id, attributeString);
                MyMapView.ShowCalloutAt(e, myCalloutDefinition);
            }
            return;
        }
    }

    private void HandleSegmentClickFlow(IReadOnlyList<IdentifyGraphicsOverlayResult> results, bool ctrl)
    {
        var targetResults = results.Where(r => r.GraphicsOverlay.Id != LayerNames.Segment && r.GraphicsOverlay.Id != "surveySetOverlay" && !r.GraphicsOverlay.Id.EndsWith("Sample Unit")).ToList();
        if (targetResults.Count > 0)
        {
            //both segment and another graphic (apart from ignored overlay)
            var targetResult = results.FirstOrDefault();
            if (targetResult != null)
            {
                foreach (Graphic graphic in targetResult.Graphics)
                {
                    if (graphic.Symbol is TextSymbol textSymbol) return;
                    UpdateGraphicSelection(graphic, ctrl);
                    HandleDefaultGraphicClick(graphic);
                }
            }
        }
        else
        {
            //just segment
            var segmentResult = results.FirstOrDefault(r => r.GraphicsOverlay.Id == LayerNames.Segment);
            if (segmentResult != null)
            {
                foreach (Graphic graphic in segmentResult.Graphics)
                {
                    _ = HandleSegmentClick(graphic);
                    if (ctrl && _oldGraphics != null && _oldGraphics.Any() && _geometryEditor.IsStarted)
                    {
                        if (_oldGraphics.Contains(graphic))
                        {
                            //add a new graphic into the geometry editor
                            _oldGraphics.Remove(graphic);
                        }
                        else
                        {
                            //remove a new graphic from the geometry editor
                            _oldGraphics.Add(graphic);
                        }
                        UpdateGeometryEditor(_oldGraphics);
                    }
                }
            }
        }
    }

    private bool IsIgnoredOverlay(string overlayId)
    {
        return overlayId == "tempOverlay" || overlayId == "drawingDefectOverlay" || viewModel.cameraOverlays.ContainsKey(overlayId) || overlayId == "Vehicle Path" || overlayId == "surveySetOverlay";
    }

    private async void HandleNonSegmentClickFlow(IReadOnlyList<IdentifyGraphicsOverlayResult> results, bool ctrl)
    {
        var targetResults = results.Where(r => r.GraphicsOverlay.Id != "tempOverlay" && r.GraphicsOverlay.Id != "drawingDefectOverlay").ToList();
        if (targetResults.Count > 0)
        {
            //tapping gesture for the polygons on the survey set pages
            if (targetResults.Any(x => x.GraphicsOverlay.Id == "surveySetOverlay"))
            {
                var surveySetOverlay = targetResults.FirstOrDefault(x => x.GraphicsOverlay.Id == "surveySetOverlay");
                foreach (Graphic graphic in surveySetOverlay.Graphics)
                {
                    if (IsEditing)
                    {
                        if (!_geometryEditor.IsStarted)
                        {
                            _selectedGraphic = graphic;
                            _geometryEditor.Start(graphic.Geometry);
                            isCreatingGraphics = true;
                        }
                    }
                    else
                    {
                        graphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
                        graphic.Attributes.TryGetValue("Status", out var status);

                        if (status is bool statusBool)
                        {
                            if (!graphic.IsSelected)
                            {
                                bool confirmation = await App.Current.MainPage.DisplayAlert("Confirmation", $"Do you want to open Sample Unit '{jsonFilePath}' for PCI Rating? \nPlease save the current sample unit's PCI defects before proceeding.", "Yes", "Cancel");
                                if (confirmation)
                                {
                                    appState.UpdatePCIRatingMode(jsonFilePath.ToString());
                                    HighlightSurveyTemplateGraphics(jsonFilePath.ToString(), "Click");
                                }
                                return;
                            }

                            if (targetResults.Count > 1)
                            {
                                var secondTargetResult = targetResults.FirstOrDefault(x => x.GraphicsOverlay.Id != "surveySetOverlay");
                                if (secondTargetResult != null)
                                {
                                    foreach (Graphic secondGraphic in secondTargetResult.Graphics)
                                    {
                                        UpdateGraphicSelection(secondGraphic, ctrl);
                                        HandleGraphicOutsideSegmentClick(secondGraphic);
                                    }
                                }
                            }
                            return;
                        }

                        if (jsonFilePath != null)
                        {
                            appState.HighlightGraphic(jsonFilePath.ToString(), "Click");
                        }
                    }
                }
            }
            else if (targetResults.Any(x => x.GraphicsOverlay.Id.EndsWith("Sample Unit")))
            {
                var sampleUnitOverlay = targetResults.FirstOrDefault(x => x.GraphicsOverlay.Id.EndsWith("Sample Unit"));
                foreach (var graphic in sampleUnitOverlay.Graphics)
                {
                    if (graphic.Attributes.TryGetValue("PCIRatingId", out var pciRatingIdObj) && graphic.Attributes.TryGetValue("SampleUnitId", out var sampleUnitIdObj) &&
                          int.TryParse(pciRatingIdObj.ToString(), out int pciId) && int.TryParse(sampleUnitIdObj.ToString(), out int suId))
                    {
                        appState.GraphicOutSideSegmentClicked();
                        appState.SampleUnitClicked(pciId, suId);

                        if (_selectedSegment != null)
                        {
                            if (appState.segmentLayer)
                            {
                                _selectedSegment.Symbol = graphic.Symbol;
                            }
                            else
                            {
                                _selectedSegment.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Transparent, 1.0));
                            }
                        }
                    }
                }

                var pciDefectGraphic = targetResults
                .SelectMany(r => r.Graphics)
                .FirstOrDefault(g => g.Attributes.TryGetValue("Type", out var type) && type?.ToString() == "PCIDefect");
                if (pciDefectGraphic != null)
                {
                    UpdateGraphicSelection(pciDefectGraphic, ctrl);
                    HandleGraphicOutsideSegmentClick(pciDefectGraphic);
                }
            }
            else
            {
                var firstTargetResult = targetResults.FirstOrDefault();

                if (firstTargetResult != null)
                {
                    var firstGraphic = firstTargetResult.Graphics.FirstOrDefault();
                    if (firstGraphic.Symbol is TextSymbol textSymbol) return;

                    //Handle the graphics outside of segments
                    UpdateGraphicSelection(firstGraphic, ctrl);
                    HandleGraphicOutsideSegmentClick(firstGraphic);
                }
            }
        }
    }

    private async Task GetClosestVideoGraphic(MapPoint clickedMapPoint, string selectedSurvey = null)
    {
        double distanceThresholdKm = 10;

        //Remove highlight of previous graphics
        foreach (var selectedVideo in selectedVideoGraphics)
        {
            if (selectedVideo.GraphicsOverlay != null && selectedVideo.GraphicsOverlay.Graphics.Contains(selectedVideo))
                selectedVideo.IsSelected = false;
        }
        selectedVideoGraphics.Clear();

        List<Graphic> closestGraphicsList = new List<Graphic>();
        Graphic closestGraphic = null;
        double closestDistance = double.MaxValue;

        // Step 1: Get the closest graphic and its survey from the FIRST overlay
        var firstOverlay = viewModel.cameraOverlays.Values.Where(x => x.Graphics.Count > 0).FirstOrDefault();
        if (firstOverlay != null && firstOverlay.IsVisible)
        {
            if (selectedSurvey != null)
            {
                closestGraphic = GetClosestGraphicFromOverlay(firstOverlay, clickedMapPoint, distanceThresholdKm, graphic =>
                {
                    return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                           surveyName?.ToString() == selectedSurvey;
                });
            }
            else
            {
                closestGraphic = GetClosestGraphicFromOverlay(firstOverlay, clickedMapPoint, distanceThresholdKm);
                if (closestGraphic != null && closestGraphic.Attributes.TryGetValue("SurveyId", out var survey))
                {
                    selectedSurvey = survey?.ToString();
                }
            }
        }

        // Return early if nothing is found.
        if (closestGraphic == null || selectedSurvey == null)
        {
            return;
        }

        var validCameraIds = new List<string>();

        // Step 2: From all overlays, get the closest graphic from each overlay that belongs to the same survey.
        foreach (var overlay in viewModel.cameraOverlays.Values)
        {
            if (!overlay.IsVisible)
            {
                continue; // Skip overlays that are not visible
            }

            // Use a filter to include only graphics from the selected survey.
            Graphic closestOverlayGraphic = GetClosestGraphicFromOverlay(overlay, clickedMapPoint, distanceThresholdKm, graphic =>
            {
                return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                       surveyName?.ToString() == selectedSurvey;
            });

            double closestOverlayDistance = double.MaxValue;

            // Add the closest graphic from this overlay (within the survey) to the list, if found
            if (closestOverlayGraphic != null)
            {
                closestGraphicsList.Add(closestOverlayGraphic);
                validCameraIds.Add(overlay.Id);
            }
            else
            {
                appState.CloseVideoPopup(overlay.Id);
            }
        }

        var componentsToRemove = VideoHorizontalStack.Children
            .OfType<VideoPopupPage>()
            .Where(v => !validCameraIds.Contains(v.AutomationId))
            .ToList();

        foreach (var component in componentsToRemove)
        {
            VideoHorizontalStack.Children.Remove(component);
        }

        // Step 3: Process the list of closest graphics (update slider, display images, update selection).
        foreach (Graphic graphic in closestGraphicsList)
        {
            if (graphic.Attributes.TryGetValue("CameraInfo", out var cameraInfo) && graphic.Attributes.TryGetValue("ImagePath", out var filePath) && graphic.Attributes.TryGetValue("VideoFrameId", out var id))
            {
                if (!VideoPlayer.IsVisible)
                {
                    var overlay = graphic.GraphicsOverlay;
                    if (overlay != null)
                    {
                        int count = overlay.Graphics.Count(g => g.Attributes.TryGetValue("SurveyId", out var surveyName) && surveyName?.ToString() == selectedSurvey);

                        VideoPlayerSlider.Minimum = 0;
                        VideoPlayerSlider.Maximum = count - 1;
                    }
                }

                int currentFrameId = Convert.ToInt32(id);
                VideoPlayerSlider.Value = currentFrameId;
                await DisplayVideoImages(cameraInfo.ToString(), filePath.ToString());
            }
            graphic.IsSelected = true;
            selectedVideoGraphics.Add(graphic);
            if (_selectedGraphic != null && viewModel.cameraOverlays.ContainsValue(_selectedGraphic.GraphicsOverlay) && _selectedGraphic != graphic)
            {
                _selectedGraphic.IsSelected = false;
                _selectedGraphic = null;
            }
        }

        // Step 4: Get the closest segment to sync video with lcms
        var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.Segment);
        if (segmentOverlay != null)
        {
            await SyncSegmentWithVideo(segmentOverlay, clickedMapPoint, distanceThresholdKm, selectedSurvey);
        }
    }

    private async Task SyncSegmentWithVideo(GraphicsOverlay segmentOverlay, MapPoint clickedMapPoint, double distanceThresholdKm, string selectedSurvey)
    {
        var segmentGraphic = GetClosestGraphicFromOverlay(segmentOverlay, clickedMapPoint, distanceThresholdKm, graphic =>
        {
            return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                   surveyName?.ToString() == selectedSurvey;
        });

        if (segmentGraphic != null && _selectedSegment != segmentGraphic)
        {
            await HandleSegmentClick(segmentGraphic);
        }
    }

    private void UpdateGraphicSelection(Graphic graphic, bool ctrl)
    {
        if (appState.IsMultiSelected && ctrl)
        {
            if (graphic.IsSelected)
            {
                appState.graphicsToRemove.Remove(graphic);
                graphic.IsSelected = false;
            }
            else
            {
                appState.graphicsToRemove.Add(graphic);
                graphic.IsSelected = true;
            }
        }
        else if (ctrl && selectedGraphics != null && selectedGraphics.Any() && _geometryEditor.IsStarted)
        {
            if (selectedGraphics.Contains(graphic))
            {
                selectedGraphics.Remove(graphic);
            }
            else
            {
                selectedGraphics.Add(graphic);
            }
            UpdateGeometryEditor(selectedGraphics);
        }
        else
        {
            _selectedGraphic = graphic;
            graphic.IsSelected = true;
        }
    }

    private async Task HandleSegmentClick(Graphic graphic)
    {
        graphic.Attributes.TryGetValue("ImageFilePath", out var filePath);
        graphic.Attributes.TryGetValue("SectionId", out var sectionId);
        graphic.Attributes.TryGetValue("SurveyId", out var surveyId);

        var folderPath = !string.IsNullOrEmpty(filePath.ToString()) ? Path.GetDirectoryName(filePath.ToString()) : string.Empty;
        if (string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(filePath.ToString()))
        {
            string[] splittedFilename = Path.GetFileNameWithoutExtension(filePath.ToString()).Split("_");
            folderPath = await appEngine.SurveyService.GetImageFolderPath(surveyId.ToString());
            if (!folderPath.Contains("ImageResult")) folderPath = Path.Combine(folderPath, "ImageResult");
            Log.Information($"filePath value: {filePath}");
            Log.Information($"folderPath value: {folderPath}");
        }
        var fileName = !string.IsNullOrEmpty(filePath.ToString()) ? Path.GetFileNameWithoutExtension(filePath.ToString()) : string.Empty;
        var imagePath = !string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(filePath.ToString()) ? Path.Combine(folderPath, fileName) : string.Empty;
        appState.SegmentClicked(imagePath, surveyId.ToString(), sectionId.ToString());

        if (viewModel.highlightedSegmentSymbol != null && viewModel.segmentSymbol != null)
        {
            var highlightedSymbol = viewModel.highlightedSegmentSymbol;

            if (appState.segmentLayer)
            {
                var segmentSymbol = viewModel.segmentSymbol;

                graphic.Symbol = highlightedSymbol;

                if (previousGraphic != null
                    && previousGraphic != graphic
                    && previousGraphic.GraphicsOverlay != null
                    && previousGraphic.GraphicsOverlay.Id == LayerNames.Segment)
                {
                    previousGraphic.Symbol = segmentSymbol;
                }
                previousGraphic = graphic;

            }
            else
            {
                graphic.Symbol = highlightedSymbol;
                if (previousGraphic != null && previousGraphic != graphic)
                {
                    previousGraphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Transparent, 1.0));
                }
                previousGraphic = graphic;
            }
        }

        if (IsAutoRotationEnabled)
        {
            //var response = await appEngine.SegmentService.GetById(new IdRequest() { Id = (int)id });
            graphic.Attributes.TryGetValue("TrackAngle", out var trackAngle);
            if (trackAngle != null && trackAngle is double angle)
            {
                await MyMapView.SetViewpointRotationAsync(angle);
            }

        }
        _selectedSegment = graphic;

    }

    private async void HandleDefaultGraphicClick(Graphic graphic)
    {
        var attributes = graphic.Attributes;
        if (attributes.TryGetValue("Id", out var id) && attributes.TryGetValue("Table", out var table))
        {
            //send attributes to segment summary page
            if (attributes.TryGetValue("Type", out var type))
            {
                //MetaTable, Summaries, PCIDefects passing Type to segment summary
                appState.FindGraphicInfo(table.ToString(), id.ToString(), type.ToString());
            }
            else
            {
                appState.FindGraphicInfo(table.ToString(), id.ToString());
            }
        }

        //Get data to find its segment 
        if (attributes.TryGetValue("SurveyId", out var surveyId) && attributes.TryGetValue("SegmentId", out var segmentId))
        {
            var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == LayerNames.Segment);
            var matchingGraphic = segmentOverlay.Graphics.FirstOrDefault(graphic =>
            {
                // Check if the graphic has attributes matching your criteria
                if (graphic.Attributes.ContainsKey("surveyId") && graphic.Attributes.ContainsKey("segmentId"))
                {
                    // Extract surveyId and segmentId from the graphic's attributes
                    string graphicSurveyId = graphic.Attributes["surveyId"].ToString();
                    int graphicSegmentId = (int)graphic.Attributes["segmentId"];

                    // Return true if the graphic's attributes match the target criteria
                    return graphicSurveyId == surveyId.ToString() && graphicSegmentId == Convert.ToInt32(segmentId);
                }
                return false;
            });
            if (matchingGraphic != null)
            {
                await HandleSegmentClick(matchingGraphic);

                double[] segmentSize = null;

                if (matchingGraphic.Geometry is Polygon polygon)
                {
                    matchingGraphic.Attributes.TryGetValue("Width", out var width);
                    matchingGraphic.Attributes.TryGetValue("Height", out var height);
                    if (width != null && height != null)
                    {
                        segmentSize = new double[] { Convert.ToDouble(width), Convert.ToDouble(height) };
                    }
                }
                if (graphic.GraphicsOverlay.Id != "ConcreteJoint")
                {
                    graphic.Attributes.TryGetValue("x", out var x);
                    graphic.Attributes.TryGetValue("y", out var y);
                    if (x != null && y != null)
                    {
                        appState.ZoomInImages(Convert.ToDouble(x), Convert.ToDouble(y), segmentSize);
                    }

                }
                else
                {
                    appState.ResetImage();
                }
            }
            else
            {
                appState.GraphicOutSideSegmentClicked();
            }
        }
        else
        {
            appState.GraphicOutSideSegmentClicked();
        }
    }

    private void HandleGraphicOutsideSegmentClick(Graphic graphic)
    {
        var attributes = graphic.Attributes;
        if (attributes.TryGetValue("Id", out var id) && attributes.TryGetValue("Table", out var table))
        {
            appState.GraphicOutSideSegmentClicked();
            if (attributes.TryGetValue("Type", out var type))
            {
                appState.FindGraphicInfo(table.ToString(), id.ToString(), type.ToString());
            }
            else
            {
                //send table and id to segment summary
                appState.FindGraphicInfo(table.ToString(), id.ToString());
            }
        }
    }

    #endregion

    #region Survey Template

    private void UndoButton_Click(object sender, EventArgs e)
    {
        if (isRectangleDrawingMode)
        {
            if (rectanglePoints.Count > 0)
            {
                //this makes everything more complicated
                // Remove last and push to redo stack
                //var last = rectanglePoints[^1];

                //var geom = _geometryEditor.Geometry;
                //int vertexCount = GetUniqueVertexCount(geom);
                //if (vertexCount > 2 && vertexCount <= 5)
                //    rectanglePoints.RemoveAt(rectanglePoints.Count - 1);
                //redoStack.Push(last);

                //RefreshDrawing();//RefreshDrawing(isUndo : true);
                //UpdateUndoRedoSaveButtons();

                rectanglePoints.RemoveAt(rectanglePoints.Count - 1);
            }
        }

        _geometryEditor.Undo();
    }

    private void RedoButton_Click(object sender, EventArgs e)
    {
        //this makes everything more complicated
        //if (isRectangleDrawingMode)
        //{
        //    if (redoStack.Count > 0)
        //    {
        //        // Restore from redo stack
        //        var restored = redoStack.Pop();
        //        rectanglePoints.Add(restored);

        //        RefreshDrawing();
        //    }
        //    else if (rectanglePoints.Count == 0 && storedFirstTwoPoints.Count == 2)
        //    {
        //        // Case: everything has been undone, but we have backups of P1 and P2.
        //        rectanglePoints = new List<MapPoint>(storedFirstTwoPoints);

        //        RefreshDrawing();
        //    }
        //    //UpdateUndoRedoSaveButtons();
        //}
        _geometryEditor.Redo();

        if (isRectangleDrawingMode)
        {
            var geom = _geometryEditor.Geometry;
            if (geom is Polygon polygon)
            {
                var coords = polygon.Parts.FirstOrDefault()?.Points;
                if (coords == null || coords.Count == 0)
                    return;

                if (coords.Count == 4 && rectanglePoints.Count == 2)
                {
                    // Add 3rd point, not the last one
                    rectanglePoints.Add(coords[2]);
                }
                else
                {
                    // Add only the last point
                    rectanglePoints.Add(coords.Last());
                }
            }
        }
    }

    private void DiscardButton_Click(object sender, EventArgs e)
    {
        _geometryEditor.Stop();
        isCreatingGraphics = false;
    }
    private async void PolygonButton_Click(object sender, EventArgs e)
    {
        EndRectangleDrawingMode();
        SetDefaultGeometryEditorStyle();
        if (_geometryEditor.IsStarted && _selectedGraphic?.Attributes.ContainsKey("jsonFilePath") == true)
        {
            _selectedGraphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
            var fileName = Path.GetFileName((string)jsonFilePath);
            //Check if the user wants to recreate of the existing polygon
            bool recreatePolygon = await App.Current.MainPage.DisplayAlert("Confirmation", $"Do you want to recreate the existing polygon of file '{fileName}'?", "Yes", "No");
            if (recreatePolygon)
            {
                MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
                isCreatingGraphics = true;
                _geometryEditor.Start(GeometryType.Polygon);
            }
        }
        //Draw a new polygon
        else
        {
            _selectedGraphic = null;
            _geometryEditor.Start(GeometryType.Polygon);
            isCreatingGraphics = true;
        }
    }

    private bool isRectangleDrawingMode = false;
    private List<MapPoint> rectanglePoints = new List<MapPoint>();
    //private Stack<MapPoint> redoStack = new Stack<MapPoint>();    
    //private List<MapPoint> storedFirstTwoPoints = new List<MapPoint>();// Fixed backup of the first two clicks.

    private async void RectangleButton_Click(object sender, EventArgs e)
    {
        SetDefaultGeometryEditorStyle();
        if (_geometryEditor.IsStarted && _selectedGraphic?.Attributes.ContainsKey("jsonFilePath") == true)
        {
            _selectedGraphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
            var fileName = Path.GetFileName((string)jsonFilePath);
            //Check if the user wants to recreate of the existing polygon
            bool recreatePolygon = await App.Current.MainPage.DisplayAlert("Confirmation", $"Do you want to recreate the existing polygon of file '{fileName}'?", "Yes", "No");
            if (recreatePolygon)
            {
                MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
                isCreatingGraphics = true;
                _geometryEditor.Start(GeometryType.Polygon);
            }
        }
        //Draw a new polygon
        else
        {
            StartRectangleDrawingMode();

            _selectedGraphic = null;
            _geometryEditor.Start(GeometryType.Polygon);
            isCreatingGraphics = true;
        }
    }

    private void GeometryEditor_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (isRectangleDrawingMode)
        {
            if (e.PropertyName == nameof(_geometryEditor.Geometry))
            {
                if (_geometryEditor.Geometry is Polygon polygon)
                {
                    int vertexUCount = GetUniqueVertexCount(_geometryEditor.Geometry);

                    // If there are more than 4 points, disable the save button
                    SaveSurveyGraphicButton.IsEnabled = vertexUCount == 4;
                }
            }
        }
        if (IsSelectingLasRutting)
        {
            if (e.PropertyName != nameof(GeometryEditor.Geometry))
                return;

            if (_oldGraphics == null || !_oldGraphics.Any())
                return;

            if (_geometryEditor.Geometry == null || _geometryEditor.Geometry.IsEmpty)
                return;

            var originalGeometry = GeometryEngine.CombineExtents(_oldGraphics.Select(g => g.Geometry));
            var startPoint = originalGeometry.GetCenter();

            var currentGeometry = _geometryEditor.Geometry;
            var currentPoint = currentGeometry.Extent.GetCenter();

            DrawMeasurementDuringMove(startPoint, currentPoint);
        }
    }

    //private void UpdateUndoRedoSaveButtons()
    //{
    //    // Undo enabled if there is at least one item in the list
    //    UndoButton.IsEnabled = rectanglePoints.Count > 0;

    //    // Rule: if there is no Undo, then disable Redo as well
    //    if (UndoButton.IsEnabled)
    //    {
    //        RedoButton.IsEnabled = redoStack.Count > 0;
    //        if (rectanglePoints.Count == 3) RedoButton.IsEnabled = false;
    //    }
    //    else
    //    {
    //        RedoButton.IsEnabled = false;
    //    }

    //    //Save Button:
    //    var geom = _geometryEditor.Geometry;
    //    int vertexUCount = GetUniqueVertexCount(geom);

    //    SaveSurveyGraphicButton.IsEnabled = rectanglePoints.Count == 3 && ((vertexUCount == 4)); 
    //}

    private void StartRectangleDrawingMode()
    {
        isRectangleDrawingMode = true;
        rectanglePoints.Clear();

        MyMapView.GeoViewTapped -= MyMapView_GeoViewTapped;
        MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
    }

    private void EndRectangleDrawingMode()
    {
        if (isRectangleDrawingMode)
        {
            isRectangleDrawingMode = false;
            rectanglePoints.Clear();
            //need to remove GeoViewTapped event as it is only for creating rectangle
            MyMapView.GeoViewTapped -= MyMapView_GeoViewTapped;
        }
    }

    private void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
    {
        var geom = _geometryEditor.Geometry;
        int vertexUCount = GetUniqueVertexCount(geom);
        if (isRectangleDrawingMode && rectanglePoints.Count == 3)
            return;

        if (!isRectangleDrawingMode || !isCreatingGraphics)
            return;

        rectanglePoints.Add(e.Location);

        // Save a permanent backup of the first two positions
        //if (rectanglePoints.Count == 1 || rectanglePoints.Count == 2)
        //{
        //    storedFirstTwoPoints = new List<MapPoint>(rectanglePoints);
        //}
        geom = _geometryEditor.Geometry;
        if (rectanglePoints.Count == 3)
        {
            BuildAndShowRectangle(rectanglePoints);
            //duplicate
            //geom = _geometryEditor.Geometry;
            //if (geom != null)
            //{
            //    var dup = Geometry.FromJson(geom.ToJson());
            //    _geometryEditor.ReplaceGeometry(dup);
            //}
        }
        //UpdateUndoRedoSaveButtons();
    }

    private void BuildAndShowRectangle(List<MapPoint> points)
    {
        if (points.Count < 3)
        {
            // There aren't enough points to form a rectangle yet
            return;
        }

        var p1 = points[0];
        var p2 = points[1];
        var p3 = points[2];

        // Vector Base: p1 → p2
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;

        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return; // Avoid division by zero

        // Perpendicular (normalized)
        var nx = -dy / length;
        var ny = dx / length;

        // Projected distance from p3 to the base
        var px = p3.X - p1.X;
        var py = p3.Y - p1.Y;
        var height = px * nx + py * ny;

        var offsetX = nx * height;
        var offsetY = ny * height;

        // Rectangle
        var p4 = new MapPoint(p1.X + offsetX, p1.Y + offsetY, p1.SpatialReference);
        var p5 = new MapPoint(p2.X + offsetX, p2.Y + offsetY, p2.SpatialReference);

        var polygonBuilder = new PolygonBuilder(p1.SpatialReference);
        polygonBuilder.AddPoint(p1);
        polygonBuilder.AddPoint(p2);
        polygonBuilder.AddPoint(p5);
        polygonBuilder.AddPoint(p4);
        //polygonBuilder.AddPoint(p1); // no need to Close

        var rectangle = polygonBuilder.ToGeometry();
        _geometryEditor.ReplaceGeometry(rectangle);
        //_geometryEditor.Stop();
        //_geometryEditor.Start(rectangle);
    }

    private int GetUniqueVertexCount(Geometry geometry)
    {
        if (geometry is Polygon polygon && polygon.Parts.Count > 0)
        {
            var points = polygon.Parts.First().Points.ToList();

            // If the last one matches the first, remove it.
            if (points.Count > 1)
            {
                var first = points.First();
                var last = points.Last();

                if (first.X == last.X && first.Y == last.Y)
                {
                    points.RemoveAt(points.Count - 1);
                }
            }

            return points.Count; // Only unique vertices
        }

        return 0;
    }
    //private void RefreshDrawing()
    //{
    //    _geometryEditor.Stop();
    //    _geometryEditor.Start(GeometryType.Polygon);

    //    if (rectanglePoints.Count == 3)
    //    {
    //        // When there are 3: complete rectangle
    //        BuildAndShowRectangle(rectanglePoints);
    //    }
    //    else if (rectanglePoints.Count == 2)
    //    {
    //        // When there are only 2: draw base line P1–P2
    //        var p1 = rectanglePoints[0];
    //        var p2 = rectanglePoints[1];

    //        var lineBuilder = new PolygonBuilder(p1.SpatialReference);
    //        lineBuilder.AddPoint(p1);
    //        lineBuilder.AddPoint(p2);
    //        lineBuilder.AddPoint(p2); // Close at P2 → control remains at P2

    //        _geometryEditor.ReplaceGeometry(lineBuilder.ToGeometry());
    //    }
    //    else if (rectanglePoints.Count < 2)
    //    {
    //        // 0 or 1 point → Do not display anything
    //        rectanglePoints.Clear();
    //        _geometryEditor.Stop();
    //        _geometryEditor.Start(GeometryType.Polygon);
    //    }
    //}
    // Validates whether opposite sides of a quadrilateral are parallel.
    // Accepts a list of 4 coordinate pairs: [ [x1, y1], [x2, y2], [x3, y3], [x4, y4] ]
    private bool ParallelogramValidator(List<List<double>> coordinatesList)
    {
        if (coordinatesList == null)
            return false;

        // If the list contains 5 points, remove the last one (usually a closing point)
        if (coordinatesList.Count == 5)
            coordinatesList.RemoveAt(4);

        var v1 = GetVector(coordinatesList[0], coordinatesList[1]); // side 1
        var v2 = GetVector(coordinatesList[1], coordinatesList[2]); // side 2
        var v3 = GetVector(coordinatesList[2], coordinatesList[3]); // side 3
        var v4 = GetVector(coordinatesList[3], coordinatesList[0]); // side 4

        bool firstPairParallel = AreVectorsParallel(v1, v3);
        bool secondPairParallel = AreVectorsParallel(v2, v4);

        bool firstPairEqualLength = AreVectorsEqualLength(v1, v3);
        bool secondPairEqualLength = AreVectorsEqualLength(v2, v4);

        return firstPairParallel && secondPairParallel && firstPairEqualLength && secondPairEqualLength;
    }

    private (double X, double Y) GetVector(List<double> a, List<double> b)
    {
        return (b[0] - a[0], b[1] - a[1]);
    }

    private bool AreVectorsParallel((double X, double Y) v1, (double X, double Y) v2)
    {
        double cross = v1.X * v2.Y - v1.Y * v2.X;
        return Math.Abs(cross) < 1e-8;
    }

    private bool AreVectorsEqualLength((double X, double Y) v1, (double X, double Y) v2)
    {
        double len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
        double len2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
        return Math.Abs(len1 - len2) < 1e-8;
    }

    private async void P2PButton_Click(object sender, EventArgs e)
    {
        EndRectangleDrawingMode();
        if (!(_geometryEditor.Tool is VertexTool))
        {
            SetDefaultGeometryEditorStyle();
        }

        _selectedGraphic = null;
        _geometryEditor.Start(GeometryType.Multipoint);
        isCreatingGraphics = true;
    }

    private async void PolylineButton_Click(Object sender, EventArgs e)
    {
        EndRectangleDrawingMode();
        if (!(_geometryEditor.Tool is VertexTool))
        {
            SetDefaultGeometryEditorStyle();
        }
        _selectedGraphic = null;
        _geometryEditor.Start(GeometryType.Polyline);
        isCreatingGraphics = true;
    }

    private async void StopGeometryEditor()
    {
        if (_geometryEditor.IsStarted)
        {
            _geometryEditor.Stop();
            isCreatingGraphics = false;
        }
    }

    private void DeleteButton_Click(object sender, EventArgs e)
    {
        if (_selectedGraphic != null)
        {
            MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
        }
        _geometryEditor.Stop();
        appState.UpdatePolygonCoordinates(null, null);
    }

    // Hides edit tools when exiting the offline map creation
    private async void CloseOffline_Click(object sender, EventArgs e)
    {
        StopGeometryEditor();
        IsOffline = false;
        SurveyTemplateVisibility = false;
    }


    private void SaveSurveyGraphic_Click(object sender, EventArgs e)
    {
        try
        {
            EndRectangleDrawingMode();
            // Check if the geometry editor is actively sketching
            if (_geometryEditor.IsStarted)
            {
                // Access the completed geometry (polygon) from the GeometryEditor
                Geometry geometry = _geometryEditor.Geometry;
                List<MapCoordinateDetails> mapDetailsList = new List<MapCoordinateDetails>();

                if (geometry is Polygon polygon)
                {
                    //Offline map
                    if (_geometryEditor.Tool is ShapeTool shapeTool)
                    {
                        Envelope offlineArea = polygon.Extent;
                        GenerateOfflineMap(offlineArea, appState.CurrentPath);
                    }
                    //Polygon
                    else if (_geometryEditor.Tool is VertexTool vertexTool)
                    {
                        var coordinatesList = ConvertGeometryToCoordinates(polygon);

                        if (isRectangleDrawingMode && !ParallelogramValidator(coordinatesList))
                        {
                            Application.Current.MainPage.DisplayAlert("Error", $"The figure is not a parallelogram. Please modify it.", "OK");
                            return;
                        }
                        var mapCoordinateDetails = new MapCoordinateDetails
                        {
                            Coordinates = coordinatesList,
                            GeometryType = GeoType.Polygon.ToString(),
                            IsFirst = false,
                        };

                        if (_selectedGraphic != null)
                        {
                            MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
                            _selectedGraphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
                            mapCoordinateDetails.FileName = jsonFilePath?.ToString();
                        }

                        mapDetailsList.Add(mapCoordinateDetails);
                        // Update Blazor UI when polygon is created
                        appState.UpdatePolygonCoordinates(coordinatesList, "Polygon");

                    }
                }
                else if (geometry is Polyline polyline)
                {
                    var coordinatesList = ConvertGeometryToCoordinates(polyline);
                    var mapCoordinateDetails = new MapCoordinateDetails
                    {
                        Coordinates = coordinatesList,
                        GeometryType = GeoType.Polyline.ToString(),
                        IsFirst = false
                    };

                    if (_selectedGraphic != null)
                    {
                        MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
                        _selectedGraphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
                        mapCoordinateDetails.FileName = jsonFilePath?.ToString();
                    }

                    mapDetailsList.Add(mapCoordinateDetails);
                    // Update Blazor UI when polygon is created
                    appState.UpdatePolygonCoordinates(coordinatesList, "Polyline");

                }
                else if (geometry is Multipoint multiPoint)
                {
                    var coordinatesList = ConvertGeometryToCoordinates(multiPoint);
                    var mapCoordinateDetails = new MapCoordinateDetails
                    {
                        Coordinates = coordinatesList,
                        GeometryType = GeoType.MultiPoint.ToString(),
                        IsFirst = false
                    };

                    if (_selectedGraphic != null)
                    {
                        MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics.Remove(_selectedGraphic);
                        _selectedGraphic.Attributes.TryGetValue("jsonFilePath", out var jsonFilePath);
                        mapCoordinateDetails.FileName = jsonFilePath?.ToString();
                    }

                    mapDetailsList.Add(mapCoordinateDetails);
                    // Update Blazor UI when polygon is created
                    appState.UpdatePolygonCoordinates(coordinatesList, "MultiPoint");

                }

                DisplayBoundaryGraphicsOnMap(mapDetailsList);
                isCreatingGraphics = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in SavePolygonButton_Click : {ex.Message}");
        }
    }

    private void DisplayBoundaryGraphicsOnMap(List<MapCoordinateDetails> mapCoordinateDetails)
    {
        foreach (var mapCoordinate in mapCoordinateDetails)
        {
            var coordinates = mapCoordinate.Coordinates;
            var filename = mapCoordinate.FileName;
            var geometryType = mapCoordinate.GeometryType;
            var sampleUnitStatus = mapCoordinate.SampleUnitStatus;
            var surveyStatus = mapCoordinate.SurveyStatus;
            var startChainage = mapCoordinate.StartChainage;

            if (geometryType == null)
            {
                Log.Error("No GeoType found in DisplayBoundaryGraphicsOnMap. Stopping the process..");
                return;
            }

            if (_geometryEditor.IsStarted)
            {
                _geometryEditor.Stop();
            }

            var attribute = filename != null ? new Dictionary<string, object> { { "jsonFilePath", filename }, { "startChainage", startChainage } } : null;

            var mapPoints = new List<MapPoint>();
            foreach (var coordinate in coordinates)
            {
                double x = coordinate[0];
                double y = coordinate[1];
                mapPoints.Add(new MapPoint(x, y, SpatialReferences.Wgs84));
            }

            Graphic graphic = null;
            System.Drawing.Color graphicColor;
            switch (surveyStatus)
            {
                case "New":
                    graphicColor = System.Drawing.Color.FromArgb(200, 255, 63, 95); //red
                    break;
                case "OnGoing":
                    graphicColor = System.Drawing.Color.FromArgb(200, 255, 181, 69); //orange
                    break;
                case "Completed":
                    graphicColor = System.Drawing.Color.FromArgb(200, 61, 203, 108);//green
                    break;
                default:
                    graphicColor = System.Drawing.Color.FromArgb(255, 255, 0, 0); //default red
                    break;
            }

            if (geometryType == GeoType.Polygon.ToString())
            {
                var polygonGeometry = new Polygon(mapPoints);
                //the polygon from JSON
                if (filename != null)
                {
                    if (sampleUnitStatus == true)
                    {
                        var symbol = viewModel.CreateFillLineSymbol(0, 255, 0, 255, 5);
                        attribute.Add("Status", sampleUnitStatus);
                        graphic = new Graphic(polygonGeometry, attribute, symbol);
                    }
                    else
                    {
                        if (!graphicColor.IsEmpty)
                        {
                            var symbol = viewModel.CreateFillLineSymbol(graphicColor.R, graphicColor.G, graphicColor.B, graphicColor.A, 5);
                            attribute.Add("Status", sampleUnitStatus);
                            graphic = new Graphic(polygonGeometry, attribute, symbol);
                        }
                    }
                }
                //new polygon
                else
                {
                    var symbol = viewModel.CreateFillLineSymbol(0, 255, 255, 180, 5);
                    graphic = new Graphic(polygonGeometry, symbol);
                }

            }
            else if (geometryType == GeoType.Polyline.ToString())
            {
                var polylineGeometry = new Polyline(mapPoints);

                if (filename != null)
                {
                    if (!graphicColor.IsEmpty)
                    {
                        var symbol = viewModel.CreateSimpleLineSymbol(graphicColor.R, graphicColor.G, graphicColor.B, graphicColor.A, 5);
                        graphic = new Graphic(polylineGeometry, attribute, symbol);
                    }
                }
                //New polyline
                else
                {
                    // Symbol for the polyline
                    var symbol = viewModel.CreateSimpleLineSymbol(0, 255, 255, 255, 5);
                    graphic = new Graphic(polylineGeometry, symbol);
                }
            }
            else if (geometryType == GeoType.MultiPoint.ToString())
            {
                var multipointGeometry = new Multipoint(mapPoints);

                if (filename != null)
                {
                    if (!graphicColor.IsEmpty)
                    {
                        var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, graphicColor, 10);
                        graphic = new Graphic(multipointGeometry, attribute, pointSymbol);
                    }
                }
                //New Mappoints
                else
                {
                    var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Aqua, 10);
                    graphic = new Graphic(multipointGeometry, pointSymbol);
                }
            }


            if (graphic != null)
            {
                var overlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay");
                if (overlay != null)
                {
                    var existingGraphic = overlay.Graphics
                        .FirstOrDefault(g => g.Attributes.TryGetValue("jsonFilePath", out var existingPath) && existingPath.ToString() == filename);

                    if (existingGraphic != null)
                    {
                        existingGraphic.Geometry = graphic.Geometry;
                        existingGraphic.Symbol = graphic.Symbol;
                        existingGraphic.Attributes.Clear();
                        foreach (var kvp in graphic.Attributes)
                        {
                            existingGraphic.Attributes[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        graphic.ZIndex = -1;
                        overlay.Graphics.Add(graphic);
                    }
                }

                //only at last, move the map viewpoint to the survey graphics
                if (mapCoordinate.IsFirst)
                {
                    MapPoint centerPoint = graphic.Geometry.Extent.GetCenter();
                    MyMapView.SetViewpointAsync(new Viewpoint(centerPoint.Y, centerPoint.X, 500));
                }
            }
        }
    }

    private List<List<double>> ConvertGeometryToCoordinates(Geometry geometry)
    {
        var coordinatesList = new List<List<double>>();

        if (geometry is Polygon polygon)
        {
            foreach (var part in polygon.Parts)
            {
                foreach (var point in part.Points)
                {
                    MapPoint wgs84Point = (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);

                    coordinatesList.Add(new List<double> { wgs84Point.X, wgs84Point.Y });
                }
            }
        }
        else if (geometry is Polyline polyline)
        {
            foreach (var part in polyline.Parts)
            {
                foreach (var point in part.Points)
                {
                    MapPoint wgs84Point = (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);

                    coordinatesList.Add(new List<double> { wgs84Point.X, wgs84Point.Y });
                }
            }
        }
        else if (geometry is Multipoint multipoint)
        {
            foreach (var point in multipoint.Points)
            {
                MapPoint wgs84Point = (MapPoint)GeometryEngine.Project(point, SpatialReferences.Wgs84);

                coordinatesList.Add(new List<double> { wgs84Point.X, wgs84Point.Y });
            }
        }

        return coordinatesList;
    }


    private void HighlightSurveyTemplateGraphics(string jsonFilePath, string status)
    {
        if (status == "Click")
        {
            if (jsonFilePath == null && previousSurveySetGraphic != null)
            {
                previousSurveySetGraphic.IsSelected = false;
                previousSurveySetGraphic = null;
                return;
            }

            // Iterate through the graphics in the graphicOverlay
            if (MyMapView?.GraphicsOverlays?.FirstOrDefault(o => o.Id == "surveySetOverlay")?.Graphics != null)
            {
                foreach (var graphic in MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics)
                {
                    // Check if the graphic has the jsonFilePath attribute
                    if (graphic.Attributes.TryGetValue("jsonFilePath", out var filePath) && filePath.ToString() == jsonFilePath)
                    {
                        graphic.IsSelected = true;

                        MapPoint centerPoint = graphic.Geometry.Extent.GetCenter();

                        //if it is too far, zoom in
                        if (MyMapView.MapScale > 20000)
                        {
                            MyMapView.SetViewpoint(new Viewpoint(centerPoint.Y, centerPoint.X, 1000));
                        }

                        Envelope visibleAreaGeographic = (Envelope)GeometryEngine.Project(MyMapView.VisibleArea.Extent, SpatialReferences.Wgs84);
                        if (!visibleAreaGeographic.Intersects(centerPoint))
                        {
                            MyMapView.SetViewpoint(new Viewpoint(centerPoint.Y, centerPoint.X, MyMapView.MapScale));
                        }

                        if (previousSurveySetGraphic != null && previousSurveySetGraphic != graphic)
                        {
                            previousSurveySetGraphic.IsSelected = false;
                        }
                        previousSurveySetGraphic = graphic;

                        // Check for start and end segments
                        if (graphic.Attributes.TryGetValue("startSegment", out var startSegment) && startSegment != null &&
                            graphic.Attributes.TryGetValue("endSegment", out var endSegment) && endSegment != null)
                        {
                            // Clear previous segment highlights
                            foreach (var segGraphic in previousSegmentsSurveySetGraphics)
                            {
                                segGraphic.IsSelected = false;
                            }
                            previousSegmentsSurveySetGraphics.Clear();

                            // Retrieve segments from the segment layer between startSegment and endSegment
                            var segmentGraphics = MyMapView.GraphicsOverlays
                                .FirstOrDefault(o => o.Id == LayerNames.Segment)?.Graphics;

                            if (segmentGraphics != null)
                            {
                                var startSegId = Convert.ToInt32(startSegment);
                                var endSegId = Convert.ToInt32(endSegment);

                                var segmentsToHighlight = segmentGraphics
                                    .Where(s =>
                                        s.Attributes.TryGetValue("segmentId", out var segmentId) &&
                                        segmentId != null &&
                                        Convert.ToInt32(segmentId) >= startSegId &&
                                        Convert.ToInt32(segmentId) <= endSegId)
                                    .ToList();

                                foreach (var segmentGraphic in segmentsToHighlight)
                                {
                                    // Highlight each segment graphic
                                    segmentGraphic.IsSelected = true;
                                    previousSegmentsSurveySetGraphics.Add(segmentGraphic);
                                }
                            }
                        }

                        return;
                    }
                }
            }
        }
        else if (status == "Edit")
        {
            IsEditing = true;
            SetDefaultGeometryEditorStyle();
            foreach (var graphic in MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics)
            {
                // Check if the graphic has the jsonFilePath attribute
                if (graphic.Attributes.TryGetValue("jsonFilePath", out var filePath) && filePath.ToString() == jsonFilePath)
                {
                    _geometryEditor.Start(graphic.Geometry);
                    _selectedGraphic = graphic;
                    isCreatingGraphics = true;
                    break;
                }
            }

            if (previousSurveySetGraphic != null)
            {
                previousSurveySetGraphic.IsSelected = false;
                previousSurveySetGraphic = null;
            }
        }
        else if (status == "PCISampleUnit")
        {
            if (MyMapView?.GraphicsOverlays?.FirstOrDefault(o => o.Id == "surveySetOverlay")?.Graphics != null)
            {
                foreach (var graphic in MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay").Graphics)
                {
                    // Check if the graphic has the jsonFilePath attribute
                    if (graphic.Attributes.TryGetValue("jsonFilePath", out var filePath) && filePath.ToString() == jsonFilePath)
                    {
                        graphic.IsSelected = false;
                        var polygonSymbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Lime, 5);
                        graphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Transparent, polygonSymbolOutline);
                    }
                }
            }
        }
    }

    private void ControlIsEditing(bool editing)
    {
        IsEditing = editing;
    }
    private VertexTool _moveOnlyGeometryEditor;
    private void SetMoveOnlyGeometryEditorStyle()
    {
        if (_moveOnlyGeometryEditor == null)
        {
            var outlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1);
            var fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Transparent, outlineSymbol);
            var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 0.5);
            var feedbackLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 1);

            _moveOnlyGeometryEditor = new VertexTool
            {
                Configuration = { AllowRotatingSelectedElement = false },
                Style = new GeometryEditorStyle
                {
                    MidVertexSymbol = null,
                    SelectedMidVertexSymbol = null,
                    LineSymbol = lineSymbol,
                    FillSymbol = fillSymbol,
                    VertexSymbol = null,  // Disable vertex editing
                    SelectedVertexSymbol = null,// Disable vertex editing
                    FeedbackLineSymbol = feedbackLineSymbol,
                    FeedbackVertexSymbol = null,
                    BoundingBoxHandleSymbol = null, //Disable scaling 
                    VertexTextSymbol = null,
                }
            };
        }
        _geometryEditor.Tool = _moveOnlyGeometryEditor;
    }

    private void ClearSurveySetOverlay()
    {
        var surveySetOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay");
        if (surveySetOverlay != null)
        {
            surveySetOverlay.Graphics.Clear();
        }

        RemoveTempGraphic();
        EndRectangleDrawingMode();
    }

    //Offline Map
    private void SelectOfflineMapArea(bool isOfflineOnly)
    {
        ShapeTool shapeTool = ShapeTool.Create(ShapeToolType.Rectangle);
        _geometryEditor.Tool = shapeTool;
        GeometryEditorStyle geometryEditorStyle = new GeometryEditorStyle
        {
            LineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 1),
            FillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(80, System.Drawing.Color.Cyan), new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Gray, 1)),
            VertexSymbol = null,
            SelectedVertexSymbol = null,
            FeedbackLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Cyan, 2),
            FeedbackVertexSymbol = null,
        };
        shapeTool.Style = geometryEditorStyle;
        if (!_geometryEditor.IsStarted)
        {
            _geometryEditor.Start(GeometryType.Polygon);
        }
        if (isOfflineOnly)
            IsOffline = true;
    }


    private ExportTileCacheJob _job;
    private Uri _serviceUri = new Uri("https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer");
    private List<double> scaleList = new List<double>()
        {
            591657527.59,295828763.80,147914381.90,73957190.95,36978595.47,18489297.74,9244648.87,4622324.43,2311162.22,1155581.11,
            577790.55,288895.28,144447.64,72223.82,36111.91,18055.95,9027.98,4513.99,2256.99,1128.50,564.25,282.12,141.06,70.53
        };
    private int totalGrids;
    private int currentGrid;
    private async void GenerateOfflineMap(Esri.ArcGISRuntime.Geometry.Envelope offlineArea, string folderPath)
    {
        try
        {
            Log.Information("Step 1 Check selected offline map size is correct");

            double mapWidth = offlineArea.Extent.Width;
            double mapHeight = offlineArea.Extent.Height;
            double mapArea = mapWidth * mapHeight;
            const double minMapArea = 100000;

            if (mapArea < minMapArea)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "The selected map area is too small. Please select a larger area.", "OK");
                return;
            }

            Log.Information("Step 2 Generate offline map");
            string offlineMapDirectory = AppPaths.OfflineMapFolder;
            if (!Directory.Exists(offlineMapDirectory))
            {
                Directory.CreateDirectory(offlineMapDirectory);
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No filepath is selected to save offline map.", "OK");
                return;
            }

            string fileName;
            if (folderPath.Contains(".tpkx"))
            {
                fileName = Path.GetFileNameWithoutExtension(folderPath);
                offlineMapDirectory = Path.Combine(offlineMapDirectory, fileName);
                Directory.CreateDirectory(offlineMapDirectory);
            }
            else
            {
                fileName = Path.GetFileName(folderPath) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            busyIndicator.IsVisible = true;

            // Create the tile layer.
            ArcGISTiledLayer mapLayer = new ArcGISTiledLayer(_serviceUri);
            await mapLayer.LoadAsync();
            Log.Information("Step 3 Generate offline map - map layer loaded");

            Map mainMap = new Map(new Basemap(mapLayer))
            {
                MaxScale = scaleList[18],
                MinScale = scaleList[13]
            };

            // Grid layout
            double gridSize = 20000000;
            int grids = (int)Math.Ceiling(mapArea / gridSize);
            int columns = (int)Math.Ceiling(Math.Sqrt(grids));
            int rows = (int)Math.Ceiling((double)grids / columns);
            double colWidth = mapWidth / columns;
            double rowHeight = mapHeight / rows;

            // Progress tracking
            totalGrids = rows * columns;
            currentGrid = 0;

            _cts = new CancellationTokenSource();
            ExportTileCacheTask exportTask = await ExportTileCacheTask.CreateAsync(_serviceUri);

            // divides selected offlinearea into smaller map files for the export to create larger maps. 
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (_cts.IsCancellationRequested) 
                        return;

                    Envelope gridArea = BuildGridArea(offlineArea, row, col, rowHeight, colWidth, rows, columns);

                    string gridFileName = $"{fileName}_R{row}_C{col}.tpkx";
                    string offlinePath = Path.Combine(offlineMapDirectory, gridFileName);

                    ExportTileCacheParameters parameters = new ExportTileCacheParameters
                    {
                        AreaOfInterest = gridArea,
                        CompressionQuality = 100
                    };

                    for (int x = 13; x <= 18; x++)
                        parameters.LevelIds.Add(x);

                    await CreateMapTiles(exportTask, parameters, offlinePath, row, col);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in GenerateOfflineMap : {ex.Message}");
            // Show an alert with the error message.
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to generate offline map: {ex.Message}", "OK");
        }
        finally
        {
            if (_job != null)
            {
                // Remove the event handlers
                _job.ProgressChanged -= Job_ProgressChanged;
            }
            busyIndicator.IsVisible = false;
            if (IsOffline)
                CloseOffline_Click(this, new EventArgs());
            else
                StopGeometryEditor();
        }
    }

    private CancellationTokenSource _cts;
    private void CancelMapCreationJobButton_Click(object sender, EventArgs e)
    {
        //user canceled the job
        _cts.Cancel();
        _job.CancelAsync();
    }

    private async void Job_ProgressChanged(object? sender, EventArgs e)
    {
        try
        {
            ExportTileCacheJob job = sender as ExportTileCacheJob;

            double overallProgress = ((double)currentGrid / totalGrids) * 100.0 + (job.Progress / 100.0) * (100.0 / totalGrids);

            // Dispatch to the UI thread.
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                // Show the percent complete and update the progress bar.
                Percentage.Text = $"{overallProgress:F0} %";
                progressBar.Progress = overallProgress / 100.0;
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Error in Job_ProgressChanged {ex.Message}");
        }
    }

    private Envelope BuildGridArea(Envelope offlineArea, int row, int col, double rowHeight, double colWidth, int columns, int rows)
    {
        double xmin = offlineArea.XMin + col * colWidth;
        double xmax = offlineArea.XMin + (col + 1) * colWidth;
        double ymin = offlineArea.YMin + row * rowHeight;
        double ymax = offlineArea.YMin + (row + 1) * rowHeight;

        if (col == columns - 1)
            xmax = offlineArea.XMax;

        if (row == rows - 1)
            ymax = offlineArea.YMax;

        return new Envelope(xmin, ymin, xmax, ymax, offlineArea.SpatialReference);
    }

    private async Task CreateMapTiles(ExportTileCacheTask exportTask, ExportTileCacheParameters parameters, string offlinePath, int row, int col)
    {
        int attempt = 0;
        bool success = false;
        int maxRetries = 3;

        while (attempt < maxRetries && !success) // MAP TILE CREATION RANDOMLY FAILS - TEMPORARY FIX TO RETRY IF TILE FAILS
        {
            attempt++;

            if (_cts.IsCancellationRequested) // Deletes directory when cancelled
            {
                var directory = Path.GetDirectoryName(offlinePath);
                Directory.Delete(directory, recursive: true);
                return;
            }

            try
            {
                _job = exportTask.ExportTileCache(parameters, offlinePath);
                _job.ProgressChanged += Job_ProgressChanged;
                _job.Start();
                Log.Information($"Exporting grid ({row},{col}) to {offlinePath}");

                TileCache resultTileCache = await _job.GetResultAsync();

                if (_job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
                {
                    currentGrid++;
                    Log.Information($"Offline map grid generated: {offlinePath}");
                    success = true;
                    //// Copy file to survey template folder if needed
                    //string secondaryOfflinePath;
                    //if (!folderPath.Contains(".tpkx"))
                    //    secondaryOfflinePath = Path.Combine(appState.CurrentPath, gridFileName);
                    //else
                    //    secondaryOfflinePath = appState.CurrentPath;

                    //if (!Path.Equals(offlinePath, secondaryOfflinePath))
                    //{
                    //    File.Copy(offlinePath, secondaryOfflinePath, overwrite: true);
                    //    Log.Information($"File copied to: {secondaryOfflinePath}");
                    //}
                }
                else if (_job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
                {
                    Log.Error($"Failed to generate offline map for grid ({row},{col})");
                    await Application.Current.MainPage.DisplayAlert("Alert", $"Generate offline map for grid ({row},{col}) failed.", "OK");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Tile r{row} c{col} FAILED: {ex.Message}");
            }
            finally
            {
                _job.ProgressChanged -= Job_ProgressChanged;
            }
        }
    }

    #endregion

    private List<Graphic> _oldGraphics = new List<Graphic>();

    private async void MultiSelectCrackClassification(object sender, EventArgs e)
    {
        await MultiSelectCracksHandler(LayerNames.CrackClassification);
        _geometryEditor.PropertyChanged -= MultiSelectCrackClassification;
    }

    private async void MultiSelectSummaryCracks(object sender, EventArgs e)
    {
        await MultiSelectCracksHandler(LayerNames.CrackSummary);
        _geometryEditor.PropertyChanged -= MultiSelectSummaryCracks;
    }

    private async Task MultiSelectCracksHandler(string layerType)
    {
        Geometry rectangleGeometry = _geometryEditor.Geometry;
        if (rectangleGeometry == null || rectangleGeometry.IsEmpty || !(rectangleGeometry is Polygon))
        {
            return;
        }
        List<Part> parts = new List<Part>(); // List to store parts of selected graphics

        var overlayIds = new List<string>();
        if (layerType == LayerNames.CrackClassification)
        {
            overlayIds = new List<string> { "Longitudinal", "Transversal", "Fatigue", "Others", "None" };
        }
        else if (layerType == LayerNames.CrackSummary)
        {
            overlayIds = new List<string> { "Crack Summary" };
        }
        if (overlayIds.Count == 0) return;


        List<GraphicsOverlay> selectableOverlays = MyMapView.GraphicsOverlays
            .Where(overlay => overlayIds.Contains(overlay.Id))
            .ToList();
        var graphicsCopy = new ConcurrentBag<Graphic>();

        // Gather all graphics from the selected overlays
        foreach (var overlay in selectableOverlays)
        {
            if (overlay.IsVisible)
            {
                foreach (var graphic in overlay.Graphics)
                {
                    graphicsCopy.Add(graphic);
                }
            }
        }

        var spatialReference = MyMapView.SpatialReference;
        if (spatialReference != null)
        {
            var projectedRectangleGeometry = GeometryEngine.Project(rectangleGeometry, spatialReference);
            try
            {
                var projectedGraphics = graphicsCopy.Select(graphic =>
                {
                    var projectedGeometry = GeometryEngine.Project(graphic.Geometry, spatialReference);
                    return new { Graphic = graphic, ProjectedGeometry = projectedGeometry };
                });

                // Use a spatial query to find graphics within the rectangle area
                var graphicsInRectangle = projectedGraphics.Where(item =>
                    GeometryEngine.Intersects(projectedRectangleGeometry, item.ProjectedGeometry))
                    .Select(item => item.Graphic).ToList();

                if (graphicsInRectangle != null && graphicsInRectangle.Count > 0)
                {
                    // Store the selected graphics
                    _oldGraphics.AddRange(graphicsInRectangle);

                    //highlight graphics
                    foreach (var graphic in graphicsInRectangle)
                    {
                        graphic.IsSelected = true;
                    }
                    _geometryEditor.Stop();
                }
                else
                {
                    _geometryEditor.Stop();
                    ClearCrackMultiSelect();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"error in selecting graphics: {ex.Message}");
            }
        }
    }

    private async void MultiSelectSegments(object sender, EventArgs e)
    {
        _geometryEditor.PropertyChanged -= MultiSelectSegments;

        GraphicsOverlay segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == LayerNames.Segment);

        var rectangleGeometry = _geometryEditor.Geometry;
        if (rectangleGeometry == null || rectangleGeometry.IsEmpty || !(rectangleGeometry is Polygon) || segmentOverlay == null) // returns if previous rectangle selected nothing
        {
            _geometryEditor.Stop();
            return;
        }

        if (rectangleGeometry != null && rectangleGeometry is Polygon && segmentOverlay != null)
        {
            List<Graphic> selectedGraphics = new List<Graphic>(); // List to store selected graphics
            List<Geometry> selectedGeometries = new List<Geometry>(); // List to store geometries of selected graphics
            List<Part> parts = new List<Part>(); // List to store parts of selected graphics

            // Step 1: Get Selected graphics with the rectangle 
            try
            {
                // Obtain the spatial reference from the MapView
                var spatialReference = MyMapView.SpatialReference;
                if (spatialReference != null)
                {
                    var projectedRectangleGeometry = GeometryEngine.Project(rectangleGeometry, spatialReference);

                    var projectedGraphics = segmentOverlay.Graphics
                         .Where(graphic => graphic.IsVisible)
                         .Select(graphic =>
                         {
                             var projectedGeometry = GeometryEngine.Project(graphic.Geometry, spatialReference);
                             return new { Graphic = graphic, ProjectedGeometry = projectedGeometry };
                         });

                    // Use a spatial query to find graphics within the rectangle area
                    var graphicsInRectangle = projectedGraphics.Where(item =>
                        GeometryEngine.Intersects(projectedRectangleGeometry, item.ProjectedGeometry))
                        .Select(item => item.Graphic);
                    if (!graphicsInRectangle.Any() || appState.segmentCount == 0)
                    {
                        IsMovingSegments = false;
                        _geometryEditor.Stop();
                        await Task.Delay(100);
                        SelectMapArea();
                        return;
                    }

                    HandleMultipleSegmentSelection(graphicsInRectangle, previousGraphics);//highlights segments 
                                                                                          // Store the selected graphics
                    selectedGraphics.AddRange(graphicsInRectangle);
                    selectedGeometries.AddRange(graphicsInRectangle.Select(graphic => graphic.Geometry));

                    Log.Information("Starting selection of segments inside free hand tool");
                    // Create parts for each selected graphic
                    foreach (var graphic in graphicsInRectangle)
                    {
                        if (graphic.Geometry is Polygon polygon)
                        {
                            foreach (var part in polygon.Parts)
                            {
                                // Convert ReadOnlyPart to Part
                                var newPart = new Part(graphic.Geometry.SpatialReference);
                                foreach (var point in part.Points)
                                {
                                    newPart.AddPoint(point);
                                }
                                parts.Add(newPart);
                            }
                        }
                    }
                    Log.Information("Finished selection of segments inside free hand tool");
                }
                else
                {
                    Console.WriteLine("Spatial Reference is null.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the spatial query: {ex.Message}");
            }

            _geometryEditor.PropertyChanged -= MultiSelectSegments;
            IsMovingSegments = true;
            _geometryEditor.Stop();
            _oldGraphics = selectedGraphics;

            //Step 2: Get the new location for the segments by joining them into one polygon to start graphic editor 
            var combinedPolygon = new Polygon(parts);

            SetMoveOnlyGeometryEditorStyle();
            _geometryEditor.Start(combinedPolygon);
            _geometryEditor.SelectGeometry();

        }
    }

    //this is triggered with enter after selecting segments to move 
    private async void MoveAndSaveMultiSegments()
    {
        IsMovingSegments = false;
        IsEditingDatabase = true;

        if (_geometryEditor.Geometry != null)
        {
            // Step 1: Get the updated geometry from the geometry editor
            Geometry updatedGeometry = _geometryEditor.Geometry;

            // Step 2: Split the updated geometry into parts
            List<Part> updatedParts = new List<Part>();

            if (updatedGeometry is Polygon polygon)
            {
                foreach (var part in polygon.Parts)
                {
                    // Convert ReadOnlyPart to Part
                    var newPart = new Part(updatedGeometry.SpatialReference);
                    foreach (var point in part.Points)
                    {
                        newPart.AddPoint(point);
                    }
                    updatedParts.Add(newPart);
                }
            }
            if (viewModel.segmentSymbol != null)
            {
                Parallel.ForEach(previousGraphics, g => g.Symbol = viewModel.segmentSymbol);
            }

            _geometryEditor.Stop();

            // Step 3: Assign each part to each old graphic and calculate offset 
            if (_oldGraphics.Count > 0 && updatedParts.Count > 0 && _oldGraphics.Count == updatedParts.Count)
            {
                //calculate offset once with the first segments of the list
                double[] offset = CalculateOffset(_oldGraphics[0].Geometry, new Polygon(new List<Part> { updatedParts[0] }));
                var segmentIds = _oldGraphics.Select(g => Convert.ToInt32(g.Attributes["Id"])).ToList();

                Parallel.ForEach(_oldGraphics.Zip(updatedParts, (g, p) => new { Graphic = g, Part = p }), async pair =>
                {
                    var graphic = pair.Graphic;
                    var part = pair.Part;

                    var attributes = graphic.Attributes;
                    var segmentId = attributes["SectionId"].ToString();
                    var segmentDBid = Convert.ToInt32(attributes["Id"]);

                    // Assign the new geometry (part) to the old graphic
                    graphic.Geometry = new Polygon(new List<Part> { part });

                    //                    segmentIds.Add(segmentDBid);

                    MoveGraphicsBySegment(segmentId, offset[0], offset[1]);
                });

                Log.Information($"Number or Segments Added:  segmentIds..Count: {segmentIds.Count} ");

                SegmentMovementRequest movementRequest = new SegmentMovementRequest
                {
                    SegmentIds = segmentIds,
                    HorizontalOffset = offset[0],
                    VerticalOffset = offset[1]
                };

                // Await all the update tasks
                await appEngine.SegmentService.UpdateSegmentOffsetInDB(movementRequest);
            }
        }

        //remove the database update message if geometry editor is null
        IsEditingDatabase = false;
    }

    private double[] CalculateOffset(Geometry oldGeometry, Esri.ArcGISRuntime.Geometry.Polygon newGeometry)
    {
        MapPoint oldCentroid = GeometryEngine.LabelPoint(oldGeometry as Polygon) as MapPoint;
        MapPoint newCentroid = GeometryEngine.LabelPoint(newGeometry) as MapPoint;
        double offsetX = newCentroid.X - oldCentroid.X;
        double offsetY = newCentroid.Y - oldCentroid.Y;

        return new double[] { offsetX, offsetY };
    }

    private List<Graphic> previousGraphics = new List<Graphic>();
    private List<SimpleFillSymbol> previousSymbols = new List<SimpleFillSymbol>();

    /// <summary>
    /// Highlights the segments that are selected and adds them to previousGraphics(Listgraphic)
    /// </summary>
    /// <param name="segmentGraphics"></param>
    /// <param name="previousGraphics"></param>
    private void HandleMultipleSegmentSelection(IEnumerable<Graphic> segmentGraphics, List<Graphic> previousGraphics)
    {
        var highlightedSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(191, 0, 255), 1));
        var segmentGraphicSet = new HashSet<Graphic>(segmentGraphics);

        foreach (var graphic in segmentGraphicSet)
        {
            if (graphic.Symbol is SimpleFillSymbol fillSymbol) // Duplicates and saves previous symbol colour 
            {
                var clone = new SimpleFillSymbol(
                    fillSymbol.Style,
                    fillSymbol.Color,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, fillSymbol.Outline.Color, fillSymbol.Outline.Width)
                );
                previousSymbols.Add(clone);
            }
            graphic.Symbol = highlightedSymbol;
        }
        if (viewModel.segmentSymbol != null)
        {
            for (int i = previousGraphics.Count - 1; i >= 0; i--)
            {
                var previousGraphic = previousGraphics[i];
                if (!segmentGraphicSet.Contains(previousGraphic))
                {
                    previousGraphic.Symbol = viewModel.segmentSymbol;
                    previousGraphics.RemoveAt(i);
                }
            }
        }

        previousGraphics.AddRange(segmentGraphicSet);
    }

    private Graphic FindGraphicForLayerAndId(string layerID, int itemId)
    {
        foreach (var overlay in MyMapView.GraphicsOverlays)
        {
            if (overlay.Id == layerID)
            {
                foreach (var graphic in overlay.Graphics)
                {
                    if (graphic.Attributes.TryGetValue("Id", out var graphicItemId) && graphicItemId.Equals(itemId))
                    {
                        return graphic;
                    }
                }
            }
        }
        return null;

    }
    private async void Handle_TextChanged(object sender, System.EventArgs e)
    {
        try
        {
            GraphicsOverlay pinOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "pinOverlay");
            if (pinOverlay != null)
            {
                MyMapView.GraphicsOverlays.Remove(pinOverlay);
            }

            // Get the coordinate in the search bar
            //string[] coord = MySearchBar.Text.Split(",");
            // if (coord.Length != 2)
            //{
            // / await App.Current.MainPage.DisplayAlert("Alert", "Invalid coordinate format. Please use 'longitude, latitude' format.", "OK");
            // return;
            //}
            double longitude, latitude;

            //if (!double.TryParse(coord[0], out longitude) || !double.TryParse(coord[1], out latitude))
            {
                await App.Current.MainPage.DisplayAlert("Alert", "Invalid coordinate format. Please use 'longitude, latitude' format.", "OK");
                return;
            }

            if (!CoordinateExists(longitude, latitude))
            {
                await App.Current.MainPage.DisplayAlert("Alert", "The provided coordinate does not exist.", "OK");
                return;
            }

            Envelope fullExtent = MyMapView.Map.Basemap.BaseLayers[0].FullExtent;
            Envelope currentGeoExtent = (Envelope)fullExtent.Project(SpatialReferences.Wgs84);
            MapPoint mapPoint = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
            if (!currentGeoExtent.Contains(mapPoint))
            {
                //MySearchBar.Text = string.Empty;
                await App.Current.MainPage.DisplayAlert("Alert", "The coordinate is not within the current offline map.", "OK");
            }
            else
            {
                Graphic point = await GraphicForPoint(mapPoint);
                GraphicsOverlay newOverlay = new GraphicsOverlay { Id = "pinOverlay" };
                newOverlay.Graphics.Add(point);
                MyMapView.GraphicsOverlays.Add(newOverlay);

                await MyMapView.SetViewpointCenterAsync(mapPoint, 5000);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
        }
    }
    private async Task<Graphic> GraphicForPoint(MapPoint point)
    {
        // Get current assembly that contains the image.
        Assembly currentAssembly = Assembly.GetExecutingAssembly();

        // Get image as a stream from the resources.
        Stream resourceStream = currentAssembly.GetManifestResourceStream(
            "DataView2.Resources.Images.pin.png");

        // Create new symbol using asynchronous factory method from stream.
        PictureMarkerSymbol pinSymbol = await PictureMarkerSymbol.CreateAsync(resourceStream);
        pinSymbol.Width = 30;
        pinSymbol.Height = 30;
        // The image is a pin; offset the image so that the pinpoint
        //     is on the point rather than the image's true center.
        pinSymbol.LeaderOffsetX = 30;
        pinSymbol.OffsetY = 14;
        return new Graphic(point, pinSymbol);
    }

    private bool CoordinateExists(double longitude, double latitude)
    {
        // Longitude should be in the range -180 to 180, and latitude should be in the range -90 to 90.
        if (longitude >= -180 && longitude <= 180 && latitude >= -90 && latitude <= 90)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SaveNewBoundaryPolygon()
    {
        if (_geometryEditor.IsStarted)
        {
            Geometry geometry = _geometryEditor.Geometry;

            if (geometry is Polygon polygon)
            {
                List<MapPoint> boundaryPoints = new List<MapPoint>();

                foreach (var part in polygon.Parts)
                {
                    foreach (var point in part.Points)
                    {
                        boundaryPoints.Add(point);
                    }
                }

                if (polygon.SpatialReference != SpatialReferences.Wgs84)
                {
                    boundaryPoints = boundaryPoints.Select(p => (MapPoint)GeometryEngine.Project(p, SpatialReferences.Wgs84)).ToList();
                }

                viewModel.DrawBoundariesOnMap(boundaryPoints);
                _geometryEditor.Stop();
                appState.NotifyGeometryEditorStopped();
            }
        }
    }

    private async void HandleDeleteSegments(List<Graphic> selectedGraphics)
    {
        try
        {
            if (isDeletingDefects)
            {
                return;
            }

            isDeletingDefects = true;

            // Create a copy of the selectedGraphics list
            var graphicsToDelete = new List<Graphic>(selectedGraphics);

            if (selectedGraphics.Count > 0)
            {
                string message;
                if (selectedGraphics.Count == 1)
                {
                    message = "Are you sure you want to delete a selected segment and its associated defects?";

                }
                else
                {
                    message = $"Are you sure you want to delete the {selectedGraphics.Count} selected segments and their associated defects?";
                }

                bool result = await App.Current.MainPage.DisplayAlert("Confirmation", message, "Yes", "No");

                if (result)
                {
                    RemoveAssociatedGraphics(graphicsToDelete);

                    await DeleteDefectsWithQuery(graphicsToDelete);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        finally
        {
            isDeletingDefects = false;
        }
    }

    private async void RemoveAssociatedGraphics(List<Graphic> segmentGraphics)
    {
        try
        {
            var overlayIds = TableNameHelper.GetAllLCMSOverlayIds();

            var tasks = segmentGraphics.Select(segmentGraphic =>
            {
                if (segmentGraphic.Attributes.TryGetValue("SurveyId", out var surveyId) && segmentGraphic.Attributes.TryGetValue("SegmentId", out var segmentId))
                {
                    var surveyIdStr = surveyId?.ToString();
                    var segmentIdStr = segmentId?.ToString();

                    foreach (var overlayId in overlayIds)
                    {
                        var overlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == overlayId);
                        if (overlay != null)
                        {
                            var graphicsToRemove = overlay.Graphics.Where(graphic =>
                                graphic.Attributes.TryGetValue("SurveyId", out var gSurveyId) &&
                                graphic.Attributes.TryGetValue("SegmentId", out var gSegmentId) &&
                                gSurveyId?.ToString() == surveyIdStr &&
                                gSegmentId?.ToString() == segmentIdStr).ToList();

                            foreach (var graphic in graphicsToRemove)
                            {
                                overlay.Graphics.Remove(graphic);
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            });
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in removing associated defects.{ex.Message}");
        }
    }

    public async Task DeleteDefectsWithQuery(List<Graphic> graphics)
    {
        try
        {
            var deletionInfosByTable = new ConcurrentDictionary<string, ConcurrentBag<string>>();
            bool isSegmentSelected = false;
            var crackingIdList = new List<CrackReference>();
            var tasks = graphics.Select(graphic =>
            {
                string id = string.Empty, table = string.Empty;
                if (graphic.Attributes.ContainsKey("LASfileId"))
                {
                    if (!graphic.Attributes.TryGetValue("LASfileId", out var idLasObj))
                        return Task.CompletedTask;
                    else
                    {
                        if (idLasObj != null)
                        {
                            id = idLasObj.ToString();
                            table = "LASFile";
                        }
                    }
                }
                else
                {
                    if (!graphic.Attributes.TryGetValue("Id", out var idObj) || !graphic.Attributes.TryGetValue("Table", out var tableObj))
                        return Task.CompletedTask;
                    else
                    {
                        id = idObj.ToString();
                        table = tableObj.ToString();
                    }
                }

                if (table == LayerNames.CornerBreak)
                {
                    var idArray = id.Split(',');

                    foreach (var idStr in idArray)
                    {
                        deletionInfosByTable.AddOrUpdate(table, new ConcurrentBag<string> { idStr }, (key, bag) => { bag.Add(idStr); return bag; });
                    }
                }
                else if (graphic.Attributes.TryGetValue("Type", out var graphicType) && graphicType.ToString() == "MetaTable")
                {
                    deletionInfosByTable.AddOrUpdate("MetaTableValue", new ConcurrentBag<string> { id }, (key, bag) => { bag.Add(id); return bag; });
                }
                else
                {
                    if (table.Contains("IRI"))
                    {
                        table = LayerNames.Roughness;
                    }

                    if (table.Contains("Rut") && !table.Contains("Las"))
                    {
                        table = LayerNames.Rutting;
                    }

                    if (table == LayerNames.Segment)
                    {
                        isSegmentSelected = true;
                    }

                    deletionInfosByTable.AddOrUpdate(table, new ConcurrentBag<string> { id }, (key, bag) => { bag.Add(id); return bag; });
                
                    //Delete associated spalling along with concrete joint
                    if (table == LayerNames.ConcreteJoint)
                    {
                        if (graphic.Attributes.TryGetValue("JointId", out var jointIdObj) &&
                        graphic.Attributes.TryGetValue("SurveyId", out var surveyIdObj) &&
                        graphic.Attributes.TryGetValue("SegmentId", out var segmentIdObj) &&
                        graphic.Attributes.TryGetValue("JointDirection", out var jointDirectionObj))
                        {
                            string jointIdStr = jointIdObj as string;
                            string surveyIdStr = surveyIdObj as string;
                            int segmentIdInt = Convert.ToInt32(segmentIdObj);
                            string jointDirectionStr = jointDirectionObj as string;

                            if (jointIdStr == null && surveyIdStr == null && jointDirectionStr == null) return Task.CompletedTask;

                            string numberPart = jointIdStr.Substring(1);
                            if (int.TryParse(numberPart, out int jointNumber))
                            {
                                //get spalling from db
                                var query = $"SELECT * FROM LCMS_Spalling_Raw WHERE SurveyId = '{surveyIdStr}' AND SegmentId = {segmentIdInt} AND JointId = {jointNumber} And JointDirection = '{jointDirectionStr}'";
                                var spallingIds = appEngine.SegmentService.ExecuteQueryAndReturnIds(query).Result;

                                if (spallingIds != null && spallingIds.Count > 0)
                                {
                                    foreach (var spallingId in spallingIds)
                                    {
                                        deletionInfosByTable.AddOrUpdate(LayerNames.Spalling, new ConcurrentBag<string> { spallingId.ToString() }, (key, bag) => { bag.Add(spallingId.ToString()); return bag; });
                                    }
                                }
                            }
                        }
                    }
                    else if (table == LayerNames.Cracking)
                    {
                        //Get cracking Id for those that need to be updated
                        if (graphic.Attributes.TryGetValue("CrackId", out var crackIdObj) &&
                        graphic.Attributes.TryGetValue("SurveyId", out var surveyIdObj) &&
                        graphic.Attributes.TryGetValue("SegmentId", out var segmentIdObj))
                        {
                            int crackId = Convert.ToInt32(crackIdObj);
                            int segmentId = Convert.ToInt32(segmentIdObj);
                            string surveyId = surveyIdObj.ToString();

                            var newCrackingSet = new CrackReference
                            {
                                CrackId = crackId,
                                SegmentId = segmentId,
                                SurveyId = surveyId
                            };
                            if (!crackingIdList.Contains(newCrackingSet))
                            {
                                crackingIdList.Add(newCrackingSet);
                            }
                        }
                    }
                }

                graphic.GraphicsOverlay?.Graphics.Remove(graphic);
                return Task.CompletedTask; // Return a completed task
            });

            await Task.WhenAll(tasks);

            var finalDeletionInfos = deletionInfosByTable.Select(kvp => new DeletionInfo
            {
                Table = kvp.Key,
                Id = string.Join(",", kvp.Value.Distinct())
            }).ToList();

            var defectDeleted = await appEngine.SegmentService.DeleteDefectsWithQueryAsync(finalDeletionInfos);
            //Please don't add alert message with IdReply defectDeleted here ---> this causes UI error!!

            //on removal of segment hide menu on map (only segment)
            if (isSegmentSelected == true)
            {
                appState.GraphicOutSideSegmentClicked();
                appState.CloseSegmentSummaryMenu();
                //recalculate the remaining segments on the map and update the count
                GraphicsOverlay segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == LayerNames.Segment);
                if (segmentOverlay != null)
                {
                    appState.SegmentsLoad(segmentOverlay.Graphics.Count);
                }
            }

            //Update crack summary 
            if (crackingIdList.Count > 0)
            {
                var response = await appEngine.CrackSummaryService.RefreshCrackSummaries(crackingIdList);
                if (response.Updated.Count > 0 || response.Deleted.Count > 0)
                {
                    //update crack summary graphic if crack summary overlay is loaded
                    var crackSummaryOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.CrackSummary);
                    if (crackSummaryOverlay != null)
                    {
                        foreach (var updatedCrackSummary in response.Updated)
                        {
                            var matchingGraphic = crackSummaryOverlay.Graphics
                                .FirstOrDefault(g => g.Attributes.TryGetValue("id", out var val) && val?.ToString() == updatedCrackSummary.Id.ToString());

                            if (matchingGraphic != null)
                            {
                                //get new crack summary graphic and replace it with existing one
                                var graphic = GeneralMapHelper.ParseSimpleGeoJson(updatedCrackSummary.GeoJSON);
                                matchingGraphic.Geometry = graphic.Geometry;
                            }
                        }
                    }
                }
            }

            //Update segment summary
            var segmentSurveypairs = new HashSet<(int segmentId, string surveyId)>();

            foreach (Graphic graphic in graphics)
            {
                if (graphic.Attributes.TryGetValue("SegmentId", out var segmentIdObj) &&
                        graphic.Attributes.TryGetValue("SurveyId", out var surveyIdObj))
                {
                    int segmentId = Convert.ToInt32(segmentIdObj.ToString());
                    string surveyId = surveyIdObj?.ToString();
                    segmentSurveypairs.Add((segmentId, surveyId)); // Adds to surveypairs if affected segment is not in hashset
                }
            }

            if (segmentSurveypairs.Count != 0)
            {
                var requestList = segmentSurveypairs
                    .Select(pair => new SurveyAndSegmentRequest
                    {
                        SurveyId = pair.surveyId,
                        SegmentId = pair.segmentId
                    })
                    .ToList();
                await appEngine.SegmentService.CalculateSegmentSummaryFromMap(requestList);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in deleting DeleteDefectsWithQuery :  {ex.Message}");
        }
    }


    private void FocusFromQC(string layer, int id)
    {

        var graphicToFocus = FindGraphicForLayerAndId(layer, id);

        if (graphicToFocus != null)
        {

            MapPoint centerPoint = graphicToFocus.Geometry.Extent.GetCenter();
            MyMapView.SetViewpointAsync(new Viewpoint(centerPoint.Y, centerPoint.X, 100));

            HandleSegmentClick(graphicToFocus);

            //MapPoint wkid3857CenterPoint = (MapPoint)GeometryEngine.Project(centerPoint, SpatialReference.Create(3857));

            //double centerX = MyMapView.Width / 2;
            //double centerY = MyMapView.Height / 2;
            //Microsoft.Maui.Graphics.Point point = new Microsoft.Maui.Graphics.Point((float)centerX, (float)centerY);


            // ProcessGraphicsAtMapPoint(point, wkid3857CenterPoint);
        }
    }

    private void HighlightQCFilterGraphics(string tableName, List<int> ids)
    {
        //Multi layers
        if (MultiLayerNameMappings.ContainsKey(tableName))
        {
            MultiLayerNameMappings.TryGetValue(tableName, out var layerNames);
            if (layerNames != null && layerNames.Count() > 0)
            {
                foreach (var layer in layerNames)
                {
                    var targetOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == layer);
                    if (targetOverlay != null)
                    {
                        var matchingGraphics = targetOverlay.Graphics
                             .Where(g => g.Attributes.ContainsKey("Id") && ids.Contains(Convert.ToInt32(g.Attributes["Id"])))
                             .ToList();

                        appState.graphicsToRemove.AddRange(matchingGraphics);

                        foreach (var graphic in matchingGraphics)
                        {
                            graphic.IsSelected = true;
                        }
                    }
                }
            }
        }
        else
        {
            var targetOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == tableName);
            if (targetOverlay != null)
            {
                //treat corner break differently
                if (tableName == LayerNames.CornerBreak)
                {
                    foreach (var graphic in targetOverlay.Graphics)
                    {
                        if (graphic.Attributes.TryGetValue("Id", out var IdObj))
                        {
                            var idList = IdObj.ToString().Split(',').ToList();
                            var idIntList = idList.Select(int.Parse).ToList();
                            if (idIntList.Any(id => ids.Contains(id)))
                            {
                                // Add to graphicsToRemove and select the graphic
                                appState.graphicsToRemove.Add(graphic);
                                graphic.IsSelected = true;
                            }
                        }
                    }
                }
                else
                {
                    var matchingGraphics = targetOverlay.Graphics
                    .Where(g => g.Attributes.ContainsKey("Id") && ids.Contains(Convert.ToInt32(g.Attributes["Id"])))
                    .ToList();

                    appState.graphicsToRemove.AddRange(matchingGraphics);

                    foreach (var graphic in matchingGraphics)
                    {
                        graphic.IsSelected = true;
                    }
                }
            }
        }
    }

    private void SelectMapArea()
    {
        var freeHandTool = new FreehandTool();
        freeHandTool.Style = new GeometryEditorStyle
        {
            LineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Null, System.Drawing.Color.Cyan, 1),
            FillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.FromArgb(80, System.Drawing.Color.Cyan), new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Gray, 1)),
            VertexSymbol = null,
            SelectedVertexSymbol = null,
            FeedbackLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Cyan, 1),
            FeedbackVertexSymbol = null,
            BoundingBoxHandleSymbol = null //Disable scaling 
        };

        _geometryEditor.Tool = freeHandTool;

        if (!_geometryEditor.IsStarted)
        {
            _geometryEditor.Start(GeometryType.Polygon);

            if (IsSelectingCracks)
                _geometryEditor.PropertyChanged += MultiSelectCrackClassification;

            if (IsSelectingSummaryCracks)
                _geometryEditor.PropertyChanged += MultiSelectSummaryCracks;

            if (IsSelectingSegments)
                _geometryEditor.PropertyChanged += MultiSelectSegments;

            if (IsSelectingLasRutting)
                _geometryEditor.PropertyChanged += MultiSelectLasRutting;
        }
    }


    private System.Threading.Timer debounceTimer;
    private const int debounceDelayMilliseconds = 700; // Adjust the delay as needed

    //changes related to get visible area from the map & check for the available coordinates
    private void MapView_GeoViewVisibleAreaChanged(object sender, EventArgs e)
    {
        if (!isViewpointChanging)
        {

            Envelope visibleArea = MyMapView.VisibleArea.Extent;

            Envelope visibleAreaGeographic = (Envelope)GeometryEngine.Project(visibleArea, SpatialReferences.Wgs84);


            List<double> visibleAreaCoordinates = new List<double>
            {

                visibleAreaGeographic.XMin, visibleAreaGeographic.YMin, visibleAreaGeographic.XMax, visibleAreaGeographic.YMax
            };

            double zoomLevel = MyMapView.MapScale;


            debounceTimer?.Dispose(); // Cancel previous timer

            // Updates measurement label when zooming screen
            if (multiPoints != null && distanceLabelGraphic != null && currentPointerMapPoint != null)
            {
                // Convert map point to screen coordinates
                var screenPoint = MyMapView.LocationToScreen(currentPointerMapPoint);

                // Offset label slightly above cursor
                var offsetScreenPoint = new Point(screenPoint.X, screenPoint.Y - 10);

                // Convert back to map coordinates
                var offsetMapPoint = MyMapView.ScreenToLocation(offsetScreenPoint);

                // Update the label geometry
                distanceLabelGraphic.Geometry = offsetMapPoint;
            }
        }
    }

    //Maps Interfaces handling:        
    MapSyncMessage mapSyncMessage = new();    
    private bool _isApplyingRemoteView;
    private async void HandleSynchronizationToggle()
    {
        InitializeSynchronization();
       
        _syncEnabled = !_syncEnabled;
       // SharedScrMessage(_syncEnabled
       //? "Local DataView took Map control"
       //: "Local DataView released Map control");
        // Notify other instances
        await _syncService.SendAsync(new MapSyncMessage
        {
            IsControlMessage = true,
            EnableSync = _syncEnabled
        });
        // UI feedback
        //SharedScrMessage(_syncEnabled ? "Map synchronization enabled" : "Map synchronization disabled");

        //Switch:
        _isMapSynchronizedEnabled = _syncEnabled;
        OnPropertyChanged(nameof(IsMapSynchronizedEnabled));
    } 
    private void InitializeSynchronization()
    {
        if (_syncService != null)
            return;
        _syncService = new InstanceSyncService();
        _syncService.OnMessageReceived += ApplyRemoteViewpoint;

        _syncService.StartServer();
    }

    private double _lastAppliedX = double.NaN;
    private double _lastAppliedY = double.NaN;
    private async void ApplyRemoteViewpoint(MapSyncMessage msg)
    {
        if (msg.IsControlMessage)
        {       
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _ignoreMapSyncToggle = true;

                bool enabled = msg.EnableSync.GetValueOrDefault();

                // Switch reflects the current state
                _syncEnabled = enabled;
                IsMapSynchronizedEnabled = enabled;

                //SharedScrMessage(
                //    enabled
                //        ? "Map synchronization enabled by another DataView instance"
                //        : "Map synchronization disabled by another DataView instance");

                _ignoreMapSyncToggle = false;
            });

            return; // Control messages NEVER move the map
        }

        if (!_syncEnabled)
            return;

        // If User is interacting, Do not apply remote notion
        if (_localUserIsDragging)
            return;
       
        try
        {
            _isApplyingRemoteView = true;
            _syncRemoteActive = true;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Validate coordinates (WGS84)
                    if (double.IsNaN(msg.Latitude) || double.IsNaN(msg.Longitude) ||
                        msg.Latitude < -90 || msg.Latitude > 90 ||
                        msg.Longitude < -180 || msg.Longitude > 180)
                    {
                        SharedScrMessage(
                            $"Invalid WGS84 coordinates received: Lat={msg.Latitude}, Lon={msg.Longitude}");
                        return;
                    }
                    // Create WGS84 point (IPC contract)
                    var pointWgs84 = new MapPoint(
                        msg.Longitude,
                        msg.Latitude,
                        SpatialReferences.Wgs84
                    );
                    // Project to current map spatial reference
                    var mapPoint = (MapPoint)GeometryEngine.Project(
                        pointWgs84,
                        MyMapView.SpatialReference
                    );
                    // Validate scale
                    double scale = double.IsNaN(msg.Scale) || msg.Scale <= 0 ? 5000 : msg.Scale;
                    double rotation = double.IsNaN(msg.Rotation) ? 0 : msg.Rotation;

                    var viewpoint = new Viewpoint(mapPoint, scale, rotation);

                    // Calcular duración suave según distancia
                    double distance = double.IsNaN(_lastAppliedX) ? 0 :
                        Math.Sqrt(Math.Pow(mapPoint.X - _lastAppliedX, 2) + Math.Pow(mapPoint.Y - _lastAppliedY, 2));

                    int durationMs = msg.IsFinal ? 0 : Math.Min(200, (int)(distance * 8)); // max 200ms
                    durationMs = Math.Max(durationMs, 50); // min 50ms for minimal softness

                    // Apply view
                    await MyMapView.SetViewpointAsync(viewpoint, TimeSpan.FromMilliseconds(durationMs));

                    // Save last position applied
                    _lastAppliedX = mapPoint.X;
                    _lastAppliedY = mapPoint.Y;

                }
                catch (ArgumentOutOfRangeException ex)
                {
                    SharedScrMessage($"[ERROR] ApplyRemoteViewpoint - Argument out of range: {ex.Message}");
                }
                catch (Exception ex)
                {
                    SharedScrMessage($"[ERROR] ApplyRemoteViewpoint - Unexpected: {ex}");
                }
            });
        }
        finally
        {
            _isApplyingRemoteView = false;
        }
    }

    private double _lastSentLatitude;
    private double _lastSentLongitude;
    private double _lastSentScale;
    private double _lastSentRotation;
    private async void OnLocalViewpointChanged(object? sender, EventArgs e)
    {
        // Do not send while applying a remote message
        if (_isApplyingRemoteView || _syncRemoteActive)
            return;

        //User is interacting locally
        _localUserIsDragging = true;

        // IPC is active but I'm not the owner
        //if (_syncService != null && !_syncEnabled)
        //    return;
        // Sync disabled → do not emit
        if (_syncService == null || !_syncEnabled)
            return;

        _localUserIsDragging = true;

        var vp = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
        if (vp?.TargetGeometry is not MapPoint center)
            return;
        var wgs84Point = (MapPoint)GeometryEngine.Project(center, SpatialReferences.Wgs84);
        //bool isFinal = !MyMapView.IsNavigating;
        //await _syncService?.SendAsync(new MapSyncMessage
        //{
        //    Latitude = wgs84Point.Y,
        //    Longitude = wgs84Point.X,
        //    Scale = vp.TargetScale,
        //    Rotation = MyMapView.Rotation,
        //    IsFinal = isFinal
        //});

        if (Math.Abs(wgs84Point.X - _lastSentLongitude) < 0.00001 &&
           Math.Abs(wgs84Point.Y - _lastSentLatitude) < 0.00001 &&
           Math.Abs(vp.TargetScale - _lastSentScale) < 0.1 &&
           Math.Abs(MyMapView.Rotation - _lastSentRotation) < 0.1)
        {
            return; // Nothing significant changed, do not send
        }

            // Update last sent position
            _lastSentLatitude = wgs84Point.Y;
        _lastSentLongitude = wgs84Point.X;
        _lastSentScale = vp.TargetScale;
        _lastSentRotation = MyMapView.Rotation;

        // Sent message to the rest of instances
        bool isFinal = !MyMapView.IsNavigating;
        await _syncService?.SendAsync(new MapSyncMessage
        {
            Latitude = wgs84Point.Y,
            Longitude = wgs84Point.X,
            Scale = vp.TargetScale,
            Rotation = MyMapView.Rotation,
            IsFinal = isFinal
        });


    }
    protected async override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
        {
            appState.OnToggleMapSynchronization -= HandleSynchronizationToggle;
        }
    }
    private async void HandleForceReleaseMapSync()
    {
        if (_syncService == null)
            return;
        // Always release control for other instances
        if (_syncEnabled == true)
            await _syncService.SendAsync(new MapSyncMessage
            {
                IsControlMessage = true,
                EnableSync = false
            });
        // Reset local state safely
        _syncEnabled = false;
        _syncRemoteActive = false;

        //Switch:
        IsMapSynchronizedEnabled = false;
    }

    private async void OnNavigationCompleted(object? sender, EventArgs e)
    {
        _localUserIsDragging = false;

        var vp = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
        if (vp?.TargetGeometry is MapPoint center)
        {
            var wgs84Point = (MapPoint)GeometryEngine.Project(center, SpatialReferences.Wgs84);
            await _syncService?.SendAsync(new MapSyncMessage
            {
                Latitude = wgs84Point.Y,
                Longitude = wgs84Point.X,
                Scale = vp.TargetScale,
                Rotation = MyMapView.Rotation,
                IsFinal = true
            });
        }
    }

    private void SharedScrMessage(string message)
    {
        string _instanceId = Process.GetCurrentProcess().Id.ToString();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage.DisplayAlert(
                $"Shared Map message:",
                message,
                "OK");
        });
    }


    private List<Microsoft.Maui.Controls.Button> _allMapButtons;
    private void InitializeMapButtons()
    {
        _allMapButtons = new()
        {
            PolylineMeasurementButton,
            PolygonMeasurementButton,
            offsetButton,
            MoveSegmentsButton,
            ManageCrackButton,
            RemoveGraphicButton,
            LasFileButton,
            AutoRepeatLasButton,
            RecalculateButton,
            QcButton,
            PrintScreenButton,
            SegmentationButton,
            BrightnessButton,
            RecreateOverlayButton
        };
    }
    private void ToggleMapViewButtonPopup(Microsoft.Maui.Controls.Button button, ContentView page)
    {
        var currentPageVisible = page.IsVisible;
        DeactivateAllMapButtons();
        page.IsVisible = !currentPageVisible;

        if (button != null)
        {
            button.Background = page.IsVisible
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#90D3D3D3");
        }

        appState.IsPopupOpen = page.IsVisible;
    }
    private void OffsetButton_Clicked(object sender, EventArgs e)
    => ToggleMapViewButtonPopup(offsetButton, OffsetPage);

    private void RemoveDoubleUp_Clicked(object sender, EventArgs e)
        => ToggleMapViewButtonPopup(RemoveGraphicButton, RemoveGraphicsPage);

    private void RecalculateIRI_Clicked(object sender, EventArgs e)
        => ToggleMapViewButtonPopup(RecalculateButton, RecalculatePage);

    private void QcButton_Clicked(object sender, EventArgs e)
        => ToggleMapViewButtonPopup(QcButton, QCModePage);

    private void RecalculateChainage_Clicked(object sender, EventArgs e)
        => ToggleMapViewButtonPopup(RecalculateButton, RecalculateChainagePage);

    private void RecreateOverlayButton_Clicked(object sender, EventArgs e)
        => ToggleMapViewButtonPopup(RecreateOverlayButton, RecreateOverlayPage);

    private void MoveSegmentButton_Clicked(object sender, EventArgs e)
    {
        if (!IsSelectingSegments && !IsMovingSegments)
        {
            DeactivateAllMapButtons();
            IsSelectingSegments = true;
            SelectMapArea();
            MoveSegmentsButton.Background = Color.FromArgb("#FFFFFF");
        }
        else
        {
            ClearMultiSelectSegments();
        }
    }

    //Reprocess Segments selected
    private async Task ReprocessSegment()
    {

        if (_oldGraphics != null && _oldGraphics.Any())
        {
            var firstGraphic = _oldGraphics.FirstOrDefault();
            if (firstGraphic != null && firstGraphic.Attributes != null)
            {
                //firstGraphic.Attributes.TryGetValue("ImageFilePath", out object pathFisFiles);
                if (firstGraphic.Attributes.TryGetValue("SurveyId", out object currentSurveyId) && currentSurveyId is string surveyId && !string.IsNullOrWhiteSpace(surveyId))
                {
                    Survey survey = await appEngine.SurveyService.GetSurveyEntityByExternalId((string)currentSurveyId);

                    string folderPath = Directory.GetParent(survey.ImageFolderPath).FullName;
                    if (appState.newReprocessFolder != "")
                        folderPath = appState.newReprocessFolder;

                    appState.reprocessFolder = folderPath;
                    FindFisFiles();

                    appState.isReprocessingSegments = true;
                    appState.processStatus = false;
                    appState.SetMenuPage("/ImportData");
                }
            }
        }
    }

    // Scans for fis files from selected segments and checks if fis files exist in reprocess folder.
    private void FindFisFiles()
    {
        //clear previous files prior to process new selection
        appState.fisFilesForProcessing.Clear();

        foreach (var graphic in _oldGraphics)
        {
            if (graphic.Attributes.TryGetValue("ImageFilePath", out object imageFilePathValue))
            {
                string fisFile = imageFilePathValue as string;
                fisFile = fisFile.Replace("ImageResult\\", "").Replace("jpg", "fis");

                appState.fisFilesForProcessing.Add(fisFile);
            }
        }

        appState.incorrectReprocessFolder = !appState.fisFilesForProcessing.Any(fis => File.Exists(Path.Combine(appState.reprocessFolder, fis)));
    }

    private void RefreshMapVisibility()
    {
        if (appState.isReprocessingSegments && appState.processStatus)
        {
            MoveSegmentsButton.Background = Color.FromArgb("#90D3D3D3");
            _geometryEditor.Stop();
            _geometryEditor.PropertyChanged -= MultiSelectSegments;
            _oldGraphics.Clear();

            if (viewModel.segmentSymbol != null)
            {
                var segmentSymbol = viewModel.segmentSymbol;

                foreach (var previousGraphic in previousGraphics)
                {
                    previousGraphic.Symbol = segmentSymbol;
                }
            }

            // Clear the previousGraphics list since the highlights are removed
            previousGraphics.Clear();
            IsMovingSegments = false;
            IsSelectingSegments = false;
            IsSelectingCracks = false;
            IsSelectingLasRutting = false;

            //Reprocess Segments selected
            appState.isReprocessingSegments = false;
            appState.incorrectReprocessFolder = false;
        }
    }

    private void ClosePopupInMapClicked()
    {
        // Reset all button backgrounds
        foreach (var button in _allMapButtons)
        {
            button.Background = Color.FromArgb("#90D3D3D3");
        }

        //make all pages invisible
        OffsetPage.IsVisible = false;
        RecalculatePage.IsVisible = false;
        QCModePage.IsVisible = false;
        RemoveGraphicsPage.IsVisible = false;
        EditCracksContainerPage.IsVisible = false;
        RecalculateChainagePage.IsVisible = false;
        ImportSegmentSurveysPage.IsVisible = false;
        ManualSegmentationPage.IsVisible = false;
        RecreateOverlayPage.IsVisible = false;
    }

    private void CloseAllPopupsAfterDatasetChange()
    {
        //close drawing tool
        if (SurveyTemplateVisibility)
            SurveyTemplateVisibility = false;
        if (DrawingToolVisibility)
            DrawingToolVisibility = false;
        //close video
        if (VideoPlayer.IsVisible == true)
            CloseVideoBtn_Clicked(this, new EventArgs());
        ClosePopupInMapClicked();
    }

    private async void MoveGraphicsBySegment(string segmentId, double horizontalOffset, double verticalOffset)
    {
        try
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(MyMapView.GraphicsOverlays, overlay =>
                {
                    bool hasSegmentId = overlay.Graphics.Any(graphic => graphic.Attributes.ContainsKey("SegmentId"));

                    // Skip the entire overlay if no graphic has the SegmentId attribute
                    if (!hasSegmentId || overlay.Id == "Segment")
                        return;

                    Parallel.ForEach(overlay.Graphics, graphic =>
                    {
                        var segmentatt = graphic.Attributes["SegmentId"]?.ToString();
                        if (segmentatt == segmentId)
                        {
                            // Log initial position
                            var initialGeometry = graphic.Geometry;

                            // Move the graphic
                            Geometry moved = initialGeometry.Move(horizontalOffset, verticalOffset);

                            graphic.Geometry = moved;
                        }
                    });

                });
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Error in applying offset on the map : {ex.Message}");
        }
    }

    private async void ApplyOffsetToGraphics(List<string> surveyIds, List<string> defects, double horizontalOffset, double verticalOffset)
    {
        try
        {
            //Apply offset to the existing graphics
            await Task.Run(async () =>
            {
                var overlays = MyMapView.GraphicsOverlays.Where(overlay => defects.Contains(overlay.Id)).ToList();

                var tasks = overlays
                    .Select(async overlay =>
                    {
                        var overlayTasks = overlay.Graphics.Select(async graphic =>
                        {
                            var survey = graphic.Attributes["SurveyId"]?.ToString();
                            if (surveyIds.Contains(survey))
                            {
                                //var segmentIdObject = graphic.Attributes["SegmentId"];
                                //if (segmentIdObject != null && Convert.ToInt32(segmentIdObject) == -1)
                                //{
                                //    return; // Skip this graphic if SegmentId is -1
                                //}

                                var trackAngleObject = graphic.Attributes["TrackAngle"];
                                var trackAngle = Convert.ToDouble(trackAngleObject);
                                var originalGeometry = graphic.Geometry;

                                if (originalGeometry is Polygon polygon)
                                {
                                    var updatedVertices = new List<MapPoint>();

                                    foreach (var part in polygon.Parts)
                                    {
                                        foreach (var vertex in part.Points)
                                        {
                                            var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(horizontalOffset, verticalOffset, vertex.X, vertex.Y, trackAngle);
                                            var updatedPoint = new MapPoint(newCoordinate[0], newCoordinate[1], vertex.SpatialReference);
                                            updatedVertices.Add(updatedPoint);
                                        }
                                    }

                                    var newPolygon = new Polygon(updatedVertices);
                                    graphic.Geometry = newPolygon;
                                }
                                else if (originalGeometry is Polyline polyline)
                                {
                                    var updatedPoints = new List<MapPoint>();

                                    foreach (var part in polyline.Parts)
                                    {
                                        foreach (var vertex in part.Points)
                                        {
                                            var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(horizontalOffset, verticalOffset, vertex.X, vertex.Y, trackAngle);
                                            var updatedPoint = new MapPoint(newCoordinate[0], newCoordinate[1], vertex.SpatialReference);
                                            updatedPoints.Add(updatedPoint);
                                        }
                                    }

                                    var newPolyline = new Polyline(updatedPoints);
                                    graphic.Geometry = newPolyline;
                                }
                                else if (originalGeometry is MapPoint point)
                                {
                                    var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(horizontalOffset, verticalOffset, point.X, point.Y, trackAngle);
                                    var updatedPoint = new MapPoint(newCoordinate[0], newCoordinate[1], point.SpatialReference);
                                    graphic.Geometry = updatedPoint;
                                }
                            }
                        });

                        await Task.WhenAll(overlayTasks);
                    });

                await Task.WhenAll(tasks);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Error in applying offset on the map : {ex.Message}");
        }
    }

    private async void RemoveHighlightedDefects(string surveyId, string surveyName)
    {
        if (appState.graphicsToRemove.Any())
        {
            await DeleteDefectsWithQuery(appState.graphicsToRemove);
            appState.graphicsToRemove.Clear();
            appState.IsMultiSelected = false;
            appState.NotifyProcessingCompletedFromMap();

            if (surveyId != null && surveyName != null)
            {
                //save boundary
                var boundaryOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "tempOverlay");
                if (boundaryOverlay != null)
                {
                    // Find the boundary graphic using the identifier
                    var boundaryGraphic = boundaryOverlay.Graphics.FirstOrDefault(g => g.Attributes.ContainsKey("IsTemp") && (bool)g.Attributes["IsTemp"]);
                    if (boundaryGraphic != null)
                    {
                        await viewModel.SaveTempBoundary(boundaryOverlay, boundaryGraphic, surveyId, surveyName);
                        appState.UpdateTableNames(true);
                    }
                }
            }
        }
    }

    private async void ChangeViewPointToShapefile(string shapefileName)
    {
        var overlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == shapefileName);
        if (overlay != null && overlay.Graphics.Count > 0)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            // Iterate through all graphics to find the overall extent
            foreach (var graphic in overlay.Graphics)
            {
                var graphicExtent = graphic.Geometry.Extent;
                if (graphicExtent != null)
                {
                    if (graphicExtent.XMin < minX) minX = graphicExtent.XMin;
                    if (graphicExtent.YMin < minY) minY = graphicExtent.YMin;
                    if (graphicExtent.XMax > maxX) maxX = graphicExtent.XMax;
                    if (graphicExtent.YMax > maxY) maxY = graphicExtent.YMax;
                }
            }

            // Create an envelope from the min and max coordinates
            var overallExtent = new Envelope(minX, minY, maxX, maxY, overlay.Graphics[0].Geometry.SpatialReference);

            // Create a new viewpoint to show the entire overlay extent
            var newViewpoint = new Viewpoint(overallExtent);

            // Set the viewpoint of the map
            await MyMapView.SetViewpointAsync(newViewpoint);
        }
    }

    void OnSliderValueChanged(object sender, ValueChangedEventArgs args)
    {
        double value = args.NewValue;
        MyMapView.Map.OperationalLayers.FirstOrDefault().Opacity = value;
    }

    private void OnToggleSliderClicked(object sender, EventArgs e)
    {
        if (appState.isUsingOnlineMap)
        {
            // Toggle the visibility of the slider
            OpacitySlider.IsVisible = !OpacitySlider.IsVisible;
            if (OpacitySlider.IsVisible)
            {
                BrightnessButton.Background = Color.FromArgb("#FFFFFF");
            }
            else
            {
                BrightnessButton.Background = Color.FromArgb("#90D3D3D3");
            }
        }
    }

    //Graphic drawing tool
    public void SetDrawingToolVisibility(string name, string geoType, bool isPciRating)
    {
        AddDefectGraphicButton.IsEnabled = true;
        if (!isPciRating)
        {
            RemoveTempGraphic();
        }

        if (name != null)
        {
            if (DrawingToolVisibility)
            {
                appState.SetBottomMenuVisibility(null, null, null, false);
            }
            if (geoType == "Point")
            {
                AddDefectGraphicButton.Text = "\ue21c";
            }
            else if (geoType == "Polygon")
            {
                AddDefectGraphicButton.Text = "\ue21f";
            }
            else if (geoType == "Polyline")
            {
                AddDefectGraphicButton.Text = "\ue1a8";
            }
            else if (geoType == "MultiPolygon")
            {
                AddDefectGraphicButton.Text = "\ue2d7";
            }
            if (isPciRating)
            {
                //PCI Rating defect
                PCIDrawingToolVisibility = true;
            }
            else
            {
                PCIDrawingToolVisibility = false;
            }

            DrawingToolVisibility = true;
            AddDefectGraphic_Click(this, new EventArgs());
        }
        else
        {
            if (IsPolylineActive || IsPolygonActive)
            {
                ClearMeasurementButtons();
            }
            DrawingToolVisibility = false;
            appState.SetBottomMenuVisibility(null, null, null, false);
        }
    }


    private Keycode keycodeSelected = null;

    public void SetDrawingKeycodes(string name, Keycode keycode)
    {
        if (name != null)
        {
            if (keycode.EventKeyType == "Point")
            {
                IsDrawingKeycodePoint = true;
            }
            else
            {
                IsDrawingKeycodeLine = true;
            }
            keycodeSelected = keycode;

        }
        else
        {

            IsDrawingKeycodePoint = false;
            IsDrawingKeycodeLine = false;
        }

    }

    public void SaveKeycode_Click(object sender, EventArgs e)
    {

        var tempOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");

        List<keycodeFromOtherSaveRequest> keycodesToSave = new List<keycodeFromOtherSaveRequest>();

        if (keycodeSelected != null)
        {

            if (tempOverlay.Graphics.Count > 0)
            {

                if (keycodeSelected.EventKeyType == "Point") //Save points 
                {
                    foreach (var graphic in tempOverlay.Graphics)
                    {
                        Geometry geo = GeometryEngine.Project(graphic.Geometry, SpatialReferences.Wgs84);

                        keycodeFromOtherSaveRequest newkeycodeRquest = new keycodeFromOtherSaveRequest
                        {
                            exampleKeycode = keycodeSelected,
                            Longitude = ((MapPoint)geo).X,
                            Latitude = ((MapPoint)geo).Y
                        };
                        keycodesToSave.Add(newkeycodeRquest);
                    }

                    var saveKeycodeResponse = appEngine.KeycodeService.SaveListKeycodes(keycodesToSave);
                }
                else //Save points for line keycode
                {
                    foreach (var graphic in tempOverlay.Graphics)
                    {
                        if (graphic.Geometry is Polyline)
                            continue;
                        Geometry geo = GeometryEngine.Project(graphic.Geometry, SpatialReferences.Wgs84);

                        // Check the color of the graphic's symbol
                        var symbol = graphic.Symbol as SimpleMarkerSymbol;
                        string continuousStatus;

                        // Determine continuous status based on the graphic's color
                        if (symbol != null)
                        {
                            System.Drawing.Color color = symbol.Color;
                            continuousStatus = (color.ToArgb() == System.Drawing.Color.FromArgb(255, 0, 255, 0).ToArgb())
                                ? "STARTED" // Green
                                : "ENDED"; // Red
                        }
                        else
                        {
                            // Default status if for some reason the symbol is not recognized
                            continuousStatus = "Unknown";
                        }

                        keycodeFromOtherSaveRequest newKeycodeRequest = new keycodeFromOtherSaveRequest
                        {
                            exampleKeycode = keycodeSelected,
                            Longitude = ((MapPoint)geo).X,
                            Latitude = ((MapPoint)geo).Y,
                            ContinuousStatus = continuousStatus
                        };

                        keycodesToSave.Add(newKeycodeRequest);
                    }
                    var saveKeycodeResponse = appEngine.KeycodeService.SaveListKeycodes(keycodesToSave);
                }

            }
        }


        //Reload overlay and refresh map keycodes



        keycodeSelected = null;
        DrawingToolVisibility = false;
        PCIDrawingToolVisibility = false;
        isCreatingGraphics = false;
        IsDrawingKeycodePoint = false;
        IsDrawingKeycodeLine = false;
        SetDrawingToolVisibility(null, null, false);
        appState.InvokeCloseDrawingTool();

    }

    public void ClearMeasurementButtons()
    {
        if (newOverlay != null)
        {
            newOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(newOverlay);
            MyMapView.DismissCallout();
        }
        if (messageBoxMeasurement.IsVisible)
        {
            messageBoxMeasurement.IsVisible = false;
            appState.PendingMeasurementField = null;
        }
        IsPolylineActive = false;
        IsPolygonActive = false;
        PolygonMeasurementButton.Background = Color.FromArgb("#90D3D3D3");
        PolylineMeasurementButton.Background = Color.FromArgb("#90D3D3D3");
        multiPoints = null;
    }

    public void RemoveTempGraphic()
    {
        // Remove temp graphic and overlay
        var tempOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");
        if (tempOverlay != null)
        {
            tempOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(tempOverlay);
        }
    }
    private void AddDefectGraphic_Click(object sender, EventArgs e)
    {
        SetDefaultGeometryEditorStyle();
        isCreatingGraphics = true;

        if (AddDefectGraphicButton.Text == "\ue21c") //Point
        {
            _geometryEditor.Start(GeometryType.Point);
        }
        else if (AddDefectGraphicButton.Text == "\ue21f" || AddDefectGraphicButton.Text == "\ue2d7") //Polygon or MultiPolygon
        {
            _geometryEditor.Start(GeometryType.Polygon);
        }
        else if (AddDefectGraphicButton.Text == "\ue1a8") //Polyline
        {
            _geometryEditor.Start(GeometryType.Polyline);
        }
    }


    private void RemovePCIDefectGraphic(Guid defectGuid)
    {
        var drawingOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == "drawingDefectOverlay");
        if (drawingOverlay != null)
        {
            var graphicToRemove = drawingOverlay.Graphics
           .FirstOrDefault(graphic => graphic.Attributes.TryGetValue("Id", out var idObj) && defectGuid.ToString() == idObj.ToString());

            if (graphicToRemove != null)
            {
                drawingOverlay.Graphics.Remove(graphicToRemove);
            }
        }
    }

    private async void SavePCIDefect_Click(object sender, EventArgs e)
    {
        double qty = 0.0;
        JToken coordinateJToken = null;

        try
        {
            //Save PCI Defect
            if (_selectedGraphic != null)
            {
                _selectedGraphic.IsSelected = false;
                _selectedGraphic = null;
            }

            isCreatingGraphics = false;
            var geometry = _geometryEditor.Geometry;
            var projectedGeometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);

            if (projectedGeometry is Polygon polygon)
            {
                List<MapPoint> coordinates = new List<MapPoint>();
                foreach (var part in polygon.Parts)
                {
                    foreach (var point in part.Points)
                    {
                        coordinates.Add(point);
                    }
                }
                double area = GeometryEngine.AreaGeodetic(polygon, AreaUnits.SquareMeters, GeodeticCurveType.Geodesic);
                qty = Math.Round(area, 2);
                coordinateJToken = FormatCoordinateForGeoJSON(GeoType.Polygon, coordinates);
            }
            else if (projectedGeometry is Polyline polyline)
            {
                List<MapPoint> coordinates = new List<MapPoint>();

                foreach (var part in polyline.Parts)
                {
                    foreach (var point in part.Points)
                    {
                        coordinates.Add(point);
                    }
                }
                double totalLength = 0.0;

                // Calculate geodetic distance between consecutive points
                for (int i = 0; i < coordinates.Count - 1; i++)
                {
                    var current = coordinates[i];
                    var next = coordinates[i + 1];

                    double length = GeometryEngine.DistanceGeodetic(
                        current, next,
                        LinearUnits.Meters,
                        AngularUnits.Degrees,
                        GeodeticCurveType.Geodesic).Distance;

                    totalLength += length;
                }

                qty = Math.Round(totalLength, 2);
                coordinateJToken = FormatCoordinateForGeoJSON(GeoType.Polyline, coordinates);
            }
            else if (projectedGeometry is MapPoint point)
            {
                qty = 1;
                coordinateJToken = FormatCoordinateForGeoJSON(GeoType.Point, new List<MapPoint> { point });
            }

            if (coordinateJToken != null && appState.CurrentPCIDefectGuid != null)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "Id", appState.CurrentPCIDefectGuid }
                };

                var graphic = CreateGraphicForNewlyDrawnDefect(geometry, attributes);

                //Add graphic in the overlay
                var drawingOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == "drawingDefectOverlay");
                if (drawingOverlay == null)
                {
                    //If no overlay, create one
                    var newOverlay = new GraphicsOverlay { Id = "drawingDefectOverlay" };
                    MyMapView.GraphicsOverlays.Add(newOverlay);
                    newOverlay.Graphics.Add(graphic);
                }
                else
                {
                    drawingOverlay.Graphics.Add(graphic);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in SavePCIDefect_Click : " + ex.Message);
        }
        finally
        {
            _geometryEditor.Stop();
            DrawingToolVisibility = false;
            PCIDrawingToolVisibility = false;
            //send the qty and coordiante to PCI Rating Mode
            appState.SendDefectGraphicInfo(qty, coordinateJToken);
        }
    }

    private async void SaveAddedDefect_Click(object sender, EventArgs e)
    {
        try
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            string logString = "Step 2: Polygon Coordinates\n";

            if (_selectedGraphic != null)
            {
                _selectedGraphic.IsSelected = false;
                _selectedGraphic = null;
            }

            var geometry = _geometryEditor.Geometry;
            var projectedGeometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);

            var points = new List<MapPoint>();

            if (projectedGeometry is Polygon polygon)
            {
                foreach (var part in polygon.Parts)
                {
                    foreach (var point in part.Points)
                    {
                        points.Add(point);
                        logString += $"Polygon Point: X = {point.X}, Y = {point.Y}\n";
                    }
                }
            }
            Log.Information(logString);
            var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "Segment");

            if (segmentOverlay != null)
            {
                var intersectingGraphic = segmentOverlay.Graphics.FirstOrDefault(graphic => graphic.Geometry.Intersects(projectedGeometry));

                if (intersectingGraphic != null)
                {
                    string surveyId = intersectingGraphic.Attributes.ContainsKey("SurveyId") ? intersectingGraphic.Attributes["SurveyId"].ToString() : null;
                    double? segmentId = intersectingGraphic.Attributes.ContainsKey("SegmentId") ? Convert.ToDouble(intersectingGraphic.Attributes["SegmentId"]) : (double?)null;
                    string imagePath = intersectingGraphic.Attributes.ContainsKey("ImageFilePath") ? intersectingGraphic.Attributes["ImageFilePath"].ToString() : null;
                    double? trackAngle = intersectingGraphic.Attributes.ContainsKey("TrackAngle") ? Convert.ToDouble(intersectingGraphic.Attributes["TrackAngle"]) : (double?)null;
                    double? altitude = intersectingGraphic.Attributes.ContainsKey("Altitude") ? Convert.ToDouble(intersectingGraphic.Attributes["Altitude"]) : (double?)null;

                    //Draw graphic in the graphics overlay along with survey Id and segment Id

                    attributes = new Dictionary<string, object>
                    {
                        { "SurveyId", surveyId },
                        { "SegmentId", segmentId.Value },
                        { "ImageFileIndex", Path.GetFileName(imagePath)},
                        { "GPSTrackAngle", trackAngle.Value },
                        { "GPSAltitude", altitude.Value }
                    };

                    //Open bottom panel to add properties
                    appState.SetBottomMenuVisibility(appState.CurrentDrawingToolLayer, surveyId, segmentId.Value.ToString(), true);

                }
                //Outside of the segments
                else
                {
                    //Open bottom panel to add properties
                    appState.SetBottomMenuVisibility(appState.CurrentDrawingToolLayer, null, null, true);
                }
            }
            else
            {
                appState.SetBottomMenuVisibility(appState.CurrentDrawingToolLayer, null, null, true);
            }

            var graphic = CreateGraphicForNewlyDrawnDefect(geometry, attributes);

            var newOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");
            if (newOverlay == null)
            {
                newOverlay = new GraphicsOverlay { Id = "drawingDefectOverlay" };
                MyMapView.GraphicsOverlays.Add(newOverlay);
            }

            newOverlay.Graphics.Add(graphic);

            if (appState.CurrentDrawingToolLayer == LayerNames.Bleeding && newOverlay.Graphics.Count < 2)
            {
                await App.Current.MainPage.DisplayAlert("DataView2", "Bleeding needs both left and right graphics. Please draw a right bleeding.", "OK");
                _geometryEditor.Start(GeometryType.Polygon);
            }
            else
            {
                isCreatingGraphics = false;
                AddDefectGraphicButton.IsEnabled = false;
                _geometryEditor.Stop();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    public Graphic CreateGraphicForNewlyDrawnDefect(Geometry geometry, Dictionary<string, object> attributes)
    {
        Esri.ArcGISRuntime.Symbology.Symbol symbol;
        switch (geometry)
        {
            case Polygon _:
                symbol = new SimpleFillSymbol
                {
                    Color = System.Drawing.Color.FromArgb(128, 255, 0, 0), // Semi-transparent red
                    Outline = new SimpleLineSymbol
                    {
                        Color = System.Drawing.Color.FromArgb(255, 0, 0), // Red outline
                        Width = 2
                    }
                };
                break;

            case Polyline _:
                symbol = new SimpleLineSymbol
                {
                    Color = System.Drawing.Color.FromArgb(255, 0, 0), // Red line
                    Width = 2
                };
                break;

            case MapPoint _:
                symbol = new SimpleMarkerSymbol
                {
                    Color = System.Drawing.Color.FromArgb(255, 0, 0), // Red marker
                    Style = SimpleMarkerSymbolStyle.Circle,
                    Size = 10
                };
                break;

            default:
                throw new ArgumentException("Unsupported geometry type.");
        }

        return new Graphic(geometry, attributes, symbol);
    }
    private async void SavePropertiesInExistingTable(string tableName, Dictionary<string, object> defectFields, bool isLCMSTable)
    {
        var graphicsOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "drawingDefectOverlay");
        if (graphicsOverlay == null || graphicsOverlay.Graphics.Count == 0)
        {
            throw new InvalidOperationException("No graphics found.");
        }

        var graphics = graphicsOverlay.Graphics.ToList();
        Geometry geometry = null;
        if (graphics.Count == 1)
        {
            geometry = graphics.FirstOrDefault().Geometry;
        }
        else
        {
            geometry = GeometryEngine.Union(graphicsOverlay.Graphics.Select(g => g.Geometry));
        }
        var projectedGeometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);

        var attributes = graphicsOverlay.Graphics[0].Attributes;

        if (attributes.Count == 0)
        {
            //Outside of segment
            if (defectFields.TryGetValue("SurveyId", out var surveyIdObj) && surveyIdObj != null)
            {
                CreateDummySegment(surveyIdObj.ToString(), projectedGeometry);
            }
        }
        else
        {
            foreach (var kvp in attributes)
            {
                // Convert value to string if needed
                defectFields[kvp.Key] = kvp.Value;
            }
        }

        if (isLCMSTable)
        {
            //Save a new defect in LCMS table
            SaveLCMSTableDefect(tableName, defectFields, projectedGeometry);
        }


        else
        {
            //Save a new instance in the existing meta table
            if (defectFields.TryGetValue("SurveyId", out var surveyIdObj) && defectFields.TryGetValue("SegmentId", out var segmentIdObj))
            {
                var surveyId = surveyIdObj.ToString();
                if (int.TryParse(segmentIdObj?.ToString(), out var segmentId))
                {
                    await SaveMetaTableDefect(tableName, defectFields, projectedGeometry, surveyId, segmentId);
                }
            }
        }

        //close the drawing tool
        appState.InvokeCloseDrawingTool();
    }

    private JToken FormatCoordinateForGeoJSON(GeoType type, List<MapPoint> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
        {
            throw new ArgumentException("Coordinates list cannot be null or empty.");
        }

        JArray coordinateArray = null;
        switch (type)
        {
            case GeoType.Point:
                coordinateArray = new JArray(coordinates[0].X, coordinates[0].Y);
                break;
            case GeoType.Polyline:
                coordinateArray = new JArray(coordinates.Select(p => new JArray(p.X, p.Y)));
                break;
            case GeoType.Polygon:
                coordinateArray = new JArray { new JArray(coordinates.Select(p => new JArray(p.X, p.Y))) };
                break;
            default:
                throw new ArgumentException($"Invalid geometry type: {type}");
        }

        return coordinateArray;
    }

    private async void SaveLCMSTableDefect(string tableName, Dictionary<string, object> defectFields, Geometry projectedGeometry)
    {
        try
        {
            defectFields["Table"] = tableName;

            if (projectedGeometry is Esri.ArcGISRuntime.Geometry.MapPoint mapPoint)
            {
                var latitude = mapPoint.Y;
                var longitude = mapPoint.X;
                defectFields["GPSLatitude"] = latitude;
                defectFields["GPSLongitude"] = longitude;
                defectFields["RoundedGPSLatitude"] = Math.Round(Convert.ToDecimal(latitude), 4);
                defectFields["RoundedGPSLongitude"] = Math.Round(Convert.ToDecimal(longitude), 4);

                var formattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Point, new List<MapPoint> { mapPoint });
                var image = defectFields["ImageFileIndex"].ToString();
                string idFieldName = defectFields.Keys.FirstOrDefault(key => key.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
                string id = defectFields.ContainsKey(idFieldName) ? defectFields[idFieldName].ToString() : string.Empty;

                if (tableName == LayerNames.CornerBreak)
                {
                    var quarterId = defectFields["QuarterId"].ToString();
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Point, formattedCoordinates, id, image, tableName, quarterId);
                }
                else if (tableName == LayerNames.Potholes)
                {
                    var majorDiameter = Convert.ToDouble(defectFields["MajorDiameter_mm"]);
                    var minorDiameter = Convert.ToDouble(defectFields["MinorDiameter_mm"]);
                    var diameter = (majorDiameter + minorDiameter) / 2;
                    if (diameter == 0)
                    {
                        diameter = 30;
                    }
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Point, formattedCoordinates, id, image, tableName, diameter.ToString());
                }
                else
                {
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Point, formattedCoordinates, id, image, tableName);
                }
            }
            else if (projectedGeometry is Esri.ArcGISRuntime.Geometry.Polyline polyline)
            {
                var points = polyline.Parts.SelectMany(part => part.Points).ToList();
                var firstPoint = points.First();
                var lastPoint = points.Last();

                defectFields["GPSLatitude"] = firstPoint.Y;
                defectFields["GPSLongitude"] = firstPoint.X;
                defectFields["EndGPSLatitude"] = lastPoint.Y;
                defectFields["EndGPSLongitude"] = lastPoint.X;
                defectFields["RoundedGPSLatitude"] = Math.Round(Convert.ToDecimal(firstPoint.Y), 4);
                defectFields["RoundedGPSLongitude"] = Math.Round(Convert.ToDecimal(firstPoint.X), 4);

                var formattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Polyline, new List<MapPoint> { firstPoint, lastPoint });

                var image = defectFields["ImageFileIndex"].ToString();
                string idFieldName = defectFields.Keys.FirstOrDefault(key => key.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
                string id = defectFields.ContainsKey(idFieldName) ? defectFields[idFieldName].ToString() : string.Empty;

                if (tableName == LayerNames.Cracking)
                {
                    var nodeId = defectFields["NodeId"].ToString();
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Polyline, formattedCoordinates, id, image, tableName, nodeId);
                }
                else if (tableName == LayerNames.ConcreteJoint)
                {
                    defectFields["EndGPSAltitude"] = defectFields.ContainsKey("GPSAltitude") ? defectFields["GPSAltitude"] : 0.0;
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Polyline, formattedCoordinates, id, image, tableName);
                }
                else if (tableName == LayerNames.CurbDropOff)
                {
                    var tableType = defectFields["Type"].ToString();
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Polyline, formattedCoordinates, id, image, tableName, tableType);
                }
                else
                {
                    var newFormattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Polyline, points);
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Polyline, newFormattedCoordinates, id, image, tableName);
                }

            }
            else if (projectedGeometry is Esri.ArcGISRuntime.Geometry.Polygon polygon)
            {
                var multipolygonParts = polygon.Parts.Select(part =>
                   part.Points.Select(p => new JArray(p.X, p.Y)).ToList()).ToList();

                var latitude = polygon.Parts.First().Points.First().Y;
                var longitude = polygon.Parts.First().Points.First().X;
                defectFields["GPSLatitude"] = latitude;
                defectFields["GPSLongitude"] = longitude;
                defectFields["RoundedGPSLatitude"] = Math.Round(Convert.ToDecimal(latitude), 4);
                defectFields["RoundedGPSLongitude"] = Math.Round(Convert.ToDecimal(longitude), 4);

                var geoJsonType = multipolygonParts.Count > 1 ? GeoType.MultiPolygon : GeoType.Polygon;
                JToken formattedCoordinates = null;
                if (geoJsonType == GeoType.MultiPolygon)
                {
                    formattedCoordinates = new JArray(multipolygonParts.Select(part => new JArray(part)));
                }
                else //Polygon
                {
                    var coordinates = polygon.Parts.SelectMany(part => part.Points).ToList();
                    formattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Polygon, coordinates);
                }

                var image = defectFields["ImageFileIndex"].ToString();
                string idFieldName = defectFields.Keys.FirstOrDefault(key => key.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
                string id = defectFields.ContainsKey(idFieldName) ? defectFields[idFieldName].ToString() : string.Empty;

                if (tableName == LayerNames.Spalling)
                {
                    var spallingId = defectFields["SpallingId"].ToString();
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(GeoType.Polygon, formattedCoordinates, id, image, tableName, spallingId);

                }
                else
                {
                    if (tableName == LayerNames.Bleeding)
                    {
                        var lastPart = polygon.Parts.LastOrDefault();
                        if (lastPart != null && lastPart.Points.Any())
                        {
                            defectFields["GPSRightLatitude"] = lastPart.Points.First().Y;
                            defectFields["GPSRightLongitude"] = lastPart.Points.First().X;
                        }
                        else
                        {
                            defectFields["GPSRightLatitude"] = 0.0;
                            defectFields["GPSRightLongitude"] = 0.0;
                        }
                    }
                    defectFields["GeoJSON"] = GeneralHelper.CreateNewGeoJson(geoJsonType, formattedCoordinates, id, image, tableName);
                }
            }

            defectFields["SurveyDate"] = DateTime.Now;

            var keyValueFields = new List<KeyValueField>();

            foreach (var kvp in defectFields)
            {
                var keyValueField = new KeyValueField
                {
                    Key = kvp.Key,
                    Value = ConvertToString(kvp.Value),
                    Type = GetTypeString(kvp.Value)
                };

                keyValueFields.Add(keyValueField);
            }

            //Send dictionary to the service side
            var response = await appEngine.SegmentService.SaveUserDefinedDefect(keyValueFields);
            if (response.Id == -1)
            {
                await App.Current.MainPage.DisplayAlert("Error", "There was an issue saving the new defect to the database.", "OK");
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Success", $"A new {tableName} entry has been successfully saved to the database.", "OK");

                //Draw this new graphic only if the overlay is loaded
                var matchingOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == tableName);
                if (matchingOverlay != null && matchingOverlay.Graphics.Count > 0)
                {
                    var newAttributes = new Dictionary<string, object>();
                    newAttributes["Id"] = response.Id;

                    foreach (var defectField in defectFields)
                    {
                        string defectKey = defectField.Key;
                        object defectValue = defectField.Value;

                        if (defectKey == "GPSTrackAngle")
                        {
                            newAttributes["TrackAngle"] = defectValue;
                            continue;
                        }

                        if (defectKey.Contains("GPS") || defectKey.Contains("Image") || defectKey == "SurveyDate" || defectKey == "PavementType")
                        {
                            continue;
                        }

                        if (defectKey != null && !string.IsNullOrEmpty(defectKey.ToString()))
                        {
                            newAttributes[defectKey] = defectValue;
                        }
                    }
                    //treat corner break differently
                    if (tableName == LayerNames.CornerBreak)
                    {
                        var cornerBreak = await appEngine.CornerBreakService.GetById(new IdRequest { Id = response.Id });
                        if (cornerBreak != null)
                        {
                            var list = new List<LCMS_Corner_Break> { cornerBreak };
                            var cornerBreakOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == tableName);
                            if (cornerBreakOverlay != null)
                            {
                                List<Graphic> graphicsbag = new List<Graphic>();
                                viewModel.CreateCornerBreakGraphicss(list, graphicsbag);
                                cornerBreakOverlay.Graphics.Add(graphicsbag.FirstOrDefault());
                            }
                        }
                    }
                    else
                    {
                        DrawNewlySavedGraphic(tableName, newAttributes, matchingOverlay);
                    }

                    //appState.UpdateTableNames();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in SaveLCMSTableDefect : " + ex.Message);
        }
    }

    private async Task SaveMetaTableDefect(string tableName, Dictionary<string, object> defectFields, Geometry projectedGeometry, string surveyId, int segmentId, bool last = true)
    {
        try
        {
            var metaTable = await appEngine.MetaTableService.GetByName(tableName);
            if (metaTable == null)
            {
                return;
            }

            var result = ProcessMetaTableGeometry(projectedGeometry, tableName);
            double trackAngle = 0.0;

            if (defectFields.TryGetValue("GPSTrackAngle", out var trackAngleObj))
            {
                trackAngle = (double)trackAngleObj;
            }
            else
            {
                trackAngle = 0.0;
            }

            var imageFileIndex = defectFields.TryGetValue("ImageFileIndex", out var indexObj) ? indexObj?.ToString() : null;
            long? lrpNumber = null;
            double chainage;

            if (defectFields.TryGetValue("LRPNumber", out var lrpNumberObj) && long.TryParse(lrpNumberObj?.ToString(), out var parsedLrp))
            {
                lrpNumber = parsedLrp;
            }

            if (defectFields.TryGetValue("Chainage", out var chainageObj) && double.TryParse(chainageObj?.ToString(), out var parsedChainage))
            {
                chainage = parsedChainage;
            }
            else
            {
                chainage = 0.0;
            }

            var metaTableValue = new Core.Models.Other.MetaTableValue
            {
                TableId = metaTable.Id,
                TableName = metaTable.TableName,
                GeoJSON = result.geojson,
                GPSLatitude = result.latitude,
                GPSLongitude = result.longitude,
                RoundedGPSLatitude = Math.Round(result.latitude, 4),
                RoundedGPSLongitude = Math.Round(result.longitude, 4),
                GPSTrackAngle = trackAngle,
                SurveyId = surveyId,
                SegmentId = segmentId,
                LRPNumber = lrpNumber,
                Chainage = chainage,
                ImageFileIndex = imageFileIndex
            };

            var columnToPropertyMap = new Dictionary<string, (string strProperty, string decProperty)>();
            for (int i = 1; i <= 25; i++)
            {
                var columnName = metaTable.GetType().GetProperty($"Column{i}")?.GetValue(metaTable) as string;
                var columnType = metaTable.GetType().GetProperty($"Column{i}Type")?.GetValue(metaTable) as string;

                if (!string.IsNullOrEmpty(columnName))
                {
                    columnToPropertyMap[columnName] = (strProperty: $"StrValue{i}", decProperty: $"DecValue{i}");
                }
            }

            foreach (var kvp in defectFields)
            {
                var columnName = kvp.Key;
                var value = kvp.Value;

                if (columnToPropertyMap.TryGetValue(columnName, out var properties))
                {
                    var (strProperty, decProperty) = properties;
                    if (value != null)
                    {
                        if (value is string stringValue)
                        {
                            // Set string value
                            metaTableValue.GetType().GetProperty(strProperty)?.SetValue(metaTableValue, stringValue);
                        }
                        else
                        {
                            // Set numeric values
                            try
                            {
                                decimal decimalValue = Convert.ToDecimal(value);
                                metaTableValue.GetType().GetProperty(decProperty)?.SetValue(metaTableValue, decimalValue);
                            }
                            catch
                            {
                                Log.Error("Skipping this metaTableValue due to its invalid value format, neither string nor numeric values.");
                            }
                        }
                    }
                }
            }

            var response = await appEngine.MetaTableService.Create(metaTableValue);

            if (last)
            {
                if (response.Id > 0)
                {
                    await App.Current.MainPage.DisplayAlert("Success", $"A new {tableName} entry has been successfully saved to the database.", "OK");

                    //Update Layer menu
                    appState.UpdateTableNames();

                    var matchingOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == metaTable.TableName);
                    if (matchingOverlay != null && matchingOverlay.Graphics.Count > 0)
                    {
                        //As this layer is loaded already, draw new graphic on the map
                        var newAttributes = new Dictionary<string, object>(defectFields);
                        newAttributes["Id"] = response.Id;
                        newAttributes["GeoJSON"] = result.geojson;
                        newAttributes["Type"] = "MetaTable";
                        DrawNewlySavedGraphic(tableName, newAttributes, matchingOverlay);
                    }
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "There was an issue saving the new defect to the database.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    private string ConvertToString(object value)
    {
        if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        return value.ToString();
    }

    private string GetTypeString(object value)
    {
        if (value is DateTime)
            return "DateTime";
        if (value is string)
            return "String";
        if (value is int || value is double || value is float || value is decimal)
            return "Number";
        return "Unknown";
    }

    private async void CreateDummySegment(string surveyId, Geometry projectedGeometry)
    {
        var sqlCommand = $"SELECT * from LCMS_Segment WHERE SurveyId = '{surveyId}' AND SegmentId = -1 ";

        var segmentResponse = await appEngine.SegmentService.QueryAsync(sqlCommand);

        if (segmentResponse.Count() == 0)
        {
            MapPoint firstPoint = projectedGeometry switch
            {
                MapPoint mPoint => mPoint,
                Polyline polyline => polyline.Parts.SelectMany(part => part.Points).FirstOrDefault(),
                Polygon polygon => polygon.Parts.SelectMany(part => part.Points).FirstOrDefault(),
                _ => null
            };
            double longitude = firstPoint != null ? firstPoint.X : 0.0;
            double latitude = firstPoint != null ? firstPoint.Y : 0.0;
            double[] coordinate = [longitude, latitude];
            var jsonDataObject = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[]
                    {
                        new List<double[]>()
                        {
                            coordinate
                        }
                    }
                },
                properties = new
                {
                    id = -1,
                    file = "dummy",
                    type = "Segment"
                }
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(jsonDataObject);
            //Create a new segment;
            var newSegment = new LCMS_Segment
            {
                SurveyId = surveyId,
                SectionId = "-1",
                SegmentId = -1,
                ImageFilePath = "dummy",
                GPSLatitude = latitude,
                GPSLongitude = longitude,
                GPSAltitude = 0.0,
                GPSTrackAngle = 0.0,
                GeoJSON = jsonData,
                RoundedGPSLatitude = Math.Round(latitude, 4),
                RoundedGPSLongitude = Math.Round(longitude, 4),
                Width = 0.0,
                Height = 0.0,
            };

            var createResponse = await appEngine.SegmentService.Create(newSegment);
        }
    }
    private void DrawNewlySavedGraphic(string overlayTableName, Dictionary<string, object> defectFields, GraphicsOverlay overlay)
    {
        try
        {
            var geoJson = defectFields["GeoJSON"].ToString();
            var attributes = defectFields
            .Where(kvp => kvp.Key != "GeoJSON")  // Exclude GeoJSON
            .ToDictionary();
            var graphic = GeneralMapHelper.ParseSimpleGeoJson(geoJson, attributes);
            string symbolType = null;
            if (graphic != null)
            {
                var existingSymbol = overlay.Graphics.FirstOrDefault().Symbol;

                if (graphic.Geometry is Polygon polygon)
                {
                    if (existingSymbol is SimpleFillSymbol fillSymbol)
                        symbolType = fillSymbol.Style == SimpleFillSymbolStyle.Null ? "FillLine" : "Fill";

                    var symbol = viewModel.GetGraphicSymbol(overlayTableName, GeoType.Polygon.ToString());
                    if (symbol != null)
                    {
                        graphic.Symbol = symbol;
                    }
                    else
                    {
                        graphic.Symbol = existingSymbol;
                    }
                }
                else if (graphic.Geometry is Polyline polyline)
                {
                    symbolType = "Line";
                    var symbol = viewModel.GetGraphicSymbol(overlayTableName, GeoType.Polyline.ToString());
                    if (symbol != null)
                    {
                        graphic.Symbol = symbol;
                    }
                    else
                    {
                        graphic.Symbol = existingSymbol;
                    }
                }
                else if (graphic.Geometry is MapPoint point)
                {
                    symbolType = "Point";
                    var symbol = viewModel.GetGraphicSymbol(overlayTableName, GeoType.Point.ToString());
                    if (symbol != null)
                    {
                        graphic.Symbol = symbol;
                    }
                    else
                    {
                        graphic.Symbol = existingSymbol;
                    }
                }

                //overwrite the symbol if color code exists
                var colorCodeInfo = appState.ColorCodeInfo.Where(x => x.TableName == overlayTableName).ToList();
                if (colorCodeInfo.Any())
                {
                    //apply color code
                    var propertyName = colorCodeInfo.FirstOrDefault().Property;
                    if (propertyName != null && graphic.Attributes.TryGetValue(propertyName, out var propertyValueObj))
                    {
                        var symbol = viewModel.SymbolFromColorList(propertyValueObj, colorCodeInfo, symbolType);
                        if (symbol != null)
                        {
                            graphic.Symbol = symbol;
                        }
                    }
                }

            }
            overlay.Graphics.Add(graphic);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in creating a new graphic on the map {ex.Message}");
        }
    }

    private (string geojson, string geoType, double latitude, double longitude) ProcessMetaTableGeometry(Geometry geometry, string tableName)
    {
        double latitude = 0.0;
        double longitude = 0.0;
        string geojson = string.Empty;
        string geoType = string.Empty;

        if (geometry is Esri.ArcGISRuntime.Geometry.MapPoint mapPoint)
        {
            latitude = mapPoint.Y;
            longitude = mapPoint.X;

            var formattedCoordinate = FormatCoordinateForGeoJSON(GeoType.Point, new List<MapPoint> { mapPoint });
            geojson = GeneralHelper.CreateNewGeoJson(GeoType.Point, formattedCoordinate, "null", "null", tableName);
            geoType = GeoType.Point.ToString();
        }
        else if (geometry is Esri.ArcGISRuntime.Geometry.Polyline polyline)
        {
            latitude = polyline.Parts.First().Points.First().Y;
            longitude = polyline.Parts.First().Points.First().X;

            var coordinates = polyline.Parts.SelectMany(part => part.Points).ToList();
            var formattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Polyline, coordinates);
            geojson = GeneralHelper.CreateNewGeoJson(GeoType.Polyline, formattedCoordinates, "null", "null", tableName);
            geoType = GeoType.Polyline.ToString();
        }
        else if (geometry is Esri.ArcGISRuntime.Geometry.Polygon polygon)
        {
            latitude = polygon.Parts.First().Points.First().Y;
            longitude = polygon.Parts.First().Points.First().X;

            var coordinates = polygon.Parts.SelectMany(part => part.Points).ToList();
            var formattedCoordinates = FormatCoordinateForGeoJSON(GeoType.Polygon, coordinates);
            geojson = GeneralHelper.CreateNewGeoJson(GeoType.Polygon, formattedCoordinates, "null", "null", tableName);
            geoType = GeoType.Polygon.ToString();
        }

        return (geojson, geoType, latitude, longitude);
    }

    //private bool _isVideoImageEmbedded = true;
    private bool IsVideoImageEmbedded = true;

    private bool isPlaying = false;
    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (isPlaying != value)
            {
                isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(ButtonText)); // Update the button text
            }
        }
    }
    private int playbackSpeed = 0;
    private Dictionary<string, Microsoft.Maui.Controls.Window> videoPopupWindows = new Dictionary<string, Microsoft.Maui.Controls.Window>();
    private bool _isMinimized = false;
    public string ButtonText => IsPlaying ? "\uF04C" : "\uF04B"; // Play or Stop icon
    private DateTime lastKeyPressTime;

    public bool AreControlsVisible => !_isMinimized;
    public string MinimizeButtonText => _isMinimized ? "\uF065" : "\uF068";
    private void ToggleMinimizeButton_Clicked(object sender, EventArgs e)
    {
        _isMinimized = !_isMinimized;

        // Manually update bindings
        OnPropertyChanged(nameof(AreControlsVisible));
        OnPropertyChanged(nameof(MinimizeButtonText));
    }
    private async Task DisplayVideoImages(string cameraInfo, string imagePath, bool isVideoRunning = false)
    {
        try
        {
            if (!IsVideoImageEmbedded)
            {
                if (videoPopupWindows.ContainsKey(cameraInfo))
                {
                    if (isVideoRunning)
                    {
                        var videoPopupWindow = videoPopupWindows[cameraInfo];
                        if (videoPopupWindow?.Page is ContentPage contentPage && contentPage.Content is VideoPopupPage videoPage)
                        {
                            videoPage.UpdateImage(imagePath);
                        }
                    }
                    else
                    {
                        var videoPopupWindow = videoPopupWindows[cameraInfo];
                        if (videoPopupWindow?.Page is ContentPage contentPage && contentPage.Content is VideoPopupPage videoPage)
                        {
                            videoPage.UpdateBothImageandBitmap(imagePath);
                        }
                        videoPopupWindow.Page.Dispatcher.Dispatch(() =>
                        {
                            var winUIWindow = videoPopupWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                            winUIWindow?.Activate();
                        });
                    }
                }
                else
                {
                    if (App.Current.Windows[0].Handler.PlatformView is Microsoft.UI.Xaml.Window mainWindow)
                    {
                        var bounds = mainWindow.Bounds;

                        int newXPosition = (int)bounds.Left;
                        int newYPosition = (int)bounds.Top;

                        var videoPage = new VideoPopupPage(imagePath, cameraInfo);
                        var newVideoPopupWindow = new Microsoft.Maui.Controls.Window
                        {
                            Page = new ContentPage
                            {
                                Content = videoPage
                            }
                        };

                        newVideoPopupWindow.Width = 400;
                        newVideoPopupWindow.Height = 300;
                        newVideoPopupWindow.Title = cameraInfo;
                        newVideoPopupWindow.X = newXPosition;
                        newVideoPopupWindow.Y = newYPosition;

                        newVideoPopupWindow.Destroying += (s, e) =>
                        {
                            videoPopupWindows.Remove(cameraInfo);
                            if (videoPopupWindows.Count == 0)
                            {
                                IsPlaying = false;
                            }
                        };

                        SetVideoPlayerVisibility(true);
                        App.Current?.OpenWindow(newVideoPopupWindow);

                        newVideoPopupWindow.Page.Dispatcher.Dispatch(() =>
                        {
                            var winUIWindow = newVideoPopupWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

                            // Bring the window to the front
                            winUIWindow.Activate();
                        });

                        videoPopupWindows.Add(cameraInfo, newVideoPopupWindow);
                    }
                }
            }
            else
            {

                var existingVideoComponent = VideoHorizontalStack.Children
                  .OfType<VideoPopupPage>()
                  .FirstOrDefault(v => v.AutomationId == cameraInfo);

                if (existingVideoComponent != null)
                {
                    if (isVideoRunning)
                    {
                        existingVideoComponent.UpdateImage(imagePath);
                    }
                    else
                    {
                        existingVideoComponent.UpdateBothImageandBitmap(imagePath);
                    }
                }
                else
                {
                    // Create a new VideoPopupPage and add it to the stack
                    var videoComponent = new VideoPopupPage(imagePath, cameraInfo)
                    {
                        AutomationId = cameraInfo,
                    };
                    videoComponent.ResizeImageMaxWidth(400);
                    AddVideoComponentToGrid(videoComponent);
                    SetVideoPlayerVisibility(true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    private void CloseVideoBtn_Clicked(object sender, EventArgs e)
    {
        SetVideoPlayerVisibility(false);
        if (videoPopupWindows.Count > 0)
        {
            foreach (var popupWindow in videoPopupWindows.Values)
            {
                App.Current?.CloseWindow(popupWindow);
            }
            videoPopupWindows.Clear();
        }
        else
        {
            var videoComponents = VideoHorizontalStack.Children
               .OfType<VideoPopupPage>()
               .ToList();
            foreach (var videoComponent in videoComponents)
            {
                RemoveVideoComponentFromGrid(videoComponent);
            }
            foreach (var selectedVideoGraphic in selectedVideoGraphics)
            {
                selectedVideoGraphic.IsSelected = false;
            }
            selectedVideoGraphics.Clear();
        }
    }

    private void CloseVideoImages(string cameraInfo)
    {
        try
        {
            if (videoPopupWindows.ContainsKey(cameraInfo))
            {
                var videoPopupWindow = videoPopupWindows[cameraInfo];
                if (videoPopupWindow != null)
                {
                    //Close the window
                    App.Current?.CloseWindow(videoPopupWindow);
                    videoPopupWindows.Remove(cameraInfo);
                }
                if (videoPopupWindows.Count == 0)
                {
                    SetVideoPlayerVisibility(false);
                }
            }
            else
            {
                var existingVideoComponent = VideoHorizontalStack.Children
                  .OfType<VideoPopupPage>()
                  .FirstOrDefault(v => v.AutomationId == cameraInfo);
                var itemsToRemove = new List<Graphic>();

                if (existingVideoComponent != null)
                {
                    RemoveVideoComponentFromGrid(existingVideoComponent);
                    foreach (var selectedVideo in selectedVideoGraphics)
                    {
                        if (selectedVideo.GraphicsOverlay != null && selectedVideo.GraphicsOverlay.Id == existingVideoComponent.AutomationId)
                        {
                            selectedVideo.IsSelected = false;
                            itemsToRemove.Add(selectedVideo);
                        }
                    }

                    if (VideoHorizontalStack.Count == 0)
                    {
                        SetVideoPlayerVisibility(false);
                    }

                    foreach (var item in itemsToRemove)
                    {
                        selectedVideoGraphics.Remove(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in CloseVideoImages {ex.Message}");
        }
    }
    private void AddVideoComponentToGrid(VideoPopupPage videoComponent)
    {
        VideoHorizontalStack.Add(videoComponent);
    }

    private void RemoveVideoComponentFromGrid(VideoPopupPage videoComponent)
    {
        VideoHorizontalStack.Remove(videoComponent);
    }
    private async void PlayButton_Clicked(object sender, EventArgs e)
    {
        // Check debounce interval
        if (DateTime.Now - lastKeyPressTime < TimeSpan.FromSeconds(0.3))
        {
            return; // Ignore this event if it's within 0.3 seconds of the last press
        }
        lastKeyPressTime = DateTime.Now; //Set the current time

        //When the video player reached to the last one, set everything to 0
        if (VideoPlayerSlider.Value >= VideoPlayerSlider.Maximum)
        {
            VideoPlayerSlider.Value = 0;

            for (int i = 0; i < selectedVideoGraphics.Count; i++)
            {
                var selectedVideo = selectedVideoGraphics[i];
                selectedVideo.IsSelected = false;
                var graphic = selectedVideo.GraphicsOverlay.Graphics.FirstOrDefault(g =>
                {
                    if (g.Attributes.TryGetValue("VideoFrameId", out var videoFrameId))
                    {
                        return (int)videoFrameId == 0;
                    }
                    return false;
                });
                selectedVideoGraphics[i] = graphic;
            }
            return;
        }

        IsPlaying = !IsPlaying;

        if (!IsPlaying) //pause
        {
            await PauseVideoPlayer();
        }
        else //play
        {
            await StartPlayingVideo();
        }
    }

    private async Task PauseVideoPlayer()
    {
        if (videoPopupWindows.Count > 0)
        {
            for (int i = 0; i < selectedVideoGraphics.Count; i++)
            {
                var selectedVideo = selectedVideoGraphics[i];
                if (selectedVideo != null && selectedVideo.Attributes.TryGetValue("ImagePath", out var filePath) &&
                    selectedVideo.Attributes.TryGetValue("CameraInfo", out var cameraInfo))
                {
                    await DisplayVideoImages(cameraInfo.ToString(), filePath.ToString());
                }
            }
        }
    }
    private async Task StartPlayingVideo()
    {
        if (viewModel.cameraOverlays.Count > 0)
        {
            while (IsPlaying && VideoPlayerSlider.Value <= VideoPlayerSlider.Maximum)
            {
                await MoveVideoGraphics(1, false, true);

                if (VideoPlayerSlider.Value == VideoPlayerSlider.Maximum)
                {
                    IsPlaying = false;
                    foreach (var video in selectedVideoGraphics)
                    {
                        if (video.Attributes.TryGetValue("CameraInfo", out var cameraInfo) &&
                            video.Attributes.TryGetValue("ImagePath", out var filePath))
                        {
                            await DisplayVideoImages(cameraInfo.ToString(), filePath.ToString(), false);
                        }
                    }
                }
            }
        }
    }

    private async Task MoveVideoGraphics(int num, bool isSliderDragged = false, bool isPlaying = false)
    {
        if (selectedVideoGraphics.Count > 0)
        {
            double trackAngle = 0.0;
            var tasks = new List<Task>();
            for (int i = 0; i < selectedVideoGraphics.Count; i++)
            {
                var selectedVideo = selectedVideoGraphics[i];
                selectedVideo.Attributes.TryGetValue("VideoFrameId", out var selectedVideoId);

                var currentId = Convert.ToInt32(selectedVideoId) + num;

                if (!isPlaying)
                {
                    if (currentId < 0)
                    {
                        currentId = 0; // Reset to minimum
                    }
                    else if (currentId >= selectedVideo.GraphicsOverlay.Graphics.Count)
                    {
                        currentId = selectedVideo.GraphicsOverlay.Graphics.Count - 1; // Reset to maximum valid ID
                    }
                }

                // Find the graphic with matching VideoFrameId
                var graphic = selectedVideo.GraphicsOverlay.Graphics.FirstOrDefault(g =>
                {
                    if (g.Attributes.TryGetValue("VideoFrameId", out var videoFrameId))
                    {
                        return (int)videoFrameId == currentId;
                    }
                    return false;
                });

                if (graphic != null && graphic.Attributes.TryGetValue("ImagePath", out var filePath) &&
                    graphic.Attributes.TryGetValue("CameraInfo", out var cameraInfo))
                {
                    selectedVideo.IsSelected = false;
                    tasks.Add(DisplayVideoImages(cameraInfo.ToString(), filePath.ToString(), isPlaying)); // Run async
                    selectedVideoGraphics[i] = graphic;
                    graphic.IsSelected = true;
                }
            }

            if (selectedVideoGraphics.First().Geometry is MapPoint point)
            {
                await MyMapView.SetViewpointCenterAsync(point);

                if (_selectedSegment != null)
                {
                    string currentSurvey = _selectedSegment.Attributes["SurveyId"].ToString();

                    var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.Segment);
                    if (segmentOverlay != null)
                    {
                        var segmentGraphic = GetClosestGraphicFromOverlay(segmentOverlay, point, 5, graphic =>
                        {
                            return graphic.Attributes.TryGetValue("SurveyId", out var surveyName) &&
                                   surveyName?.ToString() == currentSurvey;
                        });

                        if (segmentGraphic != null)
                        {
                            tasks.Add(HandleSegmentClick(segmentGraphic));
                        }
                    }
                }
            }

            await Task.WhenAll(tasks);

            if (isPlaying)
            {
                // Apply the user-selected playback speed
                await Task.Delay(playbackSpeed);
            }

            if (!isSliderDragged)
            {
                VideoPlayerSlider.Value += num;
            }
        }
    }
    private async void backwardBtn_Clicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        button.IsEnabled = false;

        await MoveVideoGraphics(-1);

        button.IsEnabled = true;
    }

    private async void forwardBtn_Clicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        button.IsEnabled = false;

        await MoveVideoGraphics(1);

        button.IsEnabled = true;
    }
    private void OnSpeedSelected(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.MenuFlyoutItem menuItem && menuItem.CommandParameter is string speedInMilliseconds)
        {
            // Parse the speed from the CommandParameter and set the playbackSpeed
            playbackSpeed = int.Parse(speedInMilliseconds);
        }
    }
    private void SettingBtn_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        OpenMenuFlyout(sender, e);
        Speed0ms.Text = "0ms";
        Speed300ms.Text = "300ms";
        Speed500ms.Text = "500ms";
        Speed1000ms.Text = "1000ms";

        if (playbackSpeed == 0)
        {
            Speed0ms.Text += " \u2713";
        }
        else if (playbackSpeed == 300)
        {
            Speed300ms.Text += " \u2713";
        }
        else if (playbackSpeed == 500)
        {
            Speed500ms.Text += " \u2713";
        }
        else if (playbackSpeed == 1000)
        {
            Speed1000ms.Text += " \u2713";
        }
    }

    private async void JumpToChainageClicked(object sender, EventArgs e)
    {
        string input = await App.Current.MainPage.DisplayPromptAsync(
                      "Jump to Chainage",
                      "Please enter the chainage: ",
                      "OK",
                      "Cancel",
                      keyboard: Microsoft.Maui.Keyboard.Numeric
                  );

        if (input == null) return; //cancel
        if (int.TryParse(input, out int targetChainage) && targetChainage >= 0)
        {
            var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.Segment);
            List<Graphic> closestGraphics = new List<Graphic>();
            //move to the chainage in videos
            foreach (var selectedVideo in selectedVideoGraphics)
            {
                var closestGraphic = selectedVideo.GraphicsOverlay.Graphics
                 .Where(g => g.Attributes.TryGetValue("Chainage", out var c) && double.TryParse(c.ToString(), out _))
                 .MinBy(g => Math.Abs(double.Parse(g.Attributes["Chainage"].ToString()) - targetChainage));

                if (closestGraphic != null && closestGraphic.Attributes.TryGetValue("VideoFrameId", out var videoFrameId) && double.TryParse(videoFrameId.ToString(), out double sliderValue))
                {
                    VideoPlayerSlider.Value = sliderValue;
                    closestGraphics.Add(closestGraphic);
                    await ReplaceSelectedVideoGraphics(closestGraphic);
                }
            }
            // Safely update the original collection after iteration
            selectedVideoGraphics = closestGraphics;
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Invalid Input", "Please enter a valid number.", "OK");
        }
    }

    public async void SetVideoPlayerVisibility(bool visible)
    {
        if (!visible && IsPlaying)
        {
            IsPlaying = false;
            await PauseVideoPlayer();
        }

        VideoPlayer.IsVisible = visible;
    }

    //private async void OnVideoPlayerSliderValueChanged(object sender, ValueChangedEventArgs e)
    //{
    //    //don't trigger value changes if the user is moving the slider
    //    if (isSliderMoving) return;

    //    double sliderValue = e.NewValue;
    //    await UpdateVideoGraphicWithSliderValue(sliderValue);
    //}

    private async Task ReplaceSelectedVideoGraphics(Graphic graphic)
    {
        var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.Segment);
        var selectedGraphics = graphic.GraphicsOverlay.Graphics?.Where(x => x.IsSelected).ToList();
        //Deselect all the other graphics before highlighting matching graphic
        if (selectedGraphics != null)
        {
            foreach (var selected in selectedGraphics)
            {
                selected.IsSelected = false;
            }
        }
        graphic.IsSelected = true;

        if (graphic.Attributes.TryGetValue("ImagePath", out var filePath) && graphic.Attributes.TryGetValue("CameraInfo", out var cameraInfo))
        {
            await DisplayVideoImages(cameraInfo.ToString(), filePath.ToString(), isPlaying);
            if (segmentOverlay != null && graphic.Geometry is MapPoint point && graphic.Attributes.TryGetValue("SurveyId", out var survey))
            {
                await MyMapView.SetViewpointCenterAsync(point);
                await SyncSegmentWithVideo(segmentOverlay, point, 5, survey.ToString());
            }
        }
    }

    private async Task UpdateVideoGraphicWithSliderValue(double sliderValue)
    {
        var copySelectedVideoGraphics = new List<Graphic>();
        var segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == LayerNames.Segment);
        var graphicsToCheck = selectedVideoGraphics.ToList();

        if (graphicsToCheck.Count > 0)
        {
            foreach (var graphic in graphicsToCheck)
            {
                var overlay = graphic.GraphicsOverlay;
                if (overlay != null && overlay.Graphics.Count > 0)
                {
                    var matchingGraphic = overlay.Graphics
                                       .FirstOrDefault(x => x.Attributes.TryGetValue("VideoFrameId", out var videoFrameId) && (int)videoFrameId == (int)sliderValue);

                    if (matchingGraphic != null)
                    {
                        copySelectedVideoGraphics.Add(matchingGraphic);
                        await ReplaceSelectedVideoGraphics(matchingGraphic);
                    }
                }
            }
            // Safely update the original collection after iteration
            selectedVideoGraphics = copySelectedVideoGraphics;
        }
    }

    private void OnSliderDragStarted(object sender, EventArgs e)
    {
        //isSliderMoving = true;
        // Pause playback
        if (IsPlaying)
        {
            IsPlaying = false; //Stop the video from playing
        }
    }

    private async void OnSliderDragCompleted(object sender, EventArgs e)
    {
        //isSliderMoving = false;
        // Once dragging is complete, update the graphics to reflect the final slider value
        double sliderValue = VideoPlayerSlider.Value;
        await UpdateVideoGraphicWithSliderValue(sliderValue);
    }

    private async void VideoPopup_Clicked(object sender, EventArgs e)
    {
        IsVideoImageEmbedded = !IsVideoImageEmbedded;

        if (!IsVideoImageEmbedded)
        {
            var existingVideoComponent = VideoHorizontalStack.Children
                  .OfType<VideoPopupPage>()
                  .ToList();

            if (existingVideoComponent != null && existingVideoComponent.Count > 0)
            {
                //make embedded video images to popup
                foreach (var videoComponent in existingVideoComponent)
                {
                    var cameraInfo = videoComponent.AutomationId;
                    var imagePath = videoComponent.imagePath;

                    RemoveVideoComponentFromGrid(videoComponent);
                    var newVideoPopupWindow = new Microsoft.Maui.Controls.Window
                    {
                        Page = new ContentPage
                        {
                            Content = videoComponent
                        }
                    };
                    videoComponent.ResizeImageMaxWidth(null);

                    newVideoPopupWindow.Width = 400;
                    newVideoPopupWindow.Height = 300;
                    newVideoPopupWindow.Title = cameraInfo;

                    newVideoPopupWindow.Destroying += (s, e) =>
                    {
                        videoPopupWindows.Remove(cameraInfo);
                        if (videoPopupWindows.Count == 0)
                        {
                            IsPlaying = false;
                        }
                    };
                    App.Current?.OpenWindow(newVideoPopupWindow);

                    newVideoPopupWindow.Page.Dispatcher.Dispatch(() =>
                    {
                        var winUIWindow = newVideoPopupWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                        winUIWindow?.Activate();
                    });

                    videoPopupWindows.Add(cameraInfo, newVideoPopupWindow);
                }
            }
        }
        else
        {
            foreach (var cameraInfo in videoPopupWindows.Keys.ToList())
            {
                var videoPopupWindow = videoPopupWindows[cameraInfo];

                if (videoPopupWindow?.Page is ContentPage contentPage && contentPage.Content is VideoPopupPage videoPage)
                {
                    // Remove popup window
                    App.Current?.CloseWindow(videoPopupWindow);

                    videoPage.ResizeImageMaxWidth(400);
                    if (videoPage.AutomationId == null)
                    {
                        videoPage.AutomationId = cameraInfo;
                    }
                    // Add the video component back to the embedded stack
                    AddVideoComponentToGrid(videoPage);
                    //DynamicVideoGrid.Children.Add(videoPage);
                }
            }
            SetVideoPlayerVisibility(true);
            videoPopupWindows.Clear();
        }
    }

    private async void PrintScreenButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            string documentsPath = AppPaths.DocumentsFolder;
            string fileName = $"DataView_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(documentsPath, fileName);
            ScreenshotHelper.TakeAndSave(filePath);
            await App.Current.MainPage.DisplayAlert("Screenshot Saved", $"Saved to {filePath}", "OK");

        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", $"Failed to capture or save the screenshot: {ex.Message}", "OK");
        }

    }

    private void ManageCrackGridButton_Clicked(object sender, EventArgs e)
    {
        if (!IsSelectingCracks)
        {
            DeactivateAllMapButtons();
            IsSelectingCracks = true;
            messageCrackLabel.Text = "Select the cracks classificaton to be reevaluated, select G to reevaluate";
            SelectMapArea();
            //ManageCrackSummaryButton.Background = Color.FromArgb("#FFFFFF");
            ManageCrackButton.Background = Color.FromArgb("#FFFFFF");
        }
        else
        {
            ClearCrackMultiSelect();
        }
    }

    private void ManageCrackSummaryButton_Clicked(object sender, EventArgs e)
    {
        if (!IsSelectingSummaryCracks)
        {
            DeactivateAllMapButtons();
            IsSelectingSummaryCracks = true;
            messageCrackLabel.Text = "Select the cracks summary to be reevaluated, select G to reevaluate";
            SelectMapArea();
            ManageCrackButton.Background = Color.FromArgb("#FFFFFF");
        }
        else
        {
            ClearCrackMultiSelect();
        }
    }
    private void ClearCrackMultiSelect()
    {
        if ((IsSelectingSummaryCracks || IsSelectingCracks) && _oldGraphics.Any())
        {
            foreach (var oldGraphic in _oldGraphics)
            {
                oldGraphic.IsSelected = false;
            }
            _oldGraphics.Clear();
        }

        _geometryEditor.Stop();
        ManageCrackButton.Background = Color.FromArgb("#90D3D3D3");
        IsSelectingSummaryCracks = false;
        IsSelectingCracks = false;
    }

    private void OpenMenuFlyout(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        var view = sender as View;
        var point = e.GetPosition(view);
        var element = (view.Handler as ViewHandler).PlatformView;
        element.ContextFlyout.ShowAt(element, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
        {
            Position = new Windows.Foundation.Point(point.Value.X, point.Value.Y),
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
        });
    }

    private async Task CreateIRIMetaTable(string recalculateIRIStr)
    {
        var existingMetaTable = await appEngine.MetaTableService.GetByName(recalculateIRIStr);
        if (existingMetaTable != null && existingMetaTable.TableName != null)
        {
            // MetaTable already exists, no need to create
            return;
        }

        //create a new one when metaTable doesn't exist
        var iriMetaTable = new MetaTable
        {
            TableName = recalculateIRIStr,
            GeoType = "Polyline",
            Column1 = "Average LaneIRI",
            Column1Type = ColumnType.Number.ToString(),
            Column2 = "Average LwpIRI",
            Column2Type = ColumnType.Number.ToString(),
            Column3 = "Average RwpIRI",
            Column3Type = ColumnType.Number.ToString(),
            Column4 = "Average CwpIRI",
            Column4Type = ColumnType.Number.ToString()
        };

        var response = await appEngine.MetaTableService.CreateMetaTable(iriMetaTable);
        if (response.Id == -1)
        {
            throw new Exception("MetaTable creation failed.");
        }
    }

    public class IRISummaryData
    {
        public List<MapPoint> Coordinates { get; set; } = new();
        public List<double> IRIValues { get; set; } = new();
        public List<double> Chainages { get; set; } = new();
        public void Add(MapPoint point, double iri, double chainage)
        {
            Coordinates.Add(point);
            IRIValues.Add(iri);
            Chainages.Add(chainage);
        }
        public void Reset()
        {
            Coordinates.Clear();
            IRIValues.Clear();
            Chainages.Clear();
        }
        public double Average() => IRIValues.Any() ? Math.Round(IRIValues.Average(), 2) : 0;
    }

    private void StartIRIPoint()
    {
        //allow the user to click on the map for start/end points
        IsSummarizingIRI = true;
    }

    private async Task ProcessIRIByUserDefinedMeter(double startChainage, double endChainage)
    {
        var survey = appState.IRISurveyId;
        var userDefinedMeter = appState.IRIUserDefinedMeter;
        var recalculateIRIStr = $"IRI {userDefinedMeter} Meter Section";

        try
        {
            //check if IRI metaTable exists and if not, create one
            await CreateIRIMetaTable(recalculateIRIStr);

            //delete if there is already data in db
            var hasMetaTableValue = await appEngine.MetaTableService.GetExistingMetaTableNamesBySurvey(survey);
            if (hasMetaTableValue != null && hasMetaTableValue.Any(item => item.Equals(recalculateIRIStr)))
            {
                var deletesqlString = $"DELETE FROM MetaTableValue WHERE SurveyId = '{survey}' AND TableName = '{recalculateIRIStr}'";
                var response = await appEngine.SegmentService.ExecuteSQlQueries(new List<string> { deletesqlString });
                if (response.Id == 0)
                {
                    Console.WriteLine("Recalculate IRI MetaTable have been successfully deleted.");
                }
            }

            //get all the IRI polylines
            var iriInfo = await appEngine.RoughnessService.GetBetweenChainages(new ChainagePoints { StartChainage = startChainage, EndChainage = endChainage });
            if (iriInfo == null || iriInfo.Count == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "No Roughness value was found between the start and end points of the selected survey.", "OK");
                return;
            }

            var iriInOrder = iriInfo.OrderBy(x => x.LongitudinalPositionY).ToList(); //Order by position y

            int iriSectionId = 1;
            int? previousSegmentId = null;
            var laneIRI = new IRISummaryData();
            var lwpIRI = new IRISummaryData();
            var rwpIRI = new IRISummaryData();
            var cwpIRI = new IRISummaryData();
            double subtractDistance = iriInOrder.First().LongitudinalPositionY;

            // Track if it's the very first segment
            bool isFirstSegment = true;

            foreach (var iri in iriInOrder)
            {
                if (iri.Interval != 1)
                {
                    await App.Current.MainPage.DisplayAlert("Error", $"Invalid interval detected: {iri.Interval} meters. Only 1 meter interval can be processed.", "OK");
                    return;
                }

                // Reset if two consecutive segments are missing
                if (previousSegmentId != null && (iri.SegmentId - previousSegmentId) >= 2)
                {
                    await SaveMultipleRoughnessSections(recalculateIRIStr, new[]
                    {
                        (cwpIRI, "Average CwpIRI", false),
                        (laneIRI, "Average LaneIRI", false),
                        (lwpIRI, "Average LwpIRI", false),
                        (rwpIRI, "Average RwpIRI", false)
                    }, survey, iriSectionId);

                    iriSectionId++;
                    // Reset state
                    laneIRI.Reset();
                    lwpIRI.Reset();
                    rwpIRI.Reset();
                    cwpIRI.Reset();
                    subtractDistance = iri.LongitudinalPositionY;
                    isFirstSegment = true;
                }

                var parsedLane = ParseRoughnessGeoJSON(iri.GeoJSON);
                var parsedLeft = ParseRoughnessGeoJSON(iri.LwpGeoJSON);
                var parsedRight = ParseRoughnessGeoJSON(iri.RwpGeoJSON);
                var parsedCenter = iri.CwpGeoJSON != null ? ParseRoughnessGeoJSON(iri.CwpGeoJSON) : (null, null);

                var positionY = iri.LongitudinalPositionY;

                if (positionY - subtractDistance < userDefinedMeter)
                {
                    // If it's the first segment, add the first point explicitly
                    if (isFirstSegment)
                    {
                        laneIRI.Add(parsedLane.First, iri.LaneIRI, iri.Chainage);
                        lwpIRI.Add(parsedLeft.First, iri.LwpIRI, iri.Chainage);
                        rwpIRI.Add(parsedRight.First, iri.RwpIRI, iri.Chainage);
                        if (parsedCenter != (null, null)) cwpIRI.Add(parsedCenter.First, iri.CwpIRI.Value, iri.Chainage);
                        isFirstSegment = false; // Reset the flag after the first segment
                    }
                    else
                    {
                        laneIRI.Add(parsedLane.Second, iri.LaneIRI, iri.Chainage);
                        lwpIRI.Add(parsedLeft.Second, iri.LwpIRI, iri.Chainage);
                        rwpIRI.Add(parsedRight.Second, iri.RwpIRI, iri.Chainage);
                        if (parsedCenter != (null, null)) cwpIRI.Add(parsedCenter.Second, iri.CwpIRI.Value, iri.Chainage);
                    }
                }
                else
                {
                    //Save polyline if user defined meter exceeded
                    await SaveMultipleRoughnessSections(recalculateIRIStr, new[]
                                    {
                        (cwpIRI, "Average CwpIRI", false),
                        (laneIRI, "Average LaneIRI", false),
                        (lwpIRI, "Average LwpIRI", false),
                        (rwpIRI, "Average RwpIRI", false)
                    }, survey, iriSectionId);

                    iriSectionId++;

                    //Reset
                    laneIRI.Reset();
                    lwpIRI.Reset();
                    rwpIRI.Reset();
                    cwpIRI.Reset();

                    // Start a new polyline with the remaining data
                    laneIRI.Add(parsedLane.First, iri.LaneIRI, iri.Chainage);
                    lwpIRI.Add(parsedLeft.First, iri.LwpIRI, iri.Chainage);
                    rwpIRI.Add(parsedRight.First, iri.RwpIRI, iri.Chainage);
                    if (parsedCenter != (null, null)) cwpIRI.Add(parsedCenter.First, iri.CwpIRI.Value, iri.Chainage);
                    subtractDistance = positionY;
                }
                previousSegmentId = iri.SegmentId;
            }

            //Handle any remaining coordinates after the loop
            if (laneIRI.Coordinates.Any())
            {
                await SaveMultipleRoughnessSections(recalculateIRIStr, new[]
                {
                    (cwpIRI, "Average CwpIRI", false),
                    (laneIRI, "Average LaneIRI", false),
                    (lwpIRI, "Average LwpIRI", false),
                    (rwpIRI, "Average RwpIRI", true)
                }, survey, iriSectionId);
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", "Sorry. Error occured while creating a new 'IRI 100 meter Section' table in the database", "OK");
        }
        finally
        {
            var overlay = MyMapView.GraphicsOverlays.FirstOrDefault(x => x.Id == recalculateIRIStr);
            if (overlay != null)
            {
                if (overlay.Graphics.Count > 0)
                {
                    appState.RefreshTableNames();
                }
            }
        }
    }


    private async Task SaveMultipleRoughnessSections(string tableName, IEnumerable<(IRISummaryData Data, string Type, bool IsLast)> roughnessSets, string surveyId, int sectionId)
    {
        foreach (var (data, type, isLast) in roughnessSets)
        {
            if (data.Coordinates.Any() && data.IRIValues.Any())
            {
                double avgValue = data.Average();
                var polyline = new Polyline(data.Coordinates, SpatialReferences.Wgs84);
                var dict = new Dictionary<string, object>
                {
                    { type, avgValue },
                    { "Chainage", data.Chainages.First() } //later if end chainage needed just use chianage.Last()
                };
                await SaveMetaTableDefect(tableName, dict, polyline, surveyId, sectionId, isLast);
            }
        }
    }

    private (MapPoint First, MapPoint Second) ParseRoughnessGeoJSON(string geoJson)
    {
        try
        {
            // Parse the GeoJSON string
            var geoJsonObject = JsonDocument.Parse(geoJson);

            // Extract the "coordinates" array
            var coordinates = geoJsonObject.RootElement.GetProperty("geometry").GetProperty("coordinates");

            // Get the first and second coordinates
            var firstCoordinate = coordinates[0];
            var secondCoordinate = coordinates[1];

            // Convert to arrays of doubles
            var first = new MapPoint(firstCoordinate[0].GetDouble(), firstCoordinate[1].GetDouble());
            var second = new MapPoint(secondCoordinate[0].GetDouble(), secondCoordinate[1].GetDouble());

            return (first, second);

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse GeoJSON: {ex.Message}");
        }
    }

    private void DeactivateAllMapButtons(bool lasRutActive = false)
    {
        // Clear all active modes
        if (IsSelectingSegments) ClearMultiSelectSegments();
        if (IsSelectingCracks || IsSelectingSummaryCracks) ClearCrackMultiSelect();
        if (IsProcessingMultiDefects) ClearMultiSelectDefects();
        if (IsSelectingLasRutting) ClearAutoRepeatRut();
        if (IsLasFilesMode && !lasRutActive) ClearLasFileRut();
        if (IsSelectingSegmentation) ClearSegmentation();
        if (IsPolylineActive || IsPolygonActive) ClearMeasurementButtons();
        if (IsSummarizingIRI) ClearIRISummary();

        // Close all popups
        ClosePopupInMapClicked();
    }
    private void ClearMultiSelectSegments()
    {
        if (_geometryEditor.IsStarted)
        {
            _geometryEditor.Stop();
        }
        MoveSegmentsButton.Background = Color.FromArgb("#90D3D3D3");
        if (IsMovingSegments && previousGraphics.Any())
        {
            if (viewModel.segmentSymbol != null)
            {
                var segmentSymbol = viewModel.segmentSymbol;

                foreach (var previousGraphic in previousGraphics)
                {
                    if (previousGraphic != null)
                        previousGraphic.Symbol = segmentSymbol;
                }
            }
        }

        // Clear the previousGraphics list since the highlights are removed
        _oldGraphics.Clear();
        previousGraphics.Clear();
        _geometryEditor.Stop();
        _geometryEditor.PropertyChanged -= MultiSelectSegments;
        IsMovingSegments = false;
        IsSelectingSegments = false;

        //Reprocess Segments selected
        if (appState.isReprocessingSegments)
        {
            appState.isReprocessingSegments = false;
            appState.incorrectReprocessFolder = false;
        }
    }

    private void ClearLasFileRut()
    {
        LasFileButton.Background = Color.FromArgb("#90D3D3D3");  // Inactive button style
        if (lasLinesOverlay != null)
        {
            lasLinesOverlay.Graphics.Clear();
            MyMapView.GraphicsOverlays.Remove(lasLinesOverlay);
            MyMapView.DismissCallout();


        }
        lasStartPoint = null;
        lasEndPoint = null;
        IsLasRepeatMode = false;
        IsLasFilesMode = false;
        AutoRepeatLasButton.IsVisible = false;

        try
        {
            if (_selectedGraphic != null)
            {
                _selectedGraphic.IsSelected = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in ClearLasFiles : {ex.Message}");
        }

    }

    private void ClearMultiSelectDefects()
    {
        if (_geometryEditor.IsStarted)
        {
            _geometryEditor.PropertyChanged -= MultiSelectDefects;
            isCreatingGraphics = false;
            _geometryEditor.Stop();
        }
        selectedGraphics?.Clear();
        selectedGraphics = null;

        messageBoxMultiSelect.IsVisible = false;
        messageBoxDefects.IsVisible = false;
        IsProcessingMultiDefects = false;
    }

    private async void ClearSegmentation()
    {
        IsSelectingSegmentation = false;
        SegmentationButton.Background = Color.FromArgb("#90D3D3D3");

        if (surveySegmentationId != 0)
        {
            // Delete current selected segmentation record from database when clearing segmentation
            SurveySegmentation ss1 = await appEngine.SurveySegmentationService.GetById(new IdRequest { Id = surveySegmentationId });
            await appEngine.SurveySegmentationService.DeleteObject(ss1);
        }
        //reset all data from the map
        //surveySegmentationId = 0;
        mapPointData = new MapPointData();
        //selectedSegmentsForSurvey.Clear();
        newOverlay.Graphics.Clear();
        MyMapView.GraphicsOverlays.Remove(newOverlay);
        MyMapView.DismissCallout();

    }

    private void ClearIRISummary()
    {
        //clear mapData
        mapPointData = new MapPointData();
        newOverlay?.Graphics.Clear();
        MyMapView.GraphicsOverlays.Remove(newOverlay);
        IsSummarizingIRI = false;
        appState.NotifyIRIStatus(false);
    }

    private void SurveySegmentationAuto_Clicked(object sender, EventArgs e)
    {
        SurveySegmentation_Clicked(false);
    }

    private void SurveySegmentationManual_Clicked(object sender, EventArgs e)
    {
        SurveySegmentation_Clicked(true);
    }

    private void SurveySegmentation_Clicked(bool isManual)
    {
        CancelImportedSegmentation();
        DeactivateAllMapButtons();
        SegmentationButton.Background = Color.FromArgb("#FFFFFF");
        ManualSegmentationPage.IsVisible = true;
        appState.IsPopupOpen = true;
        //appState.ManualSegmentationSelected();

    }

    private async void SurveySegmentationImport_Clicked(object sender, EventArgs e)
    {
        try
        {
            CancelImportedSegmentation();
            DeactivateAllMapButtons();
            SegmentationButton.Background = Color.FromArgb("#FFFFFF");
            appState.ImportSegmentation();
            ImportSegmentSurveysPage.IsVisible = true;
            appState.IsPopupOpen = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in SurveySegmentationImport_Clicked : {ex.Message}");
        }
    }


    //Get csv lines and display on the map, then creates table with segmentation datas
    private async void OnImportingSegmentation()
    {

        try
        {
            HashSet<Survey> surveysToSegment = appState.SelectedSurveysForSegmentation;
            List<string> surveyListCsv = new List<string>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                NewLine = Environment.NewLine
            };

            foreach (var survey in surveysToSegment)
            {
                // Get the original path
                string originalPath = survey.ImageFolderPath;

                // Remove the "LCMS" folder from the path
                string modifiedPath = originalPath;

                if (originalPath.EndsWith("LCMS" + Path.DirectorySeparatorChar))
                {
                    // Remove "LCMS\" from the end
                    modifiedPath = originalPath.Substring(0, originalPath.Length - ("LCMS" + Path.DirectorySeparatorChar).Length);
                }
                else
                {
                    // Alternatively, if "LCMS" could be anywhere in the path, remove it explicitly
                    modifiedPath = originalPath.Replace(Path.Combine("LCMS", ""), "");
                }

                // Construct the final modified path to the folder
                // No wildcard; just the directory path
                string directoryPath = Path.Combine(modifiedPath, "");

                // Retrieve all CSV files starting with "CompletedSurveyList"
                var csvFiles = Directory.GetFiles(directoryPath, "CompletedSurveyList*.csv");

                // Add each found file's path to the surveyListCsv
                surveyListCsv.AddRange(csvFiles);
            }

            // Get segmentations surveys from csv files
            foreach (var csvPath in surveyListCsv)
            {
                if (File.Exists(csvPath))
                {
                    using var reader = new StreamReader(csvPath);
                    using var csv = new CsvReader(reader, config);
                    csv.Context.RegisterClassMap<GeoJsonObjectMap>();
                    List<GeoJsonObject> geoJsonObjects = csv.GetRecords<GeoJsonObject>().ToList();
                    LoaderOverlay.IsVisible = true;


                    if (geoJsonObjects != null && geoJsonObjects.Count > 0)
                    {
                        //add surveyset layer to load graphics on the map
                        appState.HandleSurveyTemplate();
                        bool first = true;
                        List<MapCoordinateDetails> mapDetailsList = new List<MapCoordinateDetails>();

                        foreach (var item in geoJsonObjects)
                        {
                            var mapCoordinateDetails = new MapCoordinateDetails
                            {
                                Coordinates = item.geometry.coordinates.Select(inner => inner.ToList()).ToList(),
                                FileName = item.properties.surveyId,
                                GeometryType = item.geometry.type,
                                IsFirst = first,
                                SampleUnitStatus = null,
                                SurveyStatus = item.properties.Status,
                                StartChainage = item.properties.startChainage
                            };
                            mapDetailsList.Add(mapCoordinateDetails);
                            first = false;
                        }

                        if (mapDetailsList.Any())
                        {
                            DisplayBoundaryGraphicsOnMap(mapDetailsList);
                        }
                    }

                }
                else
                {
                    App.Current.MainPage.DisplayAlert("File Not Found", $"The file {csvPath} does not exist.", "OK");
                }

            }
            //show message box to accept or cancel the segmentation import
            List<SegmentationTableData> segmentationTableDatas = await ImportSelectedSegmentation();


            CreateTableForImportSegmentation(segmentationTableDatas);

        }


        catch (Exception ex)
        {
            Log.Error($"Error in OnImportingSegmentation : {ex.Message}");
        }
        finally
        {
            LoaderOverlay.IsVisible = false;
        }
    }


    private async Task CreateTableForImportSegmentation(List<SegmentationTableData> surveysToSave)
    {
        try
        {
            appState.tableDatas = surveysToSave;

            DisplaySegmentationTable();

        }
        catch (Exception ex)
        {
            Log.Error($"Error in creating table for Segmentation : {ex.Message}");
        }
    }


    private async Task<List<SegmentationTableData>> ImportSelectedSegmentation()
    {
        try
        {
            GraphicsOverlay segmentOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "Segment");
            GraphicsOverlay surveySetOverlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay");

            List<SegmentationTableData> segmentationTableDatas = new List<SegmentationTableData>();


            if (segmentOverlay != null && surveySetOverlay != null)
            {
                foreach (var line in surveySetOverlay.Graphics)
                {
                    line.Attributes.TryGetValue("jsonFilePath", out var newSurveyName);
                    line.Attributes.TryGetValue("startChainage", out var newStartChainage);
                    var exist = appEngine.SurveyService.GetSurveyEntityByName(newSurveyName?.ToString()); //this returns new survey if no survey

                    if (exist.Result.Id != 0)
                    {
                        continue; // Skip this line if survey already exists
                    }


                    if (line.Geometry is Polyline polyline)
                    {
                        var coordinatesList = ConvertGeometryToCoordinates(polyline);
                        var startpoint = coordinatesList.FirstOrDefault();
                        var endpoint = coordinatesList.LastOrDefault();
                        var lengthgeo = GeometryEngine.LengthGeodetic(polyline);

                        // Create a MapPoint using the coordinates and the spatial reference of the map view
                        MapPoint mapPointStart = new(startpoint[0], startpoint[1], polyline.SpatialReference);
                        MapPoint mapPointEnd = new(endpoint[0], endpoint[1], polyline.SpatialReference);

                        List<Graphic> possStartsegmentsGraphics = GetIntersectingGraphicsIndividual(segmentOverlay.Graphics.ToList(), mapPointStart);
                        List<Graphic> possEndSegmentsGraphics = GetIntersectingGraphicsIndividual(segmentOverlay.Graphics.ToList(), mapPointEnd);

                        // Check if either possible start or end segment graphics are missing
                        if (possStartsegmentsGraphics.Count == 0)
                        {
                            // Get closest start segments if none found
                            possStartsegmentsGraphics = GetClosestGraphicsFromOverlay(segmentOverlay, mapPointStart, 0.02);
                        }

                        if (possEndSegmentsGraphics.Count == 0)
                        {
                            // Get closest end segments if none found
                            possEndSegmentsGraphics = GetClosestGraphicsFromOverlay(segmentOverlay, mapPointEnd, 0.02);
                        }

                        // Check if both lists are still empty after attempting to find closest segments
                        if (possStartsegmentsGraphics.Count == 0 || possEndSegmentsGraphics.Count == 0)
                        {
                            continue; // Skip this line if no valid start or end segments found
                        }

                        var (selectedStartGraphic, selectedEndGraphic) = GetBestStartEndAutoSegmentationSegments(
                            possStartsegmentsGraphics,
                            possEndSegmentsGraphics,
                            lengthgeo);


                        selectedStartGraphic.Attributes.TryGetValue("SectionId", out var startSegmentId);
                        selectedStartGraphic.Attributes.TryGetValue("SurveyId", out var oldSurveyId);
                        selectedEndGraphic.Attributes.TryGetValue("SectionId", out var endSegmentId);

                        //add start and end segment attributes to the line graphic to highlight later
                        line.Attributes.Add("startSegment", startSegmentId);
                        line.Attributes.Add("endSegment", endSegmentId);



                        Survey oldsurvey = await appEngine.SurveyService.GetSurveyEntityByExternalId((string)oldSurveyId);

                        // Get chainage for this start segment and point
                        var startChainageRequest = new ChainageMapPointRequest
                        {
                            Latitude = mapPointStart.Y,
                            Longitude = mapPointStart.X,
                            SegmentId = (int)startSegmentId,
                            SurveyId = oldsurvey.SurveyIdExternal.ToString()
                        };
                        var startChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(startChainageRequest);
                        var endChainageRequest = new ChainageMapPointRequest
                        {
                            Latitude = mapPointEnd.Y,
                            Longitude = mapPointEnd.X,
                            SegmentId = (int)endSegmentId,
                            SurveyId = oldsurvey.SurveyIdExternal.ToString()
                        };
                        var endChainageResponse = await appEngine.SegmentService.GetChainageFromSegmentIdToMapPoint(endChainageRequest);

                        SegmentationTableData segmentationTableData = new SegmentationTableData
                        {
                            SurveyName = newSurveyName?.ToString(),
                            SegmentRangeStart = (int)startSegmentId,
                            SegmentRangeEnd = (int)endSegmentId,
                            length = lengthgeo,
                            Direction = newSurveyName?.ToString().Contains("Dec") == true ? "Decreasing" : newSurveyName?.ToString().Contains("Inc") == true ? "Increasing" : "Unknown",
                            OldSurvey = oldsurvey,
                            StartChainage = (double)startChainageResponse.Chainage,
                            EndChainage = (double)endChainageResponse.Chainage,
                            NewStartChainage = (double)newStartChainage
                        };

                        segmentationTableDatas.Add(segmentationTableData);

                    }
                }

            }

            return segmentationTableDatas;

        }
        catch (Exception ex)
        {
            Log.Error($"Error in ImportSelectedSegmentation : {ex.Message}");
            return null;
        }
        finally
        {
            LoaderOverlay.IsVisible = false;
        }
    }

    private (Graphic startGraphic, Graphic endGraphic) GetBestStartEndAutoSegmentationSegments(
    List<Graphic> possStartSegmentsGraphics,
    List<Graphic> possEndSegmentsGraphics,
    double lengthgeo)
    {
        Graphic bestStartGraphic = null;
        Graphic bestEndGraphic = null;
        double closestLengthDifference = double.MaxValue;

        // Check all combinations of start and end graphics
        foreach (var startGraphic in possStartSegmentsGraphics)
        {
            foreach (var endGraphic in possEndSegmentsGraphics)
            {
                // Calculate the length based on the IDs (assuming 5m per segment)
                int startId = GetGraphicId(startGraphic);
                int endId = GetGraphicId(endGraphic);
                double calculatedLength = Math.Abs(endId - startId) * 5; // 5m per segment

                // Calculate the difference from the target length
                double lengthDifference = Math.Abs(calculatedLength - lengthgeo);

                // Update the best match if this combination is closer to lengthgeo
                if (lengthDifference < closestLengthDifference)
                {
                    closestLengthDifference = lengthDifference;
                    bestStartGraphic = startGraphic;
                    bestEndGraphic = endGraphic;
                }
            }
        }

        return (bestStartGraphic, bestEndGraphic);
    }

    // Helper function to get the ID of a graphic
    private int GetGraphicId(Graphic graphic)
    {
        // Assuming the ID is stored as an attribute in the graphic
        return (int)graphic.Attributes["SectionId"];
    }

    public async void CreateSegmentatedSurveysAndSaveSegments(List<SegmentationTableData> surveysToSave)
    {
        CloseSegmentationWindow();
        LoaderOverlay.IsVisible = true;

        try
        {
            if (surveysToSave.Count > 0)
            {

                await appEngine.SurveySegmentationService.CreateSurveysAndInsertSegments(surveysToSave);

            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in CreateSegmentatedSurveysAndSaveSegments : {ex.Message}");
        }
        finally
        {
            appState.InitializeSurveyAndLayers();
            LoaderOverlay.IsVisible = false;

        }
    }

    private List<Graphic> GetIntersectingGraphicsIndividual(List<Graphic> graphics, MapPoint mapPoint)
    {
        var listSegments = new List<Graphic>();
        foreach (var g1 in graphics)
        {
            Envelope polyEnv = g1.Geometry.Extent;
            Envelope envelope = mapPoint.Extent;
            if (g1.Geometry != null && mapPoint != null)
            {
                if (!GeometryEngine.Intersects(polyEnv, envelope))
                    continue;
                if (GeometryEngine.Intersects(g1.Geometry, mapPoint))
                {
                    listSegments.Add(g1);
                }
            }

        }
        return listSegments;
    }
    public static List<Graphic> GetIntersectingGraphicsIndividual(List<Graphic> overlay1Graphics, Graphic graphic)
    {
        var listSegments = new List<Graphic>();

        foreach (var g1 in overlay1Graphics)
        {
            Envelope polyEnv = g1.Geometry.Extent;
            Envelope lineEnv = graphic.Geometry.Extent;
            if (g1.Geometry != null && graphic.Geometry != null)
            {
                if (!GeometryEngine.Intersects(polyEnv, lineEnv))
                    continue;

                if (GeometryEngine.Intersects(g1.Geometry, graphic.Geometry))
                {
                    listSegments.Add(g1);
                }
            }

        }

        return listSegments;
    }

    private Microsoft.Maui.Controls.Window _segmentationWindow;

    private void DisplaySegmentationTable()
    {
        var segmentationPage = new SegmentationPage(appEngine, appState);

        _segmentationWindow = new Microsoft.Maui.Controls.Window(segmentationPage)
        {
            Title = "Import Survey Segmentation List",
            Width = 1500,
            Height = 800

        };

        App.Current?.OpenWindow(_segmentationWindow);
    }
    private void CloseSegmentationWindow()
    {
        if (_segmentationWindow != null)
        {
            App.Current?.CloseWindow(_segmentationWindow);
            _segmentationWindow = null; // Clear the reference
        }
    }

    private void CancelImportedSegmentation()
    {
        try
        {
            //remove graphics from the map
            var overlay = MyMapView.GraphicsOverlays.FirstOrDefault(o => o.Id == "surveySetOverlay");
            if (overlay != null)
            {
                overlay.Graphics.Clear();
                MyMapView.GraphicsOverlays.Remove(overlay);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in CancelImportedSegmentation : {ex.Message}");
        }
    }

    private void DrawMeasurementDuringMove(MapPoint startPoint, MapPoint currentPoint)
    {
        if (newOverlay == null)
        {
            newOverlay = new GraphicsOverlay { Id = "MeasurementOverlay" };
            MyMapView.GraphicsOverlays.Add(newOverlay);
        }

        newOverlay.Graphics.Clear();

        var line = new Polyline(new[] { startPoint, currentPoint }, SpatialReferences.Wgs84);
        var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.White, 2);
        var lineGraphic = new Graphic(line, lineSymbol);

        var distance = GeometryEngine.DistanceGeodetic(
            startPoint,
            currentPoint,
            LinearUnits.Meters,
            AngularUnits.Degrees,
            GeodeticCurveType.Geodesic
        ).Distance;

        var textSymbol = new TextSymbol(
            $" {distance:F2} m",
            System.Drawing.Color.LightYellow,
            14,
            Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Left,
            Esri.ArcGISRuntime.Symbology.VerticalAlignment.Middle
        );

        // Calculate the midpoint of the line
        var midPoint = line.Extent.GetCenter();

        double offsetFactor = 0.1; // adjust according to the map’s zoom level or scale
        double offsetX = (currentPoint.X - startPoint.X) * offsetFactor;
        double offsetY = (currentPoint.Y - startPoint.Y) * offsetFactor;

        var textPosition = new MapPoint(
            midPoint.X + offsetX,
            midPoint.Y + offsetY,
            SpatialReferences.Wgs84
        );

        // Create and add the graphics
        var textGraphic = new Graphic(textPosition, textSymbol);

        newOverlay.Graphics.Add(lineGraphic);
        newOverlay.Graphics.Add(textGraphic);
    }


    private bool _isCollapsed = true;
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set { _isCollapsed = value; OnPropertyChanged(); }
    }

    public bool IsExpanded => !IsCollapsed;

    public double PanelWidth => IsCollapsed ? 60 : 180;

    private void ToggleSettingsPanel(object sender, EventArgs e)
    {
        IsCollapsed = !IsCollapsed;
    }
}