#ifndef LCMS_STRUCT_H_
#define LCMS_STRUCT_H_
#include <stdio.h>
#include <string.h>

struct sLcmsSurveyInfo
{
    unsigned int  uiSurveyID;
    char          acSurveyPath[1024];
    unsigned char aucSensorEnable[2];
    int           iTotalNbSections;
    double        dTotalLength_m;
    double        dMeanSpeed_kmh;
    double        dFirstTimeStamp_s;
    double        dLastTimeStamp_s;
    double        dSectionLength_m;
    int           iSectionNbProfiles;
    unsigned int  uiNbValidSections;
};

struct sLcmsRoadSectionInfo
{
    unsigned int  uiSurveyID;
    unsigned int  uiSectionID;
    double        dDistBE_m[2];
    double        dTimeBE_s[2];
    unsigned int  uiNbProfiles;       // Nb profile in this road section
    double        dSectionLength_m;   // Length of the current section (m)
    double        dSpeed_kmh;
};

struct sLcmsRoadSectionFileInfo{
    char                 acFilePathName[1024];
    sLcmsRoadSectionInfo sRdSectionInfo;
};

struct sLcmsRoadSectionImage{
    int            iSectionId;
    unsigned char *pucImageLR[2];
    int            iImageWidth;
    int            iImageHeight;
};

struct sLcmsLaserStatus{
    unsigned char  ucLaserInterlockState;
    unsigned char  ucLaserReady;
    unsigned char  ucLaserEnable;
    unsigned int   uiLaserHourMeter;
    unsigned int   uiLaserTrigCountLSB;
    unsigned int   uiLaserTrigCountMSB;
    unsigned short usLaserCurrent_mA;
    unsigned short usLaserOutputPower;
    unsigned char  ucTecEnable;
    unsigned char  ucTecFault;
    float          fTecTemp_C;
    unsigned short usTecCurrent_mA;
    unsigned short usTecVoltage_mV;
};

struct sLcmsCameraStatus{
    float         fCamGainAnalog;             // Current Camera analog gain
    float         fCamOffsetAnalog;           // Current Camera analog offset
    float         fCamGainDigital;            // Current Camera digital gain
    float         fCamExposureTime_us;        // Current Camera exposure time
    float         fCamTemp_C;
    unsigned char ucCamFaultFatal;
    unsigned char ucCamFaultWarning;
    unsigned int  uiCamStatusBits;
    char          acCamExtendedStatusData[8];
};

struct sLcmsCameraInfo{
    char acCamProductID[32];
    char acCamSerialNum[32];
    char acCamExtendedInfo[32];
};

struct sLcmsImuInfo{
    char acImuSerialNumber[8];
    char acImuTemperature[8];
};

struct sLcmsImuExtInfo{
    char acImuAccRangeL[8];
    char acImuAccRangeR[8];
    unsigned char ucImuModelL;
    unsigned char ucImuModelR;
};

struct sLcmsSensorBoardInfo{
    char acSensorModel[32];
    char acSensorSerialNumber[32];
    char acSensorCalibrationNumber[32];
    char acSensorFirmwareNumber[32];
    char acSensorFirmwareRevisionNumber[32];
    char acSensorFirmwareDateOfCompilation[32];
    char acSensorFirmwareTimeOfCompilation[32];
};

struct sLcmsCtrlModuleBoardInfo{
    char acCtrlModuleModel[32];
    char acCtrlModuleSerialNumber[32];
    char acCtrlModuleFirmwareNumber[32];
    char acCtrlModuleFirmwareRevisionNumber[32];
    char acCtrlModuleFirmwareDateOfCompilation[32];
    char acCtrlModuleFirmwareTimeOfCompilation[32];
};

struct sLcmsSensorAcquiStatus{
    unsigned int   uiTrigCount;                // Nb encoder signal pulses received by the sensor
    float          fProfileAcqRate;            // Profile acquisition rate
    unsigned int   uiNbProfilesMissed;         // Total missed profiles = Profiles Skip + Profiles Lost
    unsigned int   uiNbProfilesLost;           // Triggered profiles not acquired at all (usually due to frame rate limit)
    unsigned int   uiNbProfilesSkip;           // Acquired profiles skipped due to Profile FIFO overflow
    float          fGrabFifoPercOcc;           // Load of the acquisition board buffer FIFO
    float          fProfileFifoPercOcc;        // Load of the profile FIFO.
    unsigned int   uiNbRoadSectionMissed;      // Total missed road sections = Rd. Sec. Skip + Lost
    unsigned int   uiNbRoadSectionLost;        // Road sections not acquired due to missed profiles.
    unsigned int   uiNbRoadSectionSkip;        // Acquired Road sections skipped due to Output FIFO overflow.
    float          fOutputFifoPercOcc;         // Load of the output FIFO (Output FIFO stores road sections ready to be read)
    unsigned int   uiFrameGrabberErrorCode;    //
    unsigned int   uiDataCompressionErrorCode; //
};

struct sLcmsSensorHeadStatus{

    static const int ciSensorStatusVersion=1;  // Version of this structure

    unsigned short usSensorBoardFault;       // Sensor boad status
    float          fSensorInternalTemp_C;    // Sensor head temp
    unsigned int   uiCameraMonitorErrorCode; // Error state of the camera monitoring process (0->No Error)
    unsigned int   uiSensorMonitorErrorCode; // Error state of the sensor monitoring process (0->No Error)

    sLcmsCameraStatus sCamStatus;   // Camera status
    sLcmsLaserStatus  sLaserStatus; // Laser status

    unsigned short usSensorBoard_SystemFaultBits;

    char acReserve[62];
};

const unsigned short cusSensorParamStructCurrentVersion = 5;

struct sLcmsSensorParam{

    unsigned int   uiNbProfiles;              // Number of profiles in each road section
    unsigned int   uiProfileNbPoints;         // Number of points in each profile
    unsigned int   uiRngCompressionType;      // Range data compression type 
    char           acRngCompressionParam[32]; // Range data compression parameter
    unsigned int   uiIntCompressionType;      // Intensity data compression parameter
    char           acIntCompressionParam[30]; // Intensity data compression parameter
    unsigned short usVersion;                 // Version of this structure
    float          fIntCompressionQuality;    // Intensity data compression quality [0 to 100]
    unsigned short usAcquisitionMode;         // Trig mode : 0->Hardware; 1->Soft Periodic; 2->Soft Manual
    char           acReserved1[2];
    float          fPeriodicTriggerFreq_Hz;   // Periodic trig. freq. for the 'Soft Periodic' Acquisition Mode
    unsigned int   uiCmosRoiWidth;            // CMOS sensor ROI width
    unsigned int   uiCmosRoiHeight;           // CMOS sensor ROI height
    unsigned int   uiCmosRoiStartCol;         // CMOS sensor ROI first column
    unsigned int   uiCmosRoiStartLine;        // CMOS sensor ROI first line
    unsigned int   uiPeakDetectorType;        // Peak detection algorithm Id
    unsigned char  ucPeakDetectionThreshold;  // Min intensity value
    char           acReserved2[3];
    int            iPeakSubPixelPrecision;    // 
    char           acPeakDetectionParam[64];  // Parameters of the peak detection algorithm.
    unsigned char  ucAgcEnable;               // 0 Disable - 1 Enable
    char           acReserved3[3];
    unsigned int   uiAgcType;                 // Agc algorithm Id
    char           acAgcParameters[64];       // Parameters of the Agc algorithm
    float          fAgcProfileRoiStart_norm;  // Start position of the profile ROI for the Agc, normalized value : [0 1] (0->0; 1->uiProfileNbPoints-1)
    float          fAgcProfileRoiStop_norm;   // Stop  position of the profile ROI for the Agc, normalized value : [0 1] (0->0; 1->uiProfileNbPoints-1)
    float          fCamGainAnalog;            // Initial Camera analog gain (The AGC may change it afterward)
    float          fCamOffsetAnalog;          // Initial Camera analog offset (The AGC may change it afterward)
    float          fCamGainDigital;           // Initial Camera digital gain (The AGC may change it afterward)
    float          fCamExposureTime_us;       // Initial Camera exposure time (The AGC may change it afterward)
    float          fLaser2CamExposureDelay_us;
    unsigned short usOutputFifoLength;        // Nb of Road section that the ouptut Fifo can Hold
    char           acReserved4[1];
    unsigned char  ucActiveCfdModes;          // Bit field indicating which Cfd Modes are actives. 0->Nothing is active
    float          fSensorAngle_deg;          // In degree. As seen from on top.
    unsigned short usLaserSetPoint_mA;
    unsigned short usLaserCurrentLimit_mA;
    float          fLaserTecSetPoint_C;
    float          fLaserTecTempLimMin_C;
    float          fLaserTecTempLimMax_C;
    int            iSensorOverlap_pix;
    unsigned int   uiSCScalibID;
    int            iImageVerticalOffset_prf;
    float          fSensorPitchAngle_deg;     // Installation pitch angle of the sensor. Rotation around the X axis of the vehicle ref. frame (V). At 0deg., the Z axis of the sensor is in the Z-X plane of V
    char           acReserved[112];
};

struct sLcmsSystemInfo{

    static const int ciSystemInfoVersion=2; // Version of this structure

    unsigned char            aucSensorEnable[2];    // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0

    char                     acSoftwareVersion[16]; // Acquisition Software Version
    sLcmsCtrlModuleBoardInfo sCtrlModuleInfo;       // Control module information
    sLcmsSensorBoardInfo     asSensorBoardInfo[2];  // Sensor board information
    sLcmsCameraInfo          asCamInfo[2];          // Camera information
    sLcmsImuInfo             asImuInfo[2];          // IMU information
    sLcmsImuExtInfo          sImuExtInfo;           // extended IMU information

    char chReserve[78]; 
}; //sizeof(sLcmsSystemInfo) should be 978 bytes

struct sLcmsSystemStatus{
    
    static const int ciSystemStatusVersion=1; // Version of this structure

    char         acSystemTimeAndDate[30]; // Time and date "YYYY/MM/DD HH:MM:SS.dddd" 
    unsigned int uiSystemFault;           // 0->No Fault; Otherwise, fault detected somewhere in system.
                                          // Check bit field to identify the fault source and then
                                          // the appropriate status field for more detailed information.

    char         cLaserInterlockState;           //
    unsigned int uiCtrlModuleMonitorErrorState;  // Error state of the sensor monitoring process (0->No Error)

    unsigned char         aucSensorEnable[2];    // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0
    sLcmsSensorHeadStatus asSensorHeadStatus[2];

    char chReserve[64];
};

struct sLcmsSystemParam{

    static const int ciSystemParamVersion=1; // Version of this structure

    unsigned char aucSensorEnable[2];        // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0
    double        dOdometerCountPerMeter;    // Odometer : Nb odometer count per meter.
    double        dClockFrequency;           // Clock counts per second
    unsigned int  uiInterSensorDistance_um;  // Distance between sensors, given in micron

    unsigned char ucEncoderInputDebouncing;  // 0->Disable; 1->Enable
    unsigned char ucEncoderInputChannel;     // 0->A; 1->B; 2->A and B
    unsigned char ucEncoderOutputSelect;     // 0->Encoder input; 1->Frequency divider
    unsigned int  uiEncoderRatio;            // [1 to 2^28] EncoderRatio = (Fout/Fin)*2^28

    char chReserve[128];
};


struct sLcmsLicenseInfo{
        char           acCreationDate[7];             //JJMMAA
        char           acExpirationDate[7];           //JJMMAA
        char           acModelNumber[5];
        char           acSerialNumber[6];
        unsigned int   uiProcessModuleBitField;
        unsigned char  ucSupportedAcquiLib_MajVer;    //0-255
        unsigned char  ucSupportedAcquiLib_MinVer;    //0-255
        unsigned char  ucSupportedAnalysisLib_MajVer; //0-255
        unsigned char  ucSupportedAnalysisLib_MinVer; //0-255 
};

#pragma pack(push, 8)

struct sLcmsGeometricParam{

    static const int ciGeometricParamVersion=2; // Version of this structure

    unsigned int uiCalibrationId;
    int    iVersion;
    double adE_VIl_Rxyz[3];
    double adE_VIl_Txyz[3];
    double adE_VIr_Rxyz[3];
    double adE_VIr_Txyz[3];
    double adE_FlFr_Rxyz[3];
    double adE_FlFr_Txyz[3];
    double adE_FlLl_Rxyz[3];
    double adE_FlLl_Txyz[3];
    double adE_FrLr_Rxyz[3];
    double adE_FrLr_Txyz[3];
    double adE_VFr_Rxyz[3];
    double adE_VFr_Txyz[3];
    double dTrackWidth_mm;
    int    iEncoderSide;            //0->Left; 1->Right
    float  fV_NominalHeight_mm;
    double adE_VA1_Txyz[3];

    char   chReserve[272];
};

struct sLcmsImuCalibParam{

    static const int ciImuCalibParamVersion=2; // Version of this structure

    unsigned int uiCalibrationId;
    int          iVersion;
    char         acCalibTimeAndDate[30]; // Time and date "YYYY/MM/DD HH:MM:SS.dddd"
    char         acSerialNumber[8];
    double       adGyroBiais_XYZ[3]; //degrees

    char         chReserve[128];
};

struct sLcmsInsOxtsCalibParam{
    static const int ciInsOxtsCalibParamVersion=3; // Version of this structure

    int          iVersion;
    unsigned int uiCalibrationId;

    double       adE_VO_Rxyz[3];
    double       adE_VO_Txyz[3];

    double       adGyroBiais_XYZ[3]; //degrees

    unsigned int uiDeviceSerialNumber;

    char         chReserve[224];
};

struct sLcmsPpsInfo{
    int           iNbrPpsID;
    int           *piNbrPpsCount;
    int           iNbrInputDescr;
    int           iMaxDescrSize;
    char          **pcPpsInputDescr;
};

struct sGPSdata
{
    //if acStructVersion=GPSv1.0, uiFraction is in tenth of second
    //if acStructVersion=GPSv1.1, uiFraction is in thousandth of second
    char acStructVersion[8];
    int iGPSdataValid;
    unsigned int uiMsgID;
    unsigned int uiSatelitesInUse;
    unsigned int uiSignalQuality;
    double dAltitude;
    double dLatitude;
    double dLongitude;
    double dGroundSpeed;
    double dCourseOverGround;
    unsigned int uiHour;
    unsigned int uiMinute;
    unsigned int uiSecond;
    unsigned int uiFraction;
    unsigned int uiYear;
    unsigned int uiMonth;
    unsigned int uiDay;
    //add new members here and adjust acReserved
    char acReserved[164];
}; //size should remain at 256 bytes

#pragma pack(pop)

const unsigned short cusLcmsInvalidDataVal = 0xFFFF;
const float           cfLcmsInvalidDataVal = -10000.0f;

const float  cfLcmsGeometricParam_InvalidDataVal = -1.0e9f;
const double cdLcmsGeometricParam_InvalidDataVal = -1.0e9;

#endif
