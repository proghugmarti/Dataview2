using Microsoft.AspNetCore.Hosting;
using DataView2.WS.Processing.Services;
using Serilog;
using DataView2.WS.Processing;




var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();



IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()  // Windows Service
    .ConfigureLogging((hostContext, logging) =>
    {
        logging.ClearProviders(); 
        logging.AddSerilog(); 
    })
    .UseSerilog((hostContext, services, loggerConfig) =>
    {
        loggerConfig.ReadFrom.Configuration(hostContext.Configuration);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var environment = hostContext.HostingEnvironment;

        services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = 50 * 1024 * 1024; // 50 MB
        });


        // gRPC service registration:
        services.AddScoped<GeneralService>();
        services.AddScoped<MultiProcessingLCMS>();    
      
        services.AddHostedService<GrpcWorkerService>();
    })
    .Build();
try
{
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}