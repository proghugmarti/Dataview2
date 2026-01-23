using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using MIConvexHull;
using ProtoBuf.Grpc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class LASfile
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public List<LASPoint> LASpoints { get; set; } = new List<LASPoint>();

        [DataMember(Order = 4)]
        public string SurveyId { get; set; }

        [DataMember(Order = 5)]
        public double MinX { get; set; }

        [DataMember(Order = 6)]
        public double MaxX { get; set; }

        [DataMember(Order = 7)]
        public double MinY { get; set; }

        [DataMember(Order = 8)]
        public double MaxY { get; set; }

        [DataMember(Order = 9)]
        public double MaxZ { get; set; }

        [DataMember(Order = 10)]
        public double MinZ { get; set; }

        [DataMember(Order = 11)]
        public uint NumberOfPointRecords { get; set; }

        [DataMember(Order = 12)]
        public byte PointDataFormatId { get; set; } // now there is for type 7 or 2, if it is not of them it will take just x y z 

        [DataMember(Order = 13)]
        public ushort PointDataRecordLength { get; set; } //number of bytes that the reader should read

        [DataMember(Order = 14)]
        public string Coordinates { get; set; }

    }

    [DataContract]

    public class LASPoint
    {
        [DataMember(Order = 1)]

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public int LASfileId { get; set; }  // Foreign key to LASfile

        [DataMember(Order = 3)]
        public double X { get; set; }
        [DataMember(Order = 4)]
        public double Y { get; set; }
        [DataMember(Order = 5)]
        public double Z { get; set; }
        
        [DataMember(Order = 6)]
        [ForeignKey("LASfileId")]
        public LASfile LASfile { get; set; }

       
    }

    public class LASPointVertex : IVertex2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        // Parameterless constructor (required by MIConvexHull)
        public LASPointVertex()
        {
        }

        // Constructor to initialize X and Y values
        public LASPointVertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        // Explicit X and Y properties required by IVertex2D
        double IVertex2D.X => X;
        double IVertex2D.Y => Y;
    }
    public class DataPoint //for chart
    {
        public double X { get; set; }
        public double Y { get; set; }

        public DataPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [DataContract]
    public class LASfileRequest
    {
        [DataMember(Order = 1)]
        public string LASfileName { get; set; }

        [DataMember(Order = 2)]
        public string FilePath { get; set; }

        [DataMember(Order = 3)]
        public string SurveyId { get; set; }


    }

    [DataContract]
    public class CalculateRuttingsRequest
    {
        [DataMember(Order = 1)]
        public List<LASPoint> Points { get; set; } = new List<LASPoint>();
        [DataMember(Order = 2)]
        public double strghtEdgLength {  get; set; }
        [DataMember(Order = 3)]
        public double distanceBetweenPoints { get; set; }
        [DataMember (Order = 4)]
        public LASPoint RutPoint { get; set; }

    }

    [DataContract]
    public class CalculateMaxRuttingRequest
    {
        [DataMember(Order = 1)]
        public LASPoint Point1 { get; set; }

        [DataMember(Order = 2)]
        public LASPoint Point2 { get; set; }

        [DataMember(Order = 3)]
        public List<LASPoint> Points { get; set; } = new List<LASPoint>();
    }



    [DataContract]
    public class PointsRequest
    {
        [DataMember(Order = 1)]
        public double X1 { get; set; }
        [DataMember(Order = 2)]
        public double Y1 { get; set; }
        [DataMember(Order = 3)]
        public double X2 { get; set; }
        [DataMember(Order = 4)]
        public double Y2 { get; set; }
        [DataMember(Order = 5)]
        public List<int> LASFileIds { get; set; }
        [DataMember(Order = 6)]
        public int LasRuttingId { get; set; }

    }

    [DataContract]
    public class RuttingResult
    {
        [DataMember(Order = 1)]
        public double RutDepth { get; set; }

        [DataMember(Order = 2)]
        public  List<LASPoint> ContactPoints { get; set; }

        [DataMember(Order = 3)]
        public string Id { get; set; }

        [DataMember(Order = 4)]
        public int LasRuttingId { get; set; }


    }


    [DataContract]
    public class ChartLasPoint
    {
        public double X { get; set; } // Precomputed X value (index * spacing)
        public double Z { get; set; } // Z value of the point
        public int Id { get; set; }   // ID of the point
    }

    [ServiceContract]
    public interface ILASfileService
    {
       
        [OperationContract]
        Task<IdReply> ProcessLASfilesNewReader(List<LASfileRequest> request, CallContext context = default);

        [OperationContract]
        Task<List<LASfile>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LASfile> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<List<LASfile>> GetLasFilesByIdsAsync(List<int> ids);

        [OperationContract]
        Task<List<LASPoint>> GetAllPoints(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteByName(string name, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteFileObject(LASfile request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetFileRecordCount(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LASfile>> QueryAsyncFile(string predicate);

        [OperationContract]
        Task<CountReply> GetCountFileAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> DeletePointObject(LASPoint request, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LASPoint>> QueryAsyncPoint(string predicate);

        [OperationContract]
        Task<CountReply> GetCountPointAsync(string sqlQuery);

        [OperationContract]
        Task<CountReply> GetPointRecordCount(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LASfile> EditFileValue(LASfile request, CallContext context = default);
        [OperationContract]
        Task<LASPoint> EditPointValue(LASPoint request, CallContext context = default);
        [OperationContract]
        Task<List<LASPoint>> GetPointsAlongLineAsync(PointsRequest request);
        [OperationContract]
        Task<RuttingResult> CalculateRutting(CalculateRuttingsRequest request);

        [OperationContract]
        RuttingResult CalculateMaxRutting(CalculateMaxRuttingRequest rutRequest);

        [OperationContract]
        Task<RuttingResult> GetPointsAndCalculateRutFromLine(PointsRequest request);

        [OperationContract]
        Task<IdReply> SaveRuttingResultsToTableAsync(RuttingResult resultToSave);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<List<LASfile>> GetAllLASFilesBySurvey(string surveyId);
        [OperationContract]
        Task<LASfile> UpdateGenericData(string fieldsToUpdateSerialized);
        [OperationContract]
        Task<LASPoint> UpdateGenericDataLasPoint(string fieldsToUpdateSerialized);

    }
}