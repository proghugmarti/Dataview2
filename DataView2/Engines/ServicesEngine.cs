using DataView2.Core.Helper;
using DataView2.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.Management;


namespace DataView2.Engines
{
    public class ServicesEngine
    {
        private readonly DataView2Options _options;
        private readonly ILogger<ServicesEngine> _logger;
        private List<int> ProcessListID;
        public bool ServiceRegistred = false;
        public ServicesEngine(DataView2Options options, ILogger<ServicesEngine> logger)
        {
            _options = options;
            _logger = logger;
        }
        public bool StartAll(string grpcServiceIP)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Log.Information($"Starting services");
            ProcessListID = new List<int>();
            bool serviceInit = true;

            var serviceOption = _options.ServiceOptions?.FirstOrDefault(s => s.Name == "DataView2");

            if (serviceOption == null)
            {
                _logger.LogWarning("Service 'DataView2' not found in configuration.");
                return false;
            }

            string newPath = Path.GetFullPath(
                Path.Combine(baseDirectory, serviceOption.ExePath));

            FileInfo fi = new(newPath);

            string processName = Path.GetFileNameWithoutExtension(fi.Name);

            if (string.IsNullOrWhiteSpace(processName) ||
                string.IsNullOrWhiteSpace(fi.DirectoryName))
            {
                return false;
            }

            try
            {
                Log.Information($"Starting service '{fi.FullName}'");

                using Process? process = Process.Start(new ProcessStartInfo()
                {
                    WorkingDirectory = fi.DirectoryName ?? string.Empty,
                    FileName = fi.FullName,
                    Arguments = grpcServiceIP,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Environment =   {
                                            ["GRPC_PORT"] = grpcServiceIP.ToString()
                                        }
                });
                if (Tools.IsProcessRunningByIPPath(processName, grpcServiceIP, out int existingPid))
                {
                    ProcessListID.Add(existingPid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service ProcessName:{ProcessName}.", processName);
            }


            ServiceRegistred = serviceInit;
            return serviceInit;
        }

        public void StopAll(IConfiguration configuration)
        {
            if (ProcessListID == null || ProcessListID.Count == 0)
                return;

            foreach (int processId in ProcessListID.ToList())
            {
                try
                {
                    using var process = Process.GetProcessById(processId);

                    if (process.HasExited)
                        continue;

                    Log.Information($"Stopping service ProcessName: '{process.ProcessName}'");
                    process.Kill(true);
                }
                catch (ArgumentException)
                {
                    // Process no longer exists
                    Log.Warning($"Process with ID {processId} was not found.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, $"Invalid operation while stopping process ID {processId}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error while stopping process ID {processId}.");
                }
            }
            if (Core.MultiInstances.SharedDVInstanceStore.DVInstances.IsOnlyInstance())
            {
                var options = configuration.GetSection("DataView2Options").Get<DataView2Options>();

                var wsProcessing = options?.ServiceOptions?.FirstOrDefault(s => s.Name == "WS.Processing");

                if (!string.IsNullOrWhiteSpace(wsProcessing?.ExePath))
                {
                    int[] processIds = Tools.GetRunningProcessIdsByPath(wsProcessing.ExePath);

                    foreach (int pid in processIds)
                    {
                        Tools.StopProcessByID(pid);
                    }
                }
            }
            ProcessListID.Clear();
        }

        public void ServiceCloseVerification()
        {
            string processName = "GrpcService";
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                try
                {
                    string mainModuleFileName = process.MainModule.FileName;

                    if (mainModuleFileName.ToLower().Contains(processName.ToLower()))
                    {
                        Log.Logger.Error($"ServiceCloseVerification - Process {process.ProcessName} closed successfully.");
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"ID: {process.Id}, Name: {process.ProcessName}");
                    Log.Logger.Error(ex, $"ServiceCloseVerification - Unexpected error: {ex.Message}");
                }
            }

        }

        //private static bool IsProcessRunningByIPPath(string processName, string grpcServiceIP, out int pid)
        //{
        //    pid = -1;

        //    foreach (var proc in Process.GetProcessesByName(processName))
        //    {
        //        try
        //        {
        //            string cmdLine = GetCommandLine(proc);
        //            if (cmdLine.Contains(grpcServiceIP, StringComparison.OrdinalIgnoreCase))
        //            {
        //                pid = proc.Id;
        //                return true;
        //            }
        //        }
        //        catch { }
        //    }

        //    return false;
        //}
        

    }
}
