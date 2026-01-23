// **************************************************************************************
// Assembly         : Romdas.LFOD.RTA.exe
// Author           : Sibi K S
// Created          : 13-01-2021
//
// Last Modified By : Sibi K S
// Last Modified On : 13-01-2021
// **************************************************************************************
// <copyright file="SurveyUtility.cs" company="ROMDAS">
//     Copyright (c) 2021 All rights reserved.
// </copyright>
// <summary>
// This is the definition file of SurveyUtility class.
// </summary>
// <Revision>
// 13-01-2021       Sibi K S    #2351           Initial Version
// </Revision>
// This file includes :
//       Classes      : SurveyUtility
//       Structures   : Nil
//       Enumerations : Nil
// **************************************************************************************
namespace DataView2.Core
{
    //using Esri.ArcGISRuntime.Geometry;
    //using Romdas.LFOD.RTA.Models;
    using System;

    /// <summary>
    /// This static class do the calculations needed for GPS.
    /// </summary>
    public class SurveyUtility
    {
        /// <summary>
        /// Holds the constance for radial distance of the Earth IN METERS, 6371.1 kilometers.
        /// </summary>
        private static readonly double RadialDistance = 6371100.0;

        /// <summary>
        /// Calculates GPS positions from bearing and offset.
        /// </summary>
        /// <param name="latitude">The latitude (will be updated).</param>
        /// <param name="longitude">The longitude (will be updated).</param>
        /// <param name="bearing">The bearing.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>Returns the converted latitude and longitude.</returns>
        public static (double, double) GPSPositionFromBearingAndOffset(double latitude, double longitude, double bearing, double offset)
        {
            /*  See spreadsheet "Calcualtions.xlsx"
            lat2: = ASIN(SIN(latitude)*COS(d / R) + COS(latitude)*SIN(d / R)*COS(bearing))
            lon2 : = lon1 + ATAN2(COS(d / R) - SIN(lat1)*SIN(lat2), SIN(bearing)*SIN(d / R)*COS(lat1))
            R = radial distance of the Earth, 63711 kilometers
            Also, all values in degrees(lat1, lon1, bearing, lat2) are to be converted to radians for input, and then the output is converted back to degrees
            If you implement any formula involving atan2 in Microsoft Excel, you will need to reverse the arguments, as Excel has them the opposite way around from C++
            – conventional order is atan2(y, x), but Excel uses atan2(x, y).
            */
            double lat1_Rad = latitude * (Math.PI / 180);
            double lon1_Rad = longitude * (Math.PI / 180);
            double d = offset / RadialDistance;
            double bearing_Rad = DegreesToRadians(bearing);

            // Make a little faster now being called in real time acquisition by making function calls once
            double sin_lat1_rad = Math.Sin(lat1_Rad);
            double cos_lat1_rad = Math.Cos(lat1_Rad);
            double sin_d = Math.Sin(d);
            double cos_d = Math.Cos(d);
            double lat2_Rad = Math.Asin((sin_lat1_rad * cos_d) + (cos_lat1_rad * sin_d * Math.Cos(bearing_Rad)));
            double lon2_Rad = lon1_Rad + Math.Atan2(Math.Sin(bearing_Rad) * sin_d * cos_lat1_rad, cos_d - (sin_lat1_rad * Math.Sin(lat2_Rad)));
            latitude = lat2_Rad / (Math.PI / 180);
            longitude = lon2_Rad / (Math.PI / 180);

            return (latitude, longitude);
        }

        /// <summary>
        /// Gets the distance from two GPS coordinates.
        /// </summary>
        /// <param name="lat1">First latitude.</param>
        /// <param name="lon1">First longitude.</param>
        /// <param name="lat2">Second Latitude.</param>
        /// <param name="lon2">Second longitude.</param>
        /// <returns>The distance calculated.</returns>
        public static double DistanceInMetersBetweenEarthCoordinates(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            lat1 = DegreesToRadians(lat1);
            lat2 = DegreesToRadians(lat2);

            var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2));
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return RadialDistance * c;
        }

        /// <summary>
        /// Calculates processing time.
        /// </summary>
        /// <param name="speed_kmh">The Speed in km/h.</param>
        /// <param name="sectionlength">The section length.</param>
        /// <param name="processingTime">The processing time.</param>
        /// <returns>The calculated processing time between sections.</returns>
        public static double CalculateProcessingTime(double speed_kmh, int sectionlength, double processingTime)
        {
            const int NumofProcessingInstances = 8;
            double calcProcTime = 0.0;
            double speed_ms = speed_kmh * 1000 / 3600;
            double section_s = speed_ms / sectionlength;
            double acq_time_per_section = 1 / section_s;

            double section_proc_s = NumofProcessingInstances / processingTime;

            calcProcTime = 1 / section_proc_s;
            return calcProcTime;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        //public static MapPoint MapPoingFromFOD(FODDisplayItem fod) {
        //    MapPoint mapCenterPoint = new MapPoint(fod.Longitude, fod.Latitude, SpatialReferences.Wgs84);
        //    return mapCenterPoint;
        //}


    }
}
