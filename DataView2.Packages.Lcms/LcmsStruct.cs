using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DataView2.Packages.Lcms;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSurveyInfo
{
    public uint uiSurveyID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] public sbyte[] acSurveyPath;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] aucSensorEnable;
    public int iTotalNbSections;
    public double dTotalLength_m;
    public double dMeanSpeed_kmh;
    public double dFirstTimeStamp_s;
    public double dLastTimeStamp_s;
    public double dSectionLength_m;
    public int iSectionNbProfiles;
    public uint uiNbValidSections;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsRoadSectionInfo
{
    public uint uiSurveyID;
    public uint uiSectionID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] dDistBE_m;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] dTimeBE_s;
    public uint uiNbProfiles;       // Nb profile in this road section
    public double dSectionLength_m;   // Length of the current section (m)
    public double dSpeed_kmh;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsRoadSectionFileInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] 
    public string acFilePathName;
    public sLcmsRoadSectionInfo sRdSectionInfo;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsRoadSectionImage
{
    public int iSectionId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public IntPtr[] pucImageLR; //unsigned char* 
    public int iImageWidth;
    public int iImageHeight;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsLaserStatus
{
    public byte ucLaserInterlockState;
    public byte ucLaserReady;
    public byte ucLaserEnable;
    public uint uiLaserHourMeter;
    public uint uiLaserTrigCountLSB;
    public uint uiLaserTrigCountMSB;
    public ushort usLaserCurrent_mA;
    public ushort usLaserOutputPower;
    public byte ucTecEnable;
    public byte ucTecFault;
    public float fTecTemp_C;
    public ushort usTecCurrent_mA;
    public ushort usTecVoltage_mV;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsCameraStatus
{
    public float fCamGainAnalog; // Current Camera analog gain
    public float fCamOffsetAnalog; // Current Camera analog offset
    public float fCamGainDigital; // Current Camera digital gain
    public float fCamExposureTime_us; // Current Camera exposure time
    public float fCamTemp_C;
    public byte ucCamFaultFatal;
    public byte ucCamFaultWarning;
    public uint uiCamStatusBits;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acCamExtendedStatusData;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsCameraInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCamProductID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCamSerialNum;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCamExtendedInfo;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsImuInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acImuSerialNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acImuTemperature;
};

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsImuExtInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acImuAccRangeL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acImuAccRangeR;
    public byte ucImuModelL;
    public byte ucImuModelR;
};

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSensorBoardInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acSensorModel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] asSensorSerialNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] asSensorCalirationNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acSensorFirmwareNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acSensorFirmwareRevisionNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acSensorFirmwareDateOfCompilation;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acSensorFirmwareTimeOfCompilation;

    public sLcmsSensorBoardInfo()
    {
        acSensorModel = new sbyte[32];
        asSensorSerialNumber = new sbyte[32];
        asSensorCalirationNumber = new sbyte[32];
        acSensorFirmwareNumber = new sbyte[32];
        acSensorFirmwareRevisionNumber = new sbyte[32];
        acSensorFirmwareDateOfCompilation = new sbyte[32];
        acSensorFirmwareTimeOfCompilation = new sbyte[32];
    }

    public readonly sLcmsSensorBoardInfo Copy()
    {
        sLcmsSensorBoardInfo copy = new();

        Array.Copy(acSensorModel, copy.acSensorModel, 32);
        Array.Copy(asSensorSerialNumber, copy.asSensorSerialNumber, 32);
        Array.Copy(asSensorCalirationNumber, copy.asSensorCalirationNumber, 32);
        Array.Copy(acSensorFirmwareNumber, copy.acSensorFirmwareNumber, 32);
        Array.Copy(acSensorFirmwareRevisionNumber, copy.acSensorFirmwareRevisionNumber, 32);
        Array.Copy(acSensorFirmwareDateOfCompilation, copy.acSensorFirmwareDateOfCompilation, 32);
        Array.Copy(acSensorFirmwareTimeOfCompilation, copy.acSensorFirmwareTimeOfCompilation, 32);

        return copy;
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not sLcmsSensorBoardInfo sbi) { return false; }

        return acSensorModel.SequenceEqual(sbi.acSensorModel) &
            asSensorSerialNumber.SequenceEqual(sbi.asSensorSerialNumber) &
            asSensorCalirationNumber.SequenceEqual(sbi.asSensorCalirationNumber) &
            acSensorFirmwareNumber.SequenceEqual(sbi.acSensorFirmwareNumber) &
            acSensorFirmwareRevisionNumber.SequenceEqual(sbi.acSensorFirmwareRevisionNumber) &
            acSensorFirmwareDateOfCompilation.SequenceEqual(sbi.acSensorFirmwareDateOfCompilation) &
            acSensorFirmwareTimeOfCompilation.SequenceEqual(sbi.acSensorFirmwareTimeOfCompilation);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(acSensorModel, asSensorSerialNumber,
            asSensorCalirationNumber, acSensorFirmwareNumber, acSensorFirmwareRevisionNumber,
            acSensorFirmwareDateOfCompilation, acSensorFirmwareTimeOfCompilation);
    }

    public static bool operator ==(sLcmsSensorBoardInfo left, sLcmsSensorBoardInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(sLcmsSensorBoardInfo left, sLcmsSensorBoardInfo right)
    {
        return !(left == right);
    }
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsCtrlModuleBoardInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleModel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleSerialNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleFirmwareNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleFirmwareRevisionNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleFirmwareDateOfCompilation;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acCtrlModuleFirmwareTimeOfCompilation;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSensorAcquiStatus
{
    public uint uiTrigCount;                // Nb encoder signal pulses received by the sensor
    public float fProfileAcqRate;            // Profile acquisition rate
    public uint uiNbProfilesMissed;         // Total missed profiles = Profiles Skip + Profiles Lost
    public uint uiNbProfilesLost;           // Triggered profiles not acquired at all (usually due to frame rate limit)
    public uint uiNbProfilesSkip;           // Acquired profiles skipped due to Profile FIFO overflow
    public float fGrabFifoPercOcc;           // Load of the acquisition board buffer FIFO
    public float fProfileFifoPercOcc;        // Load of the profile FIFO.
    public uint uiNbRoadSectionMissed;      // Total missed road sections = Rd. Sec. Skip + Lost
    public uint uiNbRoadSectionLost;        // Road sections not acquired due to missed profiles.
    public uint uiNbRoadSectionSkip;        // Acquired Road sections skipped due to Output FIFO overflow.
    public float fOutputFifoPercOcc;         // Load of the output FIFO (Output FIFO stores road sections ready to be read)
    public uint uiFrameGrabberErrorCode;    //
    public uint uiDataCompressionErrorCode; //
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSensorHeadStatus
{
    public const int ciSensorStatusVersion = 1;  // Version of this structure

    public ushort usSensorBoardFault;       // Sensor boad status
    public float fSensorInternalTemp_C;    // Sensor head temp
    public uint uiCameraMonitorErrorCode; // Error state of the camera monitoring process (0->No Error)
    public uint uiSensorMonitorErrorCode; // Error state of the sensor monitoring process (0->No Error)

    public sLcmsCameraStatus sCamStatus;   // Camera status
    public sLcmsLaserStatus sLaserStatus; // Laser status

    public ushort usSensorBoard_SystemFaultBits;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 62)] public sbyte[] acReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSensorParam
{
    public const int ciSensorParamVersion = 3;  // Version of this structure

    public uint uiNbProfiles;              // Number of profiles in each road section
    public uint uiProfileNbPoints;         // Number of points in each profile
    public uint uiRngCompressionType;      // Range data compression type 
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acRngCompressionParam; // Range data compression parameter
    public uint uiIntCompressionType;      // Intensity data compression parameter
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public sbyte[] acIntCompressionParam; // Intensity data compression parameter
    public float fIntCompressionQuality;    // Intensity data compression quality [0 to 100]
    public ushort usAcquisitionMode;         // Trig mode : 0->Hardware; 1->Soft Periodic; 2->Soft Manual
    public float fPeriodicTriggerFreq_Hz;   // Periodic trig. freq. for the 'Soft Periodic' Acquisition Mode
    public uint uiCmosRoiWidth;            // CMOS sensor ROI width
    public uint uiCmosRoiHeight;           // CMOS sensor ROI height
    public uint uiCmosRoiStartCol;         // CMOS sensor ROI first column
    public uint uiCmosRoiStartLine;        // CMOS sensor ROI first line
    public uint uiPeakDetectorType;        // Peak detection algorithm Id
    public byte ucPeakDetectionThreshold;  // Min intensity value
    public int iPeakSubPixelPrecision;    // 
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public sbyte[] acPeakDetectionParam;  // Parameters of the peak detection algorithm.
    public byte ucAgcEnable;               // 0 Disable - 1 Enable
    public uint uiAgcType;                 // Agc algorithm Id
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public sbyte[] acAgcParameters;       // Parameters of the Agc algorithm
    public float fAgcProfileRoiStart_norm;  // Start position of the profile ROI for the Agc, normalized value : [0 1] (0->0; 1->uiProfileNbPoints-1)
    public float fAgcProfileRoiStop_norm;   // Stop  position of the profile ROI for the Agc, normalized value : [0 1] (0->0; 1->uiProfileNbPoints-1)
    public float fCamGainAnalog;            // Initial Camera analog gain (The AGC may change it afterward)
    public float fCamOffsetAnalog;          // Initial Camera analog offset (The AGC may change it afterward)
    public float fCamGainDigital;           // Initial Camera digital gain (The AGC may change it afterward)
    public float fCamExposureTime_us;       // Initial Camera exposure time (The AGC may change it afterward)
    public float fLaser2CamExposureDelay_us;
    public ushort usOutputFifoLength;        // Nb of Road section that the ouptut Fifo can Hold
    public float fSensorAngle_deg;          // In degree. As seen from on top.

    public ushort usLaserSetPoint_mA;
    public ushort usLaserCurrentLimit_mA;
    public float fLaserTecSetPoint_C;
    public float fLaserTecTempLimMin_C;
    public float fLaserTecTempLimMax_C;
    public int iSensorOverlap_pix;
    public uint uiSCScalibID;
    public int iImageVerticalOffset_prf;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 116)] public sbyte[] acReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSystemInfo
{
    public const int ciSystemInfoVersion = 2; // Version of this structure

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] aucSensorEnable; // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public sbyte[] acSoftwareVersion; // Acquisition Software Version
    public sLcmsCtrlModuleBoardInfo sCtrlModuleInfo;       // Control module information
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public sLcmsSensorBoardInfo[] asSensorBoardInfo; // Sensor board information
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public sLcmsCameraInfo[] asCamInfo; // Camera information
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public sLcmsImuInfo[] asImuInfo; // IMU information
    public sLcmsImuExtInfo sImuExtInfo; // extended IMU information

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 78)] public sbyte[] chReserve;
} //sizeof(sLcmsSystemInfo) should be 978 bytes

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSystemStatus
{
    public const int ciSystemStatusVersion = 1; // Version of this structure

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public sbyte[] acSystemTimeAndDate; // Time and date "YYYY/MM/DD HH:MM:SS.dddd" 
    public uint uiSystemFault;           // 0->No Fault; Otherwise, fault detected somewhere in system.
                                         // Check bit field to identify the fault source and then
                                         // the appropriate status field for more detailed information.

    public sbyte cLaserInterlockState;           //
    public uint uiCtrlModuleMonitorErrorState;  // Error state of the sensor monitoring process (0->No Error)

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] aucSensorEnable;    // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public sLcmsSensorHeadStatus[] asSensorHeadStatus;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public sbyte[] chReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsSystemParam
{
    public const int ciSystemParamVersion = 1; // Version of this structure

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] aucSensorEnable; // Sensor[0]->Left; Sensor[1]->Right; Enable==1; Disable==0
    public double dOdometerCountPerMeter; // Odometer : Nb odometer count per meter.
    public double dClockFrequency; // Clock counts per second
    public uint uiInterSensorDistance_um; // Distance between sensors, given in micron

    public byte ucEncoderInputDebouncing; // 0->Disable; 1->Enable
    public byte ucEncoderInputChannel; // 0->A; 1->B; 2->A and B
    public byte ucEncoderOutputSelect; // 0->Encoder input; 1->Frequency divider
    public uint uiEncoderRatio; // [1 to 2^28] EncoderRatio = (Fout/Fin)*2^28

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public sbyte[] chReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsLicenseInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public sbyte[] acCreationDate; //JJMMAA
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public sbyte[] acExpirationDate; //JJMMAA
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] public sbyte[] acModelNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public sbyte[] acSerialNumber;
    public uint uiProcessModuleBitField;
    public byte ucSupportedAcquiLib_MajVer; //0-255
    public byte ucSupportedAcquiLib_MinVer; //0-255
    public byte ucSupportedAnalysisLib_MajVer; //0-255
    public byte ucSupportedAnalysisLib_MinVer; //0-255 
}

// #pragma pack(push, 8)

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsGeometricParam
{
    public const int ciGeometricParamVersion = 2; // Version of this structure

    public uint uiCalibrationId;
    public int iVersion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VIl_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VIl_Txyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VIr_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VIr_Txyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FlFr_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FlFr_Txyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FlLl_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FlLl_Txyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FrLr_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_FrLr_Txyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VFr_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VFr_Txyz;
    public double dTrackWidth_mm;
    public int iEncoderSide; //0->Left; 1->Right
    public float fV_NominalHeight_mm;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VA1_Txyz;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 272)] public sbyte[] chReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsImuCalibParam
{
    public const int ciImuCalibParamVersion = 2; // Version of this structure

    public uint uiCalibrationId;
    public int iVersion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public sbyte[] acCalibTimeAndDate; // Time and date "YYYY/MM/DD HH:MM:SS.dddd"
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acSerialNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adGyroBiais_XYZ; //degrees

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public sbyte[] chReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsInsOxtsCalibParam
{
    public const int ciInsOxtsCalibParamVersion = 3; // Version of this structure

    public int iVersion;
    public uint uiCalibrationId;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VO_Rxyz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adE_VO_Txyz;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] adGyroBiais_XYZ; //degrees

    public uint uiDeviceSerialNumber;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 224)] public sbyte[] chReserve;
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sLcmsPpsInfo
{
    public int iNbrPpsID;
    public IntPtr piNbrPpsCount; // int*
    public int iNbrInputDescr;
    public int iMaxDescrSize;
    public IntPtr pcPpsInputDescr; // char**
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sGPSdata
{
    //if acStructVersion=GPSv1.0, uiFraction is in tenth of second
    //if acStructVersion=GPSv1.1, uiFraction is in thousandth of second
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public sbyte[] acStructVersion;
    public int iGPSdataValid;
    public uint uiMsgID;
    public uint uiSatelitesInUse;
    public uint uiSignalQuality;
    public double dAltitude;
    public double dLatitude;
    public double dLongitude;
    public double dGroundSpeed;
    public double dCourseOverGround;
    public uint uiHour;
    public uint uiMinute;
    public uint uiSecond;
    public uint uiFraction;
    public uint uiYear;
    public uint uiMonth;
    public uint uiDay;
    //add new members here and adjust acReserved
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164)] public sbyte[] acReserved;
} //size should remain at 256 bytes

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[StructLayout(LayoutKind.Sequential)]
public struct sML_ObjectData
{
    public float fscore;
    public int MinX, MaxX;
    public int MinY, MaxY;
    public int iID;
}

// #pragma pack(pop)

public static class LcmsStruct
{
    public const ushort cusLcmsInvalidDataVal = 0xFFFF;
    public const float cfLcmsInvalidDataVal = -10000.0f;

    public const float cfLcmsGeometricParam_InvalidDataVal = -1.0e9f;
    public const double cdLcmsGeometricParam_InvalidDataVal = -1.0e9;
}

/*
typedef int(*ErrorAndProgressCB)(uint Error, int iProgressInPourcent, void * pParam);
typedef int(*TiePointAutoDetectFinishCB)(uint ErrorCode, void * pParam, void * pParentParam); */