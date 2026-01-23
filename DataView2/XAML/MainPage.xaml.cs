using DataView2.Engines;
using DataView2.States;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.UI;
using DataView2.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;
using DataView2.XAML;
using CommunityToolkit.Maui.Views;
using DataView2.Pages;
using DataView2.Shared;
using Microsoft.AspNetCore.Components;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Pages.SurveyList;
using Esri.ArcGISRuntime.Symbology;
using Serilog;
using DataView2.Pages.PCI;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Pages.Map;
using DataView2.Core.Models.Other;
using static DataView2.Core.Helper.TableNameHelper;

namespace DataView2
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public ApplicationState appState;
        public ApplicationEngine appEngine;
        public string activePage;
        private readonly ILogger<MainPage> _logger;
        private readonly IPopupService popupService;
        private double navbarWidth = 325;

        //Image
        private Window _imageWindow;
        private ImageComponent _imageComponent;
        private string imagePath;
        private bool isPopupMode = false;
        private bool isDockingClose = false;

        public MainPage(ILogger<MainPage> logger, IPopupService popupService)
        {
            InitializeComponent();
            this.BindingContext = this;
            this.popupService = popupService;

            appEngine = MauiProgram.AppEngine;
            appState = MauiProgram.AppState;
            _logger = logger;

            appState.ActivePageChanged += ApplicationState_StateChangedAsync;
            appState.OnSegmentClicked += DisplayImages;
            appState.SurveySetPageChanged += OpenPageMode;
            appState.SurveySetClosed += CloseSurveySet;
            appState.OnSegmentSummaryClosed += CloseSegmentSummaryMenu;
            appState.OnBottomMenuChanged += SetBottomMenuVisibility;
            appState.OnCloseDrawingToolInvoked += CloseDrawingTool;
            appState.OnGraphicOutSideSegmentClicked += OpenGraphicOnlyTable;
            appState.SetOfflineMapPathHandled += () => { ShowSurveyPopup("map");};
            appState.ShowHideSurveyTemplateHandled += ShowHideSurveyTemplate;
            this.BindingContext = this;

            _imageComponent = new ImageComponent();
            RightPopup.Children.Add(_imageComponent);
            _imageComponent.PopupButtonClicked += OnImagePopupButtonClicked;
        }

        private void ShowHideSurveyTemplate(bool isChecked)
        {
            myMap.SurveyTemplateVisibility = isChecked;
        }

        private BlazorWebView _currentWebView;

        private void OpenPageMode(string page, Dictionary<string, object> param = null)
        {
            RootComponent rootComponent = new RootComponent();
            rootComponent.Selector = "#app";
            if(param != null)
            {
                rootComponent.Parameters = param;
            }
            bool openNewWindow = false;

            switch (page)
            {
                case "SurveySet":
                    rootComponent.ComponentType = typeof(SurveyEdit);
                    myMap.IsEditing = true;
                    myMap.IsSurveyTemplate = true;
                    break;
                case "SurveyList":
                    rootComponent.ComponentType = typeof(SurveyList);
                    myMap.IsEditing = false;
                    myMap.IsSurveyTemplate = true;
                    break;
                case "SampleUnits":
                    rootComponent.ComponentType = typeof(SampleUnits);
                    myMap.IsEditing = false;
                    myMap.IsSurveyTemplate = false;
                    break;
                case "PCIRatingMode":
                    rootComponent.ComponentType = typeof(PCIRatingMode);
                    DisplayWebview = false;
                    appState.HandleSurveyTemplate();
                    openNewWindow = true;
                    break;
            }

            // Remove the current WebView if it already exists
            if (blazorMenu.Children.Contains(_currentWebView))
            {
                blazorMenu.Children.Remove(_currentWebView);
            }

            // Create and configure the WebView
            _currentWebView = new BlazorWebView
            {
                HostPage = "wwwroot/index.html",
            };
            _currentWebView.RootComponents.Add(rootComponent);

            if (openNewWindow)
            {
                //new Window for PCI Rating
                var newWindow = new Window
                {
                    Title = "PCI Rating Mode",
                    Width = 700,
                    Height = 800,
                    Page = new ContentPage
                    {
                        Content = _currentWebView
                    }
                };

                WindowManager.OpenWindow(newWindow);
            }
            else
            {
                ShowHideSurveyTemplate(true);
                blazorMenu.Children.Add(_currentWebView);
                LeftColumn.Width = GridLength.Star;
            }

            if (IsImageVisible)
                IsImageVisible = false;
            if (IsTableVisible)
                IsTableVisible = false;
        }

        private void CloseSurveySet()
        {
            if (_currentWebView != null)
            {
                blazorMenu.Children.Remove(_currentWebView); // Remove the WebView from blazorMenu
                _currentWebView = null; // Set the currentWebView to null
            }

            LeftColumn.Width = navbarWidth;
            if (myMap._geometryEditor.IsStarted)
            {
                myMap._geometryEditor.Stop();
            }

            myMap.DrawingToolVisibility = false;
            myMap.IsSurveyTemplate = false;

            //update navbar page to home
            appState.SetMenuPage("/Home");
        }

        private async void DisplayImages(string imagePath, string surveyId, string sectionId)
        {
            if (isAddingDefectMenuVisible) { return; }

            IsTableVisible = true;

            if (imagePath != null && !string.IsNullOrWhiteSpace(imagePath) && this.imagePath != imagePath)
            {
                //changes to manage absolute path
                var rangeImage = imagePath + "_Range.jpg";
                var overlayImage = imagePath + "_Overlay.jpg";

                if (Path.GetExtension(imagePath) == ".jpg")
                {
                    rangeImage = imagePath.Replace(".jpg", "_Range.jpg");
                    overlayImage = imagePath.Replace(".jpg", "_Overlay.jpg");
                }

                if (!File.Exists(rangeImage) || !File.Exists(overlayImage))
                {
                    string folderPath = await appEngine.SurveyService.GetImageFolderPath(surveyId);
                    if (folderPath != null)
                    {
                        rangeImage = Path.Combine(folderPath, rangeImage);
                        overlayImage = Path.Combine(folderPath, overlayImage);
                    }

                    imagePath = Path.Combine(folderPath, imagePath);
                }
            }

            //Segment Grid - Image:
            var rangeImageDG = imagePath + "_Range.jpg";
            string[] pathDataDG = rangeImageDG.Split("_");
            
            string surveyName = string.Join(" ", pathDataDG.Take(pathDataDG.Length - 1));

            List<LCMS_Segment_Grid> segmentGridOutputs = new List<LCMS_Segment_Grid>();

            if (appState.SelectedTables.Contains(MultiLayerName.Longitudinal) || appState.SelectedTables.Contains(MultiLayerName.Transversal) || appState.SelectedTables.Contains(MultiLayerName.Fatigue))
                segmentGridOutputs = await appEngine.SegmentGridService.GetSegmentGridsBySurveyIDSectionId(new Core.Helper.Segment_Grid_Params { SectionId = sectionId, SurveyId = surveyId });

            if (_imageWindow != null)
            {
                // popup is open -> update popup
                var popupImage = (_imageWindow.Page as ContentPage).Content as ImageComponent;
                popupImage?.LoadImage(imagePath, segmentGridOutputs, surveyId);
            }
            else
            {
                //add image on the right side of the map
                _imageComponent.LoadImage(imagePath, segmentGridOutputs, surveyId);
                IsImageVisible = true;
                isPopupMode = false;
            }
            this.imagePath = imagePath;
        }

        private void OnImagePopupButtonClicked()
        {
            if (isPopupMode)
                DockImage();
            else
                PopOutImage();

            isPopupMode = !isPopupMode;
        }

        private void PopOutImage()
        {
            IsImageVisible = false;

            var popupImageComponent = new ImageComponent();
            popupImageComponent.LoadImage(_imageComponent.imagePath, _imageComponent.segmentGridOutputs, _imageComponent._surveyId);
            popupImageComponent.PopupButtonClicked += OnImagePopupButtonClicked;
            // Create popup window
            _imageWindow = new Window
            {
                Page = new ContentPage { Content = popupImageComponent },
                Width = RightPopup.Width,
                Height = RightPopup.Height,
                Title = "LCMS Image Viewer"
            };

            _imageWindow.Destroying += (s, e) =>
            {
                //close both image and segment summary
                if (!isDockingClose)
                {
                    CloseSegmentSummaryMenu();
                }
            };

            App.Current.OpenWindow(_imageWindow);
        }

        private void DockImage()
        {
            IsImageVisible = true;
            if (_imageWindow != null)
            {
                isDockingClose = true; // <-- tell Destroying this is NOT a user close
                var popupImage = (_imageWindow.Page as ContentPage).Content as ImageComponent;
                if (popupImage != null)
                {
                    _imageComponent.LoadImage(popupImage.imagePath, popupImage.segmentGridOutputs, popupImage._surveyId);
                }
                App.Current.CloseWindow(_imageWindow);
                _imageWindow = null;

                isDockingClose = false; // reset
            }
        }

        private void OpenGraphicOnlyTable()
        {
            IsTableVisible = true;
            if (IsImageVisible)
                IsImageVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isImageVisible;
        public bool IsImageVisible
        {
            get { return isImageVisible; }
            set
            {
                isImageVisible = value;
                OnPropertyChanged(nameof(IsImageVisible));

                if (IsImageVisible == true)
                {
                    OverlayColumn.Width = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    OverlayColumn.Width = 0;
                }
            }
        }
        private bool isTableVisible;
        public bool IsTableVisible
        {
            get { return isTableVisible; }
            set
            {
                if (isTableVisible != value)
                {
                    isTableVisible = value;
                    OnPropertyChanged(nameof(IsTableVisible));

                    if (IsTableVisible == true)
                    {
                        OverlayRow.Height = new GridLength(0.5, GridUnitType.Star);
                    }
                    else
                    {
                        OverlayRow.Height = 0;
                    }
                }
            }
        }

        private bool isAddingDefectMenuVisible;
        public bool IsAddingDefectMenuVisible
        {
            get { return isAddingDefectMenuVisible; }
            set
            {
                if (isAddingDefectMenuVisible != value)
                {
                    isAddingDefectMenuVisible = value;
                    appState.IsPopupOpen = value;

                    if (isAddingDefectMenuVisible)
                    {
                        IsImageVisible = false;
                        IsTableVisible = false;
                        OnPropertyChanged(nameof(IsImageVisible));
                        OnPropertyChanged(nameof(IsTableVisible));

                        OverlayRow.Height = new GridLength(0.7, GridUnitType.Star);
                    }
                    else
                    {
                        OverlayRow.Height = 0;
                    }
                    OnPropertyChanged();
                }
            }
        }

        private bool _displayWebview;
        public bool DisplayWebview
        {
            get { return _displayWebview; }
            set
            {
                if (_displayWebview != value)
                {
                    _displayWebview = value;
                }
                myMap.OnDisplayWebChanged(_displayWebview);
                OnPropertyChanged(nameof(DisplayWebview));
            }
        }

        private void CloseSegmentSummaryMenu()
        {
            try
            {
                IsImageVisible = false;
                IsTableVisible = false;

                //if image is popup, close popup too
                if (_imageWindow != null)
                {
                    App.Current.CloseWindow(_imageWindow);
                    _imageWindow = null;
                }

                //Remove highlight for selected segment
                var segmentSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(50, 0, 0, 0), new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1.0));
                var hiddenSegmentSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Transparent, 1.0));
                if (myMap.previousGraphic != null)
                {
                    if (appState.segmentLayer)
                    {
                        myMap.previousGraphic.Symbol = segmentSymbol;
                    }
                    else
                    {
                        myMap.previousGraphic.Symbol = hiddenSegmentSymbol;
                    }
                    myMap.previousGraphic = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CloseQCMenu : {ex.Message}");
            }
        }

        private async void ApplicationState_StateChangedAsync(string activepage)
        {
            Uri uri = new Uri(activepage, UriKind.RelativeOrAbsolute);
            try
            {
                if (uri.AbsolutePath.EndsWith("/") || uri.AbsolutePath.EndsWith("/Home"))
                {
                    DisplayWebview = false;

                    //if map path is alrady set for offline map
                    if (!string.IsNullOrEmpty(appState.CurrentPath) && Path.GetExtension(appState.CurrentPath) == ".tpkx") { return; }
                    else myMap.SurveyTemplateVisibility = false;

                    //Reprocess Segments selected
                    if (appState.isReprocessingSegments)
                        appState.NotifySegmentsSelected();
                }
                else if (uri.AbsolutePath.EndsWith("/NewSurvey"))
                {
                    ShowSurveyPopup("new");
                }
                else if (uri.AbsolutePath.EndsWith("/OpenSurvey"))
                {
                    ShowSurveyPopup("open");
                }
                else if (uri.AbsolutePath.EndsWith("/ImportSurvey"))
                {
                    ShowSurveyPopup("import");
                }
                else if (uri.AbsolutePath.EndsWith("/SampleUnits"))
                {
                    OpenSampleUnits(SampleUnitSetType.PCI);
                }
                else if (uri.AbsolutePath.EndsWith("/SummarySampleUnits"))
                {
                    OpenSampleUnits(SampleUnitSetType.Summary);
                }
                else
                {
                    DisplayWebview = true;
                    CloseAllPanels();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error redirecting Survey List (Boundaries): {ex.Message}");
            }
        }
        private void ShowSurveyPopup(string mode)
        {
            DisplayWebview = false;
            //ShowHideSurveyTemplate(true);
            appState.HandleSurveyTemplate();
            var popup = new SurveySetPopup(mode);
            Application.Current.MainPage.ShowPopup(popup);
        }

        private void OpenSampleUnits(SampleUnitSetType type)
        {
            DisplayWebview = false;
            myMap.SurveyTemplateVisibility = true;
            var param = new Dictionary<string, object> { { "Type", type } };
            OpenPageMode("SampleUnits", param);
            appState.HandleSurveyTemplate();
        }

        private void SetBottomMenuVisibility(string tableName, string surveyId, string segmentId, bool status)
        {
            if (status)
            {
                NewDefect.IsVisible = true;
            }
            IsAddingDefectMenuVisible = status;
        }

        private void CloseDrawingTool()
        {
            NewDefect.IsVisible = false;
            //NewTable.IsVisible = false;
            IsAddingDefectMenuVisible = false;
            myMap.DrawingToolVisibility = false;
            myMap.ClearMeasurementButtons(); // Disables measurement buttons activated from custom table
            myMap.RemoveTempGraphic();
        }

        private void CloseAllPanels()
        {
            IsAddingDefectMenuVisible = false;
            CloseSegmentSummaryMenu();
        }
    }
}