using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.ViewModels;

namespace DataView2.XAML;

public partial class LayerEditorPopup : Popup
{
    public LayerEditorPopup(string tableName, string layerType, bool pointIcon = false)
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
            { "tableName", tableName },
            { "layerType", layerType },
            { "pointIcon", pointIcon }
        };
    }
}