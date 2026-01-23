using CommunityToolkit.Mvvm.Messaging;
using DataView2.States;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.ViewModels
{
    public class SurveySetViewModel : INotifyPropertyChanged
    {
        public ApplicationState appState;

        public SurveySetViewModel()
        {
            appState = MauiProgram.AppState;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PassSurveySetPath(string path)
        {
            var param = new Dictionary<string, object> { { "first", true } };
            if (path != null)
            {
                appState.CurrentPath = Path.GetDirectoryName(path);
                appState.SurveyCsvObjFilePath = path;
                appState.ChangeSurveySetPage("SurveyList", param);
            }
         
            WeakReferenceMessenger.Default.Send(this, "ClosePopup");
        }

        public void SetOfflineMapPath(string path)
        {
            if (path != null)
            {
                appState.CurrentPath = path;
                appState.ShowHideSurveyTemplate(true);
                //appState.HandleSurveyList();
                appState.EnableRectangleDrawing(true);
                //appState.HighlightGraphic(null, "Click");
            }
            else
            {
                appState.CurrentPath = string.Empty;
            }

            WeakReferenceMessenger.Default.Send(this, "ClosePopup");
        }

        public void ManageImportData(string path)
        {
            WeakReferenceMessenger.Default.Send(this, "ClosePopup");
        }
    }

}
