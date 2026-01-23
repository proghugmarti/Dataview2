using DataView2.Core.Models.Database_Tables;
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Management;

namespace DataView2.Core.Helper
{
    public static class Tools
    {
        public static string GetMetadataDbPath()
        {
            var currentDir = AppContext.BaseDirectory;

            while (currentDir != null)
            {
                var potentialPath = Path.Combine(currentDir, "DataView2.GrpcService", "DataView_MetadataDB.db");
                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }

                currentDir = Directory.GetParent(currentDir)?.FullName;
            }

            return null;
        }


        //Projects handle:
        public static string GetPathProjectDesSerial(string jsonSerialized)
        {
            if (string.IsNullOrEmpty(jsonSerialized))
            {
                return null;
            }

            var deserializedObject = JsonSerializer.Deserialize<GeoJsonProject>(jsonSerialized);
            return deserializedObject?.Path;
        }

        public static string GetPathsProjectSerial(string path, string pathDB)
        {
            // Extract the original project name (without the "(1)" or any suffix)
            string originalProjectName = Path.GetFileNameWithoutExtension(pathDB); // e.g., "9(1)" -> "9"
            string originalDbName = originalProjectName + ".db"; // Ensure the DB name ends with .db

            // Return the original path with the original DB name
            var jsonObject = new
            {
                Path = path,  // Project directory path
                DBPath = Path.Combine(path, originalDbName) // Use the original project name for DBPath
            };

            return JsonSerializer.Serialize(jsonObject);
        }

        public static string GetPathProjectSerial(string path)
        {
            var jsonObject = new { Path = path };
            return JsonSerializer.Serialize(jsonObject);
        }

        public static string GetDbPathProjectDesSerial(string jsonSerialized)
        {
            if (string.IsNullOrWhiteSpace(jsonSerialized))
            {
                Console.WriteLine("Warning: jsonSerialized is null or empty.");
                return null;
            }

            try
            {
                var deserializedObject = JsonSerializer.Deserialize<GeoJsonProject>(jsonSerialized);
                return deserializedObject?.DBPath;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                return null;
            }
        }

        public enum dbContextType
        {
            Metadata,
            Dataset
        }

        //Coordinates formats:
        public enum cstFormats
        {
            [Description("WGS84")]
            WGS84,

            [Description("ITRF96-3")]
            ITRF96_3
        }
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static int GetFreePort(int startPort)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                if (IsPortFree(port))
                    return port;
            }

            throw new Exception("No free ports available");
        }

        public static bool IsPortFree(int port)
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            // --- Check active TCP listeners ---
            var tcpListeners = ipProperties.GetActiveTcpListeners();
            if (tcpListeners.Any(p => p.Port == port))
                return false;

            // --- Check active UDP listeners ---
            var udpListeners = ipProperties.GetActiveUdpListeners();
            if (udpListeners.Any(p => p.Port == port))
                return false;

            return true;
        }

        public static bool IsProcessPortRunning(string processName, string port)
        {
            if (string.IsNullOrWhiteSpace(processName) || string.IsNullOrWhiteSpace(port))
                return false;

            if (!int.TryParse(port, out _))
                return false;

            processName = Path.GetFileNameWithoutExtension(processName);

            // Get process IDs by name
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return false;

            var pids = processes.Select(p => p.Id.ToString()).ToHashSet();

            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            foreach (var line in output.Split('\n'))
            {
                if (!line.Contains($":{port}"))
                    continue;

                var parts = Regex.Split(line.Trim(), @"\s+");
                if (parts.Length < 5)
                    continue;

                string pid = parts[^1];

                if (pids.Contains(pid))
                    return true;
            }

            return false;
        }

        public static bool StopProcessPortRunning1(string processPath, string port)
        {
            LogClosedPort(port);
            // -----------------------------
            // Validate input port
            // -----------------------------
            if (!int.TryParse(port, out int targetPort))
                return false;

            // -----------------------------
            // Execute netstat -ano
            // -----------------------------
            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var netstat = Process.Start(psi);
            string output = netstat!.StandardOutput.ReadToEnd();
            netstat.WaitForExit();

            var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            int? killedPid = null; // 🔒 ensure only ONE process is killed

            // -----------------------------
            // Parse netstat output
            // -----------------------------
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // Only TCP entries
                if (!trimmed.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    continue;

                // parts[1] = Local Address (e.g. 0.0.0.0:5001)
                var localAddress = parts[1];
                var state = parts[3];

                // Only LISTENING ports
                if (!state.Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;

                var colonIndex = localAddress.LastIndexOf(':');
                if (colonIndex < 0)
                    continue;

                // Extract port from Local Address
                if (!int.TryParse(localAddress[(colonIndex + 1)..], out int parsedPort))
                    continue;

                // Must match requested port
                if (parsedPort != targetPort)
                    continue;

                // parts[4] = PID
                if (!int.TryParse(parts[4], out int pid))
                    continue;

                // 🔒 If we already killed one, stop completely
                if (killedPid.HasValue)
                    return true;

                try
                {
                    var process = Process.GetProcessById(pid);

                    // Validate executable path
                    var exePath = process.MainModule?.FileName;
                    if (exePath == null ||
                        !string.Equals(exePath, processPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // -----------------------------
                    // Extra safety: ensure PID only owns THIS port
                    // -----------------------------
                    bool pidHasOtherListeningPorts = lines.Any(l =>
                    {
                        var t = l.TrimStart();
                        if (!t.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                            return false;

                        if (!t.Contains($" {pid}"))
                            return false;

                        if (!t.Contains("LISTENING"))
                            return false;

                        var p = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var addr = p[1];
                        var idx = addr.LastIndexOf(':');
                        if (idx < 0)
                            return false;

                        return int.TryParse(addr[(idx + 1)..], out int otherPort)
                               && otherPort != targetPort;
                    });

                    if (pidHasOtherListeningPorts)
                        continue;

                    // -----------------------------
                    // Kill process (ONLY ONE)
                    // -----------------------------
                    process.Kill();
                    process.WaitForExit();

                    killedPid = pid;
                    return true;
                }
                catch
                {
                    // Access denied or process already exited
                    return false;
                }
            }

            // -----------------------------
            // No matching process found
            // -----------------------------
            return false;
        }

        private static void LogClosedPort(string port)
        {
            string filePath = @"C:\temp\ClosePort.txt";

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} > Port is closed: {port}";

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            File.AppendAllText(filePath, line + Environment.NewLine);
        }

        public static void StopProcessByID(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch (ArgumentException)
            {
                // El proceso ya no existe
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping process {processId}: {ex.Message}");
            }
        }

        public static int NumProcessRunning(string processPath, bool byPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
                return 0;

            string processName;

            if (byPath)
            {
                processName = Path.GetFileNameWithoutExtension(processPath);
            }
            else
            {
                processName = processPath;
            }

            if (string.IsNullOrWhiteSpace(processName))
                return 0;

            return Process.GetProcessesByName(processName).Length;
        }

        public static int[] GetRunningProcessIdsByPath(string exeFullPath)
        {
            if (string.IsNullOrWhiteSpace(exeFullPath))
                return Array.Empty<int>();

            string targetPath = Path.GetFullPath(exeFullPath);
            List<int> processIds = new();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string? processPath = process.MainModule?.FileName;

                    if (string.IsNullOrWhiteSpace(processPath))
                        continue;

                    if (string.Equals(
                        Path.GetFullPath(processPath),
                        targetPath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        processIds.Add(process.Id);
                    }
                }
                catch
                {
                    // Access denied / system process → ignorar
                }
            }

            return processIds.ToArray();
        }

        public static bool IsProcessRunningByIPPath(string processName, string grpcServiceIP, out int pid)
        {
            pid = -1;

            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    string cmdLine = GetCommandLine(proc);
                    if (cmdLine.Contains(grpcServiceIP, StringComparison.OrdinalIgnoreCase))
                    {
                        pid = proc.Id;
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }
        private static string GetCommandLine(Process process)
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["CommandLine"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }



        // Close Process Port V.2.
        // -------------------------------------------------
        // P/Invoke a GetExtendedTcpTable (IPv4, con PID)
        // -------------------------------------------------

        private const int AF_INET = 2; // IPv4

        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            uint reserved = 0);

        private const uint NO_ERROR = 0;

        // -------------------------------------------------
        // Método público que pides
        // -------------------------------------------------
        public static bool StopProcessPortRunning(string processPath, string port)
        {
            LogClosedPort(port);
            if (!int.TryParse(port, out int targetPort) || targetPort <= 0 || targetPort > 65535)
                return false;

            // 1) Obtener todas las filas TCP (IPv4) con PID
            List<MIB_TCPROW_OWNER_PID> rows;
            try
            {
                rows = GetAllTcpRows();
            }
            catch
            {
                return false;
            }

            // 2) Buscar PIDs que tengan este puerto en estado LISTEN
            var candidatePids = new HashSet<int>();

            foreach (var row in rows)
            {
                int rowPort = ConvertPort(row.localPort);
                if (rowPort != targetPort)
                    continue;

                // 2 = MIB_TCP_STATE_LISTEN
                if (row.state != 2)
                    continue;

                candidatePids.Add((int)row.owningPid);
            }

            if (candidatePids.Count == 0)
                return false;

            // 3) Filtrar por ruta exacta del ejecutable
            var finalCandidates = new List<int>();

            foreach (var pid in candidatePids)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    var exePath = p.MainModule?.FileName;

                    if (!string.IsNullOrEmpty(exePath) &&
                        string.Equals(exePath, processPath, StringComparison.OrdinalIgnoreCase))
                    {
                        finalCandidates.Add(pid);
                    }
                }
                catch
                {
                    // proceso terminó o no hay permisos; lo ignoramos
                }
            }

            // Si no hay exactamente 1 candidato, mejor no tocar nada
            if (finalCandidates.Count != 1)
                return false;

            int pidToKill = finalCandidates[0];

            // 4) Matar SOLO ese proceso
            try
            {
                using var proc = Process.GetProcessById(pidToKill);
                proc.Kill();         // IMPORTANTE: sin true
                proc.WaitForExit();

                return true;
            }
            catch
            {
                return false;
            }
        }

        // -------------------------------------------------
        // Helpers internos
        // -------------------------------------------------

        private static List<MIB_TCPROW_OWNER_PID> GetAllTcpRows()
        {
            int bufferSize = 0;

            // Primera llamada para saber tamaño de buffer
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET,
                                              TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            if (result != 0 && result != 122 /* ERROR_INSUFFICIENT_BUFFER */)
                throw new InvalidOperationException("GetExtendedTcpTable falló (1), código: " + result);

            IntPtr buffer = IntPtr.Zero;

            try
            {
                buffer = Marshal.AllocHGlobal(bufferSize);
                result = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET,
                                             TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

                if (result != NO_ERROR)
                    throw new InvalidOperationException("GetExtendedTcpTable falló (2), código: " + result);

                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = IntPtr.Add(buffer, 4); // saltar dwNumEntries

                var rows = new List<MIB_TCPROW_OWNER_PID>(numEntries);

                int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

                for (int i = 0; i < numEntries; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                    rows.Add(row);
                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }

                return rows;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        // dwLocalPort viene en network byte order en los 2 bytes bajos.
        private static int ConvertPort(uint dwLocalPort)
        {
            // Tomamos los 2 bytes bajos y los pasamos de big-endian a host-endian
            byte[] bytes = BitConverter.GetBytes(dwLocalPort);
            // bytes[0] y bytes[1] son los de menor peso (little-endian), que contienen el puerto en big-endian
            ushort portBE = BitConverter.ToUInt16(bytes, 0);
            ushort port = (ushort)IPAddress.NetworkToHostOrder((short)portBE);
            return port;
        }       


    }
}
