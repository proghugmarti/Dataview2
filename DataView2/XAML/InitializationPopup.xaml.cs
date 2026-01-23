using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Pages.Dataset;
using DataView2.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics.Metrics;

namespace DataView2.XAML;

public partial class InitializationPopup : Popup
{
    //private readonly IDatabaseRegistryService _databaseRegistryService;
    private readonly IProjectRegistryService _projectRegistryService;
    private readonly IDatabaseRegistryLocalService _databaseRegistryLocalService;
    private readonly IProjectService _projectService;
    private readonly IPopupService _popupService;

    public InitializationPopup(IProjectRegistryService projectRegistryService, IDatabaseRegistryLocalService databaseRegistryLocalService, IProjectService projectService, IPopupService popupService)
    {
        Log.Information($"========================== init popup.");

        InitializeComponent();

     
        var viewModel = new NewProjectViewModel(projectRegistryService, databaseRegistryLocalService, projectService, popupService);
        BindingContext = viewModel;
        //_databaseRegistryService = databaseRegistryService;
        _projectRegistryService = projectRegistryService;
        _databaseRegistryLocalService = databaseRegistryLocalService;
        _projectService = projectService;
        _popupService = popupService;
        MauiProgram.AppState.IsPopupOpen = true;

        // Subscribe to the message to close the popup
        WeakReferenceMessenger.Default.Register<NewProjectViewModel, string>(viewModel, "ClosePopup", (sender, message) =>
        {
            MauiProgram.AppState.IsPopupOpen = false;
            Close();
        });

    }

}