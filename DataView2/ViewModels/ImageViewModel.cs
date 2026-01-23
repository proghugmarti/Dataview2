using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DataView2.ViewModels
{
    internal class ImageViewModel : INotifyPropertyChanged
    {
        public ImageViewModel() 
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private BitmapImage rangeImage = new();
        public BitmapImage RangeImage
        {
            get => this.rangeImage;
            set
            {
                if (rangeImage != value)
                {
                    this.rangeImage = value;
                    this.OnPropertyChanged(nameof(this.RangeImage));
                }
            }
        }
        private string rangeImagePath;
        public string RangeImagePath
        {
            get
            {
                return this.rangeImagePath;
            }

            set
            {
                this.rangeImagePath = value;
                SetRangeImage(rangeImagePath);
                this.OnPropertyChanged(nameof(this.RangeImagePath));
            }
        }

        private BitmapImage intensityImage = new();
        public BitmapImage IntensityImage
        {
            get => this.intensityImage;
            set
            {
                if (intensityImage != value)
                {
                    this.intensityImage = value;
                    this.OnPropertyChanged(nameof(this.IntensityImage));
                }
            }
        }
        private string intensityImagePath;
        public string IntensityImagePath
        {
            get
            {
                return this.intensityImagePath;
            }

            set
            {
                this.intensityImagePath = value;
                SetIntensityImage(intensityImagePath);
                this.OnPropertyChanged(nameof(this.IntensityImagePath));
            }
        }
        private void SetRangeImage(string imagePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                using var ms = new MemoryStream(bytes);

                BitmapImage image = new();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();

                RangeImage = image;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void SetIntensityImage(string imagePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                using var ms = new MemoryStream(bytes);

                BitmapImage image = new();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();

                IntensityImage = image;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
