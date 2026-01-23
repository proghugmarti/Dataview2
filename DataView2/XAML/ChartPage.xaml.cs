using DataView2.Core.Models.Other;
using DataView2.Engines;
using DataView2.States;
using DataView2.ViewModels;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Color = Microsoft.Maui.Graphics.Color;
using Microsoft.Maui.Devices;

namespace DataView2.XAML;


public partial class ChartPage : ContentPage 
{
    private readonly LasChartViewModel _viewModel;
    private int rutPoint;
    private string defaultText;    

    public ChartPage(List<LASPoint> lasPoints, ApplicationEngine appEngine, ApplicationState appState)
    {
        InitializeComponent();
        _viewModel = new LasChartViewModel(lasPoints, appEngine, appState);
        BindingContext = _viewModel;

        // Bind the ListView to SavedRuttingResults
        ruttingResultsListView.SetBinding(ListView.ItemsSourceProperty, nameof(_viewModel.SavedRuttingResults));
  

        // Subscribe to the event in the ViewModel
        var viewModel = (LasChartViewModel)BindingContext;
        defaultText = _viewModel.StraightEdgeLength.ToString();      
    }

    protected override async void OnAppearing()
{
    base.OnAppearing();

    var scaleFactor = DeviceDisplay.MainDisplayInfo.Density;

    if (scaleFactor > 1.5)
    {
        bool applyAdaptation = await App.Current.MainPage.DisplayAlert(
            "High Monitor Resolution Scale Detected",
            "Would you like to apply an adaptation?",
            "Yes",
            "No"
        );

        if (applyAdaptation)
        {
            this.Scale = 0.9;

            foreach (var child in StackLayoutChart.Children)
            {
                switch (child)
                {
                    case Label lbl:
                        lbl.FontSize = 13;
                        break;
                    case Entry ent:
                        ent.FontSize = 12;
                        break;
                    case Button btn:
                        btn.FontSize = 12;
                        break;
                }
            }
        }
    }
}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.ClosedWindow();
    }

    private async void OnExportButtonClicked(object sender, EventArgs e)
    {
        await _viewModel.ExportToCsv();
    }

    private async void OnCalculateRuttingButtonClicked(object sender, EventArgs e)
    {
        if (!double.TryParse(_viewModel.StraightEdgeLengthInput, out double selectedLength) || selectedLength <= 0)
        {
            await DisplayAlert("Error", "Please enter a positive number to calculate straight edge length.", "OK");
            _viewModel.StraightEdgeLengthInput = _viewModel.StraightEdgeLength.ToString();
            return;
        }

        _viewModel.PreviousStraightEdgeLength = _viewModel.StraightEdgeLength;
        _viewModel.StraightEdgeLength = selectedLength;
    }


    private async void OnSaveRuttingButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (_viewModel.LastResult != null)
            {
                _viewModel.SavedRuttingResults.Add(_viewModel.LastResult);
            }
            else
            {
                await DisplayAlert("Error", "No result to save. Please calculate rutting first.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while saving rutting: {ex.Message}", "OK");
        }
    }

 }






