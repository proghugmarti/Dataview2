using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.States;
using DataView2.ViewModels;

namespace DataView2.XAML;

public partial class ProjectPopup : Popup
{
	private readonly IProjectRegistryService _projectRegistryService;
    private readonly IPopupService _popupService;
    private readonly ApplicationState appState; 
    public ProjectPopup(IProjectRegistryService projectRegistryService, IDatabaseRegistryLocalService databaseRegistryService, IPopupService popupService, ApplicationState applicationState )
	{
		InitializeComponent();

		var viewModel = new ProjectViewModel(projectRegistryService, databaseRegistryService, popupService, applicationState);
		
		BindingContext = viewModel;
        _popupService = popupService;
        MauiProgram.AppState.IsPopupOpen = true;


        // Subscribe to the message to close the popup
        WeakReferenceMessenger.Default.Register<ProjectViewModel, string>(viewModel, "ClosePopup", (sender, vm) =>
        {
            applicationState.isUsingOnlineMap = false;
            MauiProgram.AppState.IsPopupOpen = false;
            Close();
        });
    }


}