using DataView2.Core.Models;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Esri.ArcGISRuntime.UI;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Xml.Linq;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class ShapefileService : IShapefileService
    {
        private readonly IRepository<Shapefile> _repository;
        private readonly AppDbContextProjectData _context;

        public ShapefileService(IRepository<Shapefile> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }
        public async Task<IdReply> ProcessShapefiles(List<ShapefileRequest> request, CallContext context = default)
        {
            try
            {
                List<Shapefile> shapefileList = new List<Shapefile>();

                Parallel.ForEach(request, shapefileRequest =>
                {
                    string shapefilePath = shapefileRequest.FilePath;

                    DotSpatial.Data.Shapefile sf = DotSpatial.Data.Shapefile.OpenFile(shapefilePath);
                    sf.Reproject(DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984);
                    System.Data.DataTable attributeTable = sf.Attributes.Table;
                    DotSpatial.Data.IFeatureList features = sf.Features;

                    for (int i = 0; i < features.Count; i++)
                    {
                        var geometry = features[i].Geometry;
                        var featureType = features[i].FeatureType.ToString();
                        var featureAttributes = new Dictionary<string, object>();

                        foreach (DataColumn column in attributeTable.Columns)
                        {
                            string attributeName = column.ColumnName;
                            object attributeValue = attributeTable.Rows[i][column];
                            featureAttributes.Add(attributeName, attributeValue);
                        }

                        if (geometry is NetTopologySuite.Geometries.MultiPoint multiPoint)
                        {
                            foreach(var point in multiPoint.Geometries)
                            {
                                AddShapefile(shapefileList, shapefileRequest, featureAttributes, point, "Point");
                            }
                        }
                        else if(geometry is NetTopologySuite.Geometries.MultiLineString multiLineString)
                        {
                            foreach( var lineString in multiLineString.Geometries)
                            {
                                AddShapefile(shapefileList, shapefileRequest, featureAttributes, lineString, "Line");
                            }
                        }
                        else if (geometry is NetTopologySuite.Geometries.MultiPolygon multiPolygon)
                        {
                            foreach (var polygon in multiPolygon.Geometries)
                            {
                                AddShapefile(shapefileList, shapefileRequest, featureAttributes, polygon, "Polygon");
                            }
                        }
                        else
                        {
                            AddShapefile(shapefileList, shapefileRequest, featureAttributes, geometry, featureType);
                        }
                    }
                });

                // Retrieve all existing shapefiles from the database
                var existingShapefiles = await _repository.GetAllAsync();

                // Convert existing shapefiles into a HashSet for faster lookup
                var existingSet = existingShapefiles
                    .Select(e => new { e.ShapefileName, e.Coordinates, e.Attributes, e.ShapeType })
                    .ToHashSet();

                // Filter out duplicates efficiently
                var newShapefiles = shapefileList
                    .Where(e => !existingSet.Contains(new { e.ShapefileName, e.Coordinates, e.Attributes, e.ShapeType }))
                    .ToList();

                if (newShapefiles.Any())
                {
                    await _repository.CreateRangeAsync(newShapefiles);
                }

                return new IdReply
                {
                    Id = 0,
                    Message = "Shapefiles are successfully imported."
                };
            }
            catch (Exception ex)
            {
                Log.Information($"Error in importing shapefiles: {ex.Message}");
                return new IdReply
                {
                    Id = 1,
                    Message = "Error Occured during importing."
                }; ;
            }
        }

        private void AddShapefile(List<Shapefile> shapefileList, ShapefileRequest shapefileRequest, Dictionary<string, object> featureAttributes, NetTopologySuite.Geometries.Geometry geometry, string shapeType)
        {
            string coordinates = ConvertGeometryToString(geometry, shapeType);
            string jsonAttribute = JsonConvert.SerializeObject(featureAttributes);

            var shapefile = new Shapefile
            {
                ShapefileName = shapefileRequest.ShapefileName,
                Coordinates = coordinates,
                Attributes = jsonAttribute,
                ShapeType = shapeType
            };
            shapefileList.Add(shapefile);
        }

        private string ConvertGeometryToString(NetTopologySuite.Geometries.Geometry geometry, string shapeType)
        {
            // Ensure shapeType is in lower case for comparison
            shapeType = shapeType.ToLower();

            object coordinates;

            // Handle different geometry types
            if (shapeType == "point")
            {
                var coord = geometry.Coordinates.First();
                coordinates = new[] { coord.X, coord.Y };
            }
            else if (shapeType == "linestring" || shapeType == "line")
            {
                coordinates = geometry.Coordinates
                               .Select(c => new[] { c.X, c.Y })
                               .ToArray();
            }
            else if (shapeType == "polygon")
            {
                coordinates = new[]
                            {
                                geometry.Coordinates
                                    .Select(c => new[] { c.X, c.Y })
                                    .ToArray()
                            };
            }
            else
            {
                throw new ArgumentException("Unsupported shape type.");
            }

            var jsonObject = new
            {
                coordinates
            };

            return JsonConvert.SerializeObject(jsonObject);
        }

        public async Task<List<Shapefile>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return entities.ToList();
        }

        public async Task<List<Shapefile>> GetByName(string name, CallContext context = default)
        {
            var allEntities = await _repository.GetAllAsync();
            var shapefiles = allEntities.Where(shp => shp.ShapefileName == name).ToList();

            return shapefiles;
        }

        public async Task<List<string>> HasData(Empty empty, CallContext context = default)
        {
            try
            {
                var distinctShapefileNames = await _context.Shapefile
                                              .Select(s => s.ShapefileName)
                                              .Distinct()
                                              .ToListAsync();
                if (distinctShapefileNames.Any())
                {
                    return distinctShapefileNames;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<string>();
        }

        public async Task<IdReply> DeleteByName(string name, CallContext context = default)
        {
            try
            {
                var allEntities = await _repository.GetAllAsync();
                var entitiesWithSameName = allEntities.Where(entity => entity.ShapefileName == name).ToList();
                if (entitiesWithSameName != null && entitiesWithSameName.Any())
                {
                    await _repository.DeleteRangeAsync(entitiesWithSameName);

                    return new IdReply
                    {
                        Id = 1,
                        Message = $"Shapefile '{name}' has been deleted."
                    };
                }
                return new IdReply
                {
                    Id = 0,
                    Message = $"No data found with shapefile name '{name}'."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting entities: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = "Error occurred during deletion."
                };
            }
        }
    }
}
