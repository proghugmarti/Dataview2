using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Helper;
using DataView2.GrpcService.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class ProcessingServiceManager
    {
        private static ProcessingServiceManager _instance;
        private static readonly object _lock = new object();
        private static ProcessingStateService _staticStateService;

        private GrpcChannel _channel;
        private GeneralWorkerService.GeneralWorkerServiceClient _client;
        private readonly string _baseUrl;
        private static bool _isInitialized = false;

        private ProcessingServiceManager(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public static void SetStateService(ProcessingStateService stateService)
        {
            _staticStateService = stateService;
        }

        public static bool TryInitialize(string baseUrl, out ProcessingServiceManager instance)
        {
            lock (_lock)
            {
                if (_isInitialized && _instance != null)
                {
                    if (_instance._baseUrl == baseUrl)
                    {
                        instance = _instance;
                        return true;
                    }

                    // Port → RESET
                    _instance.Dispose();
                    _instance = null;
                    _isInitialized = false;
                }

                var tempInstance = new ProcessingServiceManager(baseUrl);

                if (tempInstance.Initialize())
                {
                    _instance = tempInstance;
                    _isInitialized = true;
                    instance = _instance;
                    return true;
                }

                instance = null;
                return false;
            }
        }
        private bool Initialize()
        {
            const int maxRetries = 3;
            const int delayMilliseconds = 5000;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    _channel = GrpcChannel.ForAddress(_baseUrl);
                    _client = new GeneralWorkerService.GeneralWorkerServiceClient(_channel);

                    var testResponse = _client.TestConnection(new EmptyWS());

                    if (testResponse.IsConnected)
                    {
                        return true;
                    }
                }
                catch
                {
                    Task.Delay(delayMilliseconds).Wait();
                }
            }

            return false;
        }     
        public async Task ProcessSurveyAsync(SurveyProcessingRequest request, int batchSize, SafeSurveyWriter<SurveyProcessingResponse> safeWriter, CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("ProcessingServiceManager must be initialized before use.");
            }

            SurveyWSProcessingRequest requestSrvc = CastRequest(request);

            string appDirectory = AppContext.BaseDirectory;
            string licensePath = Path.Combine(appDirectory, "License.txt");
            requestSrvc.LicensePath = licensePath;
            requestSrvc.BatchSize = batchSize;

            int completedWorkItems = 0;
            int totalWorkItems = request.SelectedFiles.Count();
            int percentage = 0;
            using var call = _client.ProcessSurvey(requestSrvc, cancellationToken: cancellationToken);
            await foreach (var responseSrvc in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                
                if (_staticStateService != null && responseSrvc.Message != null)
                {
                    //one processing message same as one fis file processing
                    if (responseSrvc.Message.StartsWith("Processing fis"))
                    {
                        completedWorkItems++;
                        percentage = (int)((double)completedWorkItems / totalWorkItems * 100);
                    }
                    _staticStateService.UpdateState(state =>
                    {
                        state.Stage = Core.Models.Other.ProcessingStage.ProcessingFIS;
                        state.StagePercentage = percentage;
                        state.LastMessage = responseSrvc.Message;
                    });

                    if (responseSrvc.Error != null)
                    {
                        //return error to the main UI straight away
                        await safeWriter.WriteAsync(new SurveyProcessingResponse
                        {
                            Error = responseSrvc.Error
                        });
                    }
                }
            }
        }       
        public async Task<ProcessingFisResponse> GetMultiProcessingLCMSVariables(EmptyWS request) 
        {

            if (_client == null)
            {
                throw new InvalidOperationException("ProcessingServiceManager must be initialized before use.");
            }

            try
            {
                return  _client.ProcessingLCMSVariables(new EmptyWS());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving MultiProcessingLCMSVariables: {ex.Message}");
                return new ProcessingFisResponse
                {
                    DetailLogViewHelpers = { },
                    FoundInvalidLicense = false,
                    FoundInvalidConfig = false
                };
            }

        }       
        public void Dispose()
        {
            
            try
            {
                if (_channel != null)
                {
                    _channel.ShutdownAsync().Wait();
                    _channel = null;
                }
                _channel?.Dispose();
            }
            catch { }
            _client = null;
            _channel = null;
            _isInitialized = false;
        }

        public static void Reset()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }

                _isInitialized = false;
            }
        }
        private SurveyWSProcessingRequest CastRequest(SurveyProcessingRequest request)
        {
            return new SurveyWSProcessingRequest
            {
                FolderPath = request.FolderPath,
                SelectedFiles = { request.SelectedFiles },
                CfgFolder = request.CfgFolder,
                CfgFileName = request.CfgFileName,
                ProcessingObjects = { request.ProcessingObjects }
            };
        }


    }

}
