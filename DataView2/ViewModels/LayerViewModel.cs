using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Engines;
using DataView2.MapHelpers;
using DataView2.Pages.PCI;
using DataView2.States;
using DataView2.XAML;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.Core.Models.ExportTemplate.ExportPCIToXml;
using DataView2.Core.Models.Other;

namespace DataView2.ViewModels
{
    public class LayerViewModel : INotifyPropertyChanged
    {
        public ApplicationState appState;
        public ApplicationEngine appEngine;

        public LayerViewModel()
        {
            appState = MauiProgram.AppState;
            appEngine = MauiProgram.AppEngine;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void ClosePopup()
        {
            WeakReferenceMessenger.Default.Send(this, "ClosePopup");
        }

        public void OpenLayerEditor(string tableName, string layerType)
        {
            appState.GetGraphicColor(tableName);

            //Severity color layers
            var popup = new LayerEditorPopup(tableName, layerType);
            App.Current.MainPage.ShowPopup(popup);
        }

        public async void OpenIconEditor(string tableName)
        {
            var popup = new LayerEditorPopup(tableName, "MetaTable", pointIcon:true);
            App.Current.MainPage.ShowPopup(popup);
        }

        public void OpenImportLayerPopup(string file)
        {
            var popup = new ImportLayerPopup(file);
            App.Current.MainPage.ShowPopup(popup);
        }

        public void OpenNewTablePopup(string mode, string tableName = null)
        {
            var popup = new AddNewTablePopup(mode, tableName);
            App.Current.MainPage.ShowPopup(popup);
        }
        public void OpenSelectLayersPopup()
        {
            var popup = new DeleteLayersPopup();
            App.Current.MainPage.ShowPopup(popup);
        }

        public void OpenSummariesPopup(string mode, string tableName = null)
        {
            var popup = new SummariesPopup(mode, tableName);
            App.Current.MainPage.ShowPopup(popup);
        }

        public IEnumerable<string> GetNumericProperties(string tableName)
        {
            if (tableName != null)
            {
                var dbTableName = TableNameHelper.GetDBTableName(tableName);

                if (tableName == MultiLayerName.LaneIRI || tableName == MultiLayerName.LwpIRI || tableName == MultiLayerName.RwpIRI || tableName == MultiLayerName.CwpIRI)
                {
                    dbTableName = "LCMS_Rough_Processed";
                }
                else if (tableName == MultiLayerName.LaneRut || tableName == MultiLayerName.LeftRut || tableName == MultiLayerName.RightRut)
                {
                    dbTableName = "LCMS_Rut_Processed";
                }

                var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(assembly => assembly.GetTypes())
                   .Where(type => typeof(IEntity).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract && type.Name != "XMLObject")
                   .ToList();

                var entityType = entityTypes.FirstOrDefault(t => t.Name == dbTableName);

                if (entityType != null)
                {
                    // Get the numeric fields using reflection
                    var fields = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(prop => IsNumericType(prop.PropertyType) && !prop.Name.Contains("GPS") && !prop.Name.Contains("Id"))
                            .Select(prop => prop.Name)
                            .ToList();

                    return fields;
                }
            }

            return new List<string>();
        }

        public IEnumerable<string> GetStringProperties(string tableName)
        {
            switch (tableName)
            {
                case LayerNames.Spalling:
                case LayerNames.ConcreteJoint:
                    return new List<string>
                {
                    "JointDirection"
                };
                case LayerNames.Cracking:
                case LayerNames.Potholes:
                case LayerNames.Patch:
                case LayerNames.Ravelling:
                case LayerNames.SegmentGrid:
                    return new List<string>
                {
                    "Severity"
                };
                case LayerNames.CurbDropOff:
                case LayerNames.MarkingContour:
                case LayerNames.MMO:
                case LayerNames.RumbleStrip:
                case LayerNames.SagsBumps:
                    return new List<string>
                {
                    "Type"
                };
                case LayerNames.Shove:
                    return new List<string>
                {
                    "LaneSide"
                };
                case LayerNames.Bleeding:
                    return new List<string>
                {
                    "LeftSeverity", "RightSeverity"
                };
                case LayerNames.CrackSummary:
                    return new List<string>
                {
                    "Severity", "MTQ"
                };
             
                default:
                    return Enumerable.Empty<string>();
            }
        }

        private static bool IsNumericType(System.Type type)
        {
            // Check if the type is nullable (e.g., double? or float?)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0]; // Get the underlying type
            }

            // Check for numeric types
            return type == typeof(int) || type == typeof(double) || type == typeof(float) ||
                   type == typeof(decimal) || type == typeof(long) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte);
        }


        public async void ExportPCI(string pciRatingName, string exportFolder)
        {
            try
            {
                var pciRating = await appEngine.PCIRatingService.GetByName(pciRatingName);
                if (pciRating != null && pciRating.RatingName != null)
                {
                    var pciDefects = pciRating.PCIDefects ?? new List<PCIDefects>();
                    var inspectedElementList = new List<InspectedElement>();

                    var sampleUnits = await appEngine.SampleUnitService.GetBySampleUnitSet(new Core.Models.LCMS_Data_Tables.IdRequest { Id = pciRating.SampleUnitSetId });
                    if (sampleUnits != null && sampleUnits.Count > 0)
                    {
                        //export only completed sample units
                        var status = await appEngine.PCIRatingService.GetRatingStatus(new Core.Models.LCMS_Data_Tables.IdRequest { Id = pciRating.Id });
                        var validSampleUnitIds = new List<int>(status.Where(s => s.Status).Select(s => s.SampleUnitId));
                        var filteredSampleUnits = sampleUnits.Where(x => validSampleUnitIds.Contains(x.Id)).ToList();

                        foreach (var sampleUnit in filteredSampleUnits)
                        {
                            var sampleUnitId = sampleUnit.Id;
                            var area = sampleUnit.Area_m2;
                            var numOfSlabs = sampleUnit.NumOfSlabs;

                            var parsedCoordinates = System.Text.Json.JsonSerializer.Deserialize<List<List<double>>>(sampleUnit.Coordinates);
                            if (parsedCoordinates == null) return;
                            var points = new List<MapPoint>();
                            foreach (var coord in parsedCoordinates)
                            {
                                // Assuming coordinates are in [longitude, latitude] order
                                points.Add(new MapPoint(coord[0], coord[1], SpatialReferences.Wgs84));
                            }

                            var polygon = new Polygon(points);
                            var centerPoint = GeometryEngine.LabelPoint(polygon);
                            var latitude = centerPoint.Y;
                            var longitude = centerPoint.X;

                            var latDMS = GeneralMapHelper.ConvertToDMS(latitude, true);
                            var longDMS = GeneralMapHelper.ConvertToDMS(longitude, false);

                            var centerLocation = new CenterLocation
                            {
                                Latitude = new Latitude
                                {
                                    Degrees = latDMS.degrees,
                                    Minutes = latDMS.minutes,
                                    Seconds = latDMS.seconds,
                                    NorthSouth = latDMS.direction
                                },

                                Longitude = new Longitude
                                {
                                    Degrees = longDMS.degrees,
                                    Minutes = longDMS.minutes,
                                    Seconds = longDMS.seconds,
                                    EastWest = longDMS.direction
                                }
                            };

                            var pciDistressList = new List<LevelDistress>();

                            var defectsForUnit = pciDefects
                            .Where(d => d.SampleUnitId == sampleUnitId)
                            .GroupBy(d => new { d.DefectName, d.Severity })
                            .Select(g => new
                            {
                                DefectName = g.Key.DefectName,
                                Severity = g.Key.Severity,
                                CombinedQty = g.Sum(d => d.Qty),
                                DistressCode = PCIDefectJSON.GetDistressCode(g.Key.DefectName)
                            });

                            foreach (var defect in defectsForUnit)
                            {
                                var firstSeverityLetter = defect.Severity.Substring(0, 1).ToUpper();
                                if (defect.Severity == "N/A")
                                {
                                    firstSeverityLetter = "NA";
                                }

                                var levelDistress = new LevelDistress
                                {
                                    DistressCode = defect.DistressCode,
                                    Severity = firstSeverityLetter,
                                    Quantity = defect.CombinedQty
                                };

                                pciDistressList.Add(levelDistress);
                            }

                            var pid = $"{pciRating.NetworkId}::{pciRating.BranchId}::{pciRating.SectionId}";

                            var inspectedElement = new InspectedElement
                            {
                                InspectedElementID = sampleUnitId,
                                PID = pid,
                                Size = area,
                                CenterLocation = centerLocation,
                                InspectionData = new InspectionData
                                {
                                    PCIDistresses = new PCIDistresses
                                    {
                                        LevelDistressList = pciDistressList // can be empty—still serialized
                                    }
                                }
                            };

                            //if num of slabs are not null and surface is concrete, use numOfSlabs instead of area
                            if (numOfSlabs != null && pciRating.Surface == "Concrete")
                            {
                                inspectedElement.Size = numOfSlabs.Value;
                            }

                            inspectedElementList.Add(inspectedElement);

                        }
                    }


                    var pavementData = new PavementData
                    {
                        GeospatialInspectionDataList = new List<GeospatialInspectionData>
                        {
                            new GeospatialInspectionData
                            {
                                InspectionDate = DateTime.Now.ToString("dd/MM/yyyy"),
                                Units = "Metric",
                                Level = "SAMPLE",
                                InspectedElements = inspectedElementList
                            }
                        }
                    };

                    ExportToXml(pavementData, exportFolder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public async void ExportToXml(PavementData data, string exportFolder)
        {
            var serializer = new XmlSerializer(typeof(PavementData));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            string downloadsPath = Path.Combine(exportFolder, "PCI");

            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }

            string filePath = Path.Combine(downloadsPath, $"DataView2_PCIRatingXML_{DateTime.Now.ToFileTime()}.xml");

            using (var writer = XmlWriter.Create(filePath, settings))
            {
                serializer.Serialize(writer, data);
            }
        }
    }
}
