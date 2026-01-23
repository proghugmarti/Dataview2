

using DataView2.Core.Models.Other;
using DataView2.GrpcService.Protos;
using DataView2.Core.Helper;
using Grpc.Core;
using Serilog;
using System.Collections.Concurrent;
using static DataView2.GrpcService.Protos.ProcessingStateResponse.Types;

namespace DataView2.GrpcService
{
    public class ProcessingStateService
    {

        private readonly object _lock = new();
        private readonly Timer _timer;
        private bool _isProcessing = false;
        private ProcessingState _currentState = new();
        private IServerStreamWriter<ProcessingStateResponse> _currentStream;
        private CancellationToken _currentToken;
        //private CancellationTokenSource _processSurveyCts;
        //public CancellationToken ProcessSurveyToken
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _processSurveyCts?.Token ?? CancellationToken.None;
        //        }
        //    }
        //}

        public ProcessingStateService()
        {
            _timer = new Timer(PushUpdateToUi, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void AttachStream(
           IServerStreamWriter<ProcessingStateResponse> stream,
           CancellationToken token)
        {
            lock (_lock)
            {
                _currentStream = stream;
                _currentToken = token;
                _isProcessing = true;
                //Start timer: run immediately, then every 2 seconds
                _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
            }
        }

        public void DetachStream()
        {
            lock (_lock)
            {
                //stop timer completely
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                _currentStream = null;
                _currentToken = CancellationToken.None;
                _isProcessing = false;
            }
        }

        //public void CancelProcessingToken()
        //{
        //    lock (_lock)
        //    {
        //        _processSurveyCts?.Cancel();
        //    }
        //}

        public void UpdateState(Action<ProcessingState> update)
        {
            lock (_lock)
            {
                update(_currentState);
                _currentState.LastUpdate = DateTime.UtcNow;
            }
        }

        private async void PushUpdateToUi(object _)
        {
            IServerStreamWriter<ProcessingStateResponse> stream;
            ProcessingState snapshot;
            CancellationToken token;

            lock (_lock)
            {
                if (_currentStream == null) return;
                if (!_isProcessing) return;         
                
                stream = _currentStream;
                token = _currentToken;

                snapshot = new ProcessingState
                {
                    Stage = _currentState.Stage,
                    TotalPercentage = _currentState.TotalPercentage,
                    StagePercentage = _currentState.StagePercentage,
                    LastMessage = _currentState.LastMessage,
                    LastUpdate = _currentState.LastUpdate
                };
            }

            if (token.IsCancellationRequested) return;

            try
            {
                var response = new ProcessingStateResponse
                {
                    Stage = snapshot.Stage switch
                    {
                        ProcessingStage.ReadingPrerequisites => Stage.ReadingPrereqs,
                        ProcessingStage.ProcessingFIS => Stage.ProcessingFis,
                        ProcessingStage.ProcessingXML => Stage.ProcessingXml,
                        ProcessingStage.ProcessingVideo => Stage.ProcessingVideo,
                        _ => Stage.Unknown
                    },
                    TotalPercentage = snapshot.TotalPercentage,
                    StagePercentage = snapshot.StagePercentage,
                    LastMessage = snapshot.LastMessage ?? string.Empty,
                    LastUpdate = snapshot.LastUpdate.ToString("o")
                };

                var safeWriter = new SafeSurveyWriter<ProcessingStateResponse>(stream);
                await safeWriter.WriteAsync(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
