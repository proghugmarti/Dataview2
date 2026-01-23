using MudBlazor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Platforms.Windows
{
    public class ScreenshotHelper
    {
        public static Rectangle GetAppScreenBounds()
        {
            if (App.Current.Windows[0].Handler.PlatformView is Microsoft.UI.Xaml.Window mainWindow)
            {
                // Get the native handle of the application window
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

                // Get the window rectangle
                if (GetWindowRect(hWnd, out RECT rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    // Return bounds as a Rectangle
                    return new Rectangle(rect.Left, rect.Top, width, height);
                }
                else
                {
                    throw new InvalidOperationException("Failed to get application window bounds.");
                }
            }

            throw new InvalidOperationException("Could not retrieve the application window.");
        }

        public static void TakeAndSave(string path)
        {
            var bounds = GetAppScreenBounds();

            // Capture the screenshot for the application
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }


        // Import the GetWindowRect function from the Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // RECT struct for window bounds
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;   // X position of the top-left corner
            public int Top;    // Y position of the top-left corner
            public int Right;  // X position of the bottom-right corner
            public int Bottom; // Y position of the bottom-right corner
        }
    }
}
