global using DataView2.Core;
global using DataView2.Core.MultiInstances;
global using Grpc.Net.Client;
using CommunityToolkit.Maui;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.DataHub;
using DataView2.Core.Models.DTS;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Positioning;
using DataView2.Core.Models.QC;
using DataView2.Core.Models.Setting;
using DataView2.Core.MultiInstances;
using DataView2.Core.Protos;
using DataView2.Engines;
using DataView2.Options;
using DataView2.Resources.Others;
using DataView2.States;
using DataView2.ViewModels;
using DataView2.XAML;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client.Web;
using Microcharts.Maui;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
//using Ionic.Zip;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Storage;
using Microsoft.UI.Windowing;
using MudBlazor.Services;
using ProtoBuf.Grpc.Client;
using Serilog;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System;
using System.Configuration;
using System.Diagnostics;
//using ICSharpCode.SharpZipLib.Core;
//using ICSharpCode.SharpZipLib.Zip;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Devices.Lights;
using Windows.Storage;
using Windows.UI.Notifications;

namespace DataView2
{
    public static class MauiProgram
    {
        private static IHost? _host;
        public static ApplicationEngine? AppEngine { get; private set; }
        public static DataView2.States.ApplicationState? AppState { get; private set; }
        public static IServiceProvider? Services => _host?.Services;

        //Services and version----------------------------------------------------
        private static readonly string targetFolderNames = "DataView2.GrpcService";
        private static readonly string pathInstallFolderServices = "c:\\DataView2\\DataView2Services";
        private static readonly string pathSetUpLogs = "c:\\DataView2\\Logs\\";
        private static string AppVersionKey = "AppDV2Version";
        private static string assemblyVersion = "";
        private static string currentVersion = "";
        private static string versionFile = "c:\\DataView2\\version.json";
        private static string managerserviceDB = ((int)ServiceState.Inactive).ToString();
        private static string sharedDirectoryPath = @"C:\DataView2\DataView2Services\DataView2.GrpcService\";
        private static string mutexName = "Global\\DataView2FileMutex";
        private static FileStorageManager manager = new FileStorageManager(sharedDirectoryPath, mutexName);

        public static MauiApp CreateMauiApp()
        {            
            InitialCleanupVersionFile(versionFile);
            
            var builder = MauiApp.CreateBuilder();
            Log.Information("CreateMauiApp Before Building");

            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMauiCommunityToolkit()
                .UseArcGISToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("font-awesome.ttf", "font-awesome");
                    fonts.AddFont("Lexend.ttf", "Lexend");


                });



            Log.Information("CreateMauiApp After Builder");

            builder.UseArcGISRuntime(config => config.UseApiKey("AAPKe353eaccd84a423ea42e61c7e021d3feTT91bXlkLoX4S4II4IoC0fj9JdvjTQsrKgf_vrLPYQfAD6pfp18HJVZq6V0UbpSQ"));
            builder.Services.AddMauiBlazorWebView();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Services.AddMudServices();
            builder.Services.AddTransientPopup<InitializationPopup, NewProjectViewModel>();
            builder.Services.AddTransientPopup<ProjectPopup, ProjectViewModel>();
            builder.Services.AddTransientPopup<SurveySetPopup, SurveySetViewModel>();
            builder.Services.AddTransientPopup<LayerEditorPopup, LayerViewModel>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#else
            //TEMPORARY:
            builder.Logging.AddDebug();
            
//Configuration Refresh:
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            //BaseAddress = $"http://{builder.Configuration["GrpcSeerviceIP"]}:5104";
#endif
            string strAppConfigStreamName = "DataView2.appsettings.json";
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(strAppConfigStreamName);

            builder.Configuration.AddJsonStream(stream);
            var configuration = builder.Configuration;
            int grpcPort = Tools.GetFreePort(5104);
            string BaseAddress = $"http://{builder.Configuration["GrpcSeerviceIP"]}:{grpcPort}";
            string serviceDbKey = $"serviceDB_{grpcPort}";

            EnsureServiceDbKeyExists(versionFile, serviceDbKey);
            manager.SetVariable(serviceDbKey, ((int)ServiceState.Inactive).ToString());

            builder.Logging.AddSerilog();
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
#if !DEBUG
            ServiceCloseVerificationPerInstance(serviceDbKey);
#endif
            ReviewVersion();
            SetServices();

            #region Services Injection

            builder.Services.AddSingleton<ApplicationEngine>();
            builder.Services.AddSingleton<ApplicationState>();
            builder.Services.AddSingleton<WindowManager>();

            var httpHandler = new GrpcWebHandler(
                new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler())
            );

            var defaultChannelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = null,
                MaxSendMessageSize = null
            };


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ISettingsService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IDatabaseRegistryLocalService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IProjectRegistryService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IProjectService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ISettingTablesService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ICrackClassificationConfiguration>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IGPSProcessedService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPickOutRawService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IBleedingService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ICrackingRawService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IRutProcessedService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IXMLObjectService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IRoadInspectService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IXMLObjectService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ICrackClassification>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ISegmentService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IOutputTemplateService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IOutputColumnTemplateService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IRavellingService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPotholesService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISegmentGridService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPatchService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ICornerBreakService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISpallingService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IConcreteJointService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return new SurveyProcessing.SurveyProcessingClient(channel);
            });

            builder.Services.AddTransient(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IQCFilterService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return new ExportDataService.ExportDataServiceClient(channel);
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return new GenerateReportService.GenerateReportServiceClient(channel);
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ICrackClassificationNodesService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<ICrackClassificationsService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IMapGraphicDataService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, defaultChannelOptions);
                return channel.CreateGrpcService<IBoundariesService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IShapefileService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IDatasetBackupService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IColorCodeInformationService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IFODService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISurveyService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISurveySegmentationService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IMetaTableService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPMS_Data_ReportService>();
            });   
            
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IFOD_Data_ReportService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IAoaDataService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IAuthDataHubService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ICurbDropOffService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IMarkingContourService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISealedCrackService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPumpingService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IMMOService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IMacroTextureService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IRoughnessService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IRumbleStripService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IGeometryService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IINSGeometryService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IShoveService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IGroovesService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISagsBumpsService>();
            });
            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IWaterTrapService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IVideoFrameService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ILanMarkedProcessedService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ILASfileService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ILAS_RuttingService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPCIService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPCIRatingService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISampleUnitService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISampleUnitSetService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ISummaryService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPCIDefectsService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IPASERService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ICrackSummaryService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<ICamera360FrameService>();
            });

            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IOdoDataService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IGPS_RawService>();
            });


            builder.Services.AddSingleton(services =>
            {
                var httpHandler = new GrpcWebHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = null
                });
                return channel.CreateGrpcService<IKeycodeService>();
            });

            builder.Services.AddSingleton<ReportEngine>();

            #endregion

            //builder.Services.AddSingleton(runningProcesses);

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "AAPKe353eaccd84a423ea42e61c7e021d3feTT91bXlkLoX4S4II4IoC0fj9JdvjTQsrKgf_vrLPYQfAD6pfp18HJVZq6V0UbpSQ";


            string licenseKey = "runtimelite,1000,rud9502618226,none,TRB3LNBHPBGP6XCFK232";
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(licenseKey);

            //Closing Action Method:
            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        //use Microsoft.UI.Windowing functions for window
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = AppWindow.GetFromWindowId(id);

                        var title = appWindow.Title;

                        //Before closing the window stop services:
                        appWindow.Closing += async (s, e) =>
                        {
                            if(!appWindow.Title.StartsWith("ROMDAS"))
                            {
                                if(appWindow.Title == "PCI Rating Mode")
                                {
                                    e.Cancel = true;
                                }
                                else
                                {
                                    // If it's a sub-window, allow it to close without any confirmation
                                    e.Cancel = false;
                                }
                            }
                            else
                            {
                                e.Cancel = true;
                                bool result = await App.Current.MainPage.DisplayAlert(
                                    "DataView",
                                    "Are you sure you want to close the application?",
                                    "Yes",
                                    "Cancel");

                                if (result)
                                {
#if !DEBUG
                                await CloseServicesEngineAsync(configuration);
#endif
                                    //For debugging purposes with Multi-Instance:
                                    if (!Core.MultiInstances.SharedDVInstanceStore.DVInstances.IsMainSession())
                                        await CloseServicesEngineAsync(configuration);

                                    AppState.ForceReleaseMapSync();
                                    Core.MultiInstances.SharedDVInstanceStore.RemoveCurrentInstance();
                                    await (App.Current as App)?.CloseAllWindows(configuration);
                                    App.Current.Quit();
                                }
                            }
                        };
                    });
                });

                Microsoft.Maui.Handlers.SwitchHandler.Mapper.AppendToMapping("NoLabel", (handler, view) =>
                {
                    handler.PlatformView.OnContent = null;
                    handler.PlatformView.OffContent = null;
                });
#endif
            });

#if WINDOWS
            builder.Services.AddTransient<IFolderPicker, FolderPickerWindows>();
#endif

            var result = builder.Build();

            AppEngine = result.Services?.GetRequiredService<ApplicationEngine>();

#if !DEBUG
            if (AppEngine is not null)
            {
                AppEngine.ServicesEngine.StartAll((grpcPort.ToString()));
            }
#else
            if (!Core.MultiInstances.SharedDVInstanceStore.DVInstances.IsOnlyInstance())
                AppEngine.ServicesEngine.StartAll(grpcPort.ToString());
#endif

            //For debugging purposes with Multi-Instance:
            //if (!Core.MultiInstances.SharedDVInstanceStore.DVInstances.IsMainSession())
            // AppEngine.ServicesEngine.StartAll(grpcPort.ToString());

            AppState = result.Services?.GetRequiredService<ApplicationState>();

            AppState.GrpcServiceIP = grpcPort.ToString();
            bool serviceGRPC = IsServiceActive();
            
            while (manager.GetVariable(serviceDbKey) != ((int)ServiceState.Active).ToString() || !serviceGRPC)
            {
                Thread.Sleep(3000);                
                managerserviceDB = manager.GetVariable(serviceDbKey);
                serviceGRPC = IsServiceActive();
            }
            Serilog.Log.Information($"Init Session with version:  {assemblyVersion}");
            Serilog.Log.Information($"Services Up before => UI");
            AddSetUpLogEntry(pathSetUpLogs, $"Init Session with version: {assemblyVersion}");
            return result;

        }
        private static void ServiceCloseVerificationPerInstance(string serviceDbKey)
        {
            if (assemblyVersion != currentVersion)
            {
                AppEngine.ServicesEngine.ServiceCloseVerification();
            }
            manager.SetVariable(serviceDbKey, ((int)ServiceState.Inactive).ToString());
            //SetValueVersionFile(versionFile, serviceDbKey, (int)ServiceState.Inactive);
        }
        private static void ReviewVersion()
        {
            if (!Directory.Exists(versionFile)) { Directory.CreateDirectory(Path.GetDirectoryName(versionFile)); }
            assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            currentVersion = GetVersionOrCreate(versionFile);
            if (assemblyVersion != currentVersion)
            {
                if (Directory.Exists(pathInstallFolderServices)) { Directory.Delete(pathInstallFolderServices, true); }
                SetVersion(versionFile, assemblyVersion);
                Log.Logger.Information($"Old Version {currentVersion} - Assembly Version: {assemblyVersion}");
                AddSetUpLogEntry(pathSetUpLogs, $"Old Version {currentVersion} - Assembly Version: {assemblyVersion}");
            }
        }
        private static string GetVersionOrCreate(string fileName)
        {
            string version = "0.0.0";
            var data = new { version = version };
            string jsonString = JsonSerializer.Serialize(data);
            string value = "";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, jsonString);
            }
            else
            {
                value = GetValueVersionFile(fileName, "version");
                if (value == "-1")
                {
                    File.WriteAllText(fileName, jsonString);
                    value = version;
                }
            }

            return value;
        }
        static string GetValueVersionFile(string fileName, string field)
        {
            try
            {
                string json = File.ReadAllText(fileName);

                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty(field, out JsonElement value))
                {
                    return value.ToString();
                }
                else
                {
                    Log.Logger.Error($"The field is not found in the JSON Version file.");
                    return "-1";
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"GetValueVersionFile - Error: {ex.Message}");
                return "-1";
            }
        }
        private static void SetVersion(string fileName, object value)
        { if (File.Exists(fileName)) { SetValueVersionFile(fileName, "version", value); } }
        static void SetValueVersionFile(string fileName, string field, object value)
        {
            try
            {
                string json = File.ReadAllText(fileName);
                Dictionary<string, object> datos = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (datos.ContainsKey(field))
                {
                    datos[field] = value;
                }
                else
                {
                    datos.Add(field, value);
                }

                string jsonString = JsonSerializer.Serialize(datos);

                File.WriteAllText(fileName, jsonString);
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"SetValueVersionFile - Error: {ex.Message}");
            }
        }
        private static async Task CloseServicesEngineAsync(IConfiguration configuration)
        {
            AppEngine.ServicesEngine.StopAll(configuration);
            //AppEngine.ServicesEngine.ServiceCloseVerificationPerInstance();
        }
        private static async void SetServices()
        {
            if (assemblyVersion != currentVersion || !Directory.Exists(pathInstallFolderServices))
            {
                UpdatePsInstaller();
            }
            await SetServicesAsync();
        }
        private static async Task SetServicesAsync()
        {
            try
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var serviceName = "DataView2Services";
                var folderNames = targetFolderNames.Split(',');

                if (assemblyVersion != currentVersion || !Directory.Exists(pathInstallFolderServices))
                {
                    await UnzipFileAsync(Path.Combine(path, $"{serviceName}.zip"), pathInstallFolderServices);
                }

            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.Message, "Error starting Services unzip.");
            }
        }
        public static async Task UnzipFileAsync(string sourceZipPath, string destinationFolderPath)
        {
            try
            {
                // Check if the ZIP file exists
                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                    if (!File.Exists(sourceZipPath))
                    {
                        Log.Logger.Information($"The file {sourceZipPath} does not exist.");
                        return;
                    }
                }


                // Unzip the Services
                await extractToPath(sourceZipPath, pathInstallFolderServices);
                Log.Logger.Information($"The file {sourceZipPath} has been unzipped to {destinationFolderPath}");
                AddSetUpLogEntry(pathSetUpLogs, $"The file {sourceZipPath} has been unzipped. Folder created:  {destinationFolderPath}");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Unexpected error: {ex.Message}");
            }
        }
        public static async Task extractToPath(string sourceZipPath, string destinationFolderPath)
        {
            try
            {
                var targetFolders = targetFolderNames.Split(',');

                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }

                using (ZipArchive archive = ZipFile.OpenRead(sourceZipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string entryFullName = Path.Combine(destinationFolderPath, entry.FullName);
                        string entryDir = Path.GetDirectoryName(entryFullName);

                        if (!Directory.Exists(entryDir))
                        {
                            Directory.CreateDirectory(entryDir);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(entryFullName, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Unexpected error: {ex.Message}");
            }
        }

        private static readonly string logFileNameFormat = "SetUp-DataView2-{0}.log";

        public static void AddSetUpLogEntry(string folderPath, string logEntry)
        {
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Construct the log file name based on the current date
            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            string logFileName = string.Format(logFileNameFormat, currentDate);
            string logFilePath = Path.Combine(folderPath, logFileName);

            // Append the log entry to the file
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now} >>> {logEntry}");
            }
        }
               
        private static bool IsServiceActive()
        {
            string processName = "GrpcService";
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                try
                {
                    string mainModuleFileName = process.ProcessName;

                    if (mainModuleFileName.ToLower().Contains(processName.ToLower()))
                    {
                        Log.Logger.Information($"IsServiceActive - Process {process.ProcessName} is active.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"ID: {process.Id}, Name: {process.ProcessName}");
                    Log.Logger.Error(ex, $"IsServiceActive - Service Loading");
                }
            }
            return false;
        }

        private static void UpdatePsInstaller()
        {
            Task.Run(() =>
            {
                string baseDirectory = AppContext.BaseDirectory;
                string contentFilePath = Path.Combine(baseDirectory, "iniPs1.txt");
                FileUpdater.UpdateIniFile(contentFilePath);
            });
        }


        static void InitialCleanupVersionFile(string fileName)
        {
            if (!File.Exists(fileName))
                return;

            string json = File.ReadAllText(fileName);

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                       ?? new Dictionary<string, object>();

            if (!data.ContainsKey("version"))
                return;

            var cleanData = new Dictionary<string, object>
    {
        { "version", data["version"] }
    };

            File.WriteAllText(fileName, JsonSerializer.Serialize(cleanData));
        }

        static void EnsureServiceDbKeyExists(string fileName, string key)
        {
            try
            {
                Dictionary<string, object> datos;

                if (File.Exists(fileName))
                {
                    string json = File.ReadAllText(fileName);
                    datos = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                            ?? new Dictionary<string, object>();
                }
                else
                {
                    datos = new Dictionary<string, object>();
                }

                if (!datos.ContainsKey(key))
                {
                    datos[key] = ((int)ServiceState.Inactive).ToString();

                    string jsonString = JsonSerializer.Serialize(datos);
                    File.WriteAllText(fileName, jsonString);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"EnsureServiceDbKeyExists - Error: {ex.Message}");
            }
        }

    }
}