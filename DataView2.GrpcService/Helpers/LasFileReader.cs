using DataView2.Core.Models.Other;
using System.Runtime.InteropServices;

namespace DataView2.GrpcService.Helpers
{
    public class LasFileReader :IDisposable
    {
        private readonly string _filePath;
        private BinaryReader _reader;
        private bool _disposed;



        public LasFileReader(string filePath) {
            _filePath = filePath;
            _reader = new BinaryReader(File.Open(filePath,FileMode.Open));
        }

        public LasHeader ReadHeader()
        {
            return ReadStruct<LasHeader>(_reader);
        }
        public IEnumerable<LASPoint> ReadPoints(LasHeader header)
        {
            // Move to the point data offset
            _reader.BaseStream.Seek(header.OffsetToPointData, SeekOrigin.Begin);
            for (int i = 0; i < header.NumberOfPointRecords; i++)
            {
                LASPoint lasPoint;

                // Dynamically handle different point formats
                switch (header.PointDataFormatId)
                {
                    case 2:
                        if (header.PointDataRecordLength >= Marshal.SizeOf(typeof(LasPointType2)))
                        {
                            var point2 = ReadStruct<LasPointType2>(_reader);
                            lasPoint = new LASPoint
                            {
                                X = point2.X * header.XScaleFactor + header.XOffset,
                                Y = point2.Y * header.YScaleFactor + header.YOffset,
                                Z = point2.Z * header.ZScaleFactor + header.ZOffset
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Point Data Record Length {header.PointDataRecordLength} is too small for Point Format 2.");
                        }
                        break;

                    case 7:
                        if (header.PointDataRecordLength >= Marshal.SizeOf(typeof(LasPointType7)))
                        {
                            var point7 = ReadStruct<LasPointType7>(_reader);
                            lasPoint = new LASPoint
                            {
                                X = point7.X * header.XScaleFactor + header.XOffset,
                                Y = point7.Y * header.YScaleFactor + header.YOffset,
                                Z = point7.Z * header.ZScaleFactor + header.ZOffset
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Point Data Record Length {header.PointDataRecordLength} is too small for Point Format 7.");
                        }
                        break;

                    default:
                        // Fallback for unknown formats: read X, Y, Z only
                        var rawBytes = _reader.ReadBytes(header.PointDataRecordLength);
                        if (rawBytes.Length < 12) // Minimum bytes required for X, Y, Z (4 bytes each)
                        {
                            throw new InvalidOperationException(
                                $"Point Data Record Length {header.PointDataRecordLength} is too small to read X, Y, Z.");
                        }

                        int rawX = BitConverter.ToInt32(rawBytes, 0);
                        int rawY = BitConverter.ToInt32(rawBytes, 4);
                        int rawZ = BitConverter.ToInt32(rawBytes, 8);

                        lasPoint = new LASPoint
                        {
                            X = rawX * header.XScaleFactor + header.XOffset,
                            Y = rawY * header.YScaleFactor + header.YOffset,
                            Z = rawZ * header.ZScaleFactor + header.ZOffset
                        };

                        Console.WriteLine($"Unknown Point Format {header.PointDataFormatId}: Reading X, Y, Z only.");
                        break;
                }

                yield return lasPoint;
            }
        }



        /// <summary>
        /// Reads a binary struct from a BinaryReader.
        /// </summary>

        private T ReadStruct<T>(BinaryReader reader) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] data = reader.ReadBytes(size);
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            T theStruct = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return theStruct;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _reader?.Close();
                }
                _disposed = true;
            }
        }

        ~LasFileReader()
        {
            Dispose(false);
        }

    }


    // LAS Header structure for LAS 1.4
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LasHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] FileSignature;   // "LASF"
        public ushort FileSourceId;
        public ushort GlobalEncoding;
        public uint ProjectId1;
        public ushort ProjectId2;
        public ushort ProjectId3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ProjectId4;
        public byte VersionMajor;
        public byte VersionMinor;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] SystemIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] GeneratingSoftware;
        public ushort FileCreationDayOfYear;
        public ushort FileCreationYear;
        public ushort HeaderSize;
        public uint OffsetToPointData;
        public uint NumberOfVariableLengthRecords;
        public byte PointDataFormatId;
        public ushort PointDataRecordLength;
        public uint NumberOfPointRecords;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] NumberOfPointsByReturn;
        public double XScaleFactor;
        public double YScaleFactor;
        public double ZScaleFactor;
        public double XOffset;
        public double YOffset;
        public double ZOffset;
        public double MaxX;
        public double MinX;
        public double MaxY;
        public double MinY;
        public double MaxZ;
        public double MinZ;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LasPointType7
    {
        public int X;                  // 4 bytes
        public int Y;                  // 4 bytes
        public int Z;                  // 4 bytes
        public ushort Intensity;       // 2 bytes
        public ushort PointSourceId;   // 2 bytes
        public double GpsTime;         // 8 bytes
        public ushort Red;             // 2 bytes
        public ushort Green;           // 2 bytes
        public ushort Blue;            // 2 bytes
        public byte ReturnNumber;      // 1 byte
        public byte NumberOfReturns;   // 1 byte
        public byte ScanDirectionFlag; // 1 byte
        public byte EdgeOfFlightLine;  // 1 byte
        public byte Classification;    // 1 byte
        public sbyte ScanAngleRank;    // 1 byte
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LasPointType2
    {
        public int X;                  // 4 bytes
        public int Y;                  // 4 bytes
        public int Z;                  // 4 bytes
        public ushort Intensity;       // 2 bytes
        public byte ReturnNumber;      // 1 bits
        public byte NumberOfReturns;   // 1 bits
        public byte ScanDirectionFlag; // 1 bit
        public byte EdgeOfFlightLine;  // 1 bit
        public byte Classification;    // 1 bit
        public byte UserData;          // 1 bit
        public ushort Red;             // 2 bytes
        public ushort Green;           // 2 bytes
        public ushort Blue;            // 2 bytes
    }


}
