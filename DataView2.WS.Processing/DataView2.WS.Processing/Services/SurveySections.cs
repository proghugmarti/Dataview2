using Newtonsoft.Json;
using Serilog;
using System.Text.RegularExpressions;

namespace DataView2.WS.Processing.Services
{
    public class SurveySections
    {
        List<gpsInfo> gpsPoints = new List<gpsInfo>();
        List<Feature> features = new List<Feature>();
        Double OffsetCheckValue = 2;

        public static List<int> SelectCorrectFisFiles(double closestAllowableDistance, double sectionLength, string jsonFilepath, string gpsFilepath, bool FilterOutAreas = false)
        {
            List<string> gpsFiles = new List<string>();
            List<string> jsonFiles = new List<string>();

            SurveySections surveySections = new SurveySections();

            try
            {
                if (Directory.Exists(gpsFilepath))
                    //gpsFiles = Directory.GetFiles(gpsFilepath, "GPS_Segments.txt").ToList();
                    gpsFiles = Directory.GetFiles(gpsFilepath, "outputDatatest.csv").ToList();
                //gpsFiles = Directory.GetFiles(gpsFilepath, "*.txt").ToList();
            }
            catch (Exception ex)
            {
                Log.Information("Survey folder doesn't exist!");
            }

            if (FilterOutAreas)
            {
                //jsonFilepath = jsonFilepath;// + "\\Survey Areas";
                jsonFilepath = jsonFilepath + "\\Survey Areas";

                try
                {
                    if (Directory.Exists(jsonFilepath))
                        jsonFiles = Directory.GetFiles(jsonFilepath, "*.json").ToList();
                }
                catch (Exception ex)
                {
                    Log.Information("Json folder doesn't exist!");
                }

                surveySections.readSurveySectionFiles(jsonFiles);
            }


            surveySections.readGpsCoordFile(gpsFiles);

            List<int> overlaps = surveySections.gpsPointsPotentiallyInSections();

            List<List<int>> segmentsByOverlaps = surveySections.combineSections(overlaps);
            List<SectionsInSegment> sectionsInSegment = surveySections.ruleoutDisjointedTravel(segmentsByOverlaps, sectionLength);
            List<SectionsInSegment> hopefullyFinal = surveySections.DetectAndRemoveOverlappingRuns(sectionsInSegment);
            //surveySections.WriteDataToCSV(hopefullyFinal);

            List<int> returnIndexList = new List<int>();
            foreach (var section in sectionsInSegment)
            {
                returnIndexList.AddRange(section.segments);
            }

            returnIndexList.Sort();

            return returnIndexList;

        }

        public static List<gpsInfo> getFixedPositions(string gpsFilepath)
        {
            SurveySections surveySections = new SurveySections();
            List<string> gpsFiles = new List<string>();

            try
            {
                if (Directory.Exists(gpsFilepath))
                    //gpsFiles = Directory.GetFiles(gpsFilepath, "GPS_Segments.txt").ToList();
                    //gpsFiles = Directory.GetFiles(gpsFilepath, "*.txt").ToList();
                    gpsFiles = Directory.GetFiles(gpsFilepath, "outputDatatest.csv").ToList();
            }
            catch (Exception ex)
            {
                Log.Information("Survey folder doesn't exist!");
            }

            //surveySections.readGpsCoordFile(gpsFiles);
            surveySections.readCSVGpsCoordFile(gpsFiles);
            return surveySections.gpsPoints;
        }

        public struct SectionsInSegment
        {
            public int section;
            public List<int> segments;

            public SectionsInSegment(int section, List<int> segments)
            {
                this.section = section;
                this.segments = segments;
            }
        }

        public struct gpsInfo
        {
            public int Section;
            public double Distance;
            public double Latitude;
            public double Longitude;
            public double Altitude;
            public double Angle;

            public gpsInfo(int section, double distance, double latitude, double longitude, double angle, double altitude)
            {
                this.Distance = distance;
                this.Latitude = latitude;
                this.Longitude = longitude;
                this.Altitude = altitude;
                this.Angle = angle;
                this.Section = section;
            }
        }

        public List<SectionsInSegment> DetectAndRemoveOverlappingRuns(List<SectionsInSegment> segmentsinsections)
        {
            List<SectionsInSegment> outputList = new List<SectionsInSegment>();
            foreach (SectionsInSegment segment in segmentsinsections)
            {
                int i = 1;
                double lastBearing = CalculateBearing(gpsPoints[segment.segments[0]], gpsPoints[segment.segments[0 + 1]]); //As I write this I find the idea of alot of bears very funny
                var runIndexs = new List<List<int>>();
                bool onRun = false;
                double endingBearing = lastBearing;
                while (i < segment.segments.Count - 1)
                {
                    double currentBearing = CalculateBearing(gpsPoints[segment.segments[i]], gpsPoints[segment.segments[i + 1]]);
                    if (Math.Abs(currentBearing - lastBearing) < 5 && !onRun) //Start point of a run
                    {
                        onRun = true;
                        runIndexs.Add(new List<int>());
                        runIndexs[runIndexs.Count - 1].Add(segment.segments[i]);

                    }
                    else if (Math.Abs(currentBearing - lastBearing) > 5 && Math.Abs(currentBearing - endingBearing) > 120 && onRun) //End Point of a run
                    {
                        onRun = false;
                        runIndexs[runIndexs.Count - 1].Add(segment.segments[i]);
                        runIndexs[runIndexs.Count - 1].Sort(); //Need sorted behaviour later
                        endingBearing = currentBearing;
                    }
                    lastBearing = currentBearing;
                    i++; //Lets get some point in the line which is close, get another point, Draw lines, check average distance between the two. Do we just choose the closest point
                }
                if (onRun)
                {
                    runIndexs[runIndexs.Count - 1].Add(i); //End point of a run if its the last profile
                    runIndexs[runIndexs.Count - 1].Sort(); //Need sorted behaviour later
                }

                var largestList = runIndexs[0]; //Likely need to check a list has been chosen
                List<double> bearingsList = new List<double>();
                List<List<int>> usableRunIndexList = new List<List<int>>();
                int difSwapFactor = 0;
                int otherDifSwapFactor = 1;
                int difSwapFactorMulti = 1;
                foreach (var list in runIndexs)
                {

                    if (Math.Abs(largestList[0] - largestList[1]) < Math.Abs(list[0] - list[1])) //Choose the largest list because it should have the best bearing
                    {
                        largestList = list; //Largest List will be the only one with valid data
                    }
                    int indexDiff = list[1] - list[0];
                    if (indexDiff > 25) //Has to be a sufficently long run to count
                    {
                        bearingsList.Add(CalculateBearing(gpsPoints[list[difSwapFactor] + 10 * difSwapFactorMulti], gpsPoints[list[otherDifSwapFactor] - 10 * difSwapFactorMulti])); //Done like this so the bearing should be in the same direction for all the consequtive runs
                        usableRunIndexList.Add(list);
                        difSwapFactorMulti = difSwapFactorMulti * -1;
                        if (difSwapFactorMulti > 0) { otherDifSwapFactor = 1; difSwapFactor = 0; }
                        else { otherDifSwapFactor = 0; difSwapFactor = 1; }
                    }
                    else if (indexDiff < 10)
                    {
                        bearingsList.Add(CalculateBearing(gpsPoints[list[difSwapFactor] + 3 * difSwapFactorMulti], gpsPoints[list[otherDifSwapFactor] - 3 * difSwapFactorMulti])); //Done like this so the bearing should be in the same direction for all the consequtive runs
                        usableRunIndexList.Add(list);
                        difSwapFactorMulti = difSwapFactorMulti * -1;
                        if (difSwapFactorMulti > 0) { otherDifSwapFactor = 1; difSwapFactor = 0; }
                        else { otherDifSwapFactor = 0; difSwapFactor = 1; }

                    }

                }

                double averageRunBearing = bearingsList.Average();

                double offsetBearing = 0.0;
                List<int> removedIndexs = new List<int>();

                if (bearingsList.Count >= 3) //If only two valid runs then it will be for travel purposes,
                {
                    int middleIndex = (int)(Math.Abs(largestList[1] - largestList[0]) / 2); //I always seem to get inverted directions on the inputs so lets handle that
                    offsetBearing = CalculateBearing(gpsPoints[middleIndex], gpsPoints[middleIndex + 1]); //We can apply the same bearing to everything because close points should not be close either way'
                    (double lattOffset, double longOffset) = calculateOffsetByHalfSectionWidth(gpsPoints[segment.segments[i]], offsetBearing, OffsetCheckValue); //Chosen some random point to make the offset off because it doe snot matter

                    int ii = 0, jj = 0;
                    while (ii < segment.segments.Count - 2)
                    {
                        jj = ii + 1;
                        offsetBearing = CalculateBearing(gpsPoints[segment.segments[ii]], gpsPoints[segment.segments[ii + 1]]);
                        double maxLatt = gpsPoints[segment.segments[ii]].Latitude + lattOffset;
                        double minLatt = gpsPoints[segment.segments[ii]].Latitude - lattOffset;
                        double maxLong = gpsPoints[segment.segments[ii]].Longitude + longOffset;
                        double minLong = gpsPoints[segment.segments[ii]].Longitude - longOffset;
                        while (jj < segment.segments.Count - 1)
                        {
                            //if (DoesIntersect(gpsPoints[segment.segments[jj]], minLong, maxLong, minLatt, maxLatt))
                            var holder = CalculateDistance(gpsPoints[segment.segments[ii]], gpsPoints[segment.segments[jj]]);
                            //Console.WriteLine($"Found Something {ii}, {jj}, {holder}");
                            if (CalculateDistance(gpsPoints[segment.segments[ii]], gpsPoints[segment.segments[jj]]) < OffsetCheckValue)
                            {
                                Console.WriteLine($"Found Overlap {ii}, {jj}");
                                int iiSegmentRunNum = -1;
                                int jjSegmentRunNum = -1;
                                int k = 0;
                                foreach (var indexStartStopPair in usableRunIndexList)
                                {
                                    if (segment.segments[ii] >= indexStartStopPair[0] && segment.segments[ii] <= indexStartStopPair[1]) //have to assume they will be ordered, can order them earlir
                                    {
                                        iiSegmentRunNum = k;
                                    }

                                    if (segment.segments[jj] >= indexStartStopPair[0] && segment.segments[jj] <= indexStartStopPair[1]) //have to assume they will be ordered, can order them earlir
                                    {
                                        jjSegmentRunNum = k;
                                    }
                                    k++;
                                }

                                if (iiSegmentRunNum > 0 && jjSegmentRunNum < 0) //Keep ii
                                {
                                    removedIndexs.Add(segment.segments[jj]);
                                }

                                else if (iiSegmentRunNum < 0 && jjSegmentRunNum > 0) //Keep jj
                                {
                                    removedIndexs.Add(segment.segments[ii]);
                                }

                                else if (iiSegmentRunNum > 0 && jjSegmentRunNum > 0) //Keep the one with the better bearing in the run
                                {
                                    if (Math.Abs(bearingsList[iiSegmentRunNum] - averageRunBearing) > Math.Abs(bearingsList[jjSegmentRunNum] - averageRunBearing))
                                    {
                                        //Keep JJ because its run was more consistent with the other
                                        removedIndexs.Add(segment.segments[ii]);

                                    }
                                    else ////Keep ii because its run was more consistent with the other
                                    {
                                        removedIndexs.Add(segment.segments[jj]);
                                    }
                                }

                                else
                                {
                                    removedIndexs.Add(segment.segments[jj]); //Remove the older run
                                }

                            }
                            jj++;
                        }

                        ii++;
                    }

                    //Sometimes removes sequential profiles, should add them back again
                    //segment.segments.RemoveAll(x => removedIndexs.Contains(x));
                    //outputList.Add(segment);

                }
                outputList.Add(new SectionsInSegment(segment.section, removedIndexs)); //If the segment is shorter than this we remove it without consideration

            }
            return outputList;
        }

        public (double, double) calculateOffsetByHalfSectionWidth(gpsInfo gpsInfo, double bearing, double squareCheckDistance)
        {
            (double newLatt, double newLong) = GPSPositionFromBearingAndOffset(gpsInfo.Latitude, gpsInfo.Longitude, bearing, squareCheckDistance);
            double lattOffset = newLatt - gpsInfo.Latitude;
            double longOffset = newLong - gpsInfo.Longitude;

            return (lattOffset, longOffset);
        }

        public void WriteDataToCSV(List<SectionsInSegment> sectionsInSegments)
        {
            try
            {
                // Create the base directory if it doesn't exist

                foreach (var section in sectionsInSegments)
                {
                    int sectionId = section.section;
                    string filePath = $"keptsection_{sectionId}.csv";

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write header
                        writer.WriteLine("Distance,Latitude,Longitude");

                        // Write data
                        foreach (int segment in section.segments)
                        {
                            var currentGPSpoint = gpsPoints[segment];

                            writer.WriteLine($"{currentGPSpoint.Distance},{currentGPSpoint.Latitude},{currentGPSpoint.Longitude}");

                        }
                    }

                    Console.WriteLine($"Data for section {sectionId} has been written to the file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing to the file: {ex.Message}");
            }
        }

        public List<List<int>> combineSections(List<int> overlaps)
        {

            List<List<int>> segmentsBySections = new List<List<int>>();
            for (int i = 0; i < features.Count; i++)
            {
                segmentsBySections.Add(new List<int>());
            }

            int ii = 0;
            foreach (int overlapSegment in overlaps)
            {
                if (overlapSegment >= 0)
                {
                    segmentsBySections[overlapSegment].Add(ii);
                }
                ii++;
            }

            return segmentsBySections;
        }

        public List<SectionsInSegment> ruleoutDisjointedTravel(List<List<int>> segmentsByOverlaps, double sectionLength)
        {
            int i = 0;
            List<SectionsInSegment> validOutputList = new List<SectionsInSegment>();
            foreach (List<int> overlapSegmenIndexList in segmentsByOverlaps)
            {
                if (overlapSegmenIndexList.Count < (int)(features[i].Geometry.MaximumSpan * 2 / sectionLength) && overlapSegmenIndexList.Count < 1000)
                {
                    //Do nothing
                }
                else
                {
                    List<List<int>> outputList = new List<List<int>>();
                    outputList.Add(new List<int>());
                    List<int> babyList = new List<int>();
                    int listIndex = 0;
                    int currentIndex = overlapSegmenIndexList[0];
                    foreach (int overlapSegmentIndex in overlapSegmenIndexList)
                    {
                        //if (overlapSegmentIndex <= currentIndex + 30)
                        if (true)
                        {
                            outputList[listIndex].Add(overlapSegmentIndex);
                            currentIndex = overlapSegmentIndex;
                        }
                        else
                        {
                            outputList.Add(new List<int> { overlapSegmentIndex });
                            currentIndex = overlapSegmentIndex;
                            listIndex++;
                            outputList[listIndex].Add(overlapSegmentIndex);
                        }

                    }
                    List<int> largestList = outputList[0]; // Start with the first list
                    foreach (var list in outputList)
                    {
                        if (list.Count > largestList.Count)
                        {
                            largestList = list; //Largest List will be the only one with valid data
                        }
                    }
                    validOutputList.Add(new SectionsInSegment(i, largestList));
                }
                i++;
            }

            return validOutputList;
        }



        // Method to parse the value from the line
        static double ParseValue(string line)
        {
            // Split the line by space and get the last part
            string[] parts = line.Split(' ');
            string valueStr = parts[1]; // assuming the value is before the last part

            // Parse the value to double
            double value = double.Parse(valueStr);

            return value;
        }

        public void readGpsCoordFile(List<string> fileList)
        {


            foreach (string filePath in fileList)
            {
                try
                {
                    // Read all lines from the file
                    string[] lines = File.ReadAllLines(filePath);

                    // Process the lines
                    for (int i = 1; i < lines.Length; i += 1)
                    {
                        //file has columns : sectionFile,_acquisitionCFGParams.RoadSectionLength_m,gpsData.dLongitude,gpsData.dLatitude,gpsData.dAltitude,gpsData.dCourseOverGround

                        // Extract distance, longitude, and latitude from the lines
                        string[] values = lines[i].Split(',');
                        string FisFileName = values[0];
                        string distanceLine = values[1];
                        string longitudeLine = values[2];
                        string latitudeLine = values[3];
                        string AltitudLine = values[4];
                        string AngleLine = values[5];

                        // Parse the values and add them to the lists
                        double distance = double.Parse(distanceLine);
                        double longitude = double.Parse(longitudeLine);
                        double latitude = double.Parse(latitudeLine);
                        double angle = double.Parse(AngleLine);
                        double altitude = double.Parse(AltitudLine);
                        int segment = 0;

                        string pattern = @"_(\d{6})\.fis$";
                        Match match = Regex.Match(FisFileName, pattern);

                        if (match.Success)
                        {
                            segment = Int32.Parse(match.Groups[1].Value);
                            // Console.WriteLine($"Número: {match.Groups[1].Value}");
                        }

                        gpsPoints.Add(new gpsInfo(segment, distance, latitude, longitude, angle, altitude));
                    }
                }
                catch
                {
                    Console.WriteLine("Failed To Read GPS");
                }
            }
        }

        public void readCSVGpsCoordFile(List<string> fileList)
        {


            foreach (string filePath in fileList)
            {
                try
                {
                    // Read all lines from the file
                    string[] lines = File.ReadAllLines(filePath);

                    // Process the lines
                    for (int i = 1; i < lines.Length; i += 1)
                    {
                        //file has columns : sectionFile,_acquisitionCFGParams.RoadSectionLength_m,gpsData.dLongitude,gpsData.dLatitude,gpsData.dAltitude,gpsData.dCourseOverGround

                        // Extract distance, longitude, and latitude from the lines
                        string[] values = lines[i].Split(',');
                        string FisFileName = "";
                        string distanceLine = values[0];
                        string AngleLine = values[1];
                        string latitudeLine = values[2];
                        string longitudeLine = values[3];

                        string AltitudLine = "0";


                        // Parse the values and add them to the lists
                        double distance = double.Parse(distanceLine);
                        double longitude = double.Parse(longitudeLine);
                        double latitude = double.Parse(latitudeLine);
                        double angle = double.Parse(AngleLine);
                        double altitude = double.Parse(AltitudLine);
                        int segment = 0;

                        string pattern = @"_(\d{6})\.fis$";
                        Match match = Regex.Match(FisFileName, pattern);

                        if (match.Success)
                        {
                            segment = Int32.Parse(match.Groups[1].Value);
                            // Console.WriteLine($"Número: {match.Groups[1].Value}");
                        }
                        else
                        {
                            if ((distance) % 4 == 0)
                            {
                                segment = (int)(distance / 4);
                            }
                            else
                            {
                                segment = -1;
                            }
                        }
                        gpsPoints.Add(new gpsInfo(segment, distance, latitude, longitude, angle, altitude));
                    }
                }
                catch
                {
                    Console.WriteLine("Failed To Read GPS");
                }
            }
        }

        public void readGpsCoordFileOld(List<string> fileList)
        {
            foreach (string filePath in fileList)
            {
                try
                {
                    // Read all lines from the file
                    string[] lines = File.ReadAllLines(filePath);

                    // Process the lines
                    for (int i = 0; i < lines.Length; i += 5)
                    {
                        // Extract distance, longitude, and latitude from the lines
                        string distanceLine = lines[i];
                        string latitudeLine = lines[i + 1];
                        string longitudeLine = lines[i + 2];

                        // Parse the values and add them to the lists
                        double distance = ParseValue(distanceLine);
                        double longitude = ParseValue(longitudeLine);
                        double latitude = ParseValue(latitudeLine);

                        gpsPoints.Add(new gpsInfo(i, distance, longitude, latitude, 0, 0));
                    }
                }
                catch
                {
                    Console.WriteLine("Failed To Read GPS");
                }
            }
        }

        public void readSurveySectionFiles(List<string> filenames)
        {

            foreach (string filename in filenames)
            {
                Feature section = readSurveySectionFile(filename);
                if (section != null)
                {
                    features.Add(section);
                }
            }

        }

        public List<int> gpsPointsPotentiallyInSections()
        {
            List<int> overlaps = new List<int>();
            foreach (gpsInfo gpsInfo in gpsPoints)
            {
                int i = 0;
                int overlap = -1;
                foreach (Feature surveySection in features)
                {
                    if (surveySection.Geometry.BoundingBoxIntersection(gpsInfo))
                    { overlap = i; Console.WriteLine($"Start Chainage: {i}"); break; }
                    i++;
                }
                overlaps.Add(overlap);
            }

            return overlaps;

        }

        Feature readSurveySectionFile(string jsonFilePath)
        {
            try
            {
                // Read JSON file
                string jsonContent = File.ReadAllText(jsonFilePath);

                // Deserialize JSON to Feature object
                Feature feature = JsonConvert.DeserializeObject<Feature>(jsonContent);

                // Access properties of the Feature object
                Console.WriteLine($"Survey ID: {feature.Properties.SurveyId}");
                Console.WriteLine($"Survey Description: {feature.Properties.SurveyDescription}");
                Console.WriteLine($"Start Chainage: {feature.Properties.StartChainage}");

                // Access geometry coordinates
                double maxLong = 0;
                double minLong = 0;
                double maxLatt = 0;
                double minLatt = 0;
                bool firstFlag = true;
                foreach (var coordinate in feature.Geometry.Coordinates) //Better to pull first one out then not use the if statement
                {
                    if (firstFlag)
                    {
                        maxLong = coordinate[0];
                        minLong = coordinate[0];
                        maxLatt = coordinate[1];
                        minLatt = coordinate[1];
                        firstFlag = false;
                    }
                    else
                    {
                        maxLong = Math.Max(maxLong, coordinate[0]);
                        minLong = Math.Min(minLong, coordinate[0]);
                        maxLatt = Math.Max(maxLatt, coordinate[1]);
                        minLatt = Math.Min(minLatt, coordinate[1]);
                    }

                    Console.WriteLine($"Coordinate: ({coordinate[0]}, {coordinate[1]})");
                }
                feature.Geometry.MaxLongitude = maxLong;
                feature.Geometry.MinLongitude = minLong;
                feature.Geometry.MaxLatitude = maxLatt;
                feature.Geometry.MinLatitude = minLatt;
                feature.Geometry.SetMaximumSpan();

                return feature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading JSON file: {ex.Message}");
                return null;
            }

        }

        static double CalculateBearing(gpsInfo from, gpsInfo to)
        {
            double fromLatitude = from.Latitude;
            double fromLongitude = from.Longitude;
            double toLatitude = to.Latitude;
            double toLongitude = to.Longitude;

            double deltaLongitude = toLongitude - fromLongitude;

            double y = Math.Sin(deltaLongitude) * Math.Cos(toLatitude);
            double x = Math.Cos(fromLatitude) * Math.Sin(toLatitude) - Math.Sin(fromLatitude) * Math.Cos(toLatitude) * Math.Cos(deltaLongitude);

            double bearing = Math.Atan2(y, x);

            // Convert bearing from radians to degrees
            bearing = RadianToDegree(bearing);

            // Normalize the bearing to a compass bearing (0° to 360°)
            bearing = (bearing + 360) % 360;

            return bearing;
        }

        static double DegreeToRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        static double RadianToDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }

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
            double d = offset / 6371000;
            double bearing_Rad = DegreeToRadian(bearing);

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

        public bool DoesIntersect(gpsInfo gpspoint, double MinLongitude, double MaxLongitude, double MinLatitude, double MaxLatitude)
        {
            return (gpspoint.Longitude >= MinLongitude && gpspoint.Longitude <= MaxLongitude && gpspoint.Latitude >= MinLatitude && gpspoint.Latitude <= MaxLatitude);
        }

        // Function to calculate the distance between two GPS points in meters using the Haversine formula
        public static double CalculateDistance(gpsInfo point1, gpsInfo point2)
        {
            double earthRadius = 6371000; // Earth's radius in meters

            // Convert latitude and longitude from degrees to radians
            double lat1Rad = DegreeToRadian(point1.Latitude);
            double lon1Rad = DegreeToRadian(point1.Longitude);
            double lat2Rad = DegreeToRadian(point2.Latitude);
            double lon2Rad = DegreeToRadian(point2.Longitude);

            // Haversine formula
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }



        public class Geometry
        {
            public string Type { get; set; }
            public List<List<double>> Coordinates { get; set; }

            public double MinLongitude { get; set; } = double.MaxValue;
            public double MaxLongitude { get; set; } = double.MinValue;
            public double MinLatitude { get; set; } = double.MaxValue;
            public double MaxLatitude { get; set; } = double.MinValue;

            public double MaximumSpan { get; set; }

            public bool BoundingBoxIntersection(gpsInfo gpspoint)
            {
                return (gpspoint.Longitude >= MinLongitude && gpspoint.Longitude <= MaxLongitude && gpspoint.Latitude >= MinLatitude && gpspoint.Latitude <= MaxLatitude);
            }

            public void SetMaximumSpan() //Returns the distance between gps points in meters, accurate over short distances
            {
                //halfsine formulate implimentation, 
                const double earthRadiusKm = 6371000; // Radius of the Earth in kilometers

                double dLat = (MaxLatitude - MinLatitude) * Math.PI / 180; ;
                double dLon = (MaxLongitude - MinLongitude) * Math.PI / 180; ;

                double lat1 = MinLatitude * Math.PI / 180;
                double lat2 = MaxLatitude * Math.PI / 180;

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                MaximumSpan = Math.Max((earthRadiusKm * c), 10); //Always setting some value for comparisons
            }


        }



        public class Properties
        {
            public string SurveyId { get; set; }
            public string SurveyDescription { get; set; }
            public string SurveyInstruction { get; set; }
            public int StartChainage { get; set; }
            public int Direction { get; set; }
            public int Lane { get; set; }
            public int GpsAutoStart { get; set; }
            public int GpsAutoStartType { get; set; }
            public List<string> Modules { get; set; }
            public string OperatorName { get; set; }
            public string VehicleId { get; set; }
            public int VehicleOdoCalibration { get; set; }
            public string CfgFile { get; set; }
            public string CompletedDate { get; set; }
            public List<object> UserDefinedFields { get; set; }
        }

        public class Feature
        {
            public string Type { get; set; }
            public Geometry Geometry { get; set; }
            public Properties Properties { get; set; }


        }

    }
}