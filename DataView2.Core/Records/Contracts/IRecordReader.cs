using Google.Protobuf;

namespace DataView2.Core.Records.Contracts;


public interface IRecordReader<TRecord> : IDisposable where TRecord : IMessage<TRecord>, new()
{
    public bool IsDisposed { get; }

    IAsyncEnumerable<TRecord> ReadAsync(CancellationToken cancellationToken);
}
