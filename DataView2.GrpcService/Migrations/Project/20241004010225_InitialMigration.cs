using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Boundaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false),
                    Coordinates = table.Column<string>(type: "TEXT", nullable: false),
                    BoundariesMode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boundaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrackClassificationNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CrackId = table.Column<double>(type: "REAL", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Depth = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrackClassificationNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrackClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CrackId = table.Column<double>(type: "REAL", nullable: false),
                    Length = table.Column<double>(type: "REAL", nullable: false),
                    WeightedDepth = table.Column<double>(type: "REAL", nullable: false),
                    WeightedWidth = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrackClassifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GPS_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: true),
                    LRPNum = table.Column<long>(type: "INTEGER", nullable: true),
                    GPSTime = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    AltitudeHAE = table.Column<float>(type: "REAL", nullable: true),
                    AltitudeMSL = table.Column<float>(type: "REAL", nullable: true),
                    Heading = table.Column<double>(type: "REAL", nullable: true),
                    PDOP = table.Column<float>(type: "REAL", nullable: true),
                    HDOP = table.Column<float>(type: "REAL", nullable: true),
                    GPSSource = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GPS_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Bleeding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ChainageStart = table.Column<float>(type: "REAL", nullable: true),
                    ChainageEnd = table.Column<float>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<float>(type: "REAL", nullable: true),
                    LRPNumEnd = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageEnd = table.Column<float>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    BleedingId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftBleedingIndex = table.Column<double>(type: "REAL", nullable: true),
                    LeftSeverity = table.Column<string>(type: "TEXT", nullable: true),
                    RightBleedingIndex = table.Column<double>(type: "REAL", nullable: true),
                    RightSeverity = table.Column<string>(type: "TEXT", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSRightLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSRightLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Bleeding", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Concrete_Joints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    JointId = table.Column<string>(type: "TEXT", nullable: false),
                    JointDirection = table.Column<string>(type: "TEXT", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    FaultingAvgHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    FaultingMaxHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    FaultingMinHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    BadSealLength_mm = table.Column<double>(type: "REAL", nullable: false),
                    BadSealAvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    BadSealMaxDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    SpallingLength_mm = table.Column<double>(type: "REAL", nullable: false),
                    SpallingAvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    SpallingMaxDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    SpallingAvgWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    SpallingMaxWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    MedianPercentRng = table.Column<double>(type: "REAL", nullable: false),
                    MedianPercentInt = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    EndGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    EndGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    EndGPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgRngDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    StdRngDepth_mm = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Concrete_Joints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Corner_Break",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    CornerId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuarterId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    Area_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    BreakArea_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    CNR_SpallingArea_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    AreaRatio = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Corner_Break", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Cracking_Raw",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    CrackId = table.Column<int>(type: "INTEGER", nullable: true),
                    NodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    NodeLength_mm = table.Column<double>(type: "REAL", nullable: true),
                    NodeWidth_mm = table.Column<double>(type: "REAL", nullable: true),
                    NodeDepth_mm = table.Column<double>(type: "REAL", nullable: true),
                    Severity = table.Column<string>(type: "TEXT", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Cracking_Raw", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Curb_DropOff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    Height_mm = table.Column<double>(type: "REAL", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Curb_DropOff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_FOD",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FODID = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Area = table.Column<double>(type: "REAL", nullable: false),
                    Volume = table.Column<double>(type: "REAL", nullable: false),
                    MaximumHeight = table.Column<double>(type: "REAL", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    DetectionDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    RecoveryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    FODDescription = table.Column<string>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    Operator = table.Column<string>(type: "TEXT", nullable: false),
                    ImageFile = table.Column<string>(type: "TEXT", nullable: false),
                    AverageHeight = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonNoRecovery = table.Column<string>(type: "TEXT", nullable: false),
                    FODWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    FODLength_mm = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_FOD", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Geometry_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<int>(type: "INTEGER", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<double>(type: "REAL", nullable: true),
                    Roll = table.Column<double>(type: "REAL", nullable: true),
                    Pitch = table.Column<double>(type: "REAL", nullable: true),
                    Yaw = table.Column<double>(type: "REAL", nullable: true),
                    Vel_X = table.Column<double>(type: "REAL", nullable: true),
                    Vel_Y = table.Column<double>(type: "REAL", nullable: true),
                    Vel_Z = table.Column<double>(type: "REAL", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Acc_X = table.Column<double>(type: "REAL", nullable: true),
                    Acc_Y = table.Column<double>(type: "REAL", nullable: true),
                    Acc_Z = table.Column<double>(type: "REAL", nullable: true),
                    Gyr_X = table.Column<double>(type: "REAL", nullable: true),
                    Gyr_Y = table.Column<double>(type: "REAL", nullable: true),
                    Gyr_Z = table.Column<double>(type: "REAL", nullable: true),
                    Slope = table.Column<double>(type: "REAL", nullable: false),
                    StatesOfSlope = table.Column<string>(type: "TEXT", nullable: false),
                    CrossSlope = table.Column<double>(type: "REAL", nullable: false),
                    StatesOfCrossSlope = table.Column<string>(type: "TEXT", nullable: false),
                    RadiusOfCurvature = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Geometry_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Grooves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneArea_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgInterval_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Grooves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Lane_Mark_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    LeftType = table.Column<string>(type: "TEXT", nullable: false),
                    RightType = table.Column<string>(type: "TEXT", nullable: false),
                    LeftLength_mm = table.Column<double>(type: "REAL", nullable: false),
                    RightLength_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Lane_Mark_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Marking_Contour",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chaingage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    MarkingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    AvgIntensity = table.Column<double>(type: "REAL", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Marking_Contour", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_MMO_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    MMOId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    Width_mm = table.Column<double>(type: "REAL", nullable: false),
                    Height_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_MMO_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Patch_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<int>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    PatchId = table.Column<long>(type: "INTEGER", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    Width_mm = table.Column<double>(type: "REAL", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Patch_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_PickOuts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    PickOutId = table.Column<int>(type: "INTEGER", nullable: false),
                    Area_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    MaxDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_PickOuts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Potholes_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    PotholeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Area_mm2 = table.Column<double>(type: "REAL", nullable: true),
                    MaxDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    MajorDiameter_mm = table.Column<double>(type: "REAL", nullable: false),
                    MinorDiameter_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgIntensity = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Potholes_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Pumping_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    PumpingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    Width_mm = table.Column<double>(type: "REAL", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Pumping_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Ravelling_Raw",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    SquareId = table.Column<int>(type: "INTEGER", nullable: false),
                    SquareArea_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    Algorithm = table.Column<int>(type: "INTEGER", nullable: false),
                    ALG1_RavellingIndex = table.Column<double>(type: "REAL", nullable: false),
                    ALG1_RPI = table.Column<double>(type: "REAL", nullable: true),
                    ALG1_AVC = table.Column<double>(type: "REAL", nullable: true),
                    ALG2_RI_Percent = table.Column<double>(type: "REAL", nullable: true),
                    RI_AREA_mm2 = table.Column<double>(type: "REAL", nullable: true),
                    Severity = table.Column<string>(type: "TEXT", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Ravelling_Raw", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Rough_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RoughnessId = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    LwpIRI = table.Column<double>(type: "REAL", nullable: false),
                    RwpIRI = table.Column<double>(type: "REAL", nullable: false),
                    CwpIRI = table.Column<double>(type: "REAL", nullable: true),
                    LaneIRI = table.Column<double>(type: "REAL", nullable: false),
                    Naasra = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    LwpGeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RwpGeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    CwpGeoJSON = table.Column<string>(type: "TEXT", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Rough_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Rumble_Strip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    ChainageEnd = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumEnd = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageEnd = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RumbleStripId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    Area_mm2 = table.Column<double>(type: "REAL", nullable: false),
                    NumStrip = table.Column<int>(type: "INTEGER", nullable: false),
                    StripPerMeter = table.Column<double>(type: "REAL", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Rumble_Strip", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Rut_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RutId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftDepth_mm = table.Column<double>(type: "REAL", nullable: true),
                    LeftWidth_mm = table.Column<double>(type: "REAL", nullable: true),
                    LeftCrossSection = table.Column<double>(type: "REAL", nullable: true),
                    LeftType = table.Column<int>(type: "INTEGER", nullable: true),
                    LeftMethod = table.Column<int>(type: "INTEGER", nullable: true),
                    LeftPercentDeformation = table.Column<double>(type: "REAL", nullable: true),
                    LeftValid = table.Column<int>(type: "INTEGER", nullable: true),
                    LeftInvalidRatioData = table.Column<double>(type: "REAL", nullable: true),
                    RightDepth_mm = table.Column<double>(type: "REAL", nullable: true),
                    RightWidth_mm = table.Column<double>(type: "REAL", nullable: true),
                    RightCrossSection = table.Column<double>(type: "REAL", nullable: true),
                    RightType = table.Column<int>(type: "INTEGER", nullable: true),
                    RightMethod = table.Column<int>(type: "INTEGER", nullable: true),
                    RightPercentDeformation = table.Column<double>(type: "REAL", nullable: true),
                    RightValid = table.Column<int>(type: "INTEGER", nullable: true),
                    RightInvalidRatioData = table.Column<double>(type: "REAL", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Rut_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Sags_Bumps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    SagBumpId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxDeviation = table.Column<double>(type: "REAL", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Sags_Bumps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Sealed_Cracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    SealedCrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    SmoothnessInside = table.Column<double>(type: "REAL", nullable: false),
                    SmoothnessOutside = table.Column<double>(type: "REAL", nullable: false),
                    AvgIntensity = table.Column<double>(type: "REAL", nullable: false),
                    AvgIntensityOutside = table.Column<double>(type: "REAL", nullable: false),
                    CrackAreaRatio = table.Column<double>(type: "REAL", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    AvgWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Sealed_Cracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Segment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SectionId = table.Column<string>(type: "TEXT", nullable: false),
                    ImageFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Segment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Segment_Grid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SegmentGridID = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CrackType = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Column = table.Column<int>(type: "INTEGER", nullable: false),
                    Row = table.Column<int>(type: "INTEGER", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Segment_Grid", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Shove_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    ShoveId = table.Column<int>(type: "INTEGER", nullable: false),
                    LaneSide = table.Column<string>(type: "TEXT", nullable: false),
                    ShoveHeight_mm = table.Column<double>(type: "REAL", nullable: false),
                    ShoveWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    RutDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    RutWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Shove_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Spalling_Raws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    JointId = table.Column<int>(type: "INTEGER", nullable: false),
                    JointDirection = table.Column<string>(type: "TEXT", nullable: false),
                    SpallingId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    AvgWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    Length_mm = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    QCAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Spalling_Raws", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Texture_Processed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainage = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    TextureId = table.Column<int>(type: "INTEGER", nullable: false),
                    MTDBand1 = table.Column<double>(type: "REAL", nullable: true),
                    MTDBand2 = table.Column<double>(type: "REAL", nullable: true),
                    MTDBand3 = table.Column<double>(type: "REAL", nullable: true),
                    MTDBand4 = table.Column<double>(type: "REAL", nullable: true),
                    MTDBand5 = table.Column<double>(type: "REAL", nullable: true),
                    SMTDBand1 = table.Column<double>(type: "REAL", nullable: true),
                    SMTDBand2 = table.Column<double>(type: "REAL", nullable: true),
                    SMTDBand3 = table.Column<double>(type: "REAL", nullable: true),
                    SMTDBand4 = table.Column<double>(type: "REAL", nullable: true),
                    SMTDBand5 = table.Column<double>(type: "REAL", nullable: true),
                    MPDBand1 = table.Column<double>(type: "REAL", nullable: true),
                    MPDBand2 = table.Column<double>(type: "REAL", nullable: true),
                    MPDBand3 = table.Column<double>(type: "REAL", nullable: true),
                    MPDBand4 = table.Column<double>(type: "REAL", nullable: true),
                    MPDBand5 = table.Column<double>(type: "REAL", nullable: true),
                    RMSBand1 = table.Column<double>(type: "REAL", nullable: true),
                    RMSBand2 = table.Column<double>(type: "REAL", nullable: true),
                    RMSBand3 = table.Column<double>(type: "REAL", nullable: true),
                    RMSBand4 = table.Column<double>(type: "REAL", nullable: true),
                    RMSBand5 = table.Column<double>(type: "REAL", nullable: true),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Texture_Processed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_Water_Entrapment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<int>(type: "INTEGER", nullable: false),
                    dLeftAverageWaterDepth = table.Column<double>(type: "REAL", nullable: false),
                    dLeftTotalWaterWidth = table.Column<double>(type: "REAL", nullable: false),
                    dRightAverageWaterDepth = table.Column<double>(type: "REAL", nullable: false),
                    dRightTotalWaterWidth = table.Column<double>(type: "REAL", nullable: false),
                    dWaterTrapDepth = table.Column<double>(type: "REAL", nullable: false),
                    dWaterTrapWidth = table.Column<double>(type: "REAL", nullable: false),
                    dCrossSection = table.Column<double>(type: "REAL", nullable: false),
                    dStraightEdgeCoordsPoint1X = table.Column<double>(type: "REAL", nullable: false),
                    dStraightEdgeCoordsPoint1Z = table.Column<double>(type: "REAL", nullable: false),
                    dStraightEdgeCoordsPoint2X = table.Column<double>(type: "REAL", nullable: false),
                    dStraightEdgeCoordsPoint2Z = table.Column<double>(type: "REAL", nullable: false),
                    dMidLineCoordsPoint1X = table.Column<double>(type: "REAL", nullable: false),
                    dMidLineCoordsPoint1Z = table.Column<double>(type: "REAL", nullable: false),
                    dMidLineCoordsPoint2X = table.Column<double>(type: "REAL", nullable: false),
                    dMidLineCoordsPoint2Z = table.Column<double>(type: "REAL", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_Water_Entrapment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetaTableValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableId = table.Column<int>(type: "INTEGER", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StrValue1 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue2 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue3 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue4 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue5 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue6 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue7 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue8 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue9 = table.Column<string>(type: "TEXT", nullable: true),
                    StrValue10 = table.Column<string>(type: "TEXT", nullable: true),
                    DecValue1 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue2 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue3 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue4 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue5 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue6 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue7 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue8 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue9 = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecValue10 = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaTableValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutputTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QCFilter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilterName = table.Column<string>(type: "TEXT", nullable: false),
                    FilterJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QCFilter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shapefile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShapefileName = table.Column<string>(type: "TEXT", nullable: false),
                    Coordinates = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false),
                    ShapeType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shapefile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShapefileGraphicData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Red = table.Column<int>(type: "INTEGER", nullable: false),
                    Green = table.Column<int>(type: "INTEGER", nullable: false),
                    Blue = table.Column<int>(type: "INTEGER", nullable: false),
                    Alpha = table.Column<int>(type: "INTEGER", nullable: false),
                    Thickness = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShapefileGraphicData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SummaryCrackClasifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chstart = table.Column<double>(type: "REAL", nullable: false),
                    Chend = table.Column<double>(type: "REAL", nullable: false),
                    LaneWidth = table.Column<double>(type: "REAL", nullable: false),
                    SampleArea = table.Column<double>(type: "REAL", nullable: false),
                    LongCrackLOW = table.Column<double>(type: "REAL", nullable: false),
                    LongCrackMED = table.Column<double>(type: "REAL", nullable: false),
                    LongCrackHIGH = table.Column<double>(type: "REAL", nullable: false),
                    TransCrackLOW = table.Column<double>(type: "REAL", nullable: false),
                    TransCrackMED = table.Column<double>(type: "REAL", nullable: false),
                    TransCrackHIGH = table.Column<double>(type: "REAL", nullable: false),
                    AlligatorCrackLOW = table.Column<double>(type: "REAL", nullable: false),
                    AlligatorCrackMED = table.Column<double>(type: "REAL", nullable: false),
                    AlligatorCrackHIGH = table.Column<double>(type: "REAL", nullable: false),
                    OtherCrackLOW = table.Column<double>(type: "REAL", nullable: false),
                    OtherCrackMED = table.Column<double>(type: "REAL", nullable: false),
                    OtherCrackHIGH = table.Column<double>(type: "REAL", nullable: false),
                    LongitudinalCracking = table.Column<double>(type: "REAL", nullable: false),
                    TransverseCracking = table.Column<double>(type: "REAL", nullable: false),
                    crackingWheelpaths = table.Column<double>(type: "REAL", nullable: false),
                    WheelpathsArea = table.Column<double>(type: "REAL", nullable: false),
                    FatigueArea = table.Column<double>(type: "REAL", nullable: false),
                    XmlFileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummaryCrackClasifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Survey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyIdExternal = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ImageFolderPath = table.Column<string>(type: "TEXT", nullable: true),
                    VideoFolderPath = table.Column<string>(type: "TEXT", nullable: true),
                    DataviewVersion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TableExport",
                columns: table => new
                {
                    RootPage = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    File = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableExport", x => x.RootPage);
                });

            migrationBuilder.CreateTable(
                name: "VideoFrame",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: true),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    ImageFileName = table.Column<string>(type: "TEXT", nullable: false),
                    VideoFrameId = table.Column<int>(type: "INTEGER", nullable: false),
                    CameraName = table.Column<string>(type: "TEXT", nullable: true),
                    CameraSerial = table.Column<string>(type: "TEXT", nullable: true),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFrame", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XMLObject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Parent = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XMLObject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutputColumnTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutputTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Table = table.Column<string>(type: "TEXT", nullable: false),
                    Column = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Grouped = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Operation = table.Column<string>(type: "TEXT", nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputColumnTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutputColumnTemplate_OutputTemplate_OutputTemplateId",
                        column: x => x.OutputTemplateId,
                        principalTable: "OutputTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Bleeding_RoundedCoordinates",
                table: "LCMS_Bleeding",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Bleeding_SurveyAndSegment",
                table: "LCMS_Bleeding",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Concrete_Joints_RoundedCoordinates",
                table: "LCMS_Concrete_Joints",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Concrete_Joints_SurveyAndSegment",
                table: "LCMS_Concrete_Joints",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Corner_Break_RoundedCoordinates",
                table: "LCMS_Corner_Break",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Corner_Break_SurveyAndSegment",
                table: "LCMS_Corner_Break",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Cracking_Raw_RoundedCoordinates",
                table: "LCMS_Cracking_Raw",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Cracking_Raw_SurveyAndSegment",
                table: "LCMS_Cracking_Raw",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Geometry_Processed_RoundedCoordinates",
                table: "LCMS_Geometry_Processed",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Geometry_Processed_SurveyAndSegment",
                table: "LCMS_Geometry_Processed",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Patch_Processed_RoundedCoordinates",
                table: "LCMS_Patch_Processed",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Patch_Processed_SurveyAndSegment",
                table: "LCMS_Patch_Processed",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_PickOuts_Raw_RoundedCoordinates",
                table: "LCMS_PickOuts",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_PickOuts_Raw_SurveyAndSegment",
                table: "LCMS_PickOuts",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Potholes_Processed_RoundedCoordinates",
                table: "LCMS_Potholes_Processed",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Potholes_Processed_SurveyAndSegment",
                table: "LCMS_Potholes_Processed",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Ravelling_Raw_RoundedCoordinates",
                table: "LCMS_Ravelling_Raw",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Ravelling_Raw_SurveyAndSegment",
                table: "LCMS_Ravelling_Raw",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Rut_Processed_RoundedCoordinates",
                table: "LCMS_Rut_Processed",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Rut_Processed_SurveyAndSegment",
                table: "LCMS_Rut_Processed",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Segment_RoundedCoordinates",
                table: "LCMS_Segment",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Segment_SurveyAndSegment",
                table: "LCMS_Segment",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Segment_Grid_RoundedCoordinates",
                table: "LCMS_Segment_Grid",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Segment_Grid_SurveyAndSegment",
                table: "LCMS_Segment_Grid",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Spalling_Raw_RoundedCoordinates",
                table: "LCMS_Spalling_Raws",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Spalling_Raw_SurveyAndSegment",
                table: "LCMS_Spalling_Raws",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Water_Entrapment_RoundedCoordinates",
                table: "LCMS_Water_Entrapment",
                columns: new[] { "RoundedGPSLatitude", "RoundedGPSLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_LCMS_Water_Entrapment_SurveyAndSegment",
                table: "LCMS_Water_Entrapment",
                columns: new[] { "SurveyId", "SegmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputColumnTemplate_OutputTemplateId",
                table: "OutputColumnTemplate",
                column: "OutputTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boundaries");

            migrationBuilder.DropTable(
                name: "CrackClassificationNodes");

            migrationBuilder.DropTable(
                name: "CrackClassifications");

            migrationBuilder.DropTable(
                name: "GPS_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Bleeding");

            migrationBuilder.DropTable(
                name: "LCMS_Concrete_Joints");

            migrationBuilder.DropTable(
                name: "LCMS_Corner_Break");

            migrationBuilder.DropTable(
                name: "LCMS_Cracking_Raw");

            migrationBuilder.DropTable(
                name: "LCMS_Curb_DropOff");

            migrationBuilder.DropTable(
                name: "LCMS_FOD");

            migrationBuilder.DropTable(
                name: "LCMS_Geometry_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Grooves");

            migrationBuilder.DropTable(
                name: "LCMS_Lane_Mark_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Marking_Contour");

            migrationBuilder.DropTable(
                name: "LCMS_MMO_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Patch_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_PickOuts");

            migrationBuilder.DropTable(
                name: "LCMS_Potholes_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Pumping_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Ravelling_Raw");

            migrationBuilder.DropTable(
                name: "LCMS_Rough_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Rumble_Strip");

            migrationBuilder.DropTable(
                name: "LCMS_Rut_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Sags_Bumps");

            migrationBuilder.DropTable(
                name: "LCMS_Sealed_Cracks");

            migrationBuilder.DropTable(
                name: "LCMS_Segment");

            migrationBuilder.DropTable(
                name: "LCMS_Segment_Grid");

            migrationBuilder.DropTable(
                name: "LCMS_Shove_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Spalling_Raws");

            migrationBuilder.DropTable(
                name: "LCMS_Texture_Processed");

            migrationBuilder.DropTable(
                name: "LCMS_Water_Entrapment");

            migrationBuilder.DropTable(
                name: "MetaTableValue");

            migrationBuilder.DropTable(
                name: "OutputColumnTemplate");

            migrationBuilder.DropTable(
                name: "QCFilter");

            migrationBuilder.DropTable(
                name: "Shapefile");

            migrationBuilder.DropTable(
                name: "ShapefileGraphicData");

            migrationBuilder.DropTable(
                name: "SummaryCrackClasifications");

            migrationBuilder.DropTable(
                name: "Survey");

            migrationBuilder.DropTable(
                name: "TableExport");

            migrationBuilder.DropTable(
                name: "VideoFrame");

            migrationBuilder.DropTable(
                name: "XMLObject");

            migrationBuilder.DropTable(
                name: "OutputTemplate");
        }
    }
}
