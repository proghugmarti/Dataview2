using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Services.AppDbServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.GrpcService.Data.Projects
{
    public class AppDbContextMetadataLocal : DbContext
    {
        private readonly DatabasePathProvider _databasePathProvider;

        public AppDbContextMetadataLocal(DbContextOptions<AppDbContextMetadataLocal> options, DatabasePathProvider databasePathProvider) : base(options)
        {

            _databasePathProvider = databasePathProvider;
        }

        public DbSet<Project> ProjectRegistries => Set<Project>();
        public DbSet<DatabaseRegistryLocal> DatabaseRegistry => Set<DatabaseRegistryLocal>();
        public DbSet<GeneralSetting> GeneralSettings => Set<GeneralSetting>();
        public DbSet<CrackClassificationConfiguration> CrackClassification => Set<CrackClassificationConfiguration>();

        public DbSet<MapGraphicData> MapGraphic => Set<MapGraphicData>();
        public DbSet<TablesSetting> TableExport => Set<TablesSetting>();
        public DbSet<ColorCodeInformation> ColorCodeInformation => Set<ColorCodeInformation>();
        public DbSet<DatasetBackup> Backups => Set<DatasetBackup>();

        public DbSet<MetaTable> MetaTable => Set<MetaTable>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePathProvider.GetMetadataDatabasePath()}");
            optionsBuilder.EnableSensitiveDataLogging(false);

        }

        public DatabasePathProvider GetDatabasePathProvider() => _databasePathProvider;

        public void ResetContextOptions(string newDatabasePath)
        {
            _databasePathProvider.SetDatabasePath(newDatabasePath);
            SaveChanges();

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TablesSetting>().ToView("NoTable");
            modelBuilder.ApplyConfiguration(new GeneralSettingConfiguration());
            modelBuilder.ApplyConfiguration(new CrackClassificationConfigConfiguration());
            modelBuilder.Entity<Project>()
                       .Property(p => p.IdProject)
                       .HasDefaultValueSql("(lower(hex(randomblob(16))))"); //  GUID as text
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


}
