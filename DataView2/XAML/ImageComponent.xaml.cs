using CommunityToolkit.Maui.Storage;
using DataView2.Core.Helper;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Engines;
using DataView2.States;
using DataView2.ViewModels;
using MudBlazor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;

namespace DataView2;

public partial class ImageComponent : ContentView, INotifyPropertyChanged
{
    private string _xmlPath = "";
    public string _surveyId = "";
    public string imagePath;
    private string rangePath;
    private string intensityPath;
    public List<LCMS_Segment_Grid> segmentGridOutputs;
    private PointF start;
    private PointF current;
    private ImageViewModel viewModel;
    public ApplicationEngine appEngine;
    public ApplicationState appState;
    public event Action PopupButtonClicked;

    public ImageComponent()
    {
        InitializeComponent();
        appEngine = MauiProgram.AppEngine;
        appState = MauiProgram.AppState;
        viewModel = new ImageViewModel();

        appState.OnImageZoomInRequested += CalculateDefectPositionAndCropImage;
        appState.OnResetImageClicked += ResetImageSize;
        appState.ColorCodeGraphicSet += SetColorCodeGraphicInImage;
    }

    public void LoadImage(string imagePath, List<LCMS_Segment_Grid> segmentGrids, string surveyId)
    {
        _surveyId = surveyId;
        segmentGridOutputs = segmentGrids;

        UpdateImagesOnChange(imagePath);
        _xmlPath = imagePath.Replace("ImageResult", "XmlResult") + ".xml";
    }

    private void SetColorCodeGraphicInImage(List<ColorCodeInformation> list, string labelProperty)
    {
        //Trigger UpdateImage only when the color coding is Segment Grid
        if(list.FirstOrDefault().TableName == TableNameHelper.LayerNames.SegmentGrid)
        {
            UpdateImage();
        }
    }

    public void UpdateImagesOnChange(string imagePath)
    {
        this.imagePath = imagePath;
        var rangeImage = imagePath + "_Range.jpg";
        var overlayImage = imagePath + "_Overlay.jpg";

        if (File.Exists(rangeImage) && File.Exists(overlayImage))
        {
            rangePath = rangeImage;
            intensityPath = overlayImage;

            var rangeBytes = File.ReadAllBytes(rangeImage);
            viewModel.RangeImagePath = rangeImage;
            ImageRange.Source = ImageSource.FromStream(() => new MemoryStream(rangeBytes));

            var overlayBytes = File.ReadAllBytes(overlayImage);
            viewModel.IntensityImagePath = overlayImage;
            ImageIntensity.Source = ImageSource.FromStream(() => new MemoryStream(overlayBytes));

            HideView(false);
        }
        else
        {
            HideView(true);
        }
    }

    public void HideView(bool hide)
    {
        grdImage.IsVisible = !hide;
        grdOption.IsVisible = hide;

        if (hide)
        {
            topGrid.Height = 0;
            bottomGrid.Height = Microsoft.Maui.GridLength.Star;
        }
        else
        {
            topGrid.Height = Microsoft.Maui.GridLength.Star;
            bottomGrid.Height = 0;
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnStartInteraction(object Sender, TouchEventArgs evt)
    {
        start = evt.Touches.FirstOrDefault();
    }

    private void OnDragInteraction(object sender, TouchEventArgs evt)
    {
        if (sender is GraphicsView graphicsView && !isGridVisible)
        {
            var drawable = (Drawable)Resources["drawable"];

            // Get the canvas size
            PointF canvasSize = new()
            {
                X = (int)graphicsView.Width,
                Y = (int)graphicsView.Height,
            };
            drawable.UpdateCanvasSize((float)graphicsView.Width, (float)graphicsView.Height, 1);

            var current = evt.Touches.FirstOrDefault();

            if (!current.IsEmpty)
            {
                // Ensure that the touch stays within the bounds of the canvas
                current.X = Math.Max(0, Math.Min(current.X, canvasSize.X));
                current.Y = Math.Max(0, Math.Min(current.Y, canvasSize.Y));

                // Calculate the width and height of the square
                float size = Math.Min(current.X - start.X, current.Y - start.Y);
                drawable.UpdateRectangle(start, new PointF(start.X + size, start.Y + size));
                graphicsView.Invalidate();
            }
        }
    }

    private void OnEndInteraction(object sender, TouchEventArgs evt)
    {
        if (sender is GraphicsView graphicsView && !isGridVisible)
        {
            var drawable = (Drawable)Resources["drawable"];

            // Get the canvas size
            PointF canvasSize = new()
            {
                X = (int)graphicsView.Width,
                Y = (int)graphicsView.Height,
            };

            var end = evt.Touches.FirstOrDefault();

            if (!end.IsEmpty)
            {
                // Calculate the size of the square
                float size = Math.Min(end.X - start.X, end.Y - start.Y);

                // Calculate the coordinates of the square
                PointF squareStart = new PointF(start.X, start.Y);
                PointF squareEnd = new PointF(start.X + size, start.Y + size);

                // Ensure that the coordinates stay within the bounds of the canvas
                squareStart.X = Math.Max(0, Math.Min(squareStart.X, canvasSize.X));
                squareStart.Y = Math.Max(0, Math.Min(squareStart.Y, canvasSize.Y));
                squareEnd.X = Math.Max(0, Math.Min(squareEnd.X, canvasSize.X));
                squareEnd.Y = Math.Max(0, Math.Min(squareEnd.Y, canvasSize.Y));

                // Calculate the cropped area
                Int32Rect croppedArea = new Int32Rect
                {
                    Height = (int)(squareEnd.Y - squareStart.Y),
                    Width = (int)(squareEnd.X - squareStart.X),
                    X = (int)(squareStart.X),
                    Y = (int)(squareStart.Y)
                };

                CropImageFromSelection(croppedArea, canvasSize, false);
                if (drawable.Clear())
                {
                    graphicsView.Invalidate();
                }
            }
        }
    }

    public void CropImageFromSelection(Int32Rect rectangle, PointF canvasSize, bool isAutoZoom)
    {
        if (isAutoZoom)
        {
            ResetImageSize();
        }

        BitmapImage range = viewModel.RangeImage;
        BitmapImage intensity = viewModel.IntensityImage;
        Int32Rect rectangleScaled;

        try
        {
            double hScale = (range.PixelWidth / canvasSize.X);
            double vScale = (range.PixelHeight / canvasSize.Y);
            rectangleScaled.Width = (int)(rectangle.Width * hScale);
            rectangleScaled.Height = (int)(rectangle.Height * vScale);
            rectangleScaled.X = (int)(rectangle.X * hScale);
            rectangleScaled.Y = (int)(rectangle.Y * vScale);

            if (rectangleScaled.Height == 0 || rectangleScaled.Width == 0)
                return;

            CroppedBitmap croppedRange = new(range, rectangleScaled);
            CroppedBitmap croppedIntensity = new(intensity, rectangleScaled);
            //Update RangeImage to the Cropped image

            viewModel.RangeImage = BitmapSourceToBitmap(croppedRange);

            byte[] rangeImageBytes = ConvertCroppedBitmapToBytes(croppedRange);
            ImageRange.Source = ImageSource.FromStream(() => new MemoryStream(rangeImageBytes));

            viewModel.IntensityImage = BitmapSourceToBitmap(croppedIntensity);

            byte[] intensityImageBytes = ConvertCroppedBitmapToBytes(croppedIntensity);
            ImageIntensity.Source = ImageSource.FromStream(() => new MemoryStream(intensityImageBytes));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public void ResetImageSize()
    {
        if (File.Exists(rangePath))
        {
            var bytes = File.ReadAllBytes(rangePath);
            ImageRange.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            viewModel.RangeImagePath = rangePath;
        }

        if (File.Exists(intensityPath))
        {
            var bytes = File.ReadAllBytes(intensityPath);
            ImageIntensity.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            viewModel.IntensityImagePath = intensityPath;
        }
    }

    public void CalculateDefectPositionAndCropImage(double x, double y, double[] segmentSize)
    {
        // Get the canvas size from the rangeGraphicsView
        PointF canvasSize = new()
        {
            X = (int)rangeGraphicsView.Width,
            Y = (int)rangeGraphicsView.Height,
        };
        double scaleX = canvasSize.X / segmentSize[0];
        double scaleY = canvasSize.Y / segmentSize[1];

        var defectXImage = x * scaleX;
        var defectYImage = (segmentSize[1] - y) * scaleY;

        // Define the size of the cropped area around the defect
        int cropSize = 150; // Adjust as needed

        // Calculate the coordinates of the top-left corner of the cropped area
        int cropStartX = (int)(defectXImage - cropSize / 2);
        int cropStartY = (int)(defectYImage - cropSize / 2);

        // Adjust cropStartX and cropStartY if they go beyond the boundaries of the original image
        cropStartX = Math.Max(0, cropStartX);
        cropStartY = Math.Max(0, cropStartY);

        // Ensure that cropStartX + cropSize and cropStartY + cropSize do not exceed the original image dimensions
        cropSize = Math.Min(cropSize, (int)(canvasSize.X - cropStartX));
        cropSize = Math.Min(cropSize, (int)(canvasSize.Y - cropStartY));

        // Define the cropped area rectangle
        Int32Rect croppedArea = new Int32Rect
        {
            X = cropStartX,
            Y = cropStartY,
            Width = cropSize,
            Height = cropSize
        };

        // Crop the area around the defect from the original image
        CropImageFromSelection(croppedArea, canvasSize, true);
    }


    private byte[] ConvertCroppedBitmapToBytes(BitmapSource croppedBitmap)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap.Clone()));
                encoder.Save(memoryStream);

                return memoryStream.GetBuffer();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    private BitmapImage BitmapSourceToBitmap(BitmapSource source)
    {
        try
        {
            PngBitmapEncoder encoder = new();
            MemoryStream memoryStream = new();
            BitmapImage image = new();

            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(memoryStream);

            image.BeginInit();
            image.StreamSource = new MemoryStream(memoryStream.ToArray());
            image.EndInit();
            memoryStream.Close();
            return image;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    private void OnRightClicked(object sender, TappedEventArgs e)
    {
        var drawable = (Drawable)Resources["drawable"];

        if (sender is GraphicsView graphicsView)
        {
            if (graphicsView == rangeGraphicsView || graphicsView == intensityGraphicsView)
            {
                ImageRange.Source = ImageSource.FromFile(rangePath);
                viewModel.RangeImagePath = rangePath;

                ImageIntensity.Source = ImageSource.FromFile(intensityPath);
                viewModel.IntensityImagePath = intensityPath;
            }

            if (drawable.Clear())
            {
                graphicsView.Invalidate();
            }
        }
    }

    //Segment Grid - Image
    private bool isGridVisible = false;
    private crackClassificationImage classificationImage;
    private void OnDoubleClickImage(object sender, EventArgs e)
    {
        UpdateImage();
    }

    private void UpdateImage()
    {
        try
        {
            if (isGridVisible)
            {
                intensityGraphicsView.Drawable = (IDrawable)Resources["drawable"];
                intensityGraphicsView.Invalidate();
                isGridVisible = false;
            }
            else
            {
                ResetImageSize();
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(_xmlPath);

                classificationImage = new crackClassificationImage(xmlDocument);

                int columns = classificationImage.MatrixWidth();
                int rows = classificationImage.MatrixHeight();

                intensityGraphicsView.Drawable = new GridDrawable(columns, rows, segmentGridOutputs);
                intensityGraphicsView.Invalidate();
                isGridVisible = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in UpdateImage: " + ex.Message);
        }
    }

    public void Dispose()
    {
        appState.OnImageZoomInRequested -= CalculateDefectPositionAndCropImage;
        appState.OnResetImageClicked -= ResetImageSize;
        appState.ColorCodeGraphicSet -= SetColorCodeGraphicInImage;
    }

    private void PointerEntered(object sender, PointerEventArgs e)
    {
        if (sender == rangeGraphicsView)
        {
            RangeLabel.IsVisible = false;
        }
        else if (sender == intensityGraphicsView)
        {
            IntensityLabel.IsVisible = false;
            CrackClassificationLbl.IsVisible = false;
        }
    }
    private void PointerExited(object sender, PointerEventArgs e)
    {
        if (sender == rangeGraphicsView)
        {
            RangeLabel.IsVisible = true;
        }
        else if (sender == intensityGraphicsView)
        {
            IntensityLabel.IsVisible = true;
            if (segmentGridOutputs.Count > 0) {
                CrackClassificationLbl.IsVisible = true;
            }
        }
    }

    private void btnUpdateImageSource_Clicked(object sender, EventArgs e)
    {
        appState.FolderOpenClicked(imagePath, _surveyId);
    }

    private void ImagePopup_Clicked(object sender, EventArgs e)
    {
        //call the function in main page
        PopupButtonClicked?.Invoke();
    }

    private void ToggleOverlay_Clicked(object sender, EventArgs e)
    {
        string newIntensityPath = null;
        //change the image to raw or overlay
        if (File.Exists(intensityPath))
        {
            var fileName = Path.GetFileNameWithoutExtension(intensityPath);
            var directory = Path.GetDirectoryName(intensityPath);
            var extension = Path.GetExtension(intensityPath);

            if (fileName.EndsWith("overlay", StringComparison.OrdinalIgnoreCase))
            {
                //overlay -> raw
                var newFileName = Regex.Replace(fileName, "overlay$", "Intensity", RegexOptions.IgnoreCase);
                newIntensityPath = Path.Combine(directory, newFileName + extension);
            }
            else if (fileName.EndsWith("intensity", StringComparison.OrdinalIgnoreCase))
            {
                //raw -> intensity
                var newFileName = Regex.Replace(fileName, "intensity$", "Overlay", RegexOptions.IgnoreCase);
                newIntensityPath = Path.Combine(directory, newFileName + extension);
            }

            if (newIntensityPath != null && File.Exists(newIntensityPath))
            {
                intensityPath = newIntensityPath;
                var intensityBytes = File.ReadAllBytes(newIntensityPath);
                viewModel.IntensityImagePath = newIntensityPath;
                ImageIntensity.Source = ImageSource.FromStream(() => new MemoryStream(intensityBytes));
            }
            else
            {
                Serilog.Log.Error($"No file found. {newIntensityPath}");
            }
        }
    }
}