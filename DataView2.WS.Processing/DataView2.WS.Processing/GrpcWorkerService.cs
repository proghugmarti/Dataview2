using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataView2.WS.Processing.Protos;
using DataView2.WS.Processing.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.ServiceModel.Channels;
using DataView2.Core.Helper;
using DataView2.Core.MultiInstances;

namespace DataView2.WS.Processing
{
    public class GrpcWorkerService : BackgroundService
    {
        private readonly ILogger<GrpcWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private Server _grpcServer;
        private readonly CancellationTokenSource _cts = new();
        public string logFilePath = string.Empty;
        private string _wsProcessingPort;
        private string _portBase;
        public GrpcWorkerService(ILogger<GrpcWorkerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string message = "";
            string host = _configuration["GrpcSettings:Host"] ?? "0.0.0.0";
            int port = int.TryParse( _configuration["GrpcSettings:Port"],out var parsedPort)? parsedPort: 5001;
            _portBase = port.ToString();
            string resolvedPort = GetPortByInstance();
            string envPort = Environment.GetEnvironmentVariable("GRPC_PORT");

            if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out int envParsedPort))
            {
                port = envParsedPort;
            }
            else if (!string.IsNullOrWhiteSpace(resolvedPort) && int.TryParse(resolvedPort, out int dynamicPort))
            {
                port = dynamicPort;
            }
            _wsProcessingPort = port.ToString();
            logFilePath = _configuration["DataViewGrpcSettings:DeviceLogFolder"] ?? "";
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var generalService = scope.ServiceProvider.GetRequiredService<GeneralService>();

                    message = $"Starting WSProcessing gRPC Server on {host}:{port}...";
                    //_logger.LogInformation(message);
                    Logger.WriteLog(logFilePath, message, Logger.TypeError.INFO);
                    Log.Information(message);

                    generalService.OnProcessingCancelled += HandleProcessingCancelled;

                    _grpcServer = new Server
                    {
                        Services = { GeneralWorkerService.BindService(generalService)
                    },
                        Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
                    };

                    _grpcServer.Start();
                    //_logger.LogInformation($"gRPC Server started on {host}:{port}");
                    Log.Information($"WSProcess gRPC Server started on {host}:{port}");

                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token);

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, linkedCts.Token);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                message = "Task was cancelled by the user.";
                Log.Information(message);
            }
            catch (Exception ex)
            {
                message = "Error starting Processing gRPC Server";
                Log.Error($"{message}: {ex.Message}");
            }
            finally
            {
                await StopAsync(stoppingToken);
            }
        }

        private void HandleProcessingCancelled()
        {
            //_logger.LogInformation("ProcessingService has been cancelled. Stopping gRPC Worker Service...");
            Log.Information("ProcessingService has been cancelled. Stopping gRPC Worker Service...");
            _cts.Cancel();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            string message = $"Stopping WSProcessing gRPC Server. Port resolved: {_wsProcessingPort}";
            //_logger.LogInformation(message);
            //Logger.WriteLog(logFilePath, message, Logger.TypeError.INFO);
            Log.Information(message);

            _cts.Cancel();
            await Task.Delay(1000);
            _grpcServer?.ShutdownAsync().Wait();

            message = $"WSProcessing gRPC Server stopped. Port resolved: {_wsProcessingPort}";
            //_logger.LogInformation(message);
            //Logger.WriteLog(logFilePath, message, Logger.TypeError.INFO);
            Log.Information(message);
        }

        private string GetPortByInstance()
        {
            try
            {
#if DEBUG
               if(SharedDVInstanceStore.DVInstances.IsOnlyInstance())
                return _portBase;
#endif 
                string wsProcessingPort =
                    SharedDVInstanceStore.GetProperty(propSearch: "WSProcStatus", valueSearch: "Pending", valueReturn: "WSProcPort"
                    );

                if (!string.IsNullOrWhiteSpace(wsProcessingPort))
                {
                    Log.Information($"[ProcessingService] WS Processing port resolved: {wsProcessingPort}");
                    return wsProcessingPort;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ProcessingService] Error resolving WS Processing port: {ex.Message}");
            }

            Console.WriteLine("There is no IP available for ProcessingService connection.");
            return string.Empty;
        }

    }
}