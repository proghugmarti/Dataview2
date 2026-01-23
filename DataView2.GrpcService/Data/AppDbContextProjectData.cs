using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.DTS;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Positioning;
using DataView2.Core.Models.QC;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Services.AppDbServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace DataView2.GrpcService.Data
{
    public class AppDbContextProjectData : DbContext
    {
        private readonly DatabasePathProvider _databasePathProvider;

        public AppDbContextProjectData(DbContextOptions<AppDbContextProjectData> options, DatabasePathProvider databasePathProvider) : base(options)
        {

            _databasePathProvider = databasePathProvider;
        }

        //PROCESSING TABLES
        public DbSet<GPS_Processed> GPS_Processed => Set<GPS_Processed>();
        public DbSet<XMLObject> XMLObject => Set<XMLObject>();
        public DbSet<SummaryCrackClasification> SummaryCrackClasifications => Set<SummaryCrackClasification>();
        public DbSet<TablesSetting> TableExport => Set<TablesSetting>();

        //LCMS DATA TABLES
        public DbSet<Survey> Survey => Set<Survey>();
        public DbSet<SurveySegmentation> SurveySegmentation => Set<SurveySegmentation>();
        public DbSet<LCMS_Segment> LCMS_Segment => Set<LCMS_Segment>();
        public DbSet<LCMS_PickOuts_Raw> LCMS_PickOuts_Raw => Set<LCMS_PickOuts_Raw>();
        public DbSet<LCMS_Bleeding> LCMS_Bleeding => Set<LCMS_Bleeding>();
        public DbSet<LCMS_Cracking_Raw> LCMS_Cracking_Raw => Set<LCMS_Cracking_Raw>();
        public DbSet<LCMS_Rut_Processed> LCMS_Rut_Processed => Set<LCMS_Rut_Processed>();
        public DbSet<LCMS_Ravelling_Raw> LCMS_Ravelling_Raw => Set<LCMS_Ravelling_Raw>();
        public DbSet<LCMS_Patch_Processed> LCMS_Patch_Processed => Set<LCMS_Patch_Processed>();
        public DbSet<LCMS_Potholes_Processed> LCMS_Potholes_Processed => Set<LCMS_Potholes_Processed>();
        public DbSet<LCMS_Spalling_Raw> LCMS_Spalling_Raw => Set<LCMS_Spalling_Raw>();
        public DbSet<LCMS_Corner_Break> LCMS_Corner_Break => Set<LCMS_Corner_Break>();
        public DbSet<LCMS_Concrete_Joints> LCMS_Concrete_Joints => Set<LCMS_Concrete_Joints>();
        public DbSet<LCMS_FOD> LCMS_FOD => Set<LCMS_FOD>();
        public DbSet<LCMS_Lane_Mark_Processed> LCMS_Lane_Mark_Processed => Set<LCMS_Lane_Mark_Processed>();
        public DbSet<LCMS_Curb_DropOff> LCMS_Curb_DropOff => Set<LCMS_Curb_DropOff>();
        public DbSet<LCMS_Sealed_Cracks> LCMS_Sealed_Cracks => Set<LCMS_Sealed_Cracks>();
        public DbSet<LCMS_Marking_Contour> LCMS_Marking_Contour => Set<LCMS_Marking_Contour>();
        public DbSet<LCMS_Pumping_Processed> LCMS_Pumping_Processed => Set<LCMS_Pumping_Processed>();
        public DbSet<LCMS_MMO_Processed> LCMS_MMO_Processed => Set<LCMS_MMO_Processed>();
        public DbSet<LCMS_Texture_Processed> LCMS_Texture_Processed => Set<LCMS_Texture_Processed>();
        public DbSet<LCMS_Sags_Bumps> LCMS_Sags_Bumps => Set<LCMS_Sags_Bumps>();
        public DbSet<LCMS_Rough_Processed> LCMS_Rough_Processed => Set<LCMS_Rough_Processed>(); 
        public DbSet<LCMS_Rumble_Strip> LCMS_Rumble_Strip => Set<LCMS_Rumble_Strip>();
        public DbSet<LCMS_Shove_Processed> LCMS_Shove_Processed => Set<LCMS_Shove_Processed>(); 
        public DbSet<LCMS_Geometry_Processed> LCMS_Geometry_Processed => Set<LCMS_Geometry_Processed>();
        public DbSet<LCMS_Water_Entrapment> LCMS_Water_Entrapment => Set<LCMS_Water_Entrapment>();
        public DbSet<LCMS_Segment_Grid> LCMS_Segment_Grid => Set<LCMS_Segment_Grid>();
        public DbSet<LCMS_Grooves> LCMS_Grooves => Set<LCMS_Grooves>();
        public DbSet<LCMS_PCI> LCMS_PCI => Set<LCMS_PCI>();
        public DbSet<LCMS_PASER> LCMS_PASER => Set<LCMS_PASER>();
        public DbSet<LCMS_CrackSummary> LCMS_CrackSummary => Set<LCMS_CrackSummary>();

        //EXPORT DATA TABLES

        public DbSet<OutputTemplate> OutputTemplate => Set<OutputTemplate>();
        public DbSet<OutputColumnTemplate> OutputColumnTemplate => Set<OutputColumnTemplate>();
        public DbSet<QCFilter> QCFilter => Set<QCFilter>();
        public DbSet<CrackClassifications> CrackClassifications => Set<CrackClassifications>();
        public DbSet<CrackClassificationNodes> CrackClassificationNodes => Set<CrackClassificationNodes>();
        
        //POSITIONING
        public DbSet<OdoData> OdoData => Set<OdoData>();
        public DbSet<GPS_Raw> GPS_Raw => Set<GPS_Raw>();

        //OTHER TABLES
        public DbSet<Boundary> Boundary => Set<Boundary>();
        public DbSet<Shapefile> Shapefile => Set<Shapefile>();
        public DbSet<MetaTableValue> MetaTableValue => Set<MetaTableValue>();
        public DbSet<VideoFrame> VideoFrame => Set<VideoFrame>();
        public DbSet<LASfile> LASfile => Set<LASfile>();
        public DbSet<LAS_Rutting> LAS_Rutting => Set<LAS_Rutting>();
        public DbSet<LASPoint> LASPoint => Set<LASPoint>();
        public DbSet<PCIRatings> PCIRatings => Set<PCIRatings>();
        public DbSet<PCIRatingStatus> PCIRatingStatus => Set<PCIRatingStatus>();
        public DbSet<PCIDefects> PCIDefects => Set<PCIDefects>();
        public DbSet<SampleUnit_Set> SampleUnit_Set => Set<SampleUnit_Set>();
        public DbSet<SampleUnit> SampleUnit => Set<SampleUnit>();
        public DbSet<Summary> Summary => Set<Summary>();
        public DbSet<SummaryDefect> SummaryDefect => Set<SummaryDefect>();
        public DbSet<Camera360Frame> Camera360Frame => Set<Camera360Frame>();
        public DbSet<Geometry_Processed> Geometry_Processed => Set<Geometry_Processed>();
        public DbSet<Keycode> Keycode => Set<Keycode>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePathProvider.GetDatasetDatabasePath()}");
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning));
        }

        public DatabasePathProvider GetDatabasePathProvider() => _databasePathProvider;

        public void ResetContextOptions(string newDatabasePath)
        {
            _databasePathProvider.SetDatabasePath(newDatabasePath);

            // Save changes to persist the changes to the database
            SaveChanges();

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var tablesToIndex = new List<Type>
            { 
                typeof(LCMS_Segment),
                typeof(LCMS_PickOuts_Raw),
                typeof(LCMS_Bleeding),
                typeof(LCMS_Cracking_Raw),
                typeof(LCMS_Rut_Processed),
                typeof(LCMS_Ravelling_Raw),
                typeof(LCMS_Patch_Processed),
                typeof(LCMS_Potholes_Processed),
                typeof(LCMS_Spalling_Raw),
                typeof(LCMS_Corner_Break),
                typeof(LCMS_Concrete_Joints),
                typeof(LCMS_Geometry_Processed),
                typeof(LCMS_Segment_Grid),
                typeof(LCMS_Water_Entrapment),
            };
            // Apply index creation for each table
            foreach (var tableType in tablesToIndex)
            {
                var method = typeof(AppDbContextProjectData).GetMethod(nameof(ApplyRoundedCoordinatesIndex))
                    ?.MakeGenericMethod(tableType);
                method?.Invoke(this, new object[] { modelBuilder, $"IX_{tableType.Name}_RoundedCoordinates" });
            }
            // Apply index creation for SurveyId and SegmentId
            foreach (var tableType in tablesToIndex)
            {
                var method = typeof(AppDbContextProjectData).GetMethod(nameof(ApplySurveyAndSegmentIndex))
                    ?.MakeGenericMethod(tableType);
                method?.Invoke(this, new object[] { modelBuilder, $"IX_{tableType.Name}_SurveyAndSegment" });
            }

            //Relationship between SampleUnitSet and SampleUnit
            modelBuilder.Entity<SampleUnit>()
                .HasOne(b => b.SampleUnitSet) // Navigation property
                .WithMany(s => s.SampleUnits)  // Inverse navigation
                .HasForeignKey(b => b.SampleUnitSetId) // Foreign key
                .OnDelete(DeleteBehavior.Cascade);

            //Relationship between SampleUnitSet and PCIRating
            modelBuilder.Entity<PCIRatings>()
            .HasOne(p => p.SampleUnitSet)  // PCIRating has one SampleUnitSet
            .WithMany()                    // No navigation property in SampleUnitSet
            .HasForeignKey(p => p.SampleUnitSetId) // Foreign Key
            .OnDelete(DeleteBehavior.Cascade); // Delete behavior

            //Relationship between PCIRating and PCIDefects
            modelBuilder.Entity<PCIDefects>()
                .HasOne(p => p.PCIRatings)
                .WithMany(p => p.PCIDefects)
                .HasForeignKey(p => p.PCIRatingId)
                .OnDelete(DeleteBehavior.Cascade);

            //Relationship between PCIRating and PCIRatingStatus
            modelBuilder.Entity<PCIRatingStatus>()
            .HasOne(p => p.PCIRatings)
            .WithMany(p => p.PCIRatingStatus)
            .HasForeignKey(p => p.PCIRatingId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SummaryDefect>()
                .HasOne(sd => sd.Summary)
                .WithMany(s => s.SummaryDefects)
                .HasForeignKey(sd => sd.SummaryId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public static void ApplyRoundedCoordinatesIndex<TEntity>(ModelBuilder modelBuilder, string indexName)
        where TEntity : class, IEntity
            {
                modelBuilder.Entity<TEntity>()
                    .HasIndex(x => new { x.RoundedGPSLatitude, x.RoundedGPSLongitude })
                    .HasDatabaseName(indexName);

            }
        public static void ApplySurveyAndSegmentIndex<TEntity>(ModelBuilder modelBuilder, string indexName)
        where TEntity : class, IEntity
        {
            modelBuilder.Entity<TEntity>()
                .HasIndex(x => new { x.SurveyId, x.SegmentId })
                .HasDatabaseName(indexName);
        }
    }
}

