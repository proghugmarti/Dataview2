using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.ViewModels;

namespace DataView2.XAML;

public partial class SummariesPopup : Popup
{
	public SummariesPopup(string mode, string tableName)
	{
		InitializeComponent();

        MauiProgram.AppState.IsPopupOpen = true;

        WeakReferenceMessenger.Default.Register<LayerViewModel, string>(this, "ClosePopup", (sender, vm) =>
        {
            WeakReferenceMessenger.Default.Unregister<string>(this);
            MauiProgram.AppState.IsPopupOpen = false;
            this.Close();
        });


        rootComponent.Parameters = new Dictionary<string, object>
        {
            { "mode", mode },
            { "tableName", tableName }
        };

    }
}