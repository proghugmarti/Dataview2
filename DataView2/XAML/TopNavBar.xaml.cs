using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Engines;
using DataView2.States;
using Serilog;
using System.Diagnostics;
namespace DataView2.XAML;


public partial class TopNavBar : ContentView
{
    public ApplicationState appState;
    public ApplicationEngine appEngine;
    public TopNavBar()
    {
        appState = MauiProgram.AppState;
        appEngine = MauiProgram.AppEngine;
        InitializeComponent();
        InitializeDropdowns();
    }

    // Initialises button / dropdown hover tracker.
    private void InitializeDropdowns()
    {
        RegisterDropdown(ProjectsButton, ProjectsDropdown, ProjectUnderline);
        RegisterDropdown(DatasetsButton, DatasetDropdown, DatasetsUnderline);
        RegisterDropdown(DataManagementButton, DataManagementDropdown, DataManagementUnderline);
        RegisterDropdown(ExportButton, ExportDropdown, ExportUnderline);
        RegisterDropdown(SurveyToolsButton, SurveyToolsDropdown, SurveyToolsUnderline);
        RegisterDropdown(SettingsButton, SettingsDropdown, SettingsUnderline);
        RegisterDropdown(SurveyTemplateButton, SurveyTemplatesDropdown, null, true, SurveyToolsDropdown);
        RegisterDropdown(PCIModulesButton, PCIDropdown, null, true, DataManagementDropdown);
        RegisterDropdown(OfflineMapButton, OfflineDropdown, null, true, SurveyToolsDropdown);
    }

    private class DropdownState
    {
        public Button Button { get; set; }
        public BoxView Underline { get; set; }
        public bool hoverOverButton { get; set; }
        public bool hoverOverDropdown { get; set; }

        public bool subDropdown { get; set; }

        public Frame parentDropdown { get; set; }
    }

    #region Dropdowns

    private readonly Dictionary<Frame, DropdownState> dropdownStates = new Dictionary<Frame, DropdownState>();

    // Creates pointer gesture recognisers for navbar buttons + dropdowns and adds to Dictionary to track states.
    private void RegisterDropdown(Button button, Frame dropdown, BoxView underline, bool subDropdown = false, Frame parentDropdown = null)
    {
        dropdownStates[dropdown] = new DropdownState
        {
            Button = button,
            Underline = underline,
            hoverOverButton = false,
            hoverOverDropdown = false,
            subDropdown = subDropdown,
            parentDropdown = parentDropdown
        };

        var buttonRecognizer = new PointerGestureRecognizer();
        buttonRecognizer.PointerEntered += (s, e) => ButtonPointerEntered(dropdown);
        buttonRecognizer.PointerExited += (s, e) => ButtonPointerExited(dropdown);
        button.GestureRecognizers.Add(buttonRecognizer);

        var dropdownRecognizer = new PointerGestureRecognizer();
        dropdownRecognizer.PointerEntered += (s, e) => DropdownPointerEntered(dropdown);
        dropdownRecognizer.PointerExited += (s, e) => DropdownPointerExited(dropdown);
        dropdown.GestureRecognizers.Add(dropdownRecognizer);
    }


    // Updates the button states in dictionary when entering / exiting navbar buttons
    private void ButtonPointerEntered(Frame dropdown)
    {
        // Show dropdown
        var state = dropdownStates[dropdown];
        state.hoverOverButton = true;
        dropdown.IsVisible = true;
        dropdownStates[dropdown] = state;

        if (state.Underline != null)
        {
            // Show underline
            state.Underline.Opacity = 1;
        }
    }

    private async void ButtonPointerExited(Frame dropdown)
    {
        var state = dropdownStates[dropdown];
        state.hoverOverButton = false;
        dropdownStates[dropdown] = state;
        await TryCloseDropdown(dropdown);
    }

    // Updates the dropdown states in dictionary when entering / exiting dropdown
    private void DropdownPointerEntered(Frame dropdown)
    {
        var state = dropdownStates[dropdown];
        state.hoverOverDropdown = true;
        dropdown.IsVisible = true;
        if (state.subDropdown && state.parentDropdown != null)
        {
            var parent = dropdownStates.First(x => x.Key == state.parentDropdown);
            parent.Value.hoverOverDropdown = true;
        }
        dropdownStates[dropdown] = state;
    }

    private async void DropdownPointerExited(Frame dropdown)
    {
        var state = dropdownStates[dropdown];
            state.hoverOverDropdown = false;
        dropdownStates[dropdown] = state;

        if (state.subDropdown && state.parentDropdown != null)
        {
            var parent = dropdownStates.First(x => x.Key == state.parentDropdown);
            parent.Value.hoverOverDropdown = false;
            await TryCloseDropdown(parent.Key);
        }
        await TryCloseDropdown(dropdown);
    }

    // Dropdown only closes if both hoverbutton and hoverdropdown is false
    private async Task TryCloseDropdown(Frame dropdown)
    {
        await Task.Delay(25);
        var state = dropdownStates[dropdown];


        if (state.hoverOverButton == false && state.hoverOverDropdown == false)
        {
            dropdown.IsVisible = false;

            if (state.Underline != null)
                state.Underline.Opacity = 0;
        }
    }
    #endregion

    // Home doesn't have dropdown logic only apply underline.
    private void HomeButton_PointerEntered(object sender, PointerEventArgs e)
    {
        HomeUnderline.Opacity = 1;
    }

    private void HomeButton_PointerExited(object sender, PointerEventArgs e)
    {
        HomeUnderline.Opacity = 0;
    }

    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        //appState.SetActivePage("/Home"); no need to call this
        appState.SetMenuPage("/Home");

        _ = ChangeCoordinates();
    }

    private async Task ChangeCoordinates()
    {
        var selectedDataset = appState.SelectedDataset;

        if (selectedDataset != null)
        {
            IdRequest requestId = new IdRequest() { Id = selectedDataset.Id };
            DatabaseRegistryLocal dataset = await appEngine.DatabaseRegistryService.GetActualDatasetRegistry(requestId);
            if (dataset.Name == "Default")
            {
                //handle case there is no dataset created in the project selected
                return;
            }
            appState.UpdateCoordinates(dataset.GPSLatitude, dataset.GPSLongitude);
            appState.StopGeometryEditor();
        }
    }
    private void NavButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string route)
        {
            //appState.SetActivePage(route);
            appState.SetMenuPage(route);
        }
    }   

    private void CreateMap(object sender, EventArgs e)
    {
        //appState.SetActivePage("/Home");
        appState.SetMenuPage("/Home");
        appState.CurrentPath = null;
        appState.SetOfflineMapPath();
    }   

    private void StartNewDVInstance(object sender, EventArgs e)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule!.FileName!;

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
            Application.Current.MainPage.DisplayAlert("Information", "A new instance of DataView will be open in a few seconds...", "OK");
        }
        catch (Exception ex)
        {
            Log.Information($"[DV MultiSession] Error starting new DV instance: {ex}");
        }
    }
}