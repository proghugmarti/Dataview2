using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.States
{
    public class WindowManager
    {
        private static Window _currentWindow;
        public static ApplicationState appState = MauiProgram.AppState;

        public static void FocusWindow()
        {
            //focus on the existing window
            _currentWindow.Page.Dispatcher.Dispatch(() =>
            {
                var winUIWindow = _currentWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

                // Bring the window to the front
                winUIWindow.Activate();
            });
        }
        // Method to open a new window
        public static void OpenWindow(Window window)
        {
            if (_currentWindow != null && _currentWindow.Title == window.Title)
            {
                FocusWindow();
                return;
            }

            _currentWindow = window;
            App.Current?.OpenWindow(_currentWindow);

            var nativeWindow = _currentWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            var appWindow = nativeWindow.AppWindow;
            appWindow.Closing += async (sender, args) =>
            {
                args.Cancel = true;
                bool value = await App.Current.MainPage.DisplayAlert("Warning", "Are you sure you want to close this window? Unsaved progress will be lost.", "Yes", "No");
                if (value == true)
                {
                    _currentWindow = null;
                    //Remove sample unit graphics
                    appState.ExitSurveySet(true);
                    appState.UpdateTableNames();
                    nativeWindow.Close();
                }
            };
        }

        // Method to close the current window
        public static void CloseWindow()
        {
            if (_currentWindow != null)
            {
                App.Current?.CloseWindow(_currentWindow);
                _currentWindow = null;  // Clear the reference
                                        //Remove sample unit graphics
                appState.ExitSurveySet(true);
                appState.UpdateTableNames();
            }
        }
    }
}
