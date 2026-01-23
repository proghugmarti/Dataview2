using DataView2.Core.Models.LCMS_Data_Tables;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Options;

public class QCMapData
{
    public double MapScale { get; set; }
    public SpatialReference MapSpatialReference { get; set; }
    public Envelope MapEnvelope { get; set; }
}

