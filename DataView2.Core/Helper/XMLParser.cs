using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.Core.Helper
{
    public class XMLParser
    {
        #region XML objects
        [XmlRoot(ElementName = "SensorEnable")]
        public class SensorEnable
        {
            [XmlElement(ElementName = "Left")]
            public string Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public string Right { get; set; }
        }

        [XmlRoot(ElementName = "SurveyInfo")]
        public class SurveyInfo
        {
            [XmlElement(ElementName = "SurveyID")]
            public string SurveyID { get; set; }
            [XmlElement(ElementName = "SurveyPath")]
            public string SurveyPath { get; set; }
            [XmlElement(ElementName = "SensorEnable")]
            public SensorEnable SensorEnable { get; set; }
            [XmlElement(ElementName = "FirstTimeStamp_s")]
            public string FirstTimeStamp_s { get; set; }
            [XmlElement(ElementName = "LastTimeStamp_s")]
            public string LastTimeStamp_s { get; set; }
            [XmlElement(ElementName = "SectionLength_m")]
            public string SectionLength_m { get; set; }
            [XmlElement(ElementName = "SectionNbProfiles")]
            public string SectionNbProfiles { get; set; }
            [XmlElement(ElementName = "TotalNbSections")]
            public string TotalNbSections { get; set; }
            [XmlElement(ElementName = "TotalLength_m")]
            public string TotalLength_m { get; set; }
            [XmlElement(ElementName = "MeanSpeed_kmh")]
            public string MeanSpeed_kmh { get; set; }
            [XmlElement(ElementName = "NbValidSections")]
            public string NbValidSections { get; set; }
        }

        [XmlRoot(ElementName = "RoadSectionInfo")]
        public class RoadSectionInfo
        {
            [XmlElement(ElementName = "SurveyID")]
            public string SurveyID { get; set; }
            [XmlElement(ElementName = "SectionID")]
            public string SectionID { get; set; }
            [XmlElement(ElementName = "DistanceBegin_m")]
            public string DistanceBegin_m { get; set; }
            [XmlElement(ElementName = "DistanceEnd_m")]
            public string DistanceEnd_m { get; set; }
            [XmlElement(ElementName = "TimeBegin_s")]
            public string TimeBegin_s { get; set; }
            [XmlElement(ElementName = "TimeEnd_s")]
            public string TimeEnd_s { get; set; }
            [XmlElement(ElementName = "NbProfiles")]
            public string NbProfiles { get; set; }
            [XmlElement(ElementName = "SectionLength_m")]
            public string SectionLength_m { get; set; }
            [XmlElement(ElementName = "Speed_kmh")]
            public string Speed_kmh { get; set; }
            [XmlElement(ElementName = "AcquisitionResolution_mm")]
            public string AcquisitionResolution_mm { get; set; }
            [XmlElement(ElementName = "PercentageOutOfRangeData")]
            public string PercentageOutOfRangeData { get; set; }
            [XmlElement(ElementName = "PercentageTooDarkPixelData")]
            public string PercentageTooDarkPixelData { get; set; }
        }

        [XmlRoot(ElementName = "CrackingModule_Parameters")]
        public class CrackingModule_Parameters
        {
            [XmlElement(ElementName = "CrackingModule_AutomaticPeakDetection")]
            public string CrackingModule_AutomaticPeakDetection { get; set; }
            [XmlElement(ElementName = "CrackingModule_UserDefinedPeakDetectionThresL_mm")]
            public string CrackingModule_UserDefinedPeakDetectionThresL_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_UserDefinedPeakDetectionThresR_mm")]
            public string CrackingModule_UserDefinedPeakDetectionThresR_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_AutoPavementTypeDetection")]
            public string CrackingModule_AutoPavementTypeDetection { get; set; }
            [XmlElement(ElementName = "CrackingModule_EnablePavementTransition")]
            public string CrackingModule_EnablePavementTransition { get; set; }
            [XmlElement(ElementName = "CrackingModule_PaveTransitionMinScore")]
            public string CrackingModule_PaveTransitionMinScore { get; set; }
            [XmlElement(ElementName = "CrackingModule_UserDefinedPavementType")]
            public string CrackingModule_UserDefinedPavementType { get; set; }
            [XmlElement(ElementName = "CrackingModule_EdgeCrackingEnable")]
            public string CrackingModule_EdgeCrackingEnable { get; set; }
            [XmlElement(ElementName = "LCMS2_CrackingModule_MinCrackDepth_mm")]
            public string LCMS2_CrackingModule_MinCrackDepth_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_MinCrackDepthConcrete_mm")]
            public string CrackingModule_MinCrackDepthConcrete_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_MinCrackDepthTinning_mm")]
            public string CrackingModule_MinCrackDepthTinning_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_MinCrackLength_mm")]
            public string CrackingModule_MinCrackLength_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_MinCrackLengthConcrete_mm")]
            public string CrackingModule_MinCrackLengthConcrete_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_ReportDetailedCrkStats")]
            public string CrackingModule_ReportDetailedCrkStats { get; set; }
            [XmlElement(ElementName = "CrackingModule_EnableCrackFaulting")]
            public string CrackingModule_EnableCrackFaulting { get; set; }
            [XmlElement(ElementName = "CrackingModule_ComputeCrackingIndex")]
            public string CrackingModule_ComputeCrackingIndex { get; set; }
            [XmlElement(ElementName = "CrackingModule_CrackFaultingDistanceApart_mm")]
            public string CrackingModule_CrackFaultingDistanceApart_mm { get; set; }
            [XmlElement(ElementName = "CrackingModule_ConcreteCrackwithAI")]
            public string CrackingModule_ConcreteCrackwithAI { get; set; }
        }

        [XmlRoot(ElementName = "MarkingContourModule_Parameters")]
        public class MarkingContourModule_Parameters
        {
            [XmlElement(ElementName = "MarkingContourModule_Enable")]
            public string MarkingContourModule_Enable { get; set; }
            [XmlElement(ElementName = "MarkingContourModule_ExcludeCracksOnMarking")]
            public string MarkingContourModule_ExcludeCracksOnMarking { get; set; }
            [XmlElement(ElementName = "MarkingContourModule_IntMin")]
            public string MarkingContourModule_IntMin { get; set; }
            [XmlElement(ElementName = "MarkingContourModule_MinArea_m2")]
            public string MarkingContourModule_MinArea_m2 { get; set; }
            [XmlElement(ElementName = "MarkingContourModule_MaxArea_m2")]
            public string MarkingContourModule_MaxArea_m2 { get; set; }
        }

        [XmlRoot(ElementName = "LaneMarkCharacterization_Parameters")]
        public class LaneMarkCharacterization_Parameters
        {
            [XmlElement(ElementName = "LaneMarkCharacterization_Enable")]
            public string LaneMarkCharacterization_Enable { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_MaxInMemNumber")]
            public string LaneMarkCharacterization_MaxInMemNumber { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_MaxLaneMarkWidth_mm")]
            public string LaneMarkCharacterization_MaxLaneMarkWidth_mm { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_DefinitionModel")]
            public string LaneMarkCharacterization_DefinitionModel { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_LaneMarkTypeCount")]
            public string LaneMarkCharacterization_LaneMarkTypeCount { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_LaneMarkName")]
            public string LaneMarkCharacterization_LaneMarkName { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_LaneMarkLength_mm")]
            public string LaneMarkCharacterization_LaneMarkLength_mm { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_LaneMarkSpace_mm")]
            public string LaneMarkCharacterization_LaneMarkSpace_mm { get; set; }
        }

        [XmlRoot(ElementName = "PotholeModule_Parameters")]
        public class PotholeModule_Parameters
        {
            [XmlElement(ElementName = "PotholeModule_MinWidth_mm")]
            public string PotholeModule_MinWidth_mm { get; set; }
            [XmlElement(ElementName = "PotholeModule_MaxWidth_mm")]
            public string PotholeModule_MaxWidth_mm { get; set; }
            [XmlElement(ElementName = "PotholeModule_MinAvgDepth_mm")]
            public string PotholeModule_MinAvgDepth_mm { get; set; }
            [XmlElement(ElementName = "PotholeModule_MinArea_m2")]
            public string PotholeModule_MinArea_m2 { get; set; }
            [XmlElement(ElementName = "PotholeModule_MaxAvgInt")]
            public string PotholeModule_MaxAvgInt { get; set; }
            [XmlElement(ElementName = "PotholeModule_MinMinorAxisDiameter_mm")]
            public string PotholeModule_MinMinorAxisDiameter_mm { get; set; }
            [XmlElement(ElementName = "PotholeModule_MaxRatioMaxMinAxes")]
            public string PotholeModule_MaxRatioMaxMinAxes { get; set; }
            [XmlElement(ElementName = "PotholeModule_DetectOutsideLaneLimits")]
            public string PotholeModule_DetectOutsideLaneLimits { get; set; }
        }

        [XmlRoot(ElementName = "PickoutModule_Parameters")]
        public class PickoutModule_Parameters
        {
            [XmlElement(ElementName = "PickOutModule_MinPickOutArea_cm2")]
            public string PickOutModule_MinPickOutArea_cm2 { get; set; }
            [XmlElement(ElementName = "PickOutModule_MaxPickOutArea_cm2")]
            public string PickOutModule_MaxPickOutArea_cm2 { get; set; }
            [XmlElement(ElementName = "PickOutModule_SavePerimeter")]
            public string PickOutModule_SavePerimeter { get; set; }
        }

        [XmlRoot(ElementName = "RavelingModule_Parameters")]
        public class RavelingModule_Parameters
        {
            [XmlElement(ElementName = "RavelingModule_Threshold_cm3_m2")]
            public string RavelingModule_Threshold_cm3_m2 { get; set; }
            [XmlElement(ElementName = "RavelingModule_Zone_Width_mm")]
            public string RavelingModule_Zone_Width_mm { get; set; }
            [XmlElement(ElementName = "RavelingModule_Zone_Height_mm")]
            public string RavelingModule_Zone_Height_mm { get; set; }
            [XmlElement(ElementName = "RavelingModule_ExcludeCracksOnRav")]
            public string RavelingModule_ExcludeCracksOnRav { get; set; }
            [XmlElement(ElementName = "RavelingModule_ChipSeal")]
            public string RavelingModule_ChipSeal { get; set; }
            [XmlElement(ElementName = "RavelingModule_DisableOnConcrete")]
            public string RavelingModule_DisableOnConcrete { get; set; }
            [XmlElement(ElementName = "RavelingModule_AutoPeakThresholdMode")]
            public string RavelingModule_AutoPeakThresholdMode { get; set; }
            [XmlElement(ElementName = "RavelingModule_Peak_Threshold")]
            public string RavelingModule_Peak_Threshold { get; set; }
            [XmlElement(ElementName = "RavelingModule_EnableFullWidth")]
            public string RavelingModule_EnableFullWidth { get; set; }
            [XmlElement(ElementName = "RavelingModlue_RavelingCoinAlgorithmEnable")]
            public string RavelingModlue_RavelingCoinAlgorithmEnable { get; set; }
        }

        [XmlRoot(ElementName = "JointModule_Parameters")]
        public class JointModule_Parameters
        {
            [XmlElement(ElementName = "JointModule_EvalPositionDistance_mm")]
            public string JointModule_EvalPositionDistance_mm { get; set; }
            [XmlElement(ElementName = "JointModule_AveragingWindowWidth_mm")]
            public string JointModule_AveragingWindowWidth_mm { get; set; }
            [XmlElement(ElementName = "JointModule_MeasurementDistance_mm")]
            public string JointModule_MeasurementDistance_mm { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinLength_mm_Trans")]
            public string JointDetectorModule_MinLength_mm_Trans { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinLength_mm_Longi")]
            public string JointDetectorModule_MinLength_mm_Longi { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MaxJointAngle_degree_Trans")]
            public string JointDetectorModule_MaxJointAngle_degree_Trans { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinJointAngle_degree_Trans")]
            public string JointDetectorModule_MinJointAngle_degree_Trans { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MaxJointAngle_degree_Longi")]
            public string JointDetectorModule_MaxJointAngle_degree_Longi { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinJointAngle_degree_Longi")]
            public string JointDetectorModule_MinJointAngle_degree_Longi { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinPercent_Longi_Int")]
            public string JointDetectorModule_MinPercent_Longi_Int { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinPercent_Longi_Rng")]
            public string JointDetectorModule_MinPercent_Longi_Rng { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinPercent_Trans_Int")]
            public string JointDetectorModule_MinPercent_Trans_Int { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_MinPercent_Trans_Rng")]
            public string JointDetectorModule_MinPercent_Trans_Rng { get; set; }
            [XmlElement(ElementName = "JointDetectorModule_DetectOnlyTransOrLongJoint")]
            public string JointDetectorModule_DetectOnlyTransOrLongJoint { get; set; }
        }

        [XmlRoot(ElementName = "PatchModule_Parameters")]
        public class PatchModule_Parameters
        {
            [XmlElement(ElementName = "PatchModule_MinBlobArea_m2")]
            public string PatchModule_MinBlobArea_m2 { get; set; }
            [XmlElement(ElementName = "PatchModule_MinDiff_IOSmoothness")]
            public string PatchModule_MinDiff_IOSmoothness { get; set; }
            [XmlElement(ElementName = "PatchModule_MinimumConfidentScore")]
            public string PatchModule_MinimumConfidentScore { get; set; }
            [XmlElement(ElementName = "PatchModule_MinDiff_IOAverageIntensity")]
            public string PatchModule_MinDiff_IOAverageIntensity { get; set; }
            [XmlElement(ElementName = "PatchModule_EnableDetectionOnConcrete")]
            public string PatchModule_EnableDetectionOnConcrete { get; set; }
            [XmlElement(ElementName = "PatchModule_PatchDetectOutsideLaneLimits")]
            public string PatchModule_PatchDetectOutsideLaneLimits { get; set; }
            [XmlElement(ElementName = "PatchModule_Algorithm")]
            public string PatchModule_Algorithm { get; set; }
            [XmlElement(ElementName = "PatchModule_LoadedModelName")]
            public string PatchModule_LoadedModelName { get; set; }
        }

        [XmlRoot(ElementName = "ResultRenderer_Parameters")]
        public class ResultRenderer_Parameters
        {
            [XmlElement(ElementName = "ResultRenderer_StripColorRGB")]
            public string ResultRenderer_StripColorRGB { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackSeverity0_MaxWidth_mm")]
            public string ResultRenderer_CrackSeverity0_MaxWidth_mm { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackSeverity1_MaxWidth_mm")]
            public string ResultRenderer_CrackSeverity1_MaxWidth_mm { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackSeverity2_MaxWidth_mm")]
            public string ResultRenderer_CrackSeverity2_MaxWidth_mm { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackRenderMinWidth_mm")]
            public string ResultRenderer_CrackRenderMinWidth_mm { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackColorRGB_Sev0")]
            public string ResultRenderer_CrackColorRGB_Sev0 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackColorRGB_Sev1")]
            public string ResultRenderer_CrackColorRGB_Sev1 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackColorRGB_Sev2")]
            public string ResultRenderer_CrackColorRGB_Sev2 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackColorRGB_Sev3")]
            public string ResultRenderer_CrackColorRGB_Sev3 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackBrushSize_pix")]
            public string ResultRenderer_CrackBrushSize_pix { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackRenderOffsetX_pix")]
            public string ResultRenderer_CrackRenderOffsetX_pix { get; set; }
            [XmlElement(ElementName = "ResultRenderer_CrackRenderOffsetY_pix")]
            public string ResultRenderer_CrackRenderOffsetY_pix { get; set; }
            [XmlElement(ElementName = "ResultRenderer_RavRate1_cm3_m2")]
            public string ResultRenderer_RavRate1_cm3_m2 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_RavRate2_cm3_m2")]
            public string ResultRenderer_RavRate2_cm3_m2 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_RavRate3_cm3_m2")]
            public string ResultRenderer_RavRate3_cm3_m2 { get; set; }
            [XmlElement(ElementName = "ResultRenderer_EnableRavelingDisplay")]
            public string ResultRenderer_EnableRavelingDisplay { get; set; }
            [XmlElement(ElementName = "ResultRenderer_EnableJointFaultingValueDisplay")]
            public string ResultRenderer_EnableJointFaultingValueDisplay { get; set; }
            [XmlElement(ElementName = "ResultRenderer_EnableMarkingContourDisplay")]
            public string ResultRenderer_EnableMarkingContourDisplay { get; set; }
            [XmlElement(ElementName = "ResultRenderer_Display_Multiple_Cracks")]
            public string ResultRenderer_Display_Multiple_Cracks { get; set; }
            [XmlElement(ElementName = "ResultRenderer_Display_Alligator_Cracks")]
            public string ResultRenderer_Display_Alligator_Cracks { get; set; }
            [XmlElement(ElementName = "ResultRenderer_EnablePASERDisplay")]
            public string ResultRenderer_EnablePASERDisplay { get; set; }
            [XmlElement(ElementName = "ResultRenderer_EnablePsciDisplay")]
            public string ResultRenderer_EnablePsciDisplay { get; set; }
        }

        [XmlRoot(ElementName = "ProcessingParameters")]
        public class ProcessingParameters
        {
            [XmlElement(ElementName = "GeneralParam_SensorImagesOverlap_pix")]
            public string GeneralParam_SensorImagesOverlap_pix { get; set; }
            [XmlElement(ElementName = "GeneralParam_ResultImageResolution_mm")]
            public string GeneralParam_ResultImageResolution_mm { get; set; }
            [XmlElement(ElementName = "GeneralParam_ResultImageJpgSaveQuality")]
            public string GeneralParam_ResultImageJpgSaveQuality { get; set; }
            [XmlElement(ElementName = "GeneralParam_OverlayImageJpgSaveQuality")]
            public string GeneralParam_OverlayImageJpgSaveQuality { get; set; }
            [XmlElement(ElementName = "GeneralParam_ConfigurationAngleDeg")]
            public string GeneralParam_ConfigurationAngleDeg { get; set; }
            [XmlElement(ElementName = "GeneralParam_SensorImagesVerticalOverlap_pix")]
            public string GeneralParam_SensorImagesVerticalOverlap_pix { get; set; }
            [XmlElement(ElementName = "GeneralParam_CentralBandWidth_mm")]
            public string GeneralParam_CentralBandWidth_mm { get; set; }
            [XmlElement(ElementName = "GeneralParam_NarrowCentralBandMode")]
            public string GeneralParam_NarrowCentralBandMode { get; set; }
            [XmlElement(ElementName = "GeneralParam_WheelPathWidth_mm")]
            public string GeneralParam_WheelPathWidth_mm { get; set; }
            [XmlElement(ElementName = "GeneralParam_SubSamplingFactorStitchedProfile")]
            public string GeneralParam_SubSamplingFactorStitchedProfile { get; set; }
            [XmlElement(ElementName = "GeneralParam_TransversalResolutionX_mm")]
            public string GeneralParam_TransversalResolutionX_mm { get; set; }
            [XmlElement(ElementName = "GeneralParam_ImageJpgRotate")]
            public string GeneralParam_ImageJpgRotate { get; set; }
            [XmlElement(ElementName = "CrackingModule_Parameters")]
            public CrackingModule_Parameters CrackingModule_Parameters { get; set; }
            [XmlElement(ElementName = "MarkingContourModule_Parameters")]
            public MarkingContourModule_Parameters MarkingContourModule_Parameters { get; set; }
            [XmlElement(ElementName = "LaneMarkCharacterization_Parameters")]
            public LaneMarkCharacterization_Parameters LaneMarkCharacterization_Parameters { get; set; }
            [XmlElement(ElementName = "PotholeModule_Parameters")]
            public PotholeModule_Parameters PotholeModule_Parameters { get; set; }
            [XmlElement(ElementName = "PickoutModule_Parameters")]
            public PickoutModule_Parameters PickoutModule_Parameters { get; set; }
            [XmlElement(ElementName = "RavelingModule_Parameters")]
            public RavelingModule_Parameters RavelingModule_Parameters { get; set; }
            [XmlElement(ElementName = "JointModule_Parameters")]
            public JointModule_Parameters JointModule_Parameters { get; set; }
            [XmlElement(ElementName = "PatchModule_Parameters")]
            public PatchModule_Parameters PatchModule_Parameters { get; set; }
            [XmlElement(ElementName = "ResultRenderer_Parameters")]
            public ResultRenderer_Parameters ResultRenderer_Parameters { get; set; }
        }

        [XmlRoot(ElementName = "ProcessingInformation")]
        public class ProcessingInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "RoadSectionFileName")]
            public string RoadSectionFileName { get; set; }
            [XmlElement(ElementName = "AnalyserLibVersion")]
            public string AnalyserLibVersion { get; set; }
            [XmlElement(ElementName = "DateAndTime")]
            public string DateAndTime { get; set; }
            [XmlElement(ElementName = "CpuInformation")]
            public string CpuInformation { get; set; }
            [XmlElement(ElementName = "EndProcessErrorCode")]
            public string EndProcessErrorCode { get; set; }
            [XmlElement(ElementName = "ProcessingParameters")]
            public ProcessingParameters ProcessingParameters { get; set; }
        }

        [XmlRoot(ElementName = "GPSCoordinate")]
        public class GPSCoordinate
        {
            [XmlElement(ElementName = "Longitude")]
            public string Longitude { get; set; }
            [XmlElement(ElementName = "Latitude")]
            public string Latitude { get; set; }
            [XmlElement(ElementName = "Altitude")]
            public string Altitude { get; set; }
            [XmlElement(ElementName = "Time")]
            public string Time { get; set; }
            [XmlElement(ElementName = "Date")]
            public string Date { get; set; }
            [XmlElement(ElementName = "NbrOfSatellites")]
            public string NbrOfSatellites { get; set; }
            [XmlElement(ElementName = "SignalQuality")]
            public string SignalQuality { get; set; }
            [XmlElement(ElementName = "GroundSpeed")]
            public string GroundSpeed { get; set; }
            [XmlElement(ElementName = "TrackAngle")]
            public string TrackAngle { get; set; }
        }

        [XmlRoot(ElementName = "GPSInformation")]
        public class GPSInformation
        {
            [XmlElement(ElementName = "GPSCoordinate")]
            public List<GPSCoordinate> GPSCoordinate { get; set; }
        }

        [XmlRoot(ElementName = "SectionPosition")]
        public class SectionPosition
        {
            [XmlElement(ElementName = "Longitude")]
            public string Longitude { get; set; }
            [XmlElement(ElementName = "Latitude")]
            public string Latitude { get; set; }
            [XmlElement(ElementName = "Altitude")]
            public string Altitude { get; set; }

            [XmlElement(ElementName = "Heading")]
            public string Heading { get; set; }


        }


        [XmlRoot(ElementName = "SectionGnssBasedUtcTimeBeginEnd")]
        public class SectionGnssBasedUtcTimeBeginEnd
        {
            [XmlElement(ElementName = "Begin")]
            public string Begin { get; set; }
            [XmlElement(ElementName = "End")]
            public string End { get; set; }
        }

        [XmlRoot(ElementName = "Unit")]
        public class Unit
        {
            [XmlElement(ElementName = "ResolutionX")]
            public string ResolutionX { get; set; }
            [XmlElement(ElementName = "ResolutionY")]
            public string ResolutionY { get; set; }
            [XmlElement(ElementName = "PeakDetectionThreshold")]
            public string PeakDetectionThreshold { get; set; }
            [XmlElement(ElementName = "BoundingBox")]
            public string BoundingBox { get; set; }
            [XmlElement(ElementName = "Position_Y")]
            public string Position_Y { get; set; }
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
            [XmlElement(ElementName = "Width")]
            public string Width { get; set; }
            [XmlElement(ElementName = "Depth")]
            public string Depth { get; set; }
            [XmlElement(ElementName = "Length")]
            public string Length { get; set; }
            [XmlElement(ElementName = "AVC")]
            public string AVC { get; set; }
            [XmlElement(ElementName = "RPI")]
            public string RPI { get; set; }
            [XmlElement(ElementName = "RI")]
            public string RI { get; set; }
            [XmlElement(ElementName = "RI_Area")]
            public string RI_Area { get; set; }
            [XmlElement(ElementName = "Peak_Threshold")]
            public string Peak_Threshold { get; set; }
            [XmlElement(ElementName = "RavelingIndicator")]
            public string RavelingIndicator { get; set; }
            [XmlElement(ElementName = "AffectedPercentage")]
            public string AffectedPercentage { get; set; }
            [XmlElement(ElementName = "ZoneReportListUnit")]
            public ZoneReportListUnit ZoneReportListUnit { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "MaximumDepth")]
            public string MaximumDepth { get; set; }
            [XmlElement(ElementName = "AverageDepth")]
            public string AverageDepth { get; set; }
            [XmlElement(ElementName = "MajorDiameter")]
            public string MajorDiameter { get; set; }
            [XmlElement(ElementName = "MinorDiameter")]
            public string MinorDiameter { get; set; }
            [XmlElement(ElementName = "Perimeter")]
            public string Perimeter { get; set; }
            [XmlElement(ElementName = "X1")]
            public string X1 { get; set; }
            [XmlElement(ElementName = "Y1")]
            public string Y1 { get; set; }
            [XmlElement(ElementName = "X2")]
            public string X2 { get; set; }
            [XmlElement(ElementName = "Y2")]
            public string Y2 { get; set; }
            [XmlElement(ElementName = "FaultMeasurement")]
            public string FaultMeasurement { get; set; }
            [XmlElement(ElementName = "WidthMeasurement")]
            public string WidthMeasurement { get; set; }
            [XmlElement(ElementName = "DepthMeasurement")]
            public string DepthMeasurement { get; set; }
            [XmlElement(ElementName = "TransverseEvalPositions")]
            public string TransverseEvalPositions { get; set; }
            [XmlElement(ElementName = "SamplingRegion")]
            public string SamplingRegion { get; set; }
            [XmlElement(ElementName = "Diameter")]
            public string Diameter { get; set; }
        }

        [XmlRoot(ElementName = "ResultImageInformation")]
        public class ResultImageInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "ResolutionX")]
            public string ResolutionX { get; set; }
            [XmlElement(ElementName = "ResolutionY")]
            public string ResolutionY { get; set; }
            [XmlElement(ElementName = "ImageWidth")]
            public string ImageWidth { get; set; }
            [XmlElement(ElementName = "ImageHeight")]
            public string ImageHeight { get; set; }
        }

        [XmlRoot(ElementName = "Transition")]
        public class Transition
        {
            [XmlElement(ElementName = "TransitionID")]
            public string TransitionID { get; set; }
            [XmlElement(ElementName = "Position_Y")]
            public string Position_Y { get; set; }
            [XmlElement(ElementName = "Score")]
            public string Score { get; set; }
            [XmlElement(ElementName = "RoadSide")]
            public string RoadSide { get; set; }
            [XmlElement(ElementName = "CrkPeakThresholdDown")]
            public string CrkPeakThresholdDown { get; set; }
            [XmlElement(ElementName = "CrkPeakThresholdUp")]
            public string CrkPeakThresholdUp { get; set; }
        }

        [XmlRoot(ElementName = "PavementTransitions")]
        public class PavementTransitions
        {
            [XmlElement(ElementName = "NumberTransitions")]
            public string NumberTransitions { get; set; }
            [XmlElement(ElementName = "Transition")]
            public List<Transition> Transition { get; set; }
        }

        [XmlRoot(ElementName = "PavementTypeInformation")]
        public class PavementTypeInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "AutoDetectPavementEnable")]
            public string AutoDetectPavementEnable { get; set; }
            [XmlElement(ElementName = "DetectPavementTransitionEnable")]
            public string DetectPavementTransitionEnable { get; set; }
            [XmlElement(ElementName = "AutoPeakDetectionThresholdEnable")]
            public string AutoPeakDetectionThresholdEnable { get; set; }
            [XmlElement(ElementName = "PeakDetectionThresholdLeft")]
            public string PeakDetectionThresholdLeft { get; set; }
            [XmlElement(ElementName = "PeakDetectionThresholdRight")]
            public string PeakDetectionThresholdRight { get; set; }
            [XmlElement(ElementName = "PavementTransitions")]
            public PavementTransitions PavementTransitions { get; set; }
            [XmlElement(ElementName = "PavementType")]
            public string PavementType { get; set; }
            [XmlElement(ElementName = "PavementTypeSource")]
            public string PavementTypeSource { get; set; }
        }

        [XmlRoot(ElementName = "Node")]
        public class Node
        {
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
            [XmlElement(ElementName = "Width")]
            public string Width { get; set; }
            [XmlElement(ElementName = "Depth")]
            public string Depth { get; set; }
        }

        [XmlRoot(ElementName = "Crack")]
        public class Crack
        {
            [XmlElement(ElementName = "CrackID")]
            public string CrackID { get; set; }
            [XmlElement(ElementName = "Length")]
            public string Length { get; set; }
            [XmlElement(ElementName = "WeightedDepth")]
            public string WeightedDepth { get; set; }
            [XmlElement(ElementName = "WeightedWidth")]
            public string WeightedWidth { get; set; }
            [XmlElement(ElementName = "Node")]
            public List<Node> Node { get; set; }
        }

        [XmlRoot(ElementName = "CrackList")]
        public class CrackList
        {
            [XmlElement(ElementName = "Crack")]
            public List<Crack> Crack { get; set; }
        }

        [XmlRoot(ElementName = "CrackInformation")]
        public class CrackInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "CrackList")]
            public CrackList CrackList { get; set; }
        }

        [XmlRoot(ElementName = "ZoneReportListUnit")]
        public class ZoneReportListUnit
        {
            [XmlElement(ElementName = "ZoneWidth")]
            public string ZoneWidth { get; set; }
            [XmlElement(ElementName = "ZoneHeight")]
            public string ZoneHeight { get; set; }
        }

        [XmlRoot(ElementName = "ZoneReport")]
        public class ZoneReport
        {
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
            [XmlElement(ElementName = "XMax")]
            public string XMax { get; set; }
            [XmlElement(ElementName = "YMax")]
            public string YMax { get; set; }
            [XmlElement(ElementName = "AVC")]
            public string AVC { get; set; }
            [XmlElement(ElementName = "RPI")]
            public string RPI { get; set; }
            [XmlElement(ElementName = "RI")]
            public string RI { get; set; }
            [XmlElement(ElementName = "RI_Area")]
            public string RI_Area { get; set; }
            [XmlElement(ElementName = "RI_Percent")]
            public string RI_Percent { get; set; }
        }

        [XmlRoot(ElementName = "ZoneReportList")]
        public class ZoneReportList
        {
            [XmlElement(ElementName = "ZoneWidth")]
            public string ZoneWidth { get; set; }
            [XmlElement(ElementName = "ZoneHeight")]
            public string ZoneHeight { get; set; }
            [XmlElement(ElementName = "ZoneReport")]
            public List<ZoneReport> ZoneReport { get; set; }
        }

        [XmlRoot(ElementName = "RavelingInformation")]
        public class RavelingInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "Peak_Threshold")]
            public string Peak_Threshold { get; set; }
            [XmlElement(ElementName = "RavelingIndicator")]
            public string RavelingIndicator { get; set; }
            [XmlElement(ElementName = "AffectedPercentage")]
            public string AffectedPercentage { get; set; }
            [XmlElement(ElementName = "ZoneReportList")]
            public ZoneReportList ZoneReportList { get; set; }
        }

        [XmlRoot(ElementName = "BoundingBox")]
        public class BoundingBox
        {
            [XmlElement(ElementName = "MinX")]
            public string MinX { get; set; }
            [XmlElement(ElementName = "MaxX")]
            public string MaxX { get; set; }
            [XmlElement(ElementName = "MinY")]
            public string MinY { get; set; }
            [XmlElement(ElementName = "MaxY")]
            public string MaxY { get; set; }
        }

        [XmlRoot(ElementName = "Perimeter")]
        public class Perimeter
        {
            [XmlElement(ElementName = "Node")]
            public List<Node> Node { get; set; }
        }

        [XmlRoot(ElementName = "Pothole")]
        public class Pothole
        {
            [XmlElement(ElementName = "PotholeID")]
            public string PotholeID { get; set; }
            [XmlElement(ElementName = "MaximumDepth")]
            public string MaximumDepth { get; set; }
            [XmlElement(ElementName = "AverageDepth")]
            public string AverageDepth { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "Severity")]
            public string Severity { get; set; }
            [XmlElement(ElementName = "MajorDiameter")]
            public string MajorDiameter { get; set; }
            [XmlElement(ElementName = "MinorDiameter")]
            public string MinorDiameter { get; set; }
            [XmlElement(ElementName = "AverageIntensity")]
            public string AverageIntensity { get; set; }
            [XmlElement(ElementName = "BoundingBox")]
            public BoundingBox BoundingBox { get; set; }
            [XmlElement(ElementName = "Perimeter")]
            public Perimeter Perimeter { get; set; }
        }

        [XmlRoot(ElementName = "PotholesInformation")]
        public class PotholesInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "Pothole")]
            public List<Pothole> Pothole { get; set; }
        }

        [XmlRoot(ElementName = "MarkingContour")]
        public class MarkingContour
        {
            [XmlElement(ElementName = "MarkingID")]
            public string MarkingID { get; set; }
            [XmlElement(ElementName = "BoundingBox")]
            public BoundingBox BoundingBox { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "AvgIntensity")]
            public string AvgIntensity { get; set; }
            [XmlElement(ElementName = "Perimeter")]
            public Perimeter Perimeter { get; set; }
        }

        [XmlRoot(ElementName = "MarkingContourInformation")]
        public class MarkingContourInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "MarkingContour")]
            public List<MarkingContour> MarkingContour { get; set; }
        }

        [XmlRoot(ElementName = "PatchDetectionInformation")]
        public class PatchDetectionInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "PatchData")]
            public List<Patch> PatchData { get; set; }
        }

        [XmlRoot(ElementName = "Patch")]
        public class Patch
        {
            [XmlElement(ElementName = "PatchID")]
            public string PatchID { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "ConfidenceScore")]
            public string ConfidenceScore { get; set; }
            [XmlElement(ElementName = "SeverityLevel")]
            public string SeverityLevel { get; set; }
            [XmlElement(ElementName = "BoundingBox")]
            public BoundingBox BoundingBox { get; set; }

        }

        [XmlRoot(ElementName = "ConcreteJointInformation")]
        public class ConcreteJointInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "JointList")]
            public JointList JointList { get; set; }
            [XmlElement(ElementName = "VerticalJointList")]
            public VerticalJointList VerticalJointList { get; set; }
            [XmlElement(ElementName = "SpallingCornerList")]
            public SpallingCornerList SpallingCornerList { get; set; }
        }

        [XmlRoot(ElementName = "SpallingCornerList")]
        public class SpallingCornerList
        {
            [XmlElement(ElementName = "Corner")]
            public List<Corner> Corner { get; set; }
        }

        [XmlRoot(ElementName = "JointList")]
        public class JointList
        {
            [XmlElement(ElementName = "Joint")]
            public List<Joint> Joint { get; set; }
        }

        [XmlRoot(ElementName = "VerticalJointList")]
        public class VerticalJointList
        {
            [XmlElement(ElementName = "Joint")]
            public List<Joint> Joint { get; set; }
        }

        [XmlRoot(ElementName = "Joint")]
        public class Joint
        {
            [XmlElement(ElementName = "JointID")]
            public string JointID { get; set; }
            [XmlElement(ElementName = "SpallingDefects")]
            public SpallingDefects SpallingDefects { get; set; }
            [XmlElement(ElementName = "X1")]
            public string X1 { get; set; }
            [XmlElement(ElementName = "Y1")]
            public string Y1 { get; set; }
            [XmlElement(ElementName = "X2")]
            public string X2 { get; set; }
            [XmlElement(ElementName = "Y2")]
            public string Y2 { get; set; }
            [XmlElement(ElementName = "Length")]
            public string Length { get; set; }
            [XmlElement(ElementName = "WidthMeasurements")]
            public string WidthMeasurements { get; set; }
            [XmlElement(ElementName = "AverageDepthBadSeal")]
            public string AverageDepthBadSeal { get; set; }
            [XmlElement(ElementName = "AverageDepthGoodSeal")]
            public string AverageDepthGoodSeal { get; set; }
            [XmlElement(ElementName = "BadSealantTotalLength")]
            public string BadSealantTotalLength { get; set; }
            [XmlElement(ElementName = "FaultMeasurements")]
            public string FaultMeasurements { get; set; }
            [XmlElement(ElementName = "MaxDepthSeal")]
            public string MaxDepthSeal { get; set; }

        }

        [XmlRoot(ElementName = "SpallingDefects")]
        public class SpallingDefects
        {
            [XmlElement(ElementName = "SpallingSegment")]
            public List<SpallingSegment> SpallingSegment { get; set; }
        }

        [XmlRoot(ElementName = "SpallingSegment")]
        public class SpallingSegment
        {
            [XmlElement(ElementName = "ID")]
            public string ID { get; set; }
            [XmlElement(ElementName = "AverageDepth")]
            public string AverageDepth { get; set; }
            [XmlElement(ElementName = "AverageWidth")]
            public string AverageWidth { get; set; }
            [XmlElement(ElementName = "Length")]
            public string Length { get; set; }
            [XmlElement(ElementName = "Start")]
            public Start Start { get; set; }
            [XmlElement(ElementName = "End")]
            public End End { get; set; }
        }


        [XmlRoot(ElementName = "Start")]
        public class Start
        {
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
        }

        [XmlRoot(ElementName = "End")]
        public class End
        {
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
        }

        [XmlRoot(ElementName = "Corner")]
        public class Corner
        {
            [XmlElement(ElementName = "X")]
            public string X { get; set; }
            [XmlElement(ElementName = "Y")]
            public string Y { get; set; }
            [XmlElement(ElementName = "Quarter")]
            public List<Quarter> Quarter { get; set; }
        }

        [XmlRoot(ElementName = "Quarter")]
        public class Quarter
        {
            [XmlElement(ElementName = "QuarterIndex")]
            public string QuarterIndex { get; set; }
            [XmlElement(ElementName = "AverageDepth")]
            public string AverageDepth { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "BreakArea")]
            public string BreakArea { get; set; }
            [XmlElement(ElementName = "AreaRatio")]
            public string AreaRatio { get; set; }
        }

        [XmlRoot(ElementName = "PickOut")]
        public class PickOut
        {
            [XmlElement(ElementName = "PickOutID")]
            public string PickOutID { get; set; }
            [XmlElement(ElementName = "Area")]
            public string Area { get; set; }
            [XmlElement(ElementName = "MaximumDepth")]
            public string MaximumDepth { get; set; }
            [XmlElement(ElementName = "AverageDepth")]
            public string AverageDepth { get; set; }
            [XmlElement(ElementName = "Diameter")]
            public string Diameter { get; set; }
            [XmlElement(ElementName = "BoundingBox")]
            public BoundingBox BoundingBox { get; set; }
            [XmlElement(ElementName = "Perimeter")]
            public Perimeter Perimeter { get; set; }
        }

        [XmlRoot(ElementName = "PickOutInformation")]
        public class PickOutInformation
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "Unit")]
            public Unit Unit { get; set; }
            [XmlElement(ElementName = "PickOut")]
            public List<PickOut> PickOut { get; set; }
        }

        [XmlRoot(ElementName = "SystemParam")]
        public class SystemParam
        {
            [XmlElement(ElementName = "SensorEnable")]
            public SensorEnable SensorEnable { get; set; }
            [XmlElement(ElementName = "OdometerCountPerMeter")]
            public string OdometerCountPerMeter { get; set; }
            [XmlElement(ElementName = "ClockFrequencyHz")]
            public string ClockFrequencyHz { get; set; }
            [XmlElement(ElementName = "InterSensorDistance_um")]
            public string InterSensorDistance_um { get; set; }
            [XmlElement(ElementName = "EncoderInputDebouncing")]
            public string EncoderInputDebouncing { get; set; }
            [XmlElement(ElementName = "EncoderInputChannel")]
            public string EncoderInputChannel { get; set; }
            [XmlElement(ElementName = "EncoderOutputSelect")]
            public string EncoderOutputSelect { get; set; }
            [XmlElement(ElementName = "EncoderRatio")]
            public string EncoderRatio { get; set; }
            [XmlElement(ElementName = "EncoderDivider")]
            public string EncoderDivider { get; set; }
        }

        [XmlRoot(ElementName = "CameraStatus")]
        public class CameraStatus
        {
            [XmlElement(ElementName = "GainAnalog_Perc")]
            public string GainAnalog_Perc { get; set; }
            [XmlElement(ElementName = "OffsetAnalog_Perc")]
            public string OffsetAnalog_Perc { get; set; }
            [XmlElement(ElementName = "GainDigital")]
            public string GainDigital { get; set; }
            [XmlElement(ElementName = "ExposureTime_us")]
            public string ExposureTime_us { get; set; }
            [XmlElement(ElementName = "Temp_C")]
            public string Temp_C { get; set; }
            [XmlElement(ElementName = "FaultFatal")]
            public string FaultFatal { get; set; }
            [XmlElement(ElementName = "FaultWarning")]
            public string FaultWarning { get; set; }
            [XmlElement(ElementName = "StatusBits")]
            public string StatusBits { get; set; }
        }

        [XmlRoot(ElementName = "LaserStatus")]
        public class LaserStatus
        {
            [XmlElement(ElementName = "LaserInterlockState")]
            public string LaserInterlockState { get; set; }
            [XmlElement(ElementName = "LaserReady")]
            public string LaserReady { get; set; }
            [XmlElement(ElementName = "LaserEnable")]
            public string LaserEnable { get; set; }
            [XmlElement(ElementName = "LaserHourMeter")]
            public string LaserHourMeter { get; set; }
            [XmlElement(ElementName = "LaserTrigCount")]
            public string LaserTrigCount { get; set; }
            [XmlElement(ElementName = "LaserCurrent_mA")]
            public string LaserCurrent_mA { get; set; }
            [XmlElement(ElementName = "LaserOutputPower")]
            public string LaserOutputPower { get; set; }
            [XmlElement(ElementName = "TecEnable")]
            public string TecEnable { get; set; }
            [XmlElement(ElementName = "TecFault")]
            public string TecFault { get; set; }
            [XmlElement(ElementName = "TecTemp_C")]
            public string TecTemp_C { get; set; }
            [XmlElement(ElementName = "TecCurrent_mA")]
            public string TecCurrent_mA { get; set; }
            [XmlElement(ElementName = "TecVoltage_mV")]
            public string TecVoltage_mV { get; set; }
        }

        [XmlRoot(ElementName = "Left")]
        public class Left
        {
            [XmlElement(ElementName = "SensorBoardFault")]
            public string SensorBoardFault { get; set; }
            [XmlElement(ElementName = "SensorInternalTemp_C")]
            public string SensorInternalTemp_C { get; set; }
            [XmlElement(ElementName = "CameraMonitorErrorCode")]
            public string CameraMonitorErrorCode { get; set; }
            [XmlElement(ElementName = "SensorMonitorErrorCode")]
            public string SensorMonitorErrorCode { get; set; }
            [XmlElement(ElementName = "CameraStatus")]
            public CameraStatus CameraStatus { get; set; }
            [XmlElement(ElementName = "LaserStatus")]
            public LaserStatus LaserStatus { get; set; }
            [XmlElement(ElementName = "Model")]
            public string Model { get; set; }
            [XmlElement(ElementName = "SerialNumber")]
            public string SerialNumber { get; set; }
            [XmlElement(ElementName = "CalibrationNumber")]
            public string CalibrationNumber { get; set; }
            [XmlElement(ElementName = "FirmwareNumber")]
            public string FirmwareNumber { get; set; }
            [XmlElement(ElementName = "FirmwareRevisionNumber")]
            public string FirmwareRevisionNumber { get; set; }
            [XmlElement(ElementName = "DateOfCompilation")]
            public string DateOfCompilation { get; set; }
            [XmlElement(ElementName = "FirmwareTimeOfCompilation")]
            public string FirmwareTimeOfCompilation { get; set; }
            [XmlElement(ElementName = "Temperature_C")]
            public string Temperature_C { get; set; }
            [XmlElement(ElementName = "NbProfiles")]
            public string NbProfiles { get; set; }
            [XmlElement(ElementName = "ProfileNbPoints")]
            public string ProfileNbPoints { get; set; }
            [XmlElement(ElementName = "RngCompressionType")]
            public string RngCompressionType { get; set; }
            [XmlElement(ElementName = "IntCompressionType")]
            public string IntCompressionType { get; set; }
            [XmlElement(ElementName = "IntCompressionQuality_Prec")]
            public string IntCompressionQuality_Prec { get; set; }
            [XmlElement(ElementName = "AcquisitionMode")]
            public string AcquisitionMode { get; set; }
            [XmlElement(ElementName = "PeriodicTriggerFreq_Hz")]
            public string PeriodicTriggerFreq_Hz { get; set; }
            [XmlElement(ElementName = "CmosRoiWidth")]
            public string CmosRoiWidth { get; set; }
            [XmlElement(ElementName = "CmosRoiHeight")]
            public string CmosRoiHeight { get; set; }
            [XmlElement(ElementName = "CmosRoiStartCol")]
            public string CmosRoiStartCol { get; set; }
            [XmlElement(ElementName = "PeakDetectorType")]
            public string PeakDetectorType { get; set; }
            [XmlElement(ElementName = "PeakDetectionThreshold")]
            public string PeakDetectionThreshold { get; set; }
            [XmlElement(ElementName = "PeakSubPixelPrecision")]
            public string PeakSubPixelPrecision { get; set; }
            [XmlElement(ElementName = "AgcEnable")]
            public string AgcEnable { get; set; }
            [XmlElement(ElementName = "AgcType")]
            public string AgcType { get; set; }
            [XmlElement(ElementName = "IntensityTarget")]
            public string IntensityTarget { get; set; }
            [XmlElement(ElementName = "GainTol_Perc")]
            public string GainTol_Perc { get; set; }
            [XmlElement(ElementName = "AgcProfileRoiStart_norm")]
            public string AgcProfileRoiStart_norm { get; set; }
            [XmlElement(ElementName = "AgcProfileRoiStop_norm")]
            public string AgcProfileRoiStop_norm { get; set; }
            [XmlElement(ElementName = "SaturationLimit")]
            public string SaturationLimit { get; set; }
            [XmlElement(ElementName = "PopBrighterThanTarget")]
            public string PopBrighterThanTarget { get; set; }
            [XmlElement(ElementName = "FastAdjustFactor")]
            public string FastAdjustFactor { get; set; }
            [XmlElement(ElementName = "UseDamping")]
            public string UseDamping { get; set; }
            [XmlElement(ElementName = "CamMaxGain")]
            public string CamMaxGain { get; set; }
            [XmlElement(ElementName = "CamGainAnalog_Perc")]
            public string CamGainAnalog_Perc { get; set; }
            [XmlElement(ElementName = "CamOffsetAnalog_Perc")]
            public string CamOffsetAnalog_Perc { get; set; }
            [XmlElement(ElementName = "CamGainDigital")]
            public string CamGainDigital { get; set; }
            [XmlElement(ElementName = "CamExposureTime_us")]
            public string CamExposureTime_us { get; set; }
            [XmlElement(ElementName = "Laser2CamExposureDelay_us")]
            public string Laser2CamExposureDelay_us { get; set; }
            [XmlElement(ElementName = "OutputFifoLength")]
            public string OutputFifoLength { get; set; }
            [XmlElement(ElementName = "SensorAngle_deg")]
            public string SensorAngle_deg { get; set; }
            [XmlElement(ElementName = "LaserSetPoint_mA")]
            public string LaserSetPoint_mA { get; set; }
            [XmlElement(ElementName = "LaserCurrentLimit_mA")]
            public string LaserCurrentLimit_mA { get; set; }
            [XmlElement(ElementName = "LaserTecSetPoint_C")]
            public string LaserTecSetPoint_C { get; set; }
            [XmlElement(ElementName = "LaserTecTempLimMin_C")]
            public string LaserTecTempLimMin_C { get; set; }
            [XmlElement(ElementName = "LaserTecTempLimMax_C")]
            public string LaserTecTempLimMax_C { get; set; }
            [XmlElement(ElementName = "TrigCount")]
            public string TrigCount { get; set; }
            [XmlElement(ElementName = "ProfileAcqRate")]
            public string ProfileAcqRate { get; set; }
            [XmlElement(ElementName = "NbProfilesMissed")]
            public string NbProfilesMissed { get; set; }
            [XmlElement(ElementName = "NbProfilesLost")]
            public string NbProfilesLost { get; set; }
            [XmlElement(ElementName = "NbProfilesSkip")]
            public string NbProfilesSkip { get; set; }
            [XmlElement(ElementName = "GrabFifoPercOcc")]
            public string GrabFifoPercOcc { get; set; }
            [XmlElement(ElementName = "ProfileFifoPercOcc")]
            public string ProfileFifoPercOcc { get; set; }
            [XmlElement(ElementName = "NbRoadSectionMissed")]
            public string NbRoadSectionMissed { get; set; }
            [XmlElement(ElementName = "NbRoadSectionLost")]
            public string NbRoadSectionLost { get; set; }
            [XmlElement(ElementName = "NbRoadSectionSkip")]
            public string NbRoadSectionSkip { get; set; }
            [XmlElement(ElementName = "OutputFifoPercOcc")]
            public string OutputFifoPercOcc { get; set; }
            [XmlElement(ElementName = "FrameGrabberErrorCode")]
            public string FrameGrabberErrorCode { get; set; }
            [XmlElement(ElementName = "DataCompressionErrorCode")]
            public string DataCompressionErrorCode { get; set; }
        }

        [XmlRoot(ElementName = "Right")]
        public class Right
        {
            [XmlElement(ElementName = "SensorBoardFault")]
            public string SensorBoardFault { get; set; }
            [XmlElement(ElementName = "SensorInternalTemp_C")]
            public string SensorInternalTemp_C { get; set; }
            [XmlElement(ElementName = "CameraMonitorErrorCode")]
            public string CameraMonitorErrorCode { get; set; }
            [XmlElement(ElementName = "SensorMonitorErrorCode")]
            public string SensorMonitorErrorCode { get; set; }
            [XmlElement(ElementName = "CameraStatus")]
            public CameraStatus CameraStatus { get; set; }
            [XmlElement(ElementName = "LaserStatus")]
            public LaserStatus LaserStatus { get; set; }
            [XmlElement(ElementName = "Model")]
            public string Model { get; set; }
            [XmlElement(ElementName = "SerialNumber")]
            public string SerialNumber { get; set; }
            [XmlElement(ElementName = "CalibrationNumber")]
            public string CalibrationNumber { get; set; }
            [XmlElement(ElementName = "FirmwareNumber")]
            public string FirmwareNumber { get; set; }
            [XmlElement(ElementName = "FirmwareRevisionNumber")]
            public string FirmwareRevisionNumber { get; set; }
            [XmlElement(ElementName = "DateOfCompilation")]
            public string DateOfCompilation { get; set; }
            [XmlElement(ElementName = "FirmwareTimeOfCompilation")]
            public string FirmwareTimeOfCompilation { get; set; }
            [XmlElement(ElementName = "Temperature_C")]
            public string Temperature_C { get; set; }
            [XmlElement(ElementName = "NbProfiles")]
            public string NbProfiles { get; set; }
            [XmlElement(ElementName = "ProfileNbPoints")]
            public string ProfileNbPoints { get; set; }
            [XmlElement(ElementName = "RngCompressionType")]
            public string RngCompressionType { get; set; }
            [XmlElement(ElementName = "IntCompressionType")]
            public string IntCompressionType { get; set; }
            [XmlElement(ElementName = "IntCompressionQuality_Prec")]
            public string IntCompressionQuality_Prec { get; set; }
            [XmlElement(ElementName = "AcquisitionMode")]
            public string AcquisitionMode { get; set; }
            [XmlElement(ElementName = "PeriodicTriggerFreq_Hz")]
            public string PeriodicTriggerFreq_Hz { get; set; }
            [XmlElement(ElementName = "CmosRoiWidth")]
            public string CmosRoiWidth { get; set; }
            [XmlElement(ElementName = "CmosRoiHeight")]
            public string CmosRoiHeight { get; set; }
            [XmlElement(ElementName = "CmosRoiStartCol")]
            public string CmosRoiStartCol { get; set; }
            [XmlElement(ElementName = "PeakDetectorType")]
            public string PeakDetectorType { get; set; }
            [XmlElement(ElementName = "PeakDetectionThreshold")]
            public string PeakDetectionThreshold { get; set; }
            [XmlElement(ElementName = "PeakSubPixelPrecision")]
            public string PeakSubPixelPrecision { get; set; }
            [XmlElement(ElementName = "AgcEnable")]
            public string AgcEnable { get; set; }
            [XmlElement(ElementName = "AgcType")]
            public string AgcType { get; set; }
            [XmlElement(ElementName = "IntensityTarget")]
            public string IntensityTarget { get; set; }
            [XmlElement(ElementName = "GainTol_Perc")]
            public string GainTol_Perc { get; set; }
            [XmlElement(ElementName = "AgcProfileRoiStart_norm")]
            public string AgcProfileRoiStart_norm { get; set; }
            [XmlElement(ElementName = "AgcProfileRoiStop_norm")]
            public string AgcProfileRoiStop_norm { get; set; }
            [XmlElement(ElementName = "SaturationLimit")]
            public string SaturationLimit { get; set; }
            [XmlElement(ElementName = "PopBrighterThanTarget")]
            public string PopBrighterThanTarget { get; set; }
            [XmlElement(ElementName = "FastAdjustFactor")]
            public string FastAdjustFactor { get; set; }
            [XmlElement(ElementName = "UseDamping")]
            public string UseDamping { get; set; }
            [XmlElement(ElementName = "CamMaxGain")]
            public string CamMaxGain { get; set; }
            [XmlElement(ElementName = "CamGainAnalog_Perc")]
            public string CamGainAnalog_Perc { get; set; }
            [XmlElement(ElementName = "CamOffsetAnalog_Perc")]
            public string CamOffsetAnalog_Perc { get; set; }
            [XmlElement(ElementName = "CamGainDigital")]
            public string CamGainDigital { get; set; }
            [XmlElement(ElementName = "CamExposureTime_us")]
            public string CamExposureTime_us { get; set; }
            [XmlElement(ElementName = "Laser2CamExposureDelay_us")]
            public string Laser2CamExposureDelay_us { get; set; }
            [XmlElement(ElementName = "OutputFifoLength")]
            public string OutputFifoLength { get; set; }
            [XmlElement(ElementName = "SensorAngle_deg")]
            public string SensorAngle_deg { get; set; }
            [XmlElement(ElementName = "LaserSetPoint_mA")]
            public string LaserSetPoint_mA { get; set; }
            [XmlElement(ElementName = "LaserCurrentLimit_mA")]
            public string LaserCurrentLimit_mA { get; set; }
            [XmlElement(ElementName = "LaserTecSetPoint_C")]
            public string LaserTecSetPoint_C { get; set; }
            [XmlElement(ElementName = "LaserTecTempLimMin_C")]
            public string LaserTecTempLimMin_C { get; set; }
            [XmlElement(ElementName = "LaserTecTempLimMax_C")]
            public string LaserTecTempLimMax_C { get; set; }
            [XmlElement(ElementName = "TrigCount")]
            public string TrigCount { get; set; }
            [XmlElement(ElementName = "ProfileAcqRate")]
            public string ProfileAcqRate { get; set; }
            [XmlElement(ElementName = "NbProfilesMissed")]
            public string NbProfilesMissed { get; set; }
            [XmlElement(ElementName = "NbProfilesLost")]
            public string NbProfilesLost { get; set; }
            [XmlElement(ElementName = "NbProfilesSkip")]
            public string NbProfilesSkip { get; set; }
            [XmlElement(ElementName = "GrabFifoPercOcc")]
            public string GrabFifoPercOcc { get; set; }
            [XmlElement(ElementName = "ProfileFifoPercOcc")]
            public string ProfileFifoPercOcc { get; set; }
            [XmlElement(ElementName = "NbRoadSectionMissed")]
            public string NbRoadSectionMissed { get; set; }
            [XmlElement(ElementName = "NbRoadSectionLost")]
            public string NbRoadSectionLost { get; set; }
            [XmlElement(ElementName = "NbRoadSectionSkip")]
            public string NbRoadSectionSkip { get; set; }
            [XmlElement(ElementName = "OutputFifoPercOcc")]
            public string OutputFifoPercOcc { get; set; }
            [XmlElement(ElementName = "FrameGrabberErrorCode")]
            public string FrameGrabberErrorCode { get; set; }
            [XmlElement(ElementName = "DataCompressionErrorCode")]
            public string DataCompressionErrorCode { get; set; }
        }

        [XmlRoot(ElementName = "SensorHeadStatus")]
        public class SensorHeadStatus
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "SystemStatus")]
        public class SystemStatus
        {
            [XmlElement(ElementName = "SystemTimeAndDate")]
            public string SystemTimeAndDate { get; set; }
            [XmlElement(ElementName = "SystemFaultBitField")]
            public string SystemFaultBitField { get; set; }
            [XmlElement(ElementName = "LaserInterlockState")]
            public string LaserInterlockState { get; set; }
            [XmlElement(ElementName = "CtrlModuleMonitorErrorState")]
            public string CtrlModuleMonitorErrorState { get; set; }
            [XmlElement(ElementName = "SensorEnable")]
            public SensorEnable SensorEnable { get; set; }
            [XmlElement(ElementName = "SensorHeadStatus")]
            public SensorHeadStatus SensorHeadStatus { get; set; }
        }

        [XmlRoot(ElementName = "ControlModuleInfo")]
        public class ControlModuleInfo
        {
            [XmlElement(ElementName = "Model")]
            public string Model { get; set; }
            [XmlElement(ElementName = "SerialNumber")]
            public string SerialNumber { get; set; }
            [XmlElement(ElementName = "FirmwareNumber")]
            public string FirmwareNumber { get; set; }
            [XmlElement(ElementName = "FirmwareRevisionNumber")]
            public string FirmwareRevisionNumber { get; set; }
            [XmlElement(ElementName = "DateOfCompilation")]
            public string DateOfCompilation { get; set; }
            [XmlElement(ElementName = "FirmwareTimeOfCompilation")]
            public string FirmwareTimeOfCompilation { get; set; }
        }

        [XmlRoot(ElementName = "SensorInfo")]
        public class SensorInfo
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "CameraInfo")]
        public class CameraInfo
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "ImuInfo")]
        public class ImuInfo
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "SystemInfo")]
        public class SystemInfo
        {
            [XmlElement(ElementName = "SensorEnable")]
            public SensorEnable SensorEnable { get; set; }
            [XmlElement(ElementName = "AcquisitionSoftwareVersion")]
            public string AcquisitionSoftwareVersion { get; set; }
            [XmlElement(ElementName = "ControlModuleInfo")]
            public ControlModuleInfo ControlModuleInfo { get; set; }
            [XmlElement(ElementName = "SensorInfo")]
            public SensorInfo SensorInfo { get; set; }
            [XmlElement(ElementName = "CameraInfo")]
            public CameraInfo CameraInfo { get; set; }
            [XmlElement(ElementName = "ImuInfo")]
            public ImuInfo ImuInfo { get; set; }
        }

        [XmlRoot(ElementName = "SensorParam")]
        public class SensorParam
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "SensorAcquiStatus")]
        public class SensorAcquiStatus
        {
            [XmlElement(ElementName = "Left")]
            public Left Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public Right Right { get; set; }
        }

        [XmlRoot(ElementName = "SystemData")]
        public class SystemData
        {
            [XmlElement(ElementName = "SystemParam")]
            public SystemParam SystemParam { get; set; }
            [XmlElement(ElementName = "SystemStatus")]
            public SystemStatus SystemStatus { get; set; }
            [XmlElement(ElementName = "SystemInfo")]
            public SystemInfo SystemInfo { get; set; }
            [XmlElement(ElementName = "SensorParam")]
            public SensorParam SensorParam { get; set; }
            [XmlElement(ElementName = "SensorAcquiStatus")]
            public SensorAcquiStatus SensorAcquiStatus { get; set; }
            [XmlElement(ElementName = "SensorsFlipInfo")]
            public string SensorsFlipInfo { get; set; }
            [XmlElement(ElementName = "SensorsSwapInfo")]
            public string SensorsSwapInfo { get; set; }
        }

        [XmlRoot(ElementName = "LcmsAnalyserResults")]
        public class LcmsAnalyserResults
        {
            [XmlElement(ElementName = "DataFormat")]
            public string DataFormat { get; set; }
            [XmlElement(ElementName = "SurveyInfo")]
            public SurveyInfo SurveyInfo { get; set; }
            [XmlElement(ElementName = "RoadSectionInfo")]
            public RoadSectionInfo RoadSectionInfo { get; set; }
            [XmlElement(ElementName = "ProcessingInformation")]
            public ProcessingInformation ProcessingInformation { get; set; }
            [XmlElement(ElementName = "GPSInformation")]
            public GPSInformation GPSInformation { get; set; }
            [XmlElement(ElementName = "SectionPosition")]
            public SectionPosition SectionPosition { get; set; }
            [XmlElement(ElementName = "SectionGnssBasedUtcTimeBeginEnd")]
            public SectionGnssBasedUtcTimeBeginEnd SectionGnssBasedUtcTimeBeginEnd { get; set; }
            [XmlElement(ElementName = "ResultImageInformation")]
            public ResultImageInformation ResultImageInformation { get; set; }
            [XmlElement(ElementName = "PavementTypeInformation")]
            public PavementTypeInformation PavementTypeInformation { get; set; }
            [XmlElement(ElementName = "CrackInformation")]
            public CrackInformation CrackInformation { get; set; }
            [XmlElement(ElementName = "RavelingInformation")]
            public RavelingInformation RavelingInformation { get; set; }
            [XmlElement(ElementName = "PotholesInformation")]
            public PotholesInformation PotholesInformation { get; set; }
            [XmlElement(ElementName = "MarkingContourInformation")]
            public MarkingContourInformation MarkingContourInformation { get; set; }
            [XmlElement(ElementName = "PatchDetectionInformation")]
            public PatchDetectionInformation PatchDetectionInformation { get; set; }
            [XmlElement(ElementName = "ConcreteJointInformation")]
            public ConcreteJointInformation ConcreteJointInformation { get; set; }
            [XmlElement(ElementName = "PickOutInformation")]
            public PickOutInformation PickOutInformation { get; set; }
            [XmlElement(ElementName = "SystemData")]
            public SystemData SystemData { get; set; }
            [XmlElement(ElementName = "GeometryReferenceCalibrationParams")]
            public string GeometryReferenceCalibrationParams { get; set; }
        }
        #endregion
    }
}
