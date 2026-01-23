using DataView2.Core.Records.Contracts;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Records.Sinks
{
    public class RecordReaderFileSink<TRecord> : IRecordReader<TRecord> where TRecord : IMessage<TRecord>, new()
    {
        private readonly BufferedStream _bufferedStream;
        public RecordReaderFileSink(string filePath)
        {
            _bufferedStream = new(File.OpenRead(filePath));
        }
        public bool IsDisposed { get; private set; } = false;

        public void Dispose()
        {
            if (IsDisposed) { return; }
            IsDisposed = true;

            _bufferedStream.Close();
            _bufferedStream.Dispose();

            GC.SuppressFinalize(this);
        }

        public async IAsyncEnumerable<TRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (_bufferedStream.Position < _bufferedStream.Length && !cancellationToken.IsCancellationRequested)
            {
                TRecord record = new();
                record.MergeDelimitedFrom(_bufferedStream);

                yield return record;
            }

            await Task.CompletedTask;
        }
    }
}
