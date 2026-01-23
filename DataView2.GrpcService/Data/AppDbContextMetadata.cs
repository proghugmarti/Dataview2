using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Setting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.GrpcService.Data
{
    public class AppDbContextMetadata : DbContext
    {
        public AppDbContextMetadata(DbContextOptions<AppDbContextMetadata> options) : base(options)
        {

        }

        public DbSet<ProjectRegistry> ProjectRegistries => Set<ProjectRegistry>();
        public DbSet<DatabaseRegistryLocal> DatabaseRegistry => Set<DatabaseRegistryLocal>();
        public DbSet<GeneralSetting> GeneralSettings => Set<GeneralSetting>();
        public DbSet<CrackClassificationConfiguration> CrackClassification => Set<CrackClassificationConfiguration>();

        public DbSet<MapGraphicData> MapGraphic => Set<MapGraphicData>();
        public DbSet<TablesSetting> TableExport => Set<TablesSetting>();
        public DbSet<ColorCodeInformation> ColorCodeInformation => Set<ColorCodeInformation>();
        public DbSet<DatasetBackup> Backups => Set<DatasetBackup>();

        public DbSet<MetaTable> MetaTable => Set<MetaTable>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TablesSetting>().ToView("NoTable");
            modelBuilder.ApplyConfiguration(new GeneralSettingConfiguration());
            modelBuilder.ApplyConfiguration(new CrackClassificationConfigConfiguration());
            modelBuilder.ApplyConfiguration(new MapGraphicDataConfiguration());
            modelBuilder.ApplyConfiguration(new ColorCodeInformationConfiguration());
        }
    }

    internal class CrackClassificationConfigConfiguration : IEntityTypeConfiguration<CrackClassificationConfiguration>
    {
        public void Configure(EntityTypeBuilder<CrackClassificationConfiguration> builder)
        {
            builder.HasData(
                new CrackClassificationConfiguration
                {
                    Id = 1,
                    MinSizeToStraight = 4,
                    MinSizeToAvoidMerge = 6,
                    Straightness = 0.7,
                    MinimumDeep = 0,
                    IgnoreOutLanes = true
                });
        }
    }

    internal class GeneralSettingConfiguration : IEntityTypeConfiguration<GeneralSetting>
    {
        public void Configure(EntityTypeBuilder<GeneralSetting> builder)
        {
            // Add default column at first when migrating
            builder.HasData(
                new GeneralSetting
                {
                    Id = 1,
                    Name = "IP Address",
                    Description = "DataView IP Address",
                    Type = SettingType.Float,
                    Value = "0.0.0.1",
                    Category = "NetWorking",
                },
                new GeneralSetting
                {
                    Id = 2,
                    Name = "ExportURL",
                    Description = "URL for exporting",
                    Type = SettingType.String,
                    Value = "https://dvwebservice20240808112104.azurewebsites.net", 
                    Category = "Networking",
                });
        }
    }

    internal class MapGraphicDataConfiguration : IEntityTypeConfiguration<MapGraphicData>
    {
        public void Configure(EntityTypeBuilder<MapGraphicData> builder)
        {
            //Default defects graphic symbols
            builder.HasData(
                new MapGraphicData { Id = 1, Name = LayerNames.Cracking, Red = 255, Green = 255, Blue = 255, Alpha = 255, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 2, Name = LayerNames.Ravelling, Red = 0, Green = 0, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 3, Name = LayerNames.Pickout, Red = 168, Green = 26, Blue = 35, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 4, Name = LayerNames.Potholes, Red = 44, Green = 70, Blue = 110, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 5, Name = LayerNames.Patch, Red = 150, Green = 50, Blue = 150, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 6, Name = LayerNames.Spalling, Red = 255, Green = 255, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 7, Name = LayerNames.CornerBreak, Red = 255, Green = 88, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 8, Name = LayerNames.ConcreteJoint, Red = 65, Green = 105, Blue = 225, Alpha = 255, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 9, Name = "Boundaries", Red = 255, Green = 0, Blue = 255, Alpha = 255 , Thickness = 1, SymbolType = "Fill"},
                new MapGraphicData { Id = 10, Name = LayerNames.Segment, Red = 0, Green = 0, Blue = 0, Alpha = 50, Thickness = 1, SymbolType = "Segment"},
                new MapGraphicData { Id = 11, Name = "HighlightedSegment", Red = 191, Green = 0, Blue = 255, Alpha = 255, Thickness = 1, SymbolType = "Segment"},
                new MapGraphicData { Id = 12, Name = LayerNames.CurbDropOff, Red = 255, Green = 0, Blue = 0, Alpha = 255, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 13, Name = LayerNames.MarkingContour, Red = 255, Green = 255, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 14, Name = LayerNames.SealedCrack, Red = 191, Green = 22, Blue = 83, Alpha = 255, Thickness = 10, SymbolType = "Line" },
                new MapGraphicData { Id = 15, Name = MultiLayerName.LwpIRI, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 16, Name = MultiLayerName.RwpIRI, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 17, Name = MultiLayerName.CwpIRI, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 18, Name = MultiLayerName.LaneIRI, Red = 138, Green = 43, Blue = 226, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 19, Name = LayerNames.MMO, Red = 17, Green = 247, Blue = 17, Alpha = 255, Thickness = 5, SymbolType = "FillLine" },
                new MapGraphicData { Id = 20, Name = LayerNames.Pumping, Red = 17, Green = 247, Blue = 17, Alpha = 255, Thickness = 5, SymbolType = "FillLine" },
                new MapGraphicData { Id = 21, Name = LayerNames.Shove, Red = 255, Green = 0, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 22, Name = LayerNames.RumbleStrip, Red = 255, Green = 0, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "FillLine" },
                new MapGraphicData { Id = 23, Name = LayerNames.Bleeding, Red = 255, Green = 255, Blue = 255, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 24, Name = MultiLayerName.BandTexture, Red = 255, Green = 128, Blue = 255, Alpha = 128, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 25, Name = LayerNames.Geometry, Red = 0, Green = 50, Blue = 100, Alpha = 128, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 26, Name = LayerNames.SagsBumps, Red = 0, Green = 255, Blue = 0, Alpha = 128, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 27, Name = LayerNames.WaterEntrapment, Red = 0, Green = 0, Blue = 255, Alpha = 128, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 28, Name = "LasPoint", Red = 168, Green = 26, Blue = 35, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 29, Name = MultiLayerName.LeftRut, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5 , SymbolType = "Line"},
                new MapGraphicData { Id = 30, Name = MultiLayerName.RightRut, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 31, Name = MultiLayerName.LaneRut, Red = 255, Green = 255, Blue = 0, Alpha = 255, Thickness = 5, SymbolType = "Line" },
                new MapGraphicData { Id = 32, Name = LayerNames.SegmentGrid, Red = 0, Green = 0, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill" },
                new MapGraphicData { Id = 33, Name = "LasPoints", Red = 255, Green = 165, Blue = 0, Alpha = 170, Thickness = 1, SymbolType = "Fill"},
                new MapGraphicData { Id = 34, Name = LayerNames.Grooves, Red= 255, Green = 255, Blue = 255, Alpha = 170, Thickness = 1, SymbolType = "Fill"},
                new MapGraphicData { Id = 35, Name = LayerNames.CrackSummary, Red= 255, Green = 255, Blue = 255, Alpha = 255, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 36, Name = LayerNames.Keycode, Red = 255, Green = 0, Blue = 0, Alpha = 255, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 37, Name = "LasRutting", Red = 255, Green = 0, Blue = 0, Alpha = 200, Thickness = 3, SymbolType = "Line" },
                new MapGraphicData { Id = 38, Name = MultiLayerName.AverageTexture, Red = 255, Green = 0, Blue = 0, Alpha = 128, Thickness = 1, SymbolType = "Fill" }
            );
        }
    }
    internal class ColorCodeInformationConfiguration : IEntityTypeConfiguration<ColorCodeInformation>
    {
        public void Configure(EntityTypeBuilder<ColorCodeInformation> builder)
        {
            //Default defects graphic color codes (usually by severity)
            builder.HasData(
                new ColorCodeInformation { Id = 1, TableName = LayerNames.CurbDropOff, IsStringProperty = true, Property = "Type", StringProperty = "Curb", Thickness = 3, HexColor= "#E17DFAFF" },
                new ColorCodeInformation { Id = 2, TableName = LayerNames.CurbDropOff, IsStringProperty = true, Property = "Type", StringProperty = "Dropoff", Thickness = 3, HexColor = "#00FD28FF" },
                new ColorCodeInformation { Id = 3, TableName = LayerNames.Cracking, IsStringProperty = true, Property = "Severity", StringProperty = "Very Low", Thickness = 3, HexColor = "#40FFFFFF" },
                new ColorCodeInformation { Id = 4, TableName = LayerNames.Cracking, IsStringProperty = true, Property = "Severity", StringProperty = "Low", Thickness = 3, HexColor = "#00C000FF" },
                new ColorCodeInformation { Id = 5, TableName = LayerNames.Cracking, IsStringProperty = true, Property = "Severity", StringProperty = "Medium", Thickness = 3, HexColor = "#FF970FFF" },
                new ColorCodeInformation { Id = 6, TableName = LayerNames.Cracking, IsStringProperty = true, Property = "Severity", StringProperty = "High", Thickness = 3, HexColor = "#FF0000FF" },
                new ColorCodeInformation { Id = 7, TableName = LayerNames.CrackSummary, IsStringProperty = true, Property = "Severity", StringProperty = "Very Low", Thickness = 3, HexColor = "#40FFFFFF" },
                new ColorCodeInformation { Id = 8, TableName = LayerNames.CrackSummary, IsStringProperty = true, Property = "Severity", StringProperty = "Low", Thickness = 3, HexColor = "#00C000FF" },
                new ColorCodeInformation { Id = 9, TableName = LayerNames.CrackSummary, IsStringProperty = true, Property = "Severity", StringProperty = "Medium", Thickness = 3, HexColor = "#FF970FFF" },
                new ColorCodeInformation { Id = 10, TableName = LayerNames.CrackSummary, IsStringProperty = true, Property = "Severity", StringProperty = "High", Thickness = 3, HexColor = "#FF0000FF" },
                new ColorCodeInformation { Id = 11, TableName = LayerNames.Ravelling, IsStringProperty = true, Property = "Severity", StringProperty = "Low", Thickness = 3, HexColor = "#2E9FCFAA" },
                new ColorCodeInformation { Id = 12, TableName = LayerNames.Ravelling, IsStringProperty = true, Property = "Severity", StringProperty = "Medium", Thickness = 3, HexColor = "#3569B0AA" },
                new ColorCodeInformation { Id = 13, TableName = LayerNames.Ravelling, IsStringProperty = true, Property = "Severity", StringProperty = "High", Thickness = 3, HexColor = "#BF0053AA" },
                new ColorCodeInformation { Id = 14, TableName = LayerNames.Bleeding, IsStringProperty = true, Property = "Severity", StringProperty = "Low", Thickness = 3, HexColor = "#FFFF00AA" },
                new ColorCodeInformation { Id = 15, TableName = LayerNames.Bleeding, IsStringProperty = true, Property = "Severity", StringProperty = "Medium", Thickness = 3, HexColor = "#FFA500AA" },
                new ColorCodeInformation { Id = 16, TableName = LayerNames.Bleeding, IsStringProperty = true, Property = "Severity", StringProperty = "High", Thickness = 3, HexColor = "#FF0000AA" },
                new ColorCodeInformation { Id = 17, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "Very Low", Thickness = 3, HexColor = "#00FF00AA" },
                new ColorCodeInformation { Id = 18, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "Low", Thickness = 3, HexColor = "#FFFF00AA" },
                new ColorCodeInformation { Id = 19, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "Medium", Thickness = 3, HexColor = "#FFA500AA" },
                new ColorCodeInformation { Id = 20, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "High", Thickness = 3, HexColor = "#FF0000AA" },
                new ColorCodeInformation { Id = 21, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "Very High", Thickness = 3, HexColor = "#C80000AA" },
                new ColorCodeInformation { Id = 22, TableName = LayerNames.SegmentGrid, IsStringProperty = true, Property = "Severity", StringProperty = "None", Thickness = 3, HexColor = "#000000AA" }
                );
        }
    }
}
