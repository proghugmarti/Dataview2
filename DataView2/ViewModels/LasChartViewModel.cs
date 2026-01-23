using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.Other;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;using LiveChartsCore.Kernel;
using DataView2.Pages;
using LiveChartsCore.Kernel.Sketches;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DataView2.Engines;
using System.Windows.Input;
using MudBlazor;
using Esri.ArcGISRuntime.Geometry;
using DataView2.States;
using Color = Microsoft.Maui.Graphics.Color;
using Grpc.Core;
using Serilog;
using DataView2.Core.Helper;


namespace DataView2.ViewModels
{
    public partial class LasChartViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationEngine _appEngine;
        private readonly ApplicationState _appState;
        private List<LASPoint> _lasPoints = new List<LASPoint>();
        private List<ChartLasPoint> _chartPoints = new List<ChartLasPoint>();

        public ObservableCollection<ISeries> Series { get; set; }
        public ObservableCollection<RuttingResult> SavedRuttingResults { get; set; } = new();


        public event Action<ChartLasPoint> LasPointClicked;

        private ObservableCollection<ChartLasPoint> _contactPoints = new ObservableCollection<ChartLasPoint>();
        public ObservableCollection<ChartLasPoint> ContactPoints
        {
            get => _contactPoints;
            set
            {
                _contactPoints = value;
                OnPropertyChanged(nameof(ContactPoints));
            }
        }

        private ObservableCollection<ChartLasPoint>  _savedChartPoints = new ObservableCollection<ChartLasPoint>();
        public ObservableCollection <ChartLasPoint> SavedChartPoints
        {
            get => _savedChartPoints;
            set
            {
                _savedChartPoints = value;
                OnPropertyChanged(nameof(SavedChartPoints));
            }
        }

        private RuttingResult _lastResult;
        public RuttingResult LastResult
        {
            get => _lastResult;
            set
            {
                _lastResult = value;
                OnPropertyChanged(nameof(LastResult));
            }
        }

        private double _ruttingValue;
        public double RuttingValue
        {
            get => _ruttingValue;
            set
            {
                _ruttingValue = value;
                OnPropertyChanged(nameof(RuttingValue));
            }
        }

        private double _rutDepth;
        public double RutDepth
        {
            get => _rutDepth;
            set
            {
                _rutDepth = value;
                OnPropertyChanged(nameof(RutDepth));
            }
        }

        private string _straightEdgeLengthInput = "6";
        public string StraightEdgeLengthInput
        {
            get => _straightEdgeLengthInput;
            set
            {
                _straightEdgeLengthInput = value;
                OnPropertyChanged(nameof(StraightEdgeLengthInput));
            }
        }

        private double _straightEdgeLength = 6;
        public double StraightEdgeLength
        {
            get => _straightEdgeLength;
            set
            {
                _straightEdgeLength = value;
                OnPropertyChanged(nameof(StraightEdgeLength));
            }
        }

        private double _previousStraightEdgeLength;
        public double PreviousStraightEdgeLength
        {
            get => _previousStraightEdgeLength;
            set
            {
                _previousStraightEdgeLength = value;
                OnPropertyChanged(nameof(PreviousStraightEdgeLength));
            }
        }

        private int _rutPointId;
        public int RutPointId
        {
            get => _rutPointId;
            set
            {
                _rutPointId = value;
                OnPropertyChanged(nameof(RutPointId));
            }
        }
        private List<ChartLasPoint> _selectedHighPoints = new List<ChartLasPoint>();

        private bool _isDepthPointSelected = true;        

        public bool IsDepthPointSelected
        {
            get => _isDepthPointSelected;
            set
            {
                _isDepthPointSelected = value;
                OnPropertyChanged(nameof(IsDepthPointSelected));
                OnPropertyChanged(nameof(IsContactPointSelected));
                OnPropertyChanged(nameof(DepthPointButtonColor));
                OnPropertyChanged(nameof(ContactPointButtonColor));
            }
        }

        public bool IsContactPointSelected => !_isDepthPointSelected;

        public Color DepthPointButtonColor => IsDepthPointSelected ? Color.FromArgb("#007AFF") : Color.FromArgb("#D3D3D3");
        public Color ContactPointButtonColor => IsContactPointSelected ? Color.FromArgb("#007AFF") : Color.FromArgb("#D3D3D3");

        public Command ToggleHighPointSelectionCommand => new Command(() =>
        {
            IsDepthPointSelected = !IsDepthPointSelected;
        });


        public LabelVisual Title { get; set; } = new LabelVisual
        {
            Text = "LAS Points Chart",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15)
        };
        public ICartesianAxis[] XAxes { get; set; } = [
            new Axis
            {
                Name = "Horizontal Distance (m)",

            }
        ];
        public ICartesianAxis[] YAxes { get; set; } = [
            new Axis
            {
                Name = "Altitude (mm)",
            }
        ];


        public ICommand CalculateRuttingCommand { get; }
        public IAsyncRelayCommand SaveRuttingCommand { get; }
        public ICommand DeleteRuttingResultCommand { get; }
        public ICommand SaveRuttingResultsCommand { get; private set; }



        public LasChartViewModel(List<LASPoint> lasPoints, ApplicationEngine appEngine, ApplicationState appState)
        {
            _appEngine = appEngine;
            _appState = appState;
            Series = new ObservableCollection<ISeries>();
            ContactPoints = new ObservableCollection<ChartLasPoint>();

            _lasPoints = lasPoints;
            _chartPoints = PrecomputeChartPoints(lasPoints);
            InitializeSeries(_chartPoints);


            //Commands
            CalculateRuttingCommand = new Command(async () => await CalculateRutting(RutPointId, StraightEdgeLength));
            SaveRuttingCommand = new AsyncRelayCommand(SaveRuttingResultAsync);
            DeleteRuttingResultCommand = new Command<RuttingResult>(DeleteRuttingResult);
            SaveRuttingResultsCommand = new Command(async () => await SaveRuttingResultsToTableAsync());


        }

        private List<ChartLasPoint> PrecomputeChartPoints(List<LASPoint> lasPoints)
        {
            var newChartPoints = new List<ChartLasPoint>();
            double minZ = lasPoints.Min(p => p.Z);

            // save the first xy of the list 
            LASPoint firstPoint = lasPoints[0];
          
            foreach (var lasPoint in lasPoints)
            {
                // Check if the point already exists in _chartPoints based on the Id
                var existingPoint = _chartPoints.FirstOrDefault(p => p.Id == lasPoint.Id);
                
                if (existingPoint != null)
                {
                    // Create a new ChartLasPoint but reuse the X from the existing point
                    newChartPoints.Add(new ChartLasPoint
                    {
                        X = existingPoint.X, // Reuse the X value
                        Z = existingPoint.Z, // Normalize Z value
                        Id = existingPoint.Id
                    });
                }
                else
                {
                    // Create a new point with a calculated X value
                    var newPoint = new ChartLasPoint
                    {
                        X = CalculateDistance(firstPoint, lasPoint), // Calculate X for new points
                        Z = (lasPoint.Z - minZ) * 1000, // Normalize Z value
                        Id = lasPoint.Id
                    };

                    // Add the new point to both _chartPoints and newChartPoints
                    _chartPoints.Add(newPoint);
                    newChartPoints.Add(newPoint);
                }
            }

            return newChartPoints; // Return only the new points
        }


        //copy the calculate distance here
        private double CalculateDistance(LASPoint firstPoint, LASPoint lasPoint)
        {
            var firstMapPoint = new MapPoint(firstPoint.X, firstPoint.Y, SpatialReferences.Wgs84);
            var lasMapPoint = new MapPoint(lasPoint.X, lasPoint.Y, SpatialReferences.Wgs84);

            var result = GeometryEngine.DistanceGeodetic(firstMapPoint, lasMapPoint, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

            return result.Distance;
        }
        

        private void InitializeSeries(List<ChartLasPoint> lasPoints)
        {

            // Define the static main series
            var mainSeries = new LineSeries<ChartLasPoint>
            {
                Values = lasPoints,
                Mapping = (point, index) =>
                {
                    double fixedXValue = point.X ;
                    double normalizedZ = (point.Z );
                    return new Coordinate(fixedXValue, normalizedZ);
                },
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                GeometrySize = 5,
                GeometryStroke = new SolidColorPaint(SKColors.Blue),
                GeometryFill = new SolidColorPaint(SKColors.White),
                XToolTipLabelFormatter = point => $"ID: {point.Model.Id}",
                YToolTipLabelFormatter = point => $"Z: {point.Model.Z:F2}{Environment.NewLine}X: {point.Model.X:F2}",

            };

            // Handle point clicks
            mainSeries.ChartPointPointerDown += OnPointerDown;

            // Add the main series to the Series collection
            Series.Add(mainSeries);
        }

        private async void OnPointerDown(IChartView chart, ChartPoint<ChartLasPoint, CircleGeometry, LabelGeometry> point)
        {
            if (point?.Visual is null) return;

            Console.WriteLine($"Clicked on {point.Model?.Id}");

            if (IsContactPointSelected)
            {
                SelectHighPoint(point.Model); // Selecting contact points
            }
            else
            {
                RutPointId = point.Model.Id; // Selecting depth point
                await CalculateRutting(point.Model.Id, _straightEdgeLength);
            }
        }
        private void SelectHighPoint(ChartLasPoint selectedPoint)
        {
            if (_selectedHighPoints.Contains(selectedPoint)) return; // Avoid duplicates

            _selectedHighPoints.Add(selectedPoint);
            Console.WriteLine($"Selected high point: {selectedPoint.Id}");

            // If two points are selected, calculate max rutting
            if (_selectedHighPoints.Count == 2)
            {
                var highPointsAsLas = _selectedHighPoints.Select(p => ConvertToLASPoint(p)).ToList();
                _ = CalculateMaxRuttingFromPoints(highPointsAsLas);
                _selectedHighPoints = new List<ChartLasPoint>();
            }
        }
        private LASPoint ConvertToLASPoint(ChartLasPoint chartPoint)
        {
            return _lasPoints.FirstOrDefault(p => p.Id == chartPoint.Id);
        }

        /// <summary>
        /// Calculates the rutting depth for a given rut point and straight edge length.
        /// </summary>
        /// <param name="rutPointId">The ID of the rut point.</param>
        /// <param name="straightEdgeLength">The length of the straight edge in meters.</param>
        public async Task CalculateRutting(int rutPointId, double straightEdgeLength)
        {
            try
            {
                var rutPoint = _lasPoints.FirstOrDefault(p => p.Id == rutPointId);
                if (rutPoint == null)
                {
                    // Handle error (e.g., show a message)
                    return;
                }

                var rutRequest = new CalculateRuttingsRequest
                {
                    RutPoint = rutPoint,
                    Points = _lasPoints,
                    strghtEdgLength = straightEdgeLength,
                    distanceBetweenPoints = 0.25
                };

                var result = await _appEngine.LASfileService.CalculateRutting(rutRequest);
                LastResult = result; // Store the last result
                RuttingValue = result.RutDepth;
                //SavedRuttingResults.Add(result); // Add the result to the collection

                // Update the chart with contact points
                UpdateChartWithContactPoints(PrecomputeChartPoints(result.ContactPoints));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return;
            }
        }


        //LAST RESULT MOVING LINE 
        public void UpdateChartWithContactPoints(List<ChartLasPoint> contactPoints)
{
    try
    {
        var contactChartPoints = new List<ChartLasPoint>();

        foreach (var contactPoint in contactPoints)
        {
                    // Find the corresponding ChartPoint by matching Id
            var chartPoint = _chartPoints.FirstOrDefault(p => p.Id == contactPoint.Id);
            if (chartPoint == null) continue;

                    // Use precomputed chart point for X and Z
            contactChartPoints.Add(new ChartLasPoint
            {
                        X = chartPoint.X,  // Use the precomputed normalized X
                        Z = contactPoint.Z, // Z from the contact point
                        Id = chartPoint.Id  // Keep the Id
            });
        }

                // Update the contact points series
        UpdateContactPoints(contactChartPoints);
    }
    catch (Exception ex)
    {
        Log.Error($"Error in UpdateChartWithContactPoints: {ex}");
    }
           
}

        private void UpdateContactPoints(List<ChartLasPoint> contactPoints)
        {
            try
            {
                // Check if the series for contact points exists
                var contactPointSeries = Series.FirstOrDefault(s => s is LineSeries<ChartLasPoint> && ((LineSeries<ChartLasPoint>)s).Name == "Contact Points");

                // Initialize the contact points series if it doesn't exist
                if (contactPointSeries == null)
                {
                    contactPointSeries = new LineSeries<ChartLasPoint>
                    {
                        Name = "Contact Points",
                        Values = new ObservableCollection<ChartLasPoint>(),
                        Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                        GeometrySize = 10,
                        GeometryStroke = new SolidColorPaint(SKColors.Black),
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        Fill = null,
                        Mapping = (point, chartPoint) =>
                        {
                            double chartPointY = point.Z;
                            double chartPointx = point.X;
                            return new Coordinate(chartPointx, chartPointY);
                        }
                    };
                    // Ensure UI update happens on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Series.Add(contactPointSeries);
                    });
                }

                var lineSeries = (LineSeries<ChartLasPoint>)contactPointSeries;

                    // Ensure modification happens on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                {
                    lineSeries.Values = new ObservableCollection<ChartLasPoint>(contactPoints);
                    OnPropertyChanged(nameof(Series));
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return;
            }
            
        }

        public async Task CalculateMaxRuttingFromPoints(List<LASPoint> highPoints)
        {
            try { 
                if (highPoints.Count != 2) return;

                var request = new CalculateMaxRuttingRequest
                {
                    Point1 = highPoints[0],
                    Point2 = highPoints[1],
                    Points = _lasPoints
                };

                var result = await Task.Run(() => _appEngine.LASfileService.CalculateMaxRutting(request));
                Console.WriteLine($"Max Rutting: {result.RutDepth} mm");

                // Update UI with max rutting result
                RuttingValue = result.RutDepth;

                UpdateChartWithContactPoints(PrecomputeChartPoints(result.ContactPoints));
                LastResult = result;
            }

            catch (Exception ex)
            {
                Log.Error($"Error in CalculateMaxRuttingFromPoints: {ex}");
            }
        }



        //SAVED RESULTS SERIES 
        private async Task SaveRuttingResultAsync()
        {
            if (RuttingValue != 0)
            {
                if (SavedRuttingResults.Any(r => r.Id == LastResult.Id))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Duplicate", "This Rutting Result already exists.", "OK");
                    });
                    return; 
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SavedRuttingResults.Add(LastResult);
                });
            }
            UpdateChartWithSavedResults();
        }

        public void UpdateChartWithSavedResults()
        {
            // Dictionary to track existing series by their unique ID
            var existingSeries = Series.ToDictionary(s => s.Name, s => s);

            foreach (var result in SavedRuttingResults)
            {
                // Generate unique ID from ContactPoints
                string resultId = result.Id; // This is already defined in RuttingResult

                // Check if the series already exists
                if (existingSeries.ContainsKey(resultId))
                    continue; // Skip if it already exists

                var savedChartPoints = result.ContactPoints
                    .Select(contactPoint => _chartPoints.FirstOrDefault(p => p.Id == contactPoint.Id))
                    .Where(chartPoint => chartPoint != null)
                    .Select(chartPoint => new ChartLasPoint
                    {
                        X = chartPoint.X,
                        Z = chartPoint.Z,
                        Id = chartPoint.Id
                    })
                    .ToList();

                if (savedChartPoints.Count == 0)
                    continue; // No valid points, skip

                // Create new series
                var newSeries = new LineSeries<ChartLasPoint>
                {
                    Name = resultId, // Use result ID as the series name
                    Values = new ObservableCollection<ChartLasPoint>(savedChartPoints),
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                    GeometrySize = 6,
                    GeometryStroke = new SolidColorPaint(SKColors.DarkOrange),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    Fill = null,
                    Mapping = (point, chartPoint) =>
                    {
                        return new Coordinate(point.X, point.Z);
                    }
                };

                // Add new series to the chart
                Series.Add(newSeries);
            }

            

            OnPropertyChanged(nameof(Series));
        }
        private void DeleteRuttingResult(RuttingResult result)
        {
            if (result != null)
            {
                SavedRuttingResults.Remove(result);
                // Find and remove the corresponding chart series
                var seriesToRemove = Series.FirstOrDefault(s => s.Name == result.Id);
                if (seriesToRemove != null)
                {
                    Series.Remove(seriesToRemove);
                }

                // Notify UI about the update
                OnPropertyChanged(nameof(Series));
            }
        }
        public async Task SaveRuttingResultsToTableAsync()
        {
            try
            {
                // Validate if there are any saved rutting results
                if (SavedRuttingResults == null || SavedRuttingResults.Count == 0)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "No rutting results to save.", "OK");
                    return;
                }

                // Iterate through each rutting result and save via the service
                foreach (var result in SavedRuttingResults)
                {
                    try
                    {
                        await _appEngine.LASfileService.SaveRuttingResultsToTableAsync(result);
                        _appState.RefreshTableNames();
                    }
                    catch (RpcException ex)
                    {
                        await App.Current.MainPage.DisplayAlert("Error", $"Failed to insert result {result.Id}: {ex.Status.Detail}", "OK");
                    }
                   
                }

                await App.Current.MainPage.DisplayAlert("Success", "Rutting results saved successfully.", "OK");

                // Update LayerName
                _appState.RefreshTableNames();

                // Disable las button after saving to table
                _appState.DisableLasButton();

                // Automaticlly display rut
                _appState.ToggleLayers("LasRutting", true);

            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to save rutting results: {ex.Message}", "OK");
            }
        }


        public async Task ExportToCsv()
        {
            try
            {
                string exportPath = Path.Combine(AppPaths.DocumentsFolder, "User Export Files");
                // Define the folder path and ensure it exists
                string folderPath = Path.Combine(exportPath, "Las Files Rut Export");
                Directory.CreateDirectory(folderPath); // Creates the folder if it doesn't already exist

                // Base filename with the current date
                string baseFileName = $"LASPointsData_{DateTime.Now:yyyy-MM-dd}";
                string fileExtension = ".csv";
                string fileName = baseFileName + fileExtension;
                string filePath = Path.Combine(folderPath, fileName);

                // Check if file exists, and if so, append a counter
                int counter = 1;
                while (File.Exists(filePath))
                {
                    // If file exists, create a new filename with a counter
                    fileName = $"{baseFileName} ({counter}){fileExtension}";
                    filePath = Path.Combine(folderPath, fileName);
                    counter++;
                }

                // Generate CSV content
                var csvContent = GenerateCsvContent();

                // Write CSV content to file
                File.WriteAllText(filePath, csvContent);

                // Provide feedback to the user
                await Application.Current.MainPage.DisplayAlert("Export Complete", $"Data has been exported to {filePath}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"An error occurred while exporting: {ex.Message}", "OK");
            }
        }

        private string GenerateCsvContent()
        {
            var csv = new StringBuilder();

            // Add rutting values at the top of the CSV
            csv.AppendLine("Rutting Value, Contact Point 1 X, Contact Point 1 Z, Contact Point 2 X, Contact Point 2 Z");
            // Loop through each saved rutting result
            foreach (var result in SavedRuttingResults)
            {
                if (result.ContactPoints.Count < 2)
                    continue; // Skip results that don't have exactly 2 contact points

                var contactPoint1 = _chartPoints.FirstOrDefault(cp => cp.Id == result.ContactPoints[0].Id);
                var contactPoint2 = _chartPoints.FirstOrDefault(cp => cp.Id == result.ContactPoints[1].Id);

                if (contactPoint1 != null && contactPoint2 != null)
                {
                    csv.AppendLine($"{result.RutDepth:F4}, {contactPoint1.X}, {contactPoint1.Z}, {contactPoint2.X}, {contactPoint2.Z}");
                }
            }
            csv.AppendLine();

            // Add header row
            csv.AppendLine("Latitude, Longitude, Altitude,, ID, Distance (m), Altitude (mm)");


            // Add each data point
            for (int i = 0; i < _lasPoints.Count; i++)
            {
                var lasPoint = _lasPoints[i];
                var chartPoint = _chartPoints.FirstOrDefault(cp => cp.Id == lasPoint.Id);

                if (chartPoint != null)
                {
                    csv.AppendLine($"{lasPoint.X},{lasPoint.Y},{lasPoint.Z},, {chartPoint.Id},{chartPoint.X},{chartPoint.Z}");
                }
                else
                {
                    // If there is no matching ChartLasPoint, leave the right side empty
                    csv.AppendLine($"{lasPoint.X},{lasPoint.Y},{lasPoint.Z},, , , ");
                }
            }

            return csv.ToString();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ClosedWindow()
        {
            _appState.DisableLasButton();
        }
    }
}
