using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Communication
{
    public class GPSCoordinate
    {
        /// <summary>
        /// Gets or sets longitude.
        /// </summary>
        public double Longitude { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets latitude.
        /// </summary>
        public double Latitude { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets altitude.
        /// </summary>
        public double Altitude { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets time.
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets date.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets the number of satellites.
        /// </summary>
        public int NbrOfSatellites { get; set; }

        /// <summary>
        /// Gets or sets signal quality.
        /// </summary>
        public string SignalQuality { get; set; }

        /// <summary>
        /// Gets or sets ground speed.
        /// </summary>
        public double GroundSpeed { get; set; }

        /// <summary>
        /// Gets or sets track angle.
        /// </summary>
        public double TrackAngle { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets heading.
        /// </summary>
        public double Heading { get; set; }

        
    }
    public partial class GPSInformation
    {
        /// <summary>
        /// Gets or sets GPS coordinate details.
        /// </summary>
        public GPSCoordinate GPSCoordinate { get; set; } = new();
    }
}
