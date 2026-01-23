using System.Xml;
using System.Xml.Linq;

namespace DataView2.GrpcService.Helpers
{
    public class MTQ_Classification
    {

        public class LCMSBoundingBox
        {
            public float MinX { get; set; }
            public float MinY { get; set; }
            public float MaxX { get; set; }
            public float MaxY { get; set; }

           
            public bool Intersects(LCMSBoundingBox other)
            {
                return !(other.MaxX < MinX || other.MinX > MaxX ||
                         other.MaxY < MinY || other.MinY > MaxY);
            }
        }

        public static string ClassifyCrack(LCMSBoundingBox bbox,
            List<LCMSBoundingBox> multipleCrackRegion,
            List<LCMSBoundingBox> alligatorCrackRegion,
            List<LCMSBoundingBox> transversalCrackRegion
        )
        {
            if (multipleCrackRegion.Any(r => r.Intersects(bbox)))
                return "Multiple";

            if (alligatorCrackRegion.Any(r => r.Intersects(bbox)))
                return "Alligator";

            if (transversalCrackRegion.Any(r => r.Intersects(bbox)))
                return "Transversal";

            return "Unknown";
            //// Use lType if available
            //return lType switch
            //{
            //    1 => "Transversal",
            //    2 => "Longitudinal",
            //    3 => "Alligator",
            //    _ => "Unknown"
            //};
        }
    }
}
