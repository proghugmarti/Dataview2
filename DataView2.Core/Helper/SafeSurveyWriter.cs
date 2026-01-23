using Grpc.Core;

namespace DataView2.Core.Helper
{
    public class SafeSurveyWriter<T>
    {
        private readonly IServerStreamWriter<T> _stream;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public SafeSurveyWriter(IServerStreamWriter<T> stream)
        {
            _stream = stream;
        }

        public async Task WriteAsync(T message)
        {
            await _lock.WaitAsync();
            try
            {
                await _stream.WriteAsync(message);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
