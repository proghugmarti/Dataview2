using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Core.Models.Database_Tables;
using DataView2.States;
using DataView2.ViewModels;

namespace DataView2;

public partial class SurveySetPopup : Popup
{
    public SurveySetPopup(string page)
    {
        InitializeComponent();
        MauiProgram.AppState.IsPopupOpen = true;

        // Subscribe to the message to close the popup
        WeakReferenceMessenger.Default.Register<SurveySetViewModel, string>(this, "ClosePopup", (sender, vm) =>
        {
            WeakReferenceMessenger.Default.Unregister<string>(this);
            MauiProgram.AppState.IsPopupOpen = false;
            this.Close();
        });


        if (page == "new")
        {
            rootComponent.Parameters = new Dictionary<string, object>
            {
                  { "page", "new" }
            };
        }
        else if (page == "open")
        {
            rootComponent.Parameters = new Dictionary<string, object>
            {
                  { "page", "open" }
            };
        }
        else if (page == "import")
        {
            rootComponent.Parameters = new Dictionary<string, object>
            {
                  { "page", "import" }
            };
        }
        else if (page == "set")
        {
            rootComponent.Parameters = new Dictionary<string, object>
            {
                 { "page", "open" }
            };
        }
        else if (page == "map")
        {
            rootComponent.Parameters = new Dictionary<string, object>
            {
                 { "page", "map" }
            };
        }
    }
}