using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace DataView2.Core.Helper
{
    public struct CrackNode
    {
        public double x;
        public double y;
        public double width;
        public double depth;
        public double crackId;
        public string surveyId;
        public int segmentId;
    }

    public struct Crack
    {
        public double crackId;
        public double length;
        public double weightedDepth;
        public double weightedWidth;
        public string surveyId;
        public int segmentId;
        public List<CrackNode> nodes;
    }

    public struct NodePoint
    {
        public double i;
        public double j;
        public double x;
        public double y;
        public double width;
        public double depth;
        public double length;

        public NodePoint(double a, double b, double c, double d, double e, double f, double g)
        {
            x = a;
            y = b;
            width = c;
            depth = d;
            length = e;
            i = f;
            j = g;
        }
    }

    public struct SquareStats
    {
        public double totalLength;
        public double averageWidth;
        public double averageDepth;
        public double totalWidth;
        public double totalDepth;
    }

    public class XMLCrackClassification
    {
        public static char ChooseSymbolByValue(double value)
        {
            if (value < 500)
            {
                return ' ';
            }
            else if (value < 4500)
            {
                return 'L';
            }
            else if (value < 13000)
            {
                return 'M';
            }
            else
            {
                return 'H';
            }
        }

        public static List<Crack> ImportCracks(string xmlFilename)
        {
            List<Crack> cracks = new List<Crack>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilename);            

            XmlNode rootElement = xmlDoc.DocumentElement;

            if (rootElement != null)
            {
                string surveyId = string.Empty;
                int segmentId = 0;

                foreach (XmlNode childElement in rootElement.ChildNodes)
                {
                    string elementName = childElement.Name;
                   

                    if (elementName == "RoadSectionInfo")
                    {
                        foreach (XmlNode childchildElement in childElement.ChildNodes)
                        {
                            if (childchildElement.Name == "SurveyID")
                                surveyId = childchildElement.InnerText;
                            if (childchildElement.Name == "SectionID")
                                segmentId = int.Parse(childchildElement.InnerText);
                        }
                    }

                    if (elementName == "CrackInformation")
                    {
                        foreach (XmlNode childchildElement in childElement.ChildNodes)
                        {
                            if (childchildElement.Name == "CrackList")
                            {
                                foreach (XmlNode crackListElement in childchildElement.ChildNodes)
                                {
                                    double crack_id = 0.0;
                                    double length = 0.0;
                                    double weighted_depth = 0.0;
                                    double weighted_width = 0.0;
                                    List<CrackNode> nodes = new List<CrackNode>();

                                    foreach (XmlNode crackElement in crackListElement.ChildNodes)
                                    {
                                        if (crackElement.Name == "CrackID")
                                        {
                                            crack_id = double.Parse(crackElement.InnerText);
                                        }
                                        if (crackElement.Name == "Length")
                                        {
                                            length = double.Parse(crackElement.InnerText);
                                        }
                                        if (crackElement.Name == "WeightedDepth")
                                        {
                                            weighted_depth = double.Parse(crackElement.InnerText);
                                        }
                                        if (crackElement.Name == "WeightedWidth")
                                        {
                                            weighted_width = double.Parse(crackElement.InnerText);
                                        }
                                        if (crackElement.Name == "Node")
                                        {
                                            XmlNode x = crackElement.FirstChild;
                                            XmlNode y = x.NextSibling;
                                            XmlNode width = y.NextSibling;
                                            XmlNode depth = width.NextSibling;

                                            CrackNode node = new CrackNode
                                            {
                                                x = double.Parse(x.InnerText),
                                                y = double.Parse(y.InnerText),
                                                width = double.Parse(width.InnerText),
                                                depth = double.Parse(depth.InnerText),
                                                crackId = crack_id,
                                                surveyId = surveyId,
                                                segmentId = segmentId
                                            };

                                            nodes.Add(node);
                                        }
                                    }

                                    Crack newcrack = new Crack
                                    {
                                        crackId = crack_id,
                                        length = length,
                                        weightedDepth = weighted_depth,
                                        weightedWidth = weighted_width,
                                        surveyId = surveyId,
                                        segmentId = segmentId,
                                        nodes = nodes
                                    };

                                    cracks.Add(newcrack);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Error loading XML file.");
            }

            return cracks;
        }

        public static Tuple<int, int> CrackImageSizeFinder(string xmlFilename)
        {
            Tuple<int, int> widthDepthpair = new Tuple<int, int>(0, 0);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilename);

            Console.WriteLine("XML file loaded successfully.");

            XmlNode rootElement = xmlDoc.DocumentElement;

            if (rootElement != null)
            {
                foreach (XmlNode childElement in rootElement.ChildNodes)
                {
                    string elementName = childElement.Name;
                    if (elementName == "ResultImageInformation")
                    {
                        double resolutionX = 0.0;
                        double resolutionY = 0.0;

                        foreach (XmlNode childchildElement in childElement.ChildNodes)
                        {
                            if (childchildElement.Name == "ResolutionX")
                            {
                                resolutionX = double.Parse(childchildElement.InnerText);
                            }
                            if (childchildElement.Name == "ResolutionY")
                            {
                                resolutionY = double.Parse(childchildElement.InnerText);
                            }
                            if (childchildElement.Name == "ImageWidth")
                            {
                                int width = int.Parse(childchildElement.InnerText);
                                widthDepthpair = new Tuple<int, int>((int)(width * resolutionX), widthDepthpair.Item2);
                            }
                            if (childchildElement.Name == "ImageHeight")
                            {
                                int height = int.Parse(childchildElement.InnerText);
                                widthDepthpair = new Tuple<int, int>(widthDepthpair.Item1, (int)(height * resolutionY));
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Error loading XML file.");
            }

            return widthDepthpair;
        }

        public static List<NodePoint> SeperatePavementricsCracksToSquares(Tuple<int, int> widthHeightPair, int csvCols, int csvRows, List<Crack> cracks)
        {
            int imageCols = widthHeightPair.Item1;
            int imageRows = widthHeightPair.Item2;
            int SquareColPixelLength = imageCols / csvCols;
            int SquareRowPixelLength = imageRows / csvRows;
            List<NodePoint> nodePointBySquare = new List<NodePoint>();

            for (int i = 0; i < cracks.Count; ++i)
            {
                var crack = cracks[i];

                for (int j = 1; j < crack.nodes.Count; ++j)
                {
                    var frontNode = crack.nodes[j - 1];
                    var backNode = crack.nodes[j];
                    var width = (frontNode.width + backNode.width) / 2;
                    var depth = (frontNode.depth + backNode.depth) / 2;
                    var length = Math.Sqrt(Math.Pow(frontNode.x - backNode.x, 2) + Math.Pow(frontNode.y - backNode.y, 2));
                    var x = (frontNode.x + backNode.x) / 2;
                    var y = (frontNode.y + backNode.y) / 2;
                    var iIndex = (int)(x / SquareColPixelLength);
                    var jIndex = (int)(y / SquareRowPixelLength);

                    NodePoint newNodePoint = new NodePoint(x, y, width, depth, length, iIndex, jIndex);
                    nodePointBySquare.Add(newNodePoint);
                }
            }

            return nodePointBySquare;
        }

        public static void UpdateSquareStats(List<NodePoint> nodePoints, List<List<SquareStats>> squareStatsMatrix)
        {
            int count = 0;
            List<List<int>> countMatrix = new List<List<int>>(squareStatsMatrix.Count);

            for (int i = 0; i < squareStatsMatrix.Count; i++)
            {
                countMatrix.Add(new List<int>(squareStatsMatrix[i].Count));

                for (int j = 0; j < squareStatsMatrix[i].Count; j++)
                {
                    countMatrix[i].Add(0);
                }
            }

            foreach (var nodePoint in nodePoints)
            {
                int i = (int)nodePoint.i;
                int j = (int)nodePoint.j;
                count++;

                SquareStats squareStats = squareStatsMatrix[i][j];
                squareStats.totalLength += nodePoint.length;
                squareStats.totalWidth += nodePoint.width;
                squareStats.totalDepth += nodePoint.depth;
                //squareStatsMatrix[i][j].totalLength += nodePoint.length;
                //squareStatsMatrix[i][j].totalWidth += nodePoint.width;
                //squareStatsMatrix[i][j].totalDepth += nodePoint.depth;
                squareStatsMatrix[i][j] = squareStats;
                countMatrix[i][j] += 1;
            }

            for (int i = 0; i < squareStatsMatrix.Count; ++i)
            {
                for (int j = 0; j < squareStatsMatrix[i].Count; ++j)
                {
                    if (squareStatsMatrix[i][j].totalLength > 0)
                    {
                        SquareStats squareStats = squareStatsMatrix[i][j];
                        squareStats.averageWidth = squareStatsMatrix[i][j].totalWidth / Math.Max(1, countMatrix[i][j]);
                        squareStats.averageDepth = squareStatsMatrix[i][j].totalDepth / Math.Max(1, countMatrix[i][j]);
                        //squareStatsMatrix[i][j].averageWidth = squareStatsMatrix[i][j].totalWidth / Math.Max(1, countMatrix[i][j]);
                        //squareStatsMatrix[i][j].averageDepth = squareStatsMatrix[i][j].totalDepth / Math.Max(1, countMatrix[i][j]);
                        squareStatsMatrix[i][j] = squareStats;
                    }
                }
            }
        }

        public static double EvaluateValueVolume(SquareStats nodeSquare)
        {
            double returnval = nodeSquare.averageWidth * nodeSquare.averageDepth * nodeSquare.totalLength;
            return returnval;
        }

        public static List<List<double>> DeterminePavemetricValueForCSV(List<List<SquareStats>> matrix)
        {
            List<List<double>> results = new List<List<double>>(matrix.Count);

            for (int i = 0; i < matrix.Count; ++i)
            {
                results.Add(new List<double>(matrix[i].Count));

                for (int j = 0; j < matrix[i].Count; ++j)
                {
                    double current_volume = 0.0;
                    SquareStats stats = matrix[(matrix.Count - i - 1)][(matrix[i].Count - 1 - j)];

                    if (i == 0 || j == 0 || i == (matrix.Count - 1) || j == (matrix[i].Count - 1))
                    {
                        current_volume = EvaluateValueVolume(stats);
                        results[i].Add(current_volume);
                    }
                    else
                    {
                        current_volume = (((EvaluateValueVolume(stats) * 8) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 - 1)][(matrix[i].Count - 1 - j + 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 - 0)][(matrix[i].Count - 1 - j + 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 + 1)][(matrix[i].Count - 1 - j + 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 + 1)][(matrix[i].Count - 1 - j + 0)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 + 1)][(matrix[i].Count - 1 - j - 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 + 0)][(matrix[i].Count - 1 - j - 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 - 1)][(matrix[i].Count - 1 - j - 1)]) + EvaluateValueVolume(matrix[(matrix.Count - i - 1 - 1)][(matrix[i].Count - 1 - j - 0)])) / 12);
                        results[i].Add(current_volume);
                    }
                }
            }

            return results;
        }

        public static List<List<double>> ComputeCracksFromXML(string xmlfilename, int colCount, int rowCount)
        {
            List<List<SquareStats>> squareStatsMatrix = new List<List<SquareStats>>(colCount);

            for (int i = 0; i < colCount; i++)
            {
                squareStatsMatrix.Add(new List<SquareStats>(rowCount));

                for (int j = 0; j < rowCount; j++)
                {
                    squareStatsMatrix[i].Add(new SquareStats());
                }
            }

            Tuple<int, int> widthHeightPair = CrackImageSizeFinder(xmlfilename);
            List<Crack> cracks = ImportCracks(xmlfilename);
            List<NodePoint> nodeSquares = SeperatePavementricsCracksToSquares(widthHeightPair, colCount, rowCount, cracks);
            UpdateSquareStats(nodeSquares, squareStatsMatrix);
            List<List<double>> results = DeterminePavemetricValueForCSV(squareStatsMatrix);

            return results;
        }

        public static List<List<double>> ComputeCracksFromCrackVector(List<Crack> cracks, Tuple<int, int> widthHeightPair, int colCount, int rowCount)
        {
            List<List<SquareStats>> squareStatsMatrix = new List<List<SquareStats>>(colCount);

            for (int i = 0; i < colCount; i++)
            {
                squareStatsMatrix.Add(new List<SquareStats>(rowCount));

                for (int j = 0; j < rowCount; j++)
                {
                    squareStatsMatrix[i].Add(new SquareStats());
                }
            }

            List<NodePoint> nodeSquares = SeperatePavementricsCracksToSquares(widthHeightPair, colCount, rowCount, cracks);
            UpdateSquareStats(nodeSquares, squareStatsMatrix);
            List<List<double>> results = DeterminePavemetricValueForCSV(squareStatsMatrix);

            return results;
        }
    }
}


