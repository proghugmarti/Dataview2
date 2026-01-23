using DataView2.Engines;
using DataView2.States;
using DataView2.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;
using MudBlazor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using Image = Microsoft.Maui.Controls.Image;
using Serilog;
using System.Windows.Media;
using ImageSource = Microsoft.Maui.Controls.ImageSource;

namespace DataView2.XAML;

public partial class VideoPopupPage : ContentView, INotifyPropertyChanged
{
    public ApplicationState appState;
    public string imagePath;
    public double fixedHeight;
    public VideoPopupPage(string imagePath, string cameraInfo)
	{
		InitializeComponent();
        appState = MauiProgram.AppState;
        UpdateBothImageandBitmap(imagePath);
        cameraLabel.Text = cameraInfo;
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private BitmapImage bitmapImage = new();
    public BitmapImage BitmapImage
    {
        get => this.bitmapImage;
        set
        {
            if (bitmapImage != value)
            {
                this.bitmapImage = value;
                this.OnPropertyChanged(nameof(this.BitmapImage));
            }
        }
    }

    public void UpdateImage(string imagePath)
    {
        this.imagePath = imagePath;
        VideoImage.Source = imagePath;
        fixedHeight = VideoImage.Height;
    }

    public void ResizeImageMaxWidth(double? width)
    {
        if (width.HasValue)
        {
            VideoImage.MaximumWidthRequest = width.Value; // Set a specific max width
        }
        else
        {
            VideoImage.MaximumWidthRequest = double.PositiveInfinity; // Remove the max width
        }
    }

    public void UpdateBothImageandBitmap(string imagePath)
    {
        this.imagePath = imagePath;
        SetBitmapImage(imagePath);
        VideoImage.Source = imagePath;
    }

    public void SetBitmapImage(string imagePath)
    {
        try
        {
            BitmapImage image = new();
            image.BeginInit();
            image.UriSource = new(imagePath);
            image.EndInit();
            BitmapImage = image;
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private float GetAspectRatio(BitmapImage bitmap)
    {
        return (float)bitmap.PixelWidth / bitmap.PixelHeight;
    }

    private PointF start;
    private PointF current;

    private void OnStartInteraction(object Sender, TouchEventArgs evt)
    {
        start = evt.Touches.FirstOrDefault();
    }

    private void OnDragInteraction(object sender, TouchEventArgs evt)
    {
        if (sender is GraphicsView graphicsView)
        {
            var drawable = (Drawable)Resources["drawable"];

            // Get the canvas size
            PointF canvasSize = new()
            {
                X = (int)graphicsView.Width,
                Y = (int)graphicsView.Height,
            };
            float imageWidth = BitmapImage.PixelWidth;
            float imageHeight = BitmapImage.PixelHeight;
            float aspectRatio = imageWidth / imageHeight; 

            drawable.UpdateCanvasSize((float)graphicsView.Width, (float)graphicsView.Height, aspectRatio);

            var current = evt.Touches.FirstOrDefault();

            if (!current.IsEmpty)
            {
                float dragX = current.X - start.X;
                float dragY = current.Y - start.Y;
                // Maintain the aspect ratio using the drawable's aspect ratio
                float size = Math.Min(dragX, dragY);

                // Calculate new rectangle dimensions based on aspect ratio
                float width = size;
                float height = size / aspectRatio;

                // Ensure the rectangle stays within the canvas bounds
                float endX = Math.Min(start.X + width, canvasSize.X);
                float endY = Math.Min(start.Y + height, canvasSize.Y);

                // Update the drawable rectangle with the new size and position
                drawable.UpdateRectangle(start, new PointF(endX, endY));

                // Invalidate the GraphicsView to trigger a redraw
                graphicsView.Invalidate();
            }
        }
    }

    private void OnEndInteraction(object sender, TouchEventArgs evt)
    {
        if (sender is GraphicsView graphicsView)
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
                float rectWidth = drawable.width;
                float rectHeight = drawable.height;

                PointF rectStart = new PointF(drawable.start.X, drawable.start.Y);
                PointF rectEnd = new PointF(rectStart.X + rectWidth, rectStart.Y + rectHeight);

                rectStart.X = Math.Max(0, Math.Min(rectStart.X, canvasSize.X));
                rectStart.Y = Math.Max(0, Math.Min(rectStart.Y, canvasSize.Y));
                rectEnd.X = Math.Max(0, Math.Min(rectEnd.X, canvasSize.X));
                rectEnd.Y = Math.Max(0, Math.Min(rectEnd.Y, canvasSize.Y));

                Int32Rect croppedArea = new Int32Rect
                {
                    Height = (int)(rectEnd.Y - rectStart.Y),
                    Width = (int)(rectEnd.X - rectStart.X),
                    X = (int)rectStart.X,
                    Y = (int)rectStart.Y
                };

                CropImageFromSelection(croppedArea, canvasSize);
                if (drawable.Clear())
                {
                    graphicsView.Invalidate();
                }
            }
        }
    }

    public void CropImageFromSelection(Int32Rect rectangle, PointF canvasSize)
    {
        BitmapImage bitmap = BitmapImage;
        Int32Rect rectangleScaled;

        try
        {
            double hScale = (bitmap.PixelWidth / canvasSize.X);
            double vScale = (bitmap.PixelHeight / canvasSize.Y);
            rectangleScaled.Width = (int)(rectangle.Width * hScale);
            rectangleScaled.Height = (int)(rectangle.Height * vScale);
            rectangleScaled.X = (int)(rectangle.X * hScale);
            rectangleScaled.Y = (int)(rectangle.Y * vScale);

            if (rectangleScaled.Height == 0 || rectangleScaled.Width == 0)
                return;

            CroppedBitmap cropped = new(bitmap, rectangleScaled);
            BitmapImage = BitmapSourceToBitmap(cropped);

            byte[] imageBytes = ConvertCroppedBitmapToBytes(cropped);
            VideoImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
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

    private void OnRightClicked(object sender, TappedEventArgs e)
    {
        var drawable = (Drawable)Resources["drawable"];

        if (sender is GraphicsView graphicsView)
        {
            if (graphicsView == videoGraphicsView)
            {
                VideoImage.Source = ImageSource.FromFile(imagePath);
                SetBitmapImage(imagePath);
            }

            if (drawable.Clear())
            {
                graphicsView.Invalidate();
            }
        }
    }
}