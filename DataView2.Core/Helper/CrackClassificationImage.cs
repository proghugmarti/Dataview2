using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DataView2.Core.Helper
{
    public class crackClassificationImage
    {
        private int segmentsectionId;
        private long segmentsurveyId;
        private string strSegmentsectionId;
        private string strSegmentsurveyId;

        private double LaneMarkingModule_Parameters = 0;
        private double GeneralParam_WheelPathWidth_mm = 0;
        private double GeneralParam_CentralBandWidth_mm = 0;

        private double ResolutionX = 1;
        private double ResolutionY = 1;
        private double ImageWidth;
        private double ImageHeight;
        private int matrixWidth;
        private int matrixHeight;
        private int squareSize = 250;

        public int MatrixWidth()
        {
            return matrixWidth;
        }

        public int MatrixHeight()
        {
            return matrixHeight;
        }

        public crackClassificationImage(XmlDocument doc)
        {
            getGeneralVariables(doc);
        }
        public async void getGeneralVariables(XmlDocument doc)
        {
            strSegmentsurveyId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SurveyID").InnerText;
            strSegmentsectionId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SectionID").InnerText;

            if (!String.IsNullOrEmpty(strSegmentsurveyId))
            {
                segmentsurveyId = long.Parse(strSegmentsurveyId);
            }

            if (!String.IsNullOrEmpty(strSegmentsectionId))
            {
                segmentsectionId = Int32.Parse(strSegmentsectionId);
            }

            LaneMarkingModule_Parameters = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/LaneMarkingModule_Parameters/LaneMarkingModule_RoadWidth_mm");
            GeneralParam_CentralBandWidth_mm = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_CentralBandWidth_mm");
            GeneralParam_WheelPathWidth_mm = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_WheelPathWidth_mm");
            ImageWidth = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageWidth");
            ImageHeight = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageHeight");

            ResolutionX = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionX");
            ResolutionY = await getDoubleFromXML(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionY");
            
            matrixWidth = (int)(ImageWidth * ResolutionX) / squareSize;
            matrixHeight = (int)(ImageHeight * ResolutionY) / squareSize;
        }


        public async Task<double> getDoubleFromXML(XmlDocument doc, string fieldName)
        {
            double result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Convert.ToDouble(xmlselNode.InnerText);
            }

            return result;
        }

        public enum resultType
        {
            Unknown,
            Offroad,
            WheelPath,
            Transversal,
            Longitudinal,
            Multiple,
            Alligator,
            Pending,
            Other,
        }
    }
}
