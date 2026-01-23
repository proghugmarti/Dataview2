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


namespace DataView2.XAML;


public partial class SegmentationPage : ContentPage 
{

    private readonly SegmentationTableViewModel _viewModel;

    public SegmentationPage(ApplicationEngine appEngine, ApplicationState appState)
    {
        InitializeComponent();
        _viewModel = new SegmentationTableViewModel( appEngine, appState);
    }
}



