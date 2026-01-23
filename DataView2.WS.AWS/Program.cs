using WS_AWS_CSV;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "AWS YUMA Upload Service";
});

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton<AWS_Bucket>();
builder.Services.AddHostedService<WindowsBackgroundService>();

//builder.Logging.AddConfiguration(
//    builder.Configuration.GetSection("Logging"));
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

IHost host = builder.Build();
host.Run();

