using CommunityToolkit.Maui.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Options;
using DataView2.States;
using DataView2.ViewModels;
using DataView2.XAML;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Serilog;
using System.Reflection;

namespace DataView2
{
    public partial class App : Application
    {
        private readonly IPopupService popupService;
        //private readonly IDatabaseRegistryService databaseRegistryService;
        private readonly IProjectRegistryService projectRegistryService;
        private readonly IDatabaseRegistryLocalService databaseRegistryLocalService;
        private readonly IProjectService localProjectService;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly ApplicationState appState;
        private string title = "ROMDAS DataView";        
        public List<Window> OpenWindows { get; } = new();

        public App(IPopupService popupService, IProjectRegistryService projectRegistryService, IDatabaseRegistryLocalService databaseRegistryLocalService, IProjectService projectService, ApplicationState applicationState)
        {
            InitializeComponent();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<MainPage>();
            this.popupService = popupService;
            //this.databaseRegistryService = databaseRegistryService;
            this.projectRegistryService = projectRegistryService;
            this.databaseRegistryLocalService = databaseRegistryLocalService;
            this.localProjectService = projectService;
            this.appState = applicationState;

            MainPage = new MainPage(logger, popupService);

            Log.Information("App.xaml Before DisplayPopup 1A");
            DisplayPopup();

            Log.Information("App.xaml AFTER DisplayPopup 1B");
        }


        public async void DisplayPopup()
        {
            try
            {
                Log.Information($"Trying to open pop Up and get all from ProjectService");

                List<ProjectRegistry> anyProjectsExist = await projectRegistryService.GetAll(new Empty());
                List<ProjectRegistry> availableProjects = new List<ProjectRegistry>();
                
                if (anyProjectsExist != null && anyProjectsExist.Count > 0)
                {
                    foreach (var proj in anyProjectsExist)
                    {
                        if (!File.Exists(proj.DBPath))
                        {
                            await projectRegistryService.DeleteProject(new IdRequest { Id = proj.Id });
                        }
                        else
                            availableProjects.Add(proj);
                    }
                }

                Log.Information($"2601 Before if");

                if (availableProjects.Count == 0)
                {
                    Log.Information($"2601 if Case 0");
                    var initializationPopup = new InitializationPopup(projectRegistryService, databaseRegistryLocalService, localProjectService, popupService);
                    this.popupService.ShowPopup<NewProjectViewModel>();
                }
                else
                {
                    Log.Information($"2601 if Case Else");
                    var projectPopup = new ProjectPopup(projectRegistryService, databaseRegistryLocalService, popupService, appState);
                    this.popupService.ShowPopup<ProjectViewModel>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Open pop up failed :  {ex} ");

                // If no projects are found, retry after a delay
                if (ex is RpcException rpcException && rpcException.StatusCode == StatusCode.NotFound)
                {
                    Log.Information("Retrying after 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    DisplayPopup();
                }
                else
                {
                    Log.Information("App.xaml Ending DisplayPopup Error handler 1A");
                }

            }

        }

        //Title for the window 
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            if (window != null)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;

                window.Title = $"{title} v{version}"; 

            }
            return window;


        }

        public async Task CloseAllWindows(IConfiguration configuration)
        {
            foreach (var window in OpenWindows.ToList())
            {              
                Application.Current.CloseWindow(window);
                OpenWindows.Remove(window);
            }
#if !DEBUG
            if (Core.MultiInstances.SharedDVInstanceStore.DVInstances.IsOnlyInstance())
            {
                var options = configuration.GetSection("DataView2Options").Get<DataView2Options>();

                var wsProcessing = options?.ServiceOptions?.FirstOrDefault(s => s.Name == "WS.Processing");

                if (!string.IsNullOrWhiteSpace(wsProcessing?.ExePath))
                {
                    int[] processIds = Tools.GetRunningProcessIdsByPath(wsProcessing.ExePath);

                    foreach (int pid in processIds)
                    {
                        Tools.StopProcessByID(pid);
                    }
                }
            }
#endif
        }
    }
}