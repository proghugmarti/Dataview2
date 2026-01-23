using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Data.Projects;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services;
using DataView2.GrpcService.Services.AppDbServices;
using DataView2.GrpcService.Services.LCMS_Data_Services;
using DataView2.GrpcService.Services.OtherServices;
using DataView2.GrpcService.Services.Setting_Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc.Server;
using Serilog;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Helper;
using DataView2.Core.Models.QC;
using DataView2.Core.Models.Database_Tables;
using System.Text.Json;
using DataView2.Core.Models.Other;
using DataView2.Core;
using DataView2.GrpcService.Services.DataHubServices;
using DataView2.Core.Models.DataHub;
using Azure.Storage.Blobs;
using System.Data.SQLite;
using Microsoft.Data.Sqlite;
using DataView2.Core.Models.Positioning;
using DataView2.GrpcService.Services.Positioning;
using DataView2.GrpcService.Services.ProcessingServices;
using DataView2.GrpcService;


var builder = WebApplication.CreateBuilder(args);
//SharedMemoryManager _manager = new SharedMemoryManager();
//RegistryManager _manager = new RegistryManager();
string sharedDirectoryPath = @"C:\DataView2\DataView2Services";
string mutexName = "Global\\DataView2FileMutex";
string fileName = "c:\\DataView2\\version.json";
//FileStorageManager storageManager = new FileStorageManager(sharedDirectoryPath, mutexName);
//SetValueVersionFile("serviceDB", (int)ServiceState.Inactive);

string envPort = Environment.GetEnvironmentVariable("GRPC_PORT");
int grpcPort = 5104; // default fallback
// Check if a port was passed as argument from MAUI
if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out int port))
{
    grpcPort = port;
}
else
{
    // Optional: fallback to GetFreePort if no argument is passed
    grpcPort = Tools.GetFreePort(grpcPort);
}

string serviceDbKey = $"serviceDB_{grpcPort}";
SetValueVersionFile(serviceDbKey, (int)ServiceState.Inactive);
string BaseAddress = "localhost";

SQLiteLog.Enabled = false;



builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Logging.AddSerilog();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Data.Sqlite", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System.Data.SQLite", Serilog.Events.LogEventLevel.Warning)
    //.Enrich.FromLogContext()
    .CreateLogger();


// Default projects folder creation
CreateDefaultProjectsFolder();

//Configuration



var GrpcSeerviceIP = builder.Configuration["GrpcSeerviceIP"];
builder.Services.AddSingleton<DatabasePathProvider>(provider =>
{
    // Obtain the initial path from the configuration
    var initialPath = builder.Configuration.GetConnectionString("AppDbContextProjectDataConnection");
    var initialMetadataPath = builder.Configuration.GetConnectionString("AppDbContextMetadatalocalConnection");
    return new DatabasePathProvider(initialPath, initialMetadataPath, initialPath);
});



//Database setup for Metadata 
builder.Services.AddDbContextFactory<AppDbContextMetadata>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("AppDbContextMetadataConnection")));


builder.Services.AddDbContextFactory<AppDbContextMetadataLocal>((serviceProvider, options) =>
{
    var pathProvider = serviceProvider.GetRequiredService<DatabasePathProvider>();
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContextMetadatalocalConnection"));
    //options.UseSqlite($"Data Source={pathProvider.GetDatabasePath()}");
    //.LogTo(Console.WriteLine, LogLevel.Error)
    //.EnableSensitiveDataLogging();
});

//Database setup for initial Project 
builder.Services.AddDbContextFactory<AppDbContextProjectData>((serviceProvider, options) =>
{
    var pathProvider = serviceProvider.GetRequiredService<DatabasePathProvider>();
    options.UseSqlite($"Data Source={pathProvider.GetDatabasePath()}");
});


//gRPC services
builder.Services.AddCodeFirstGrpc(config =>
{
    // Ejemplo de configuración:
    config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
    config.MaxReceiveMessageSize = null;          // . ej. 50 * 1024 * 1024
    config.MaxSendMessageSize = null;
});




ConfigureGrpcServices(builder.Services);



//GrpcWeb
builder.Services.AddGrpc(options =>
{
    // 50 MB)
    options.MaxReceiveMessageSize = null;
    options.MaxSendMessageSize = null;
});


//For datahub 
builder.Services.AddHttpClient<PMS_Data_ReportService>();
builder.Services.AddSingleton<TokenHandler>(); //authentication
builder.Services.AddSingleton(x =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
    return new BlobServiceClient(connectionString);
});




var app = builder.Build();

//app.Lifetime.ApplicationStopping.Register(() => OnApplicationStopping());
app.Lifetime.ApplicationStopping.Register(() => OnApplicationStopping());



//Database initialization and migration 


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var contextMetadata = services.GetRequiredService<AppDbContextMetadata>();
    try
    {
        Log.Information("Starting database migration for metadata...");
        contextMetadata.Database.Migrate();
        Log.Information("Database migration for metadata completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error($"Error during metadata migration: {ex}");
        Log.Error($"Failed SQL command: {ex.InnerException?.Message}");
    }

    Log.Information("Attempting to establish a connection to the metadata database...");
    contextMetadata.Database.OpenConnection();
    contextMetadata.Database.CloseConnection();
    Log.Information("Connection to the metadata database successful.");  

    var contextProject = services.GetRequiredService<AppDbContextProjectData>();
    try
    {
        Log.Information("Starting database migration for project data...");
        contextProject.Database.Migrate();
        Log.Information("Database migration for project data completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error($"Error during projectdb migration: {ex}");
        Log.Error($"Failed SQL command: {ex.InnerException?.Message}");
    }

    Log.Information("Attempting to establish a connection to the project data database...");
    contextProject.Database.OpenConnection();
    contextProject.Database.CloseConnection();
    Log.Information("Connection to the project data database successful.");

}

// Configure the HTTP request pipeline.
app.MapGrpcService<SettingsService>();
app.MapGrpcService<SettingTablesService>();
//app.MapGrpcService<DataBaseRegistryService>();
app.MapGrpcService<ProjectRegistryService>();

app.MapGrpcService<ProjectService>();
app.MapGrpcService<DataBaseRegistryLocalService>();

app.MapGrpcService<GeneralService>();
app.MapGrpcService<XMLObjectService>();
app.MapGrpcService<CrackClassificationConfigService>();
app.MapGrpcService<GPSProcessedService>();
app.MapGrpcService<CrackClassificationService>();
//app.MapGrpcService<RoadInspectService>();

//LCMS Service
app.MapGrpcService<PickOutRawService>();
app.MapGrpcService<BleedingService>();
app.MapGrpcService<WaterTrapService>();
app.MapGrpcService<RutProcessedService>();
app.MapGrpcService<CrackingRawService>();
app.MapGrpcService<SegmentService>();
app.MapGrpcService<RavellingRawService>();
app.MapGrpcService<PotholesService>();
app.MapGrpcService<PatchService>();
app.MapGrpcService<CornerBreakService>();
app.MapGrpcService<SpallingRawService>();
app.MapGrpcService<ConcreteJointService>();
app.MapGrpcService<CurbDropOffService>();
app.MapGrpcService<MarkingContourService>();
app.MapGrpcService<PumpingService>();
app.MapGrpcService<SealedCrackService>();
app.MapGrpcService<MMOService>();
app.MapGrpcService<MacroTextureService>();
app.MapGrpcService<RoughnessService>();
app.MapGrpcService<RumbleStripService>();
app.MapGrpcService<ShoveService>();
app.MapGrpcService<GroovesService>();
app.MapGrpcService<SagsBumpsService>();
app.MapGrpcService<GeometryService>();
app.MapGrpcService<INSGeometryService>();
app.MapGrpcService<PCIService>();
app.MapGrpcService<PASERService>();
app.MapGrpcService<CrackSummaryService>();

app.MapGrpcService<OutputTemplateService>();
app.MapGrpcService<OutputColumnTemplateService>();
app.MapGrpcService<QCFilterService>();
app.MapGrpcService<ExportDataService>();
app.MapGrpcService<GenerateReportService>();
app.MapGrpcService<CrackClassificationsService>();
app.MapGrpcService<CrackClassificationNodesService>();
app.MapGrpcService<MapGraphicDataService>();
app.MapGrpcService<BoundariesService>();
app.MapGrpcService<ShapefileService>();
app.MapGrpcService<ColorCodeInformationService>();
app.MapGrpcService<DatasetBackupService>();
app.MapGrpcService<FODService>();
app.MapGrpcService<SurveyService>();
app.MapGrpcService<SurveySegmentationService>();
app.MapGrpcService<SegmentGridService>();
app.MapGrpcService<MetaTableService>();
app.MapGrpcService<VideoFrameService>();
app.MapGrpcService<LaneMarkedProcessedService>();
//app.MapGrpcService<GreeterService>();
app.MapGrpcService<LASfileService>();
app.MapGrpcService<LAS_RuttingService>();
app.MapGrpcService<PCIRatingService>();
app.MapGrpcService<SampleUnitSetService>();
app.MapGrpcService<SampleUnitService>();
app.MapGrpcService<PCIDefectsService>();
app.MapGrpcService<SummaryService>();
app.MapGrpcService<Camera360FrameService>();

//Datahub
app.MapGrpcService<AuthDataHubService>();
app.MapGrpcService<PMS_Data_ReportService>();
app.MapGrpcService<FOD_Data_ReportService>();
app.MapGrpcService<AoaDataService>();
app.MapGrpcService<BlobStorageService>();


//Positioning
app.MapGrpcService<OdoDataService>();
app.MapGrpcService<GPS_RawService>();


app.MapGrpcService<KeyCodeService>();
var stateService = app.Services.GetRequiredService<ProcessingStateService>();
ProcessingServiceManager.SetStateService(stateService);


app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
//SetValueVersionFile("serviceDB", (int)ServiceState.Active);
SetValueVersionFile(serviceDbKey, (int)ServiceState.Active);
#if DEBUG

#else
//Configuration Refresh:
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
BaseAddress = $"{GrpcSeerviceIP}";
#endif
try
{
    //app.Run($"http://{BaseAddress}:5104");
    SetValueVersionFile(serviceDbKey, (int)ServiceState.Active);

    Log.Information($"[gRPC Service] Running in {BaseAddress}:{grpcPort}");
    app.Run($"http://{BaseAddress}:{grpcPort}");
}
catch (Exception ex)
{
    Log.Error($"GrpcService error going through http://{BaseAddress}:{grpcPort}  : {ex.Message}");
}




//  method to create the default projects folder
void CreateDefaultProjectsFolder()
{
    string documentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string defaultProjectsFolderName = "Data View 2 Projects";
    string defaultProjectsFolderPath = Path.Combine(documentsFolderPath, defaultProjectsFolderName);

    if (!Directory.Exists(defaultProjectsFolderPath))
    {
        Directory.CreateDirectory(defaultProjectsFolderPath);
    }

    //offline map folder
    string offlineMapsFolderName = "OfflineMap";
    string offlineMapsFolderPath = Path.Combine(defaultProjectsFolderPath, offlineMapsFolderName);
    if (!Directory.Exists(offlineMapsFolderPath))
    {
        Directory.CreateDirectory(offlineMapsFolderPath);
    }
}

//void CreateOfflineMapsFolder()
//{
//    string documentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
//    string offlineMapsFolderName = "OfflineMap";
//    string offlineMapsFolderPath = Path.Combine(documentsFolderPath, offlineMapsFolderName);

//    if (!Directory.Exists(offlineMapsFolderPath))
//    {
//        Directory.CreateDirectory(offlineMapsFolderPath);
//    }
//}

//method to configure gRPC services
void ConfigureGrpcServices(IServiceCollection services)
{

    // Register IDatabaseRegistryService
    //services.AddScoped<IDatabaseRegistryService, DataBaseRegistryService>();
    //services.AddScoped<IRepository<DatabaseRegistry>, Repository<DatabaseRegistry, AppDbContextMetadata>>();

    services.AddScoped<IProjectRegistryService, ProjectRegistryService>();
    services.AddScoped<IRepository<ProjectRegistry>, Repository<ProjectRegistry, AppDbContextMetadata>>();

    services.AddScoped<IRepository<GeneralSetting>, Repository<GeneralSetting, AppDbContextMetadata>>();
    services.AddScoped<ISettingsService, SettingsService>();

    services.AddScoped<IProjectService, ProjectService>();
    services.AddScoped<IRepository<Project>, Repository<Project, AppDbContextMetadataLocal>>();

    services.AddScoped<IDatabaseRegistryLocalService, DataBaseRegistryLocalService>();
    services.AddScoped<IRepository<DatabaseRegistryLocal>, Repository<DatabaseRegistryLocal, AppDbContextMetadataLocal>>();



    // Register IRepository for CrackClassificationConfiguration
    services.AddScoped<IRepository<CrackClassificationConfiguration>, Repository<CrackClassificationConfiguration, AppDbContextMetadata>>();
    services.AddScoped<ICrackClassificationConfiguration, CrackClassificationConfigService>();

    services.AddScoped<IRepository<SummaryCrackClasification>, Repository<SummaryCrackClasification, AppDbContextProjectData>>();

    services.AddScoped<GeneralService>();
    services.AddTransient<XmlParsingService>();

    services.AddScoped<SettingsService>();

    services.AddScoped<CrackClassificationService>();

    //LCMS Services
    services.AddScoped<IRepository<LCMS_PickOuts_Raw>, Repository<LCMS_PickOuts_Raw, AppDbContextProjectData>>();
    services.AddTransient<PickOutRawService>();

    services.AddScoped<IRepository<LCMS_Cracking_Raw>, Repository<LCMS_Cracking_Raw, AppDbContextProjectData>>();
    services.AddTransient<CrackingRawService>();

    services.AddScoped<IRepository<LCMS_Segment>, Repository<LCMS_Segment, AppDbContextProjectData>>();
    services.AddTransient<SegmentService>();

    services.AddScoped<IRepository<GPS_Processed>, Repository<GPS_Processed, AppDbContextProjectData>>();
    services.AddTransient<GPSProcessedService>();

    services.AddScoped<IRepository<LCMS_Bleeding>, Repository<LCMS_Bleeding, AppDbContextProjectData>>();
    services.AddTransient<BleedingService>();

    services.AddScoped<IRepository<LCMS_Water_Entrapment>, Repository<LCMS_Water_Entrapment, AppDbContextProjectData>>();
    services.AddTransient<WaterTrapService>();

    services.AddScoped<IRepository<LCMS_Geometry_Processed>, Repository<LCMS_Geometry_Processed, AppDbContextProjectData>>();
    services.AddTransient<GeometryService>();

    services.AddScoped<IRepository<Geometry_Processed>, Repository<Geometry_Processed, AppDbContextProjectData>>();
    services.AddScoped<IINSGeometryService, INSGeometryService>();

    services.AddScoped<IRepository<LCMS_Rut_Processed>, Repository<LCMS_Rut_Processed, AppDbContextProjectData>>();
    services.AddTransient<RutProcessedService>();

    services.AddScoped<IRepository<LCMS_Ravelling_Raw>, Repository<LCMS_Ravelling_Raw, AppDbContextProjectData>>();
    services.AddTransient<RavellingRawService>();

    services.AddScoped<IRepository<LCMS_Potholes_Processed>, Repository<LCMS_Potholes_Processed, AppDbContextProjectData>>();
    services.AddTransient<PotholesService>();

    services.AddScoped<IRepository<LCMS_Patch_Processed>, Repository<LCMS_Patch_Processed, AppDbContextProjectData>>();
    services.AddTransient<PatchService>();

    services.AddScoped<IRepository<LCMS_Corner_Break>, Repository<LCMS_Corner_Break, AppDbContextProjectData>>();
    services.AddTransient<CornerBreakService>();

    services.AddScoped<IRepository<LCMS_Spalling_Raw>, Repository<LCMS_Spalling_Raw, AppDbContextProjectData>>();
    services.AddTransient<SpallingRawService>();

    services.AddScoped<IRepository<LCMS_Concrete_Joints>, Repository<LCMS_Concrete_Joints, AppDbContextProjectData>>();
    services.AddTransient<ConcreteJointService>();

    services.AddScoped<IRepository<LCMS_Curb_DropOff>, Repository<LCMS_Curb_DropOff, AppDbContextProjectData>>();
    services.AddTransient<CurbDropOffService>();

    services.AddScoped<IRepository<LCMS_Marking_Contour>, Repository<LCMS_Marking_Contour, AppDbContextProjectData>>();
    services.AddTransient<MarkingContourService>();

    services.AddScoped<IRepository<LCMS_Pumping_Processed>, Repository<LCMS_Pumping_Processed, AppDbContextProjectData>>();
    services.AddTransient<PumpingService>();

    services.AddScoped<IRepository<LCMS_Sealed_Cracks>, Repository<LCMS_Sealed_Cracks, AppDbContextProjectData>>();
    services.AddTransient<SealedCrackService>();

    services.AddScoped<IRepository<LCMS_MMO_Processed>, Repository<LCMS_MMO_Processed, AppDbContextProjectData>>();
    services.AddTransient<MMOService>();

    services.AddScoped<IRepository<LCMS_Texture_Processed>, Repository<LCMS_Texture_Processed, AppDbContextProjectData>>();
    services.AddTransient<MacroTextureService>();

    services.AddScoped<IRepository<LCMS_Rough_Processed>, Repository<LCMS_Rough_Processed, AppDbContextProjectData>>();
    services.AddTransient<RoughnessService>();

    services.AddScoped<IRepository<LCMS_Rumble_Strip>, Repository<LCMS_Rumble_Strip, AppDbContextProjectData>>();
    services.AddTransient<RumbleStripService>();

    services.AddScoped<IRepository<LCMS_Shove_Processed>, Repository<LCMS_Shove_Processed, AppDbContextProjectData>>();
    services.AddTransient<ShoveService>();

    services.AddScoped<IRepository<LCMS_Segment_Grid>, Repository<LCMS_Segment_Grid, AppDbContextProjectData>>();
    services.AddTransient<SegmentGridService>();

    services.AddScoped<IRepository<LCMS_Grooves>, Repository<LCMS_Grooves, AppDbContextProjectData>>();
    services.AddTransient<GroovesService>();

    services.AddScoped<IRepository<LCMS_Sags_Bumps>, Repository<LCMS_Sags_Bumps, AppDbContextProjectData>>();
    services.AddTransient<SagsBumpsService>();

    services.AddScoped<IRepository<LCMS_PCI>, Repository<LCMS_PCI, AppDbContextProjectData>>();
    services.AddTransient<PCIService>();

    services.AddScoped<IRepository<LCMS_PASER>, Repository<LCMS_PASER, AppDbContextProjectData>>();
    services.AddTransient<PASERService>();

    services.AddScoped<IRepository<LCMS_CrackSummary>, Repository<LCMS_CrackSummary, AppDbContextProjectData>>();
    services.AddTransient<CrackSummaryService>();

    services.AddScoped<IRepository<LASfile>, Repository<LASfile, AppDbContextProjectData>>();
    services.AddScoped<IRepository<LASPoint>, Repository<LASPoint, AppDbContextProjectData>>();
    services.AddTransient<LASfileService>();

    services.AddScoped<IRepository<LAS_Rutting>, Repository<LAS_Rutting, AppDbContextProjectData>>();
    services.AddTransient<LAS_RuttingService>();


    services.AddScoped<IRepository<OutputTemplate>, Repository<OutputTemplate, AppDbContextProjectData>>();
    //services.AddTransient<OutputTemplateService>();

    services.AddScoped<IRepository<OutputColumnTemplate>, Repository<OutputColumnTemplate, AppDbContextProjectData>>();
    //services.AddTransient<OutputColumnTemplateService>();

    //QC
    services.AddScoped<IRepository<QCFilter>, Repository<QCFilter, AppDbContextProjectData>>();
    services.AddTransient<QCFilterService>();

    services.AddSingleton<ProcessingStateService>();

    services.AddTransient<ExportDataService>();

    services.AddScoped<IRepository<CrackClassifications>, Repository<CrackClassifications, AppDbContextProjectData>>();
    services.AddTransient<CrackClassificationsService>();

    services.AddScoped<IRepository<CrackClassificationNodes>, Repository<CrackClassificationNodes, AppDbContextProjectData>>();
    services.AddTransient<CrackClassificationNodesService>();

    //Others
    services.AddScoped<IRepository<MapGraphicData>, Repository<MapGraphicData, AppDbContextMetadata>>();
    services.AddTransient<MapGraphicDataService>();

    services.AddScoped<IRepository<Boundary>, Repository<Boundary, AppDbContextProjectData>>();
    services.AddTransient<BoundariesService>();

    services.AddScoped<IRepository<Shapefile>, Repository<Shapefile, AppDbContextProjectData>>();
    services.AddTransient<ShapefileService>();

    services.AddScoped<IRepository<ColorCodeInformation>, Repository<ColorCodeInformation, AppDbContextMetadata>>();
    services.AddTransient<ColorCodeInformationService>();

    services.AddScoped<IRepository<DatasetBackup>, Repository<DatasetBackup, AppDbContextMetadataLocal>>();
    services.AddTransient<DatasetBackupService>();

    services.AddScoped<IRepository<LCMS_FOD>, Repository<LCMS_FOD, AppDbContextProjectData>>();
    services.AddTransient<FODService>();

    services.AddScoped<IRepository<Survey>, Repository<Survey, AppDbContextProjectData>>();
    services.AddTransient<SurveyService>();

    services.AddScoped<IRepository<SurveySegmentation>, Repository<SurveySegmentation, AppDbContextProjectData>>();
    services.AddTransient<SurveySegmentationService>();

    services.AddScoped<IRepository<MetaTableValue>, Repository<MetaTableValue, AppDbContextProjectData>>();
    services.AddTransient<MetaTableService>();

    services.AddScoped<IRepository<VideoFrame>, Repository<VideoFrame, AppDbContextProjectData>>();
    services.AddTransient<VideoFrameService>();

    services.AddScoped<IRepository<LCMS_Lane_Mark_Processed>, Repository<LCMS_Lane_Mark_Processed, AppDbContextProjectData>>();
    //services.AddTransient<LaneMarkedProcessedService>();


    //Datahub services
    services.AddScoped<IRepository<PMS_Data_Report>, Repository<PMS_Data_Report, AppDbContextProjectData>>();
    services.AddTransient<PMS_Data_ReportService>();

    services.AddScoped<IRepository<FOD_Data_Report>, Repository<FOD_Data_Report, AppDbContextProjectData>>();
    services.AddTransient<FOD_Data_ReportService>();

    services.AddScoped<IRepository<AoaData>, Repository<AoaData, AppDbContextProjectData>>();
    services.AddTransient<AoaDataService>();

    services.AddHttpClient<IAuthDataHubService, AuthDataHubService>();
    services.AddScoped<IAuthDataHubService, AuthDataHubService>();

    services.AddScoped<IBlobStorageService, BlobStorageService>();

    //POSITIONING

    services.AddScoped<IRepository<OdoData>, Repository<OdoData, AppDbContextProjectData>>();
    services.AddTransient<OdoDataService>();

    services.AddScoped<IRepository<GPS_Raw>, Repository<GPS_Raw, AppDbContextProjectData>>();
    services.AddTransient<GPS_RawService>();

    services.AddScoped<IRepository<Geometry_Processed>, Repository<Geometry_Processed, AppDbContextProjectData>>();
    services.AddTransient<INSGeometryService>();


    //OTHERS
    services.AddScoped<ImageBandInfoService>();

   

    services.AddTransient<CrackClassificationConfigService>();

    services.AddScoped<IRepository<PCIRatings>, Repository<PCIRatings, AppDbContextProjectData>>();
    services.AddTransient<PCIRatingService>();

    services.AddScoped<IRepository<PCIDefects>, Repository<PCIDefects, AppDbContextProjectData>>();
    services.AddTransient<PCIDefectsService>();

    services.AddScoped<IRepository<SampleUnit>, Repository<SampleUnit, AppDbContextProjectData>>();
    services.AddTransient<SampleUnitService>();

    services.AddScoped<IRepository<SampleUnit_Set>, Repository<SampleUnit_Set, AppDbContextProjectData>>();
    services.AddTransient<SampleUnitSetService>();

    services.AddScoped<IRepository<Summary>, Repository<Summary, AppDbContextProjectData>>();
    services.AddTransient<SummaryService>();

    services.AddScoped<IRepository<Camera360Frame>, Repository<Camera360Frame, AppDbContextProjectData>>();
    services.AddTransient<Camera360FrameService>();

    services.AddScoped<IRepository<Keycode>, Repository<Keycode, AppDbContextProjectData>>();
    services.AddTransient<KeyCodeService>();
}
void OnApplicationStopping()
{
    //storageManager.SetVariable("serviceDB", ((int)ServiceState.Inactive).ToString());
    SetValueVersionFile(serviceDbKey, ((int)ServiceState.Inactive).ToString());
    Log.Information("Application is stopping. SetVariable called.");
}

static void SetValueVersionFile(string field, object value)
{
    string fileName = "c:\\DataView2\\version.json";
    string sharedDirectoryPath = @"C:\DataView2\DataView2Services\DataView2.GrpcService\";
    string mutexName = "Global\\DataView2FileMutex";
    FileStorageManager manager = new FileStorageManager(sharedDirectoryPath, mutexName);


    try
    {
        string json = File.ReadAllText(fileName);
        Dictionary<string, object> datos = JsonSerializer.Deserialize<Dictionary<string, object>>(json);              

        manager.SetVariable(field, value.ToString());

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