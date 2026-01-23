#ifndef LCMS_ANALYSER_LIB_H_
#define LCMS_ANALYSER_LIB_H_

#include "LcmsAnalyserLibStruct.h"

//C++
//#ifdef LCMSANALYSERLIB_EXPORTS
//#define LCMSANALYSERLIB_API __declspec(dllexport)
//#else
//#define LCMSANALYSERLIB_API __declspec(dllimport)
//#endif


#ifdef __BORLANDC__
#ifndef LCMSANALYSERLIB_API
#define LCMSANALYSERLIB_API extern "C" __declspec(dllimport)
#define NAME_CONV __cdecl
#endif
#endif

#if _MSC_VER || __MINGW64__ || __unix__
#ifndef LCMSANALYSERLIB_STATIC_LIB
  #ifndef LCMSANALYSERLIB_API
    #if _MSC_VER || __MINGW64__
      #define NAME_CONV __cdecl
      #ifndef LCMSANALYSERLIB_EXPORTS
        #define LCMSANALYSERLIB_API extern "C" __declspec(dllimport)
      #else
        #define LCMSANALYSERLIB_API extern "C" __declspec(dllexport)
      #endif
    #elif __unix__
      #define NAME_CONV
      #define LCMSANALYSERLIB_API
    #endif
  #endif
#else
  #ifndef LCMSANALYSERLIB_API
    #define NAME_CONV __cdecl
    #define LCMSANALYSERLIB_API
  #endif
#endif

#endif

#ifndef LCMSANALYSERLIB_API
#error Compiler not supported.
#endif

#define ERROR_CODE unsigned int
#define LCMS_ANALYSER_NO_ERROR                                          0
#define LCMS_ANALYSER_ERROR_READING_LUTS                                1
#define LCMS_ANALYSER_ERROR_LICENSE_OPTION_NOT_ALLOWED                  2
#define LCMS_ANALYSER_ERROR_EXPIRED_LICENSE                             3
#define LCMS_ANALYSER_ERROR_INVALID_LICENSE                             4
#define LCMS_ANALYSER_ERROR_LICENSE_FILE_NOT_FOUND                      5
#define LCMS_ANALYSER_ERROR_LICENCE_ACQUI_LIB_VER_NOT_SUPPORTED         6
#define LCMS_ANALYSER_ERROR_LICENCE_ANALYSIS_LIB_VER_NOT_SUPPORTED      7
#define LCMS_ANALYSER_ERROR_COULD_NOT_OPEN_FILE                         8
#define LCMS_ANALYSER_ERROR_NO_OPEN_FILE                                9
#define LCMS_ANALYSER_ERROR_TIMED_OUT                                   10
#define LCMS_ANALYSER_ERROR_INVALID_PROC_MODULE                         11
#define LCMS_ANALYSER_ERROR_NO_RESULT_AVAILABLE                         12
#define LCMS_ANALYSER_ERROR_INVALID_PARAMETER                           13
#define LCMS_ANALYSER_ERROR_NULL_POINTER                                14
#define LCMS_ANALYSER_ERROR_OUT_OF_MEMORY                               15
#define LCMS_ANALYSER_ERROR_COULD_NOT_WRITE_FILE                        16
#define LCMS_ANALYSER_ERROR_WHILE_READING_JPG_FILE                      17
#define LCMS_WAIT_INFINITE                                             -1

//Functions

LCMSANALYSERLIB_API void       NAME_CONV LcmsAnalyserDeinitialize();

//Lib Version
LCMSANALYSERLIB_API void       NAME_CONV LcmsGetLibVersion(char _acLibVersion[32]);

//Working folders
LCMSANALYSERLIB_API void       NAME_CONV LcmsSetLicensePath(const char *_pcLicensePath);
LCMSANALYSERLIB_API void       NAME_CONV LcmsSetLutsPath(const char *_pcLutsPath);
LCMSANALYSERLIB_API void       NAME_CONV LcmsSetConfigFileName(const char *_pcConfigFilePath, const char *_pcConfigFileName);
LCMSANALYSERLIB_API void       NAME_CONV LcmsSetCalibFileName(const char *_pcCalibFilePath, const char *_pcCalibFileName);
LCMSANALYSERLIB_API void       NAME_CONV LcmsSetModelsPath(const char *_pcModelsPath);

//Reading road section data
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsOpenRoadSection(const char * _pcFilename);
LCMSANALYSERLIB_API void       NAME_CONV LcmsCloseRoadSection();
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetRoadSectionInfo(sLcmsRoadSectionInfo *_pRdSectionInfo);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetSystemData(sLcmsSystemParam *_pSystemParam, sLcmsSystemStatus *_pSystemStatus, sLcmsSystemInfo * _pSystemInfo, sLcmsSensorParam *_aSensorParam, sLcmsSensorAcquiStatus *_aSensorAcquiStatus);

//Processing parameters
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetProcessingParams(const char *_pcProcessingParamsString, void *_pUserVarPtr);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsSetProcessingParams(const char *_pcProcessingParamsString, void *_pUserVarPtr);



//Reading user data
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetUserData(char * _pcUserData); 
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetUserDataSize(int &_iDataSize); 

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetTransverseProfilesX(float * _pXTransProfiles[2]);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetTransverseRngProfile(int _iProfileIndex, float _fProfileSlope, float* _pfRngProfX, float* _pfRngProfZ, int& _iNbrValidPts);

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetStitchedRngProfile(int _iProfileIndex,float _fLeftProfileSlope,float _fRightProfileSlope,
                                                                   float *_pfRngProfX, float *_pfRngProfZ,int &_iNbrValidPts);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetStitchedRngIntProfile(int _iProfileIndex,float _fLeftProfileSlope,float _fRightProfileSlope,
                                                                   float *_pfRngProfX, float *_pfRngProfZ, unsigned char *_pucInt, int &_iNbrValidPts);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetStitchedRngProfileCalib(int _iProfileIndex,float *_pfRngProfX, float *_pfRngProfZ,int &_iNbrValidPts);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetStitchedRngProfileVehicle(int _iProfileIndex, float *_pfRngProfX, float *_pfRngProfZ, int &_iNbrValidPts);

//Get survey information 
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetSurveyInfo(sLcmsSurveyInfo * _psSurveyInfo, int _iMilliseconds=LCMS_WAIT_INFINITE);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetSurveyRoadSectionList(sLcmsRoadSectionFileInfo *_psRoadSectionInfo, int _iNbMaxElem, int _iMilliseconds=LCMS_WAIT_INFINITE);

//Road analysis : select processing block
LCMSANALYSERLIB_API void       NAME_CONV LcmsGetProcessingModuleSelection     (unsigned int *_puiProcessSelectBitField);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsAddProcessingModuleToSelection   (unsigned int _uiProcessSelectBitField);
LCMSANALYSERLIB_API void       NAME_CONV LcmsRemoveProcessingModuleToSelection(unsigned int _uiProcessSelectBitField);

//Perform analysis of the road section
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsProcessRoadSection();

typedef int(*LcmsRangeProgressCallback)(void*, int, int, float);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsProcessRoadSectionRange(const char* _pcFilename, unsigned int _uiRdSectIDBeg, unsigned int _uiRdSectIDEnd, LcmsRangeProgressCallback callback, void* context);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsAbortRoadSectionRange();

//Road section analysis : Get result
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetResult(char **_pcXmlResultString, unsigned int *_puiStringLength);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetResultImage(int _iImageType, sLcmsResultImage **_psResultImage);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetNumberFODs(int *_iFODNumbers);


LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsCreateOverlayImage         (const char *_pcXmlResultString,   unsigned int _uiProcessSelectBitField, char *_pcOptions, sLcmsResultImage *_psBackgroundImage, sLcmsResultImage **_psOverlayImage); //_pucBackgroundImage must be gray scale (type LCMS_IMAGE_MERGED_INTENSITY or LCMS_IMAGE_MERGED_RANGE)
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsCreateOverlayImageFromFiles(const char *_pcXmlResultFileName, unsigned int _uiProcessSelectBitField, char *_pcOptions, const char *_pcBackgroundImageFileName, sLcmsResultImage **_psOverlayImage);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsSaveResultShapefile(const char *_pcXmlResultString, unsigned int _uiProcessSelectBitField, const char *_pcShapefileBaseName, bool _bGlobalShpfile = true, const char* _pcNameSuffix = "");


LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsSaveResultImage(const char * _pcFilename, sLcmsResultImage *_psResultImage);

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsComputeLongitudinalProfile       (float _fStartPositionInSurvey_m, float _fLongProfileLength_m, const char *_pcFilenamePrefix, int _iReturnAtCompletion);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetComputeLongProfileStatus      (float &_fPercCompletion, int &_iDone);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLongProfileIRIvalues(float &_fLeftIRI,float &_fRightIRI,float &_fCenterIRI);

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV _LcmsGetLongProfileNbrPtsElevation(int _aiNbrPtsElevation[3]);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV _LcmsGetLongProfileElevation(float* _apfElevationProfileLeft, float* _apfElevationProfileCenter, float* _apfElevationProfileRight);

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLongProfileElevation(int _aiNbrPtsElevation[3],float *_apfElevatiovProfile[3]);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsWaitComputeLongProfileCompletion (float &_fPercCompletion, int &_iDone, int _iWaitTimeout_ms);
LCMSANALYSERLIB_API void       NAME_CONV LcmsAbortComputeLongProfile();

//Get license infomation :
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLicenseInfo(sLcmsLicenseInfo* _psLicenseInfo);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLicenseInfoEx(sLcmsLicenseInfo* _psLicenseInfo, unsigned int* _puiExtendedInfo);
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLicenseInfoForString(const char * _pcLicenseString, sLcmsLicenseInfo *_psLicenseInfo);


//Gestion error 201004 Corrupted File
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsGetLastCorruptedFilePathName(char * _pcFilename);
// Check if FIS file is corrupted in anyway 
LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsCheckCorruptedFile          (char* _pcFileName, unsigned int &_uiSurveyID, unsigned int &_uiRdSectID, unsigned int &_uiErrorCode);

LCMSANALYSERLIB_API ERROR_CODE NAME_CONV LcmsInsertGpsInfo2Fis(const char * _pcFilenameIn, const char * _pcFilenameOut,
                                                               unsigned int _iNbrGPSCoords, sGPSdata *_psGpsData);
LCMSANALYSERLIB_API void NAME_CONV LcmsGetLutsInfo(int _aiLutInfo[2]);
#endif
