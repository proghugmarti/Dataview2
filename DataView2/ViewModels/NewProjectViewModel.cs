using DataView2.Core.Models;
using Google.Protobuf.WellKnownTypes;
using CommunityToolkit.Maui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using MudBlazor;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DataView2.XAML;
using CommunityToolkit.Maui.Views;
using DataView2.Core.Models.Database_Tables;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.Json;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Helper;
using Windows.Devices.Geolocation;

namespace DataView2.ViewModels
{
    public class NewProjectViewModel : INotifyPropertyChanged
    {
        //private readonly IDatabaseRegistryService _datasetRegistryService;
        private readonly IProjectRegistryService _projectRegistryService;
        private readonly IProjectService _localProjectService;
        private readonly IDatabaseRegistryLocalService _datasetRegistryService;
        private readonly IPopupService _popupService;
        private string _projectName;
        private string _projectLocation;
        private string _projectType;
        NewDatabaseRequest requestDataBase = new NewDatabaseRequest { NewDatabasePath = "", DbType = nameof(Tools.dbContextType.Metadata) };
        public bool DisplayWebview { get; set; } = true;
        public NewProjectViewModel(IProjectRegistryService projectRegistryService, IDatabaseRegistryLocalService databaseRegistryLocalService, IProjectService projectService, IPopupService popupService)
        {
            //_datasetRegistryService = databaseRegistryService ?? throw new ArgumentNullException(nameof(databaseRegistryService));
            _projectRegistryService = projectRegistryService ?? throw new ArgumentNullException(nameof(projectRegistryService));
            _localProjectService = projectService ?? throw new ArgumentNullException(nameof(projectRegistryService));
            _datasetRegistryService = databaseRegistryLocalService ?? throw new ArgumentNullException(nameof(databaseRegistryLocalService));
            _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));

        }

        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProjectLocation
        {
            get => _projectLocation;
            set
            {
                if (_projectLocation != value)
                {
                    _projectLocation = value;
                    OnPropertyChanged();
                }
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<bool> CheckIfRegistryExistsAsync()
        {
            var registries = await _datasetRegistryService.GetAll(new Empty());
            return registries != null && registries.Count > 0;
        }

        public async Task<IdReply> CreateNewProjectAsync(string projectName, string projectFolderPathDes, string idProject)
        {
            try
            {
                var projects = await _projectRegistryService.GetAll(new Empty());
                var projectFound = projects.FirstOrDefault(p => p.Name == projectName);

                if (projectFound != null)
                {
                    // Project found
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Project with same name already exists"
                    };
                }
                else
                {
                    string projectFolder = Path.Combine(projectFolderPathDes, projectName);
                    string projectDbPath = Path.Combine(projectFolderPathDes, projectName, $"{projectName}.db");
                    var newProjectRequest = new ProjectRegistry
                    {
                        Name = projectName,
                        CreatedAtActionResult = DateTime.Now,
                        UpdatedAtActionResult = DateTime.Now,
                        FolderPath = projectFolder,
                        DBPath = projectDbPath,
                        IdProject = idProject
                    };
                    //Creating the folder project:
                    if (!Directory.Exists(projectFolder))
                    {
                        Directory.CreateDirectory(projectFolder);
                        Directory.CreateDirectory(Path.Combine(projectFolder, "Datasets"));
                        //Directory.CreateDirectory(Path.Combine(projectFolder, "OfflineMap")); don't create offline map folder here
                    }

                    var IdReply = await _projectRegistryService.Create(newProjectRequest);

                    return IdReply;
                }
            }
            catch (Exception ex)
            {
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in creation of Project : {ex.Message}"
                };
            }
        }

        //public async Task<IdReply> ImportProjectAsync(ProjectRegistry projectRegistry)
        //{
        //    try
        //    {
        //        var projectName = projectRegistry.Name;
        //        var projects = await _projectRegistryService.GetAll(new Empty());
        //        var projectFound = projects.FirstOrDefault(p => p.Name == projectName);

        //        if (projectFound != null)
        //        {
        //            // Project found
        //            return new IdReply
        //            {
        //                Id = -1,
        //                Message = "Project with same name already exists"
        //            };
        //        }
        //        else
        //        {
        //            var IdReply = await _projectRegistryService.Import(projectRegistry);
        //            return IdReply;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new IdReply
        //        {
        //            Id = -1,
        //            Message = $"Error in creation of Project : {ex.Message}"
        //        };
        //    }
        //}

        public async Task<IdReply> CreateNewLocalProjectAsync(string projectName, string projectFolderPathDes)
        {
            try
            {
                var projects = await _localProjectService.GetAll(new Empty());
                var projectFound = projects.FirstOrDefault(p => p.Name == projectName);

                if (projectFound != null)
                {
                    // Project found
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Project with same name already exists"
                    };
                }
                else
                {
                    string projectFolder = Path.Combine(projectFolderPathDes, projectName);
                    string projectDbPath = Path.Combine(projectFolderPathDes, projectName, $"{projectName}.db");
                    var newProjectRequest = new Project
                    {
                        Name = projectName,
                        CreatedAtActionResult = DateTime.Now,
                        UpdatedAtActionResult = DateTime.Now,
                        FolderPath = projectFolder,
                        DBPath = projectDbPath
                    };
                    //Creating the folder project:
                    if (!Directory.Exists(projectFolder))
                        Directory.CreateDirectory(projectFolder);


                    string newPath = Path.Combine(projectFolder, $"{projectName}.db");
                    // Change the database to the newly created one
                    requestDataBase.NewDatabasePath = newPath;
                    await _datasetRegistryService.ChangeDatabase(requestDataBase);
                    var IdReply = await _localProjectService.Create(newProjectRequest);

                    return IdReply;
                }
            }
            catch (Exception ex)
            {
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in creation of Project : {ex.Message}"
                };
            }
        }
        public async Task<IdReply> CreateNewDatasetAsync(string datasetName, string datasetLocation, int projectID)
        {
            try
            {
                var newDatasetRequest = new NewDatasetRequest
                {
                    DatasetName = datasetName,
                    DatasetLocation = datasetLocation,
                    ProjectId = projectID
                };
                //string pathDBDataSet = Path.Combine(datasetLocation, $"{datasetName}.db");

                var result = await _datasetRegistryService.CreateNewDatasetAsync(newDatasetRequest);

                // Check if dataset creation is successful
                if (result != null && result.Id > 0)
                {
                    // Send a message to close the popup
                    WeakReferenceMessenger.Default.Send(this, "ClosePopup");
                    // await ChangeDatabaseAsync(pathDBDataSet);
                    return result;
                }
                else if (result != null && result.Id == -1)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Failed to create dataset: Dataset name already exists."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Dataset: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Failed to create dataset."
            };
        }

        public async Task<bool> ExistProjectAsync(string projectName)
        {
            var existResult = await _projectRegistryService.CheckExistByName(projectName);
            return existResult.Id == -1 ? true : false;
        }

        public async Task<ProjectRegistry> ProjectRegistryGetById(IdRequest idrequest)
        {
            try
            {
                var result = await _projectRegistryService.GetById(idrequest);

                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Project: {ex.Message}");
            }
            return new ProjectRegistry
            {
                Id = 0,
                Name = "Not Found",
                CreatedAtActionResult = DateTime.MinValue,
                UpdatedAtActionResult = DateTime.MinValue,
                GPSLatitude = 0.0,
                GPSLongitude = 0.0,
                RoundedGPSLatitude = 0.0,
                RoundedGPSLongitude = 0.0,
                IdProject = ""
            };
        }
        public async Task<Project> localProjectRegistryGetById(IdRequest idrequest, string dbPath)
        {
            try
            {
                if (dbPath != "")
                {
                    requestDataBase.NewDatabasePath = dbPath;
                    await _datasetRegistryService.ChangeDatabase(requestDataBase);
                }

                var result = await _localProjectService.GetById(idrequest);

                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Project: {ex.Message}");
            }
            return new Project
            {
                Id = 0,
                Name = "Not Found",
                CreatedAtActionResult = DateTime.MinValue,
                UpdatedAtActionResult = DateTime.MinValue,
                GPSLatitude = 0.0,
                GPSLongitude = 0.0,
                RoundedGPSLatitude = 0.0,
                RoundedGPSLongitude = 0.0
            };
        }

        public async Task<Project> localProjectRegistryGetByName(string projectName, string dbPath)
        {
            try
            {
                if (dbPath != "")
                {
                    requestDataBase.NewDatabasePath = dbPath;
                    await _datasetRegistryService.ChangeDatabase(requestDataBase);
                }

                var result = await _localProjectService.GetByName(projectName);

                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Project: {ex.Message}");
            }
            return new Project
            {
                Id = 0,
                Name = "Not Found",
                CreatedAtActionResult = DateTime.MinValue,
                UpdatedAtActionResult = DateTime.MinValue,
                GPSLatitude = 0.0,
                GPSLongitude = 0.0,
                RoundedGPSLatitude = 0.0,
                RoundedGPSLongitude = 0.0
            };
        }

        public async Task<IdReply> UpdateProjectAsync(ProjectRegistry project)
        {
            try
            {
                var result = await _projectRegistryService.RenameProject(project);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating external IdProject: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "New project linked."
            };
        }

        public async Task<IdReply> UpdateLocalProjectRegistryAsync(Project project)
        {
            try
            {
                var idreplay = await _localProjectService.RenameProject(project);
                return idreplay;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Project: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "New Dataset failed."
            };
        }
    }
}
