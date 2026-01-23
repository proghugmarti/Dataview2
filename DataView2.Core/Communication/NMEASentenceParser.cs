using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NmeaParser;
using NmeaParser.Messages;


namespace DataView2.Core.Communication
{
    public class NMEASentenceParser
    {
        /// <summary>
        /// Holds the constant to convert Knots to km/h.
        /// </summary>
        private const float KnotsToKmPerH = 1.852f;

        /// <summary>
        /// Initializes a new instance of the <see cref="NMEASentenceParser"/> class.
        /// </summary>
        public NMEASentenceParser()
        {
        }

        /// <summary>
        /// Holds the data detected event handler.
        /// </summary>
        public event Action<NMEASentenceParser, GPSCoordinate> DataProcessed = (arg1, args2) => { };

        /// <summary>
        /// Gets or sets the current GPS information.
        /// </summary>
        public GPSInformation CurrentGPSInfo { get; set; }

        
        /// <summary>
        /// Converts the GPS quality enumeration value to string.
        /// </summary>
        /// <param name="quality">ENum value of GPS quality.</param>
        /// <returns>The converted string quality.</returns>
        private static string GetGPSQuality(Gga.FixQuality quality)
        {
            try
            {
                string gpsQuality = string.Empty;
                switch (quality)
                {
                    case Gga.FixQuality.Invalid:
                        gpsQuality = "Invalid";
                        break;
                    case Gga.FixQuality.GpsFix:
                        gpsQuality = "GPS Fix";
                        break;
                    case Gga.FixQuality.DgpsFix:
                        gpsQuality = "DGPS Fix";
                        break;
                    case Gga.FixQuality.PpsFix:
                        gpsQuality = "PPS Fix";
                        break;
                    case Gga.FixQuality.Rtk:
                        gpsQuality = "RTK";
                        break;
                    case Gga.FixQuality.FloatRtk:
                        gpsQuality = "Float RTK";
                        break;
                    case Gga.FixQuality.Estimated:
                        gpsQuality = "Estimated";
                        break;

                    case Gga.FixQuality.ManualInput:
                        gpsQuality = "Manual Input";
                        break;

                    case Gga.FixQuality.Simulation:
                        gpsQuality = "Simulation";
                        break;
                    default:
                        break;
                }

                return gpsQuality;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                return string.Empty;
            }
        }

        /// <summary>
        /// This is being processed from queue.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">NMEA message.</param>
        public void OnDataProcess(object sender, NmeaMessage e)
        {
            this.CurrentGPSInfo = new();

            try
            {
                if (e is NmeaParser.Messages.Rmc rmcMessage)
                {
                    if (!this.ParseRMC(rmcMessage))
                    {
                        throw new Exception("RMC sentence parsing is failed.");
                    }
                }
                else if (e is NmeaParser.Messages.Gsa gsaMessage)
                {
                    if (!this.ParseGSA(gsaMessage))
                    {
                        throw new Exception("GSA sentence parsing is failed.");
                    }
                }
                else if (e is NmeaParser.Messages.Gga ggaMessage)
                {
                    if (!this.ParseGGA(ggaMessage))
                    {
                        throw new Exception("GGA sentence parsing is failed.");
                    }
                }

                // Raise the event
                if (this.DataProcessed is not null)
                {
                    this.DataProcessed(this, this.CurrentGPSInfo.GPSCoordinate);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unsupported NMEA message type: " + ex);

            }
        }

        /// <summary>
        /// Parses GSA string.
        /// </summary>
        /// <param name="message">The NMEA sentence.</param>
        /// <returns>The status of the operation.</returns>
        private bool ParseGSA(Gsa message)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Parses GGA string.
        /// </summary>
        /// <param name="message">The NMEA sentence.</param>
        /// <returns>The status of the operation.</returns>
        private bool ParseGGA(Gga message)
        {

            try
            {
                this.CurrentGPSInfo.GPSCoordinate.Latitude = message.Latitude;
                this.CurrentGPSInfo.GPSCoordinate.Longitude = message.Longitude;
                this.CurrentGPSInfo.GPSCoordinate.Altitude = message.Altitude;
                this.CurrentGPSInfo.GPSCoordinate.NbrOfSatellites = message.NumberOfSatellites;
                this.CurrentGPSInfo.GPSCoordinate.SignalQuality = GetGPSQuality(message.Quality);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Parses RMC string.
        /// </summary>
        /// <param name="message">The NMEA sentence.</param>
        /// <returns>The status of the operation.</returns>
        private bool ParseRMC(Rmc message)
        {
            try
            {
                this.CurrentGPSInfo.GPSCoordinate.Latitude = message.Latitude;
                this.CurrentGPSInfo.GPSCoordinate.Longitude = message.Longitude;
                this.CurrentGPSInfo.GPSCoordinate.GroundSpeed = message.Speed * KnotsToKmPerH;
                this.CurrentGPSInfo.GPSCoordinate.Date = $"{message.FixTime.Day:D2}/{message.FixTime.Month:D2}/" +
                    $"{message.FixTime.Year:D2}";
                this.CurrentGPSInfo.GPSCoordinate.Time = $"{message.FixTime.Hour:D2}:{message.FixTime.Minute:D2}:" +
                    $"{message.FixTime.Second:D2}.{message.FixTime.Millisecond:D3}";

                this.CurrentGPSInfo.GPSCoordinate.Heading = message.Course;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                return false;
            }
        }
     
    }
}
