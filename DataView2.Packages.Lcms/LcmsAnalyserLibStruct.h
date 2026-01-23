//LcmsAnalyserLibStruct.h

#ifndef _LCMS_ANALYSER_LIB_STRUCT_H_
#define _LCMS_ANALYSER_LIB_STRUCT_H_

#include "LcmsStruct.h"

#define LCMS_MAX_STR_LEN    2048;

#define LCMS_OFF 0
#define LCMS_ON  1

#define LCMS_LICENSE_ROAD 0
#define LCMS_LICENSE_IMUS 1
#define LCMS_LICENSE_RAIL 2

#define SYSTEM_MODEL_LCMS_1 0
#define SYSTEM_MODEL_LCMS_2 1

#define LCMS_PROC_MODULE_ALL                        0xFFFFFFFF

/*
  _____   ____          _____
 |  __ \ / __ \   /\   |  __ \
 | |__) | |  | | /  \  | |  | |
 |  _  /| |  | |/ /\ \ | |  | |
 | | \ \| |__| / ____ \| |__| |
 |_|  \_\\____/_/    \_\_____/

*/
			
#define LCMS_PROC_MODULE_LANE_MARKING               0x00000002
#define LCMS_PROC_MODULE_CRACKING                   0x00000004
#define LCMS_PROC_MODULE_RUTTING                    0x00000008 
#define LCMS_PROC_MODULE_MACRO_TEXTURE              0x00000010 
#define LCMS_PROC_MODULE_POTHOLES                   0x00000020
#define LCMS_PROC_MODULE_COMPILATION                0x00000040
#define LCMS_PROC_MODULE_RAVELING                   0x00000080
#define LCMS_PROC_MODULE_LONG_PROFILE               0x00000100 
#define LCMS_PROC_MODULE_CONCRETE_PAVMNT_JOINT      0x00000200
#define LCMS_PROC_MODULE_DROPOFF_CURB               0x00000400
#define LCMS_PROC_MODULE_SEALED_CRACKING            0x00000800
#define LCMS_PROC_MODULE_FOD_DETECTOR               0x00002000
#define LCMS_PROC_MODULE_SLOPE_AND_CROSS_SLOPE      0x00010000
#define LCMS_PROC_MODULE_PICKOUT                    0x00080000
#define LCMS_PROC_MODULE_BLEEDING                   0x00100000
#define LCMS_PROC_MODULE_MANMADEOBJECT              0x00200000
#define LCMS_PROC_MODULE_PATCHDETECTION             0x00400000
#define LCMS_PROC_MODULE_PUMPINGDETECTION           0x04000000
#define LCMS_PROC_MODULE_PASER                      0x08000000

/*
  _____            _____ _
 |  __ \     /\   |_   _| |
 | |__) |   /  \    | | | |
 |  _  /   / /\ \   | | | |
 | | \ \  / ____ \ _| |_| |____
 |_|  \_\/_/    \_\_____|______|

*/

#define LRAIL_PROC_MODULE_RAIL_DETECTION            0x00000002
#define LRAIL_PROC_MODULE_TIE_DETECTION             0x00000004
#define LRAIL_PROC_MODULE_SURFACE_DEFECTS           0x00000008
#define LRAIL_PROC_MODULE_TIE_RATING                0x00000010
#define LRAIL_PROC_MODULE_FASTENER_DETECTION        0x00000020
#define LRAIL_PROC_MODULE_TIE_PLATE_DETECTION       0x00000040
#define LRAIL_PROC_MODULE_GAGEWIDTH_CANT            0x00000080
#define LRAIL_PROC_MODULE_TURNOUT_DETECTION         0x00000100
#define LRAIL_PROC_MODULE_SPIKE_DETECTION           0x00000200
#define LRAIL_PROC_MODULE_BALLAST_INSPECTION        0x00000400
#define LRAIL_PROC_MODULE_JOINT_DETECTION           0x00000800
#define LRAIL_PROC_MODULE_RAIL_WEAR                 0x00001000


#define LCMS_RESULT_IMAGE_INTENSITY                 0x00000001
#define LCMS_RESULT_IMAGE_RANGE                     0x00000002
#define LCMS_RESULT_IMAGE_OVERLAY_INTENSITY_RGB32   0x00000004
#define LCMS_RESULT_IMAGE_OVERLAY_RANGE_RGB32       0x00000008
#define LCMS_RESULT_IMAGE_3D                        0x00000010
#define LCMS_RESULT_IMAGE_OVERLAY_3D_RGB32          0x00000020
#define LCMS_RESULT_IMAGE_OVERLAY_BLACK_RGB32       0x00000080
#define LCMS_RESULT_IMAGE_TEXTURE_RGB32             0x00000100
#define LCMS_RESULT_IMAGE_SHAPEFILE                 0x00000200
#define LCMS_RESULT_IMAGE_LASFILE                   0x00000400


struct sLcmsResultImage{
    int   iImageType;
    int   iWidth;
    int   iHeight;
    void *pvImageData; // pvImageData may be grayscale (unsigned char)
                       // or RGB32 (RGBQUAD), according to its type.
};

struct sSurfPreviewPt_GeoRef{
    unsigned short usPosI; //Preview point position along I
    unsigned short usPosJ; //Preview point position along J
    unsigned char  ucInt;  //Preview point intensity value
    unsigned char  ucInvalid; //Indicates that the point have been interpolated
    double         dUtmX;  //UTM X coordinate (Easting)
    double         dUtmY;  //UTM Y coordinate (Northing)
    double         dUtmZ;  //UTM Z coordinate (Altitude)
};

#endif //_LCMS_ANALYSER_LIB_STRUCT_H_