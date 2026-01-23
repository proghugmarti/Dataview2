using Microsoft.Maui.Hosting;

namespace DataView2;
using CommunityToolkit.Maui.Views;
using DataView2.Components.Pages;
using Microsoft.AspNetCore.Components.WebView.Maui;

public partial class SimplePopup : Popup
{
    public SimplePopup()
    {
        InitializeComponent();



        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Counter)
        });

    }
}