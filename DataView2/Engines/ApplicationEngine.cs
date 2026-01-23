 
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Options;
using DataView2.States;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.DTS;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.QC;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.DataHub;
using DataView2.Core.Models.Positioning;
using DataView2.Core.Protos;

namespace DataView2.Engines
{
    public class ApplicationEngine : IDisposable
    {

        public ApplicationEngine(
            IBleedingService bleedingService,
            IPickOutRawService pickOutRawService,
            ICrackingRawService crackingRawService,
            IRutProcessedService rutProcessedService,
            IConfiguration configuration,
            ISettingsService settingsService,
            IGPSProcessedService gpsProcessedService,
            IRoadInspectService roadInspectService,
            IDatabaseRegistryLocalService databaseRegistryService,
            IProjectService localProjectService,
            IProjectRegistryService projectRegistryService,
            ISegmentService segmentService,
            IRavellingService ravellingService,
            IPotholesService potholesService,
            IPatchService patchService,
            IConcreteJointService concreteJointService,
            ICornerBreakService cornerBreakService,
            ISpallingService spallingService,
            ICurbDropOffService curbDropOffService,
            IMarkingContourService markingContourService,
            IPumpingService pumpingService,
            ISealedCrackService sealedCrackService,
            IWaterTrapService waterTrapService,
            IMacroTextureService macroTextureService,
            IMMOService mmoService,
            IRumbleStripService rumbleStripService,
            IRoughnessService roughnessService,
            IShoveService shoveService,
            IGroovesService groovesService,
            ISagsBumpsService sagsBumpsService,
            ILogger<ServicesEngine> serviceManagerLogger,
            IOutputTemplateService outputTemplateService,
            IOutputColumnTemplateService outputColumnTemplateService,
            IXMLObjectService xMLObjectService,
            ICrackClassification crackClassificationService,
            ISettingTablesService settingTablesService,
            IQCFilterService qCFilterService,
            ICrackClassificationNodesService crackClassificationNodesService,
            ICrackClassificationsService crackClassificationsService,
            IMapGraphicDataService mapGraphicDataService,
            IBoundariesService boundariesService,
            IShapefileService shapefileService,
            IColorCodeInformationService colorCodeInformationService,
            IDatasetBackupService datasetBackupService,
            IFODService fODService,
            ISurveyService surveyService,
            ISurveySegmentationService surveySegmentationService,
            IMetaTableService metaTableService,
            IAuthDataHubService authDataHubService,
            IPMS_Data_ReportService pMS_Data_ReportService,
            IAoaDataService aoaDataService,
            IFOD_Data_ReportService fOD_Data_ReportService,
            IGeometryService geometryService,
            IINSGeometryService insGeometryService,
            IVideoFrameService videoFrameService,
            ISegmentGridService segmentGridService,
            ILanMarkedProcessedService lanMarkedProcessedService,
            IPCIService pciService,
            ILASfileService lasfileSerevice,
            ILAS_RuttingService las_RuttingService,
            IPCIRatingService pciRatingService,
            IPCIDefectsService pciDefectsService,
            ISampleUnitSetService sampleUnitSetService,
            ISampleUnitService sampleUnitService,
            ISummaryService summaryService,
            IPASERService paserService,
            ICrackSummaryService crackSummaryService,
            ICamera360FrameService camera360FrameService,
            ExportDataService.ExportDataServiceClient exportDataService,
            IKeycodeService keycodeService
            
            )
        {
            BleedingService = bleedingService;
            PickOutRawService = pickOutRawService;
            CrackingRawService = crackingRawService;
            RutProcessedService = rutProcessedService;
#if DEBUG
            RomdasOptions = configuration.GetSection("DataView2Options_Debug").Get<DataView2Options>()!;
#else
            RomdasOptions = configuration.GetSection("DataView2Options").Get<DataView2Options>()!;
#endif

            ServicesEngine = new(RomdasOptions, serviceManagerLogger);
            SettingsService = settingsService;
            GPSProcessedService = gpsProcessedService;
            RoadInspectService = roadInspectService;
            DatabaseRegistryService = databaseRegistryService;

            LocalProjectService = localProjectService;
            ProjectRegistryService = projectRegistryService;
            SegmentService = segmentService;
            RavellingService = ravellingService;
            PotholesService = potholesService;
            PatchService = patchService;
            ConcreteJointService = concreteJointService;
            CornerBreakService = cornerBreakService;
            SpallingService = spallingService;
            CurbDropOffService = curbDropOffService;
            PumpingService = pumpingService;
            SealedCrackService = sealedCrackService;
            WaterTrapService = waterTrapService;
            MarkingContourService = markingContourService;
            MMOService = mmoService;
            MacroTextureService = macroTextureService;
            RoughnessService = roughnessService;
            RumbleStripService = rumbleStripService;
            ShoveService = shoveService;
            GroovesService = groovesService;
            SagsBumpsService = sagsBumpsService;
            OutputTemplateService = outputTemplateService;
            OutputColumnTemplateService = outputColumnTemplateService;
            XMLObjectService = xMLObjectService;
            CrackClassificationService = crackClassificationService;
            SettingTablesService = settingTablesService;
            QCFilterService = qCFilterService;
            CrackClassificationNodesService = crackClassificationNodesService;
            CrackClassificationsService = crackClassificationsService;
            MapGraphicDataService = mapGraphicDataService;
            BoundariesService = boundariesService;
            ShapefileService = shapefileService;
            ColorCodeInformationService = colorCodeInformationService;
            DatasetBackupService = datasetBackupService;
            FODService = fODService;
            SurveyService = surveyService;
            SurveySegmentationService = surveySegmentationService;
            MetaTableService = metaTableService;
            AuthDataHubService = authDataHubService;
            PMSDataReportService = pMS_Data_ReportService;
            FOD_Data_ReportService = fOD_Data_ReportService;
            AoaDataService = aoaDataService;
            SegmentGridService = segmentGridService;
            GeometryService = geometryService;
            INSGeometryService = insGeometryService;
            VideoFrameService = videoFrameService;
            LASfileService = lasfileSerevice;
            LAS_RuttingService = las_RuttingService;
            PCIService = pciService;
            LanMarkedProcessedService = lanMarkedProcessedService;
            PCIRatingService = pciRatingService;
            PCIDefectsService = pciDefectsService;
            SampleUnitSetService = sampleUnitSetService;
            SampleUnitService = sampleUnitService;
            SummaryService = summaryService;
            PASERService = paserService;
            CrackSummaryService = crackSummaryService;
            Camera360FrameService = camera360FrameService;
            ExportDataService = exportDataService;
            KeycodeService = keycodeService;
        }

        public IBleedingService BleedingService { get; }
        public IPickOutRawService PickOutRawService { get; }
        public ICrackingRawService CrackingRawService { get; }
        public IRutProcessedService RutProcessedService { get; }
        public DataView2Options RomdasOptions { get; }
        public ServicesEngine ServicesEngine { get; }
        public ISettingsService SettingsService { get; }
        public IRoadInspectService RoadInspectService { get; }
        public IGPSProcessedService GPSProcessedService { get; }
        public IDatabaseRegistryLocalService DatabaseRegistryService { get; }
        public IProjectService LocalProjectService { get; }
        public IProjectRegistryService ProjectRegistryService { get; }
        public ISegmentService SegmentService { get; }
        public IRavellingService RavellingService { get; }
        public IPotholesService PotholesService { get; }
        public IPatchService PatchService { get; }  
        public IConcreteJointService ConcreteJointService { get; }
        public ICornerBreakService CornerBreakService { get; }
        public ISpallingService SpallingService { get; }
        public ICurbDropOffService CurbDropOffService { get; }
        public IMarkingContourService MarkingContourService { get; }
        public IPumpingService PumpingService { get; }
        public ISealedCrackService SealedCrackService { get; }
        public IWaterTrapService WaterTrapService { get; }
        public IMMOService MMOService { get; }
        public IMacroTextureService MacroTextureService { get; }
        public IRoughnessService RoughnessService { get; }
        public IRumbleStripService RumbleStripService { get; }
        public IShoveService ShoveService { get; }
        public IGroovesService GroovesService { get; }
        public ISagsBumpsService SagsBumpsService { get; }
        public IPCIService PCIService { get; }
        public IOutputTemplateService OutputTemplateService { get; }
        public IOutputColumnTemplateService OutputColumnTemplateService { get; }
        public IXMLObjectService XMLObjectService { get; }
        public ICrackClassification CrackClassificationService { get; }
        public ISettingTablesService SettingTablesService { get; internal set; }
        public IQCFilterService QCFilterService { get; }
        public ICrackClassificationNodesService CrackClassificationNodesService { get; }
        public ICrackClassificationsService CrackClassificationsService { get; }
        public IMapGraphicDataService MapGraphicDataService { get; }
        public IBoundariesService BoundariesService { get; }
        public IShapefileService ShapefileService { get; }
        public IColorCodeInformationService ColorCodeInformationService { get; }
        public IDatasetBackupService DatasetBackupService { get; }
        public IFODService FODService {  get; }
        public IPASERService PASERService { get; }
        public ICrackSummaryService CrackSummaryService;
        public ISurveyService SurveyService { get; }
        public ISurveySegmentationService SurveySegmentationService { get; }
        public IMetaTableService MetaTableService { get; }
        public IAuthDataHubService AuthDataHubService { get; }
        public IPMS_Data_ReportService PMSDataReportService { get; }
        public IFOD_Data_ReportService FOD_Data_ReportService { get; }
        public IAoaDataService AoaDataService { get; }
        public ISegmentGridService SegmentGridService { get; }
        public IGeometryService GeometryService { get; }
        public IINSGeometryService INSGeometryService { get; }

        public IVideoFrameService VideoFrameService { get; }
        public ILASfileService LASfileService { get; }

        public ILAS_RuttingService LAS_RuttingService { get; }
        public ILanMarkedProcessedService LanMarkedProcessedService { get; }
        public IPCIRatingService PCIRatingService { get; }
        public IPCIDefectsService PCIDefectsService { get; }
        public ISampleUnitSetService SampleUnitSetService { get; }
        public ISampleUnitService SampleUnitService { get; }
        public ISummaryService SummaryService { get; }
        public ICamera360FrameService Camera360FrameService { get; }

        public ExportDataService.ExportDataServiceClient ExportDataService { get; }

        public IKeycodeService KeycodeService { get; }

        public void Dispose()
        {
           // ServicesEngine.StopAll();
            GC.SuppressFinalize(this);
        }
    }
}