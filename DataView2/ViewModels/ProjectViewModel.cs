using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Engines;
using DataView2.States;
using DataView2.XAML;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;

namespace DataView2.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged
    {
        private readonly IProjectRegistryService _projectRegistryService;
        private readonly IDatabaseRegistryLocalService _databaseRegistryService;
        private readonly IPopupService _popupService;
        private readonly ApplicationState appState;
        public bool DisplayWebview { get; set; } = true;

        public ProjectRegistry ProjectRegistry { get; private set; }

        public Project LocalProject { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public ProjectViewModel(IProjectRegistryService projectRegistryService, IDatabaseRegistryLocalService databaseRegistryService, IPopupService popupService, ApplicationState applicationState)
        {
            _projectRegistryService = projectRegistryService ?? throw new ArgumentNullException(nameof(projectRegistryService));
            _databaseRegistryService = databaseRegistryService ?? throw new ArgumentNullException(nameof(databaseRegistryService));
            _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
            appState = applicationState ?? throw new ArgumentNullException(nameof (applicationState));
        }

        public void ChangeProjectAsync(ProjectRegistry baseProject, Project localProject)
        {
            if (baseProject == null)
            {
                Debug.WriteLine("Base project is null, skipping ChangeProjectAsync.");
                return;
            }

            if (localProject == null)
            {
                Debug.WriteLine("Local project is null, skipping ChangeProjectAsync.");
                return;
            }

            if (baseProject.Id <= 0) 
            {
                Debug.WriteLine($"Base project with ID {baseProject.Id} is deleted or invalid.");
                return;
            }

            ProjectRegistry = baseProject;
            appState.UpdateBaseProject(baseProject);

            LocalProject = localProject;
            appState.UpdateProject(localProject);

            WeakReferenceMessenger.Default.Send(this, "ClosePopup");
        }

        public async Task<List<string>> CopyDatasets(string folderDatasetsToChange, string targetDatasetsDirectory, string dataBase)
        {
            List<string> updatePathsDataSetqry = new List<string>();

            if (!Directory.Exists(folderDatasetsToChange))
            {
                Console.WriteLine("Origin folder does not exist: " + folderDatasetsToChange);
                return null;
            }
            if (Directory.Exists(targetDatasetsDirectory))
            {
                Directory.Delete(targetDatasetsDirectory, true);
                Directory.CreateDirectory(targetDatasetsDirectory);
            }
            else
            {
                Directory.CreateDirectory(targetDatasetsDirectory);
            }
            string[] files = Directory.GetFiles(folderDatasetsToChange);

            DatsetPathRequest listDataSets = new DatsetPathRequest { DatsetPaths = files.ToList(), folderDataSetToChange = folderDatasetsToChange, folderDataSetTarget = targetDatasetsDirectory, DatabasePath = dataBase };
            ListRequest result = null;
            if (listDataSets.DatsetPaths.Count > 0)
                result = await _databaseRegistryService.ChangeDatasets(listDataSets);

            if (result != null)
            {
                updatePathsDataSetqry = result.ListData;
            }
            return updatePathsDataSetqry;
        }
    }

}
