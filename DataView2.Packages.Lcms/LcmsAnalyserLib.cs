using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace DataView2.Packages.Lcms
{

    public partial class LcmsAnalyserLib
    {
        //if this doesn't work check the properties of LcmsAnalyserLib.dll (located in the same folder)
        //and set it to "copy always"..
        private string LcmsAnalyserDll;


        public enum ErrorCodes
        {
            LCMS_ANALYSER_NO_ERROR = 0,
            LCMS_ANALYSER_ERROR_READING_LUTS = 1,
            LCMS_ANALYSER_ERROR_LICENSE_OPTION_NOT_ALLOWED = 2,
            LCMS_ANALYSER_ERROR_EXPIRED_LICENSE = 3,
            LCMS_ANALYSER_ERROR_INVALID_LICENSE = 4,
            LCMS_ANALYSER_ERROR_LICENSE_FILE_NOT_FOUND = 5,
            LCMS_ANALYSER_ERROR_LICENCE_ACQUI_LIB_VER_NOT_SUPPORTED = 6,
            LCMS_ANALYSER_ERROR_LICENCE_ANALYSIS_LIB_VER_NOT_SUPPORTED = 7,
            LCMS_ANALYSER_ERROR_COULD_NOT_OPEN_FILE = 8,
            LCMS_ANALYSER_ERROR_NO_OPEN_FILE = 9,
            LCMS_ANALYSER_ERROR_TIMED_OUT = 10,
            LCMS_ANALYSER_ERROR_INVALID_PROC_MODULE = 11,
            LCMS_ANALYSER_ERROR_NO_RESULT_AVAILABLE = 12,
            LCMS_ANALYSER_ERROR_INVALID_PARAMETER = 13,
            LCMS_ANALYSER_ERROR_NULL_POINTER = 14,
            LCMS_ANALYSER_ERROR_OUT_OF_MEMORY = 15,
            LCMS_ANALYSER_ERROR_COULD_NOT_WRITE_FILE = 16,
            LCMS_ANALYSER_ERROR_WHILE_READING_JPG_FILE = 17,
            LCMS_WAIT_INFINITE = -1
        }


        private IntPtr hModule;

        // Delegados para las funciones
        public delegate void LcmsAnalyserDeinitializeDelegate();
        public delegate void LcmsGetLibVersionDelegate([MarshalAs(UnmanagedType.LPArray)] byte[] _acLibVersion);
        public delegate void LcmsSetLicensePathDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcLicensePath);
        public delegate void LcmsSetLutsPathDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcLutsPath);
        public delegate void LcmsSetConfigFileNameDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcConfigFilePath, [MarshalAs(UnmanagedType.LPStr)] string _pcConfigFileName);
        public delegate void LcmsSetCalibFileNameDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcCalibFilePath, [MarshalAs(UnmanagedType.LPStr)] string _pcCalibFileName);
        public delegate void LcmsSetModelsPathDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcModelsPath);
        //Reading road section data
        public delegate uint LcmsOpenRoadSectionDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcFilename);
        public delegate void LcmsCloseRoadSectionDelegate();
        public delegate uint LcmsGetRoadSectionInfoDelegate(ref sLcmsRoadSectionInfo _pRdSectionInfo);
        public delegate uint LcmsGetSystemDataDelegate(ref sLcmsSystemParam _pSystemParam, ref sLcmsSystemStatus _pSystemStatus, ref sLcmsSystemInfo _pSystemInfo, ref sLcmsSensorParam _aSensorParam, ref sLcmsSensorAcquiStatus _aSensorAcquiStatus);

        // Processing parameters
        public delegate uint LcmsGetProcessingParamsDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcProcessingParamsString, IntPtr _pUserVarPtr);
        public delegate uint LcmsSetProcessingParamsDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcProcessingParamsString, IntPtr _pUserVarPtr);

        // Reading user data
        public delegate uint LcmsGetUserDataDelegate(sbyte[] _pcUserData);
        public delegate uint LcmsGetUserDataSizeDelegate(ref int _iDataSize);
        public delegate uint LcmsGetTransverseProfilesXDelegate(ref float[] _pXTransProfiles, int arraySize);
        public delegate uint LcmsGetStitchedRngProfileDelegate(int _iProfileIndex, float _fLeftProfileSlope, float _fRightProfileSlope, ref float _pfRngProfX, ref float _pfRngProfZ, ref int _iNbrValidPts);
        public delegate uint LcmsGetStitchedRngIntProfileDelegate(int _iProfileIndex, float _fLeftProfileSlope, float _fRightProfileSlope, ref float _pfRngProfX, ref float _pfRngProfZ, ref byte[] _pucInt, ref int _iNbrValidPts);
        public delegate uint LcmsGetStitchedRngProfileCalibDelegate(int _iProfileIndex, ref float _pfRngProfX, ref float _pfRngProfZ, ref int _iNbrValidPts);
        public delegate uint LcmsGetStitchedRngProfileVehicleDelegate(int _iProfileIndex, ref float _pfRngProfX, ref float _pfRngProfZ, ref int _iNbrValidPts);

        // Get survey information
        public delegate uint LcmsGetSurveyInfoDelegate(ref sLcmsSurveyInfo _psSurveyInfo, int _iMilliseconds);
        public delegate uint LcmsGetSurveyRoadSectionListDelegate(IntPtr _psRoadSectionInfo, int _iNbMaxElem, int _iMilliseconds);

        // Road analysis: select processing block
        public delegate void LcmsGetProcessingModuleSelectionDelegate(ref uint _puiProcessSelectBitField);
        public delegate uint LcmsAddProcessingModuleToSelectionDelegate(uint _uiProcessSelectBitField);
        public delegate void LcmsRemoveProcessingModuleToSelectionDelegate(uint _uiProcessSelectBitField);

        // Perform analysis of the road section
        public delegate int LcmsProcessRoadSectionDelegate();

        // Road section analysis: Get result
        public delegate uint LcmsGetResultDelegate(ref IntPtr _pcXmlResultString, ref uint _puiStringLength);
        public delegate uint LcmsGetResultImageDelegate(int _iImageType, ref IntPtr _psResultImage);
        public delegate uint LcmsGetNumberFODsDelegate(ref int _iFODNumbers);
        public delegate uint LcmsCreateOverlayImageDelegate(string _pcXmlResultString, uint _uiProcessSelectBitField, ref sbyte[] _pcOptions, ref sLcmsResultImage _psBackgroundImage, ref IntPtr _psOverlayImage);
        public delegate uint LcmsCreateOverlayImageFromFilesDelegate(sbyte[] _pcXmlResultFileName, uint _uiProcessSelectBitField, sbyte[] _pcOptions, sbyte[] _pcBackgroundImageFileName, ref IntPtr _psOverlayImage);
        public delegate uint LcmsSaveResultShapefileDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcXmlResultString, uint _uiProcessSelectBitField, [MarshalAs(UnmanagedType.LPStr)] string _pcShapefileBaseName, bool _bGlobalShpfile, [MarshalAs(UnmanagedType.LPStr)] string _pcNameSuffix);
        public delegate uint LcmsSaveResultImageDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcFilename, ref sLcmsResultImage _psResultImage);
        public delegate uint LcmsComputeLongitudinalProfileDelegate(float _fStartPositionInSurvey_m, float _fLongProfileLength_m, string _pcFilenamePrefix, int _iReturnAtCompletion);
        public delegate uint LcmsGetComputeLongProfileStatusDelegate(ref float _fPercCompletion, ref int _iDone);
        public delegate uint LcmsGetLongProfileIRIvaluesDelegate(ref float _fLeftIRI, ref float _fRightIRI, ref float _fCenterIRI);
        public delegate uint LcmsGetLongProfileNbrPtsElevationDelegate(int[] _aiNbrPtsElevation);
        public delegate uint LcmsGetLongProfileElevationDelegate(int[] _aiNbrPtsElevation, float[] _apfElevatiovProfile);
        public delegate uint LcmsWaitComputeLongProfileCompletionDelegate(ref float _fPercCompletion, ref int _iDone, int _iWaitTimeout_ms);
        public delegate void LcmsAbortComputeLongProfileDelegate();


        // Get license infomation :
        public delegate uint LcmsGetLicenseInfoDelegate(ref sLcmsLicenseInfo _psLicenseInfo);
        public delegate uint LcmsGetLicenseInfoForStringDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcLicenseString, ref sLcmsLicenseInfo _psLicenseInfo);

        // Gestion error 201004 Corrupted File
        public delegate uint LcmsGetLastCorruptedFilePathNameDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcFilename);
        // Check if FIS file is corrupted in anyway
        public delegate uint LcmsCheckCorruptedFileDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcFileName, ref uint _uiSurveyID, ref uint _uiRdSectID, ref uint _uiErrorCode);
        public delegate uint LcmsInsertGpsInfo2FisDelegate([MarshalAs(UnmanagedType.LPStr)] string _pcFilenameIn, [MarshalAs(UnmanagedType.LPStr)] string _pcFilenameOut, uint _iNbrGPSCoords, ref sGPSdata _psGpsData);
        public delegate void LcmsGetLutsInfoDelegate(int[] _aiLutInfo);

        // Punteros a las funciones
        public LcmsAnalyserDeinitializeDelegate LcmsAnalyserDeinitialize;
        public LcmsGetLibVersionDelegate LcmsGetLibVersion;
        public LcmsSetLicensePathDelegate LcmsSetLicensePath;
        public LcmsSetLutsPathDelegate LcmsSetLutsPath;
        public LcmsSetConfigFileNameDelegate LcmsSetConfigFileName;
        public LcmsSetCalibFileNameDelegate LcmsSetCalibFileName;
        public LcmsSetModelsPathDelegate LcmsSetModelsPath;

        public LcmsOpenRoadSectionDelegate LcmsOpenRoadSection;
        public LcmsCloseRoadSectionDelegate LcmsCloseRoadSection;
        public LcmsGetRoadSectionInfoDelegate LcmsGetRoadSectionInfo;
        public LcmsGetSystemDataDelegate LcmsGetSystemData;

        public LcmsGetProcessingParamsDelegate LcmsGetProcessingParams;
        public LcmsSetProcessingParamsDelegate LcmsSetProcessingParams;

        public LcmsGetUserDataDelegate LcmsGetUserData;
        public LcmsGetUserDataSizeDelegate LcmsGetUserDataSize;
        public LcmsGetTransverseProfilesXDelegate LcmsGetTransverseProfilesX;
        public LcmsGetStitchedRngProfileDelegate LcmsGetStitchedRngProfile;
        public LcmsGetStitchedRngIntProfileDelegate LcmsGetStitchedRngIntProfile;
        public LcmsGetStitchedRngProfileCalibDelegate LcmsGetStitchedRngProfileCalib;
        public LcmsGetStitchedRngProfileVehicleDelegate LcmsGetStitchedRngProfileVehicle;

        public LcmsGetSurveyInfoDelegate LcmsGetSurveyInfo;
        public LcmsGetSurveyRoadSectionListDelegate LcmsGetSurveyRoadSectionList;

        public LcmsGetProcessingModuleSelectionDelegate LcmsGetProcessingModuleSelection;
        public LcmsAddProcessingModuleToSelectionDelegate LcmsAddProcessingModuleToSelection;
        public LcmsRemoveProcessingModuleToSelectionDelegate LcmsRemoveProcessingModuleToSelection;

        public LcmsProcessRoadSectionDelegate LcmsProcessRoadSection;

        public LcmsGetResultDelegate LcmsGetResult;
        public LcmsGetResultImageDelegate LcmsGetResultImage;
        public LcmsGetNumberFODsDelegate LcmsGetNumberFODs;
        public LcmsCreateOverlayImageDelegate LcmsCreateOverlayImage;
        public LcmsCreateOverlayImageFromFilesDelegate LcmsCreateOverlayImageFromFiles;
        public LcmsSaveResultShapefileDelegate LcmsSaveResultShapefile;
        public LcmsSaveResultImageDelegate LcmsSaveResultImage;
        public LcmsComputeLongitudinalProfileDelegate LcmsComputeLongitudinalProfile;
        public LcmsGetComputeLongProfileStatusDelegate LcmsGetComputeLongProfileStatus;
        public LcmsGetLongProfileIRIvaluesDelegate LcmsGetLongProfileIRIvalues;
        public LcmsGetLongProfileNbrPtsElevationDelegate LcmsGetLongProfileNbrPtsElevation;
        public LcmsGetLongProfileElevationDelegate LcmsGetLongProfileElevation;
        public LcmsWaitComputeLongProfileCompletionDelegate LcmsWaitComputeLongProfileCompletion;
        public LcmsAbortComputeLongProfileDelegate LcmsAbortComputeLongProfile;

        public LcmsGetLicenseInfoDelegate LcmsGetLicenseInfo;
        public LcmsGetLicenseInfoForStringDelegate LcmsGetLicenseInfoForString;

        public LcmsGetLastCorruptedFilePathNameDelegate LcmsGetLastCorruptedFilePathName;
        public LcmsCheckCorruptedFileDelegate LcmsCheckCorruptedFile;
        public LcmsInsertGpsInfo2FisDelegate LcmsInsertGpsInfo2Fis;
        public LcmsGetLutsInfoDelegate LcmsGetLutsInfo;


        public LcmsAnalyserLib(int numberInstance)
        {
            uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); ;
            string instanceFolder = Path.Combine(baseDir, $"LcmsDll{numberInstance}");


            string dllFullPath = Path.Combine(instanceFolder, $"LcmsAnalyserLib.dll");

            if (!File.Exists(dllFullPath))
                throw new FileNotFoundException("DLL not found", dllFullPath);

        hModule = LoadLibrary(dllFullPath);
      //  hModule = LoadLibraryEx(dllFullPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);

            if (hModule == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library: {dllFullPath}");
            }

            // Cargar las funciones
            LcmsSetLicensePath = LoadFunction<LcmsSetLicensePathDelegate>("LcmsSetLicensePath");
            LcmsAnalyserDeinitialize = LoadFunction<LcmsAnalyserDeinitializeDelegate>("LcmsAnalyserDeinitialize");
            LcmsGetLibVersion = LoadFunction<LcmsGetLibVersionDelegate>("LcmsGetLibVersion");
            LcmsSetLicensePath = LoadFunction<LcmsSetLicensePathDelegate>("LcmsSetLicensePath");
            LcmsSetLutsPath = LoadFunction<LcmsSetLutsPathDelegate>("LcmsSetLutsPath");
            LcmsSetConfigFileName = LoadFunction<LcmsSetConfigFileNameDelegate>("LcmsSetConfigFileName");
            LcmsSetCalibFileName = LoadFunction<LcmsSetCalibFileNameDelegate>("LcmsSetCalibFileName");
            LcmsSetModelsPath = LoadFunction<LcmsSetModelsPathDelegate>("LcmsSetModelsPath");
            LcmsOpenRoadSection = LoadFunction<LcmsOpenRoadSectionDelegate>("LcmsOpenRoadSection");
            LcmsCloseRoadSection = LoadFunction<LcmsCloseRoadSectionDelegate>("LcmsCloseRoadSection");
            LcmsGetRoadSectionInfo = LoadFunction<LcmsGetRoadSectionInfoDelegate>("LcmsGetRoadSectionInfo");
            LcmsGetSystemData = LoadFunction<LcmsGetSystemDataDelegate>("LcmsGetSystemData");
            LcmsGetProcessingParams = LoadFunction<LcmsGetProcessingParamsDelegate>("LcmsGetProcessingParams");
            LcmsSetProcessingParams = LoadFunction<LcmsSetProcessingParamsDelegate>("LcmsSetProcessingParams");
            LcmsGetUserData = LoadFunction<LcmsGetUserDataDelegate>("LcmsGetUserData");
            LcmsGetUserDataSize = LoadFunction<LcmsGetUserDataSizeDelegate>("LcmsGetUserDataSize");
            LcmsGetTransverseProfilesX = LoadFunction<LcmsGetTransverseProfilesXDelegate>("LcmsGetTransverseProfilesX");
            LcmsGetStitchedRngProfile = LoadFunction<LcmsGetStitchedRngProfileDelegate>("LcmsGetStitchedRngProfile");
            LcmsGetStitchedRngIntProfile = LoadFunction<LcmsGetStitchedRngIntProfileDelegate>("LcmsGetStitchedRngIntProfile");
            LcmsGetStitchedRngProfileCalib = LoadFunction<LcmsGetStitchedRngProfileCalibDelegate>("LcmsGetStitchedRngProfileCalib");
            LcmsGetStitchedRngProfileVehicle = LoadFunction<LcmsGetStitchedRngProfileVehicleDelegate>("LcmsGetStitchedRngProfileVehicle");
            LcmsGetSurveyInfo = LoadFunction<LcmsGetSurveyInfoDelegate>("LcmsGetSurveyInfo");
            LcmsGetSurveyRoadSectionList = LoadFunction<LcmsGetSurveyRoadSectionListDelegate>("LcmsGetSurveyRoadSectionList");
            LcmsGetProcessingModuleSelection = LoadFunction<LcmsGetProcessingModuleSelectionDelegate>("LcmsGetProcessingModuleSelection");
            LcmsAddProcessingModuleToSelection = LoadFunction<LcmsAddProcessingModuleToSelectionDelegate>("LcmsAddProcessingModuleToSelection");
            LcmsRemoveProcessingModuleToSelection = LoadFunction<LcmsRemoveProcessingModuleToSelectionDelegate>("LcmsRemoveProcessingModuleToSelection");
            LcmsProcessRoadSection = LoadFunction<LcmsProcessRoadSectionDelegate>("LcmsProcessRoadSection");
            LcmsGetResult = LoadFunction<LcmsGetResultDelegate>("LcmsGetResult");
            LcmsGetResultImage = LoadFunction<LcmsGetResultImageDelegate>("LcmsGetResultImage");
            LcmsGetNumberFODs = LoadFunction<LcmsGetNumberFODsDelegate>("LcmsGetNumberFODs");
            LcmsCreateOverlayImage = LoadFunction<LcmsCreateOverlayImageDelegate>("LcmsCreateOverlayImage");
            LcmsCreateOverlayImageFromFiles = LoadFunction<LcmsCreateOverlayImageFromFilesDelegate>("LcmsCreateOverlayImageFromFiles");
            LcmsSaveResultShapefile = LoadFunction<LcmsSaveResultShapefileDelegate>("LcmsSaveResultShapefile");
            LcmsSaveResultImage = LoadFunction<LcmsSaveResultImageDelegate>("LcmsSaveResultImage");
            LcmsComputeLongitudinalProfile = LoadFunction<LcmsComputeLongitudinalProfileDelegate>("LcmsComputeLongitudinalProfile");
            LcmsGetComputeLongProfileStatus = LoadFunction<LcmsGetComputeLongProfileStatusDelegate>("LcmsGetComputeLongProfileStatus");
            LcmsGetLongProfileIRIvalues = LoadFunction<LcmsGetLongProfileIRIvaluesDelegate>("LcmsGetLongProfileIRIvalues");
            //LcmsGetLongProfileNbrPtsElevation = LoadFunction<LcmsGetLongProfileNbrPtsElevationDelegate>("LcmsGetLongProfileNbrPtsElevation");
            LcmsGetLongProfileElevation = LoadFunction<LcmsGetLongProfileElevationDelegate>("LcmsGetLongProfileElevation");
            LcmsWaitComputeLongProfileCompletion = LoadFunction<LcmsWaitComputeLongProfileCompletionDelegate>("LcmsWaitComputeLongProfileCompletion");
            LcmsAbortComputeLongProfile = LoadFunction<LcmsAbortComputeLongProfileDelegate>("LcmsAbortComputeLongProfile");
            LcmsGetLicenseInfo = LoadFunction<LcmsGetLicenseInfoDelegate>("LcmsGetLicenseInfo");
            LcmsGetLicenseInfoForString = LoadFunction<LcmsGetLicenseInfoForStringDelegate>("LcmsGetLicenseInfoForString");
            LcmsGetLastCorruptedFilePathName = LoadFunction<LcmsGetLastCorruptedFilePathNameDelegate>("LcmsGetLastCorruptedFilePathName");
            LcmsCheckCorruptedFile = LoadFunction<LcmsCheckCorruptedFileDelegate>("LcmsCheckCorruptedFile");
            LcmsInsertGpsInfo2Fis = LoadFunction<LcmsInsertGpsInfo2FisDelegate>("LcmsInsertGpsInfo2Fis");
            LcmsGetLutsInfo = LoadFunction<LcmsGetLutsInfoDelegate>("LcmsGetLutsInfo");
            // No olvides liberar la DLL cuando ya no sea necesaria
            // FreeLibrary(hModule);
        }

        private T LoadFunction<T>(string functionName)
        {
            IntPtr pAddressOfFunctionToCall = GetProcAddress(hModule, functionName);
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                throw new Exception($"Failed to get address for {functionName}");
            }

            return (T)(object)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(T));
        }

       

        // P/Invoke para cargar y liberar la DLL
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public void Dispose()
        {
            //FreeLibrary(hModule);
        }
      
    }
}

