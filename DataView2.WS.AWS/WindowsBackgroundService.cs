using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WS_AWS_CSV
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private string? _LogSrvFile;
        private AWS_Bucket _aWS_Bucket;
        private int _repeatInMinutes = 0;
        public WindowsBackgroundService(
         AWS_Bucket aWS_Bucket, ILogger<WindowsBackgroundService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _aWS_Bucket = aWS_Bucket;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                bool result = false;
                _aWS_Bucket = new AWS_Bucket(_logger, _configuration);
                _repeatInMinutes = Int32.Parse(_configuration["RepeatInMinutes"]);
                _LogSrvFile = _configuration["LogSrv"];
                if (_repeatInMinutes > 0)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        result = await _aWS_Bucket.UploadFolderToS3();
                        if (result)
                        {
                            _aWS_Bucket.MoveFilesUploaded();
                            _aWS_Bucket.ClearSubDirectory();
                        }
                        PrintProcessResult(result);
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else
                {
                    result = await _aWS_Bucket.UploadFolderToS3();
                    if (result)
                    {
                        _aWS_Bucket.MoveFilesUploaded();
                        _aWS_Bucket.ClearSubDirectory();
                    }
                    PrintProcessResult(result);
                }
                string fileContent = File.ReadAllText(_LogSrvFile);
                //Uncomment to see the summary in a Event Viewer:
                _logger.LogWarning(fileContent);
                Environment.Exit(1);
            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
                _logger.LogWarning($"Process canceled by the System.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"No files were uploaded. Error uploading files to Amazon S3: {ex.Message}");
                Environment.Exit(1);
            }
        }
        private void PrintProcessResult(bool result)
        {
            if (result)
                _logger.LogWarning($"Process ended successfully.");
            else
                _logger.LogError($"Process ended Unsuccessfully.");
        }
    }
}
