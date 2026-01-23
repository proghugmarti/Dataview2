using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace DataView2.Core.MultiInstances
{ 
    public sealed class InstanceSyncService
{
    private readonly string PIPE_NAME;
    private readonly string _instanceId;
    private readonly Dictionary<string, NamedPipeClientStream> _pipeClients = new();

    private CancellationTokenSource _cts = new();

    public event Action<MapSyncMessage>? OnMessageReceived;

    public InstanceSyncService()
    {
        _instanceId = Process.GetCurrentProcess().Id.ToString();
        PIPE_NAME = $"DV_MAP_SYNC_PIPE_{_instanceId}";
    }

    public void Start()
    {
        StartServer();
        ConnectToOtherInstances();
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    // ----------------------------------------------------
    // SERVER
    // ----------------------------------------------------
    public void StartServer()
    {
        Debug.WriteLine($"[PIPE SERVER] Waiting for connection in instance {_instanceId}");
        Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                var server = new NamedPipeServerStream(
                    PIPE_NAME,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(_cts.Token);
                Debug.WriteLine($"[PIPE SERVER] Client connected in instance {_instanceId}");

                _ = Task.Run(() => ReadFromClient(server));
            }
        });
    }

    private async Task ReadFromClient(NamedPipeServerStream pipe)
    {
        using var reader = new StreamReader(pipe);

        while (pipe.IsConnected && !_cts.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
                break;

            var msg = JsonSerializer.Deserialize<MapSyncMessage>(line);
            if (msg == null || msg.SourceInstanceId == _instanceId)
                continue;

            OnMessageReceived?.Invoke(msg);
        }
    }

    // ----------------------------------------------------
    // CLIENT
    // ----------------------------------------------------
    public void ConnectToOtherInstances()
    {
        var instances = SharedDVInstanceStore.Read();
        Debug.WriteLine($"[PIPE CLIENT] Instances found in DV_Instances.json:");
        foreach (var kvp in instances)
        {
            Debug.WriteLine($"[PIPE CLIENT] -> InstanceId: {kvp.Key}");
        }
        foreach (var kvp in instances)
        {
            if (kvp.Key == _instanceId)
                continue;

            StartClient(kvp.Key);
        }
    }

    private void StartClient(string targetInstanceId)
    {
        Task.Run(async () =>
        {
            try
            {
                using var client = new NamedPipeClientStream(
                    ".",
                    $"DV_MAP_SYNC_PIPE_{targetInstanceId}",
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);

                await client.ConnectAsync(2000, _cts.Token);

                using var writer = new StreamWriter(client)
                {
                    AutoFlush = true
                };

                while (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(Timeout.Infinite, _cts.Token);
                }
            }
            catch
            {

            }
        });
    }

    // ----------------------------------------------------
    // SEND
    // ----------------------------------------------------
    public async Task SendAsync(MapSyncMessage message)
    {
        message.SourceInstanceId = _instanceId;
        message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // ---------- Read all connected instances ----------
        var instances = SharedDVInstanceStore.Read();

        foreach (var kvp in instances)
        {
            var targetInstanceId = kvp.Key;

            // ---------- Skip self ----------
            if (targetInstanceId == _instanceId)
                continue;

            try
            {
                Debug.WriteLine($"[PIPE CLIENT] Preparing to send message to instance {targetInstanceId}");

                // ---------- Create a new client for this send ----------
                using var client = new NamedPipeClientStream(
                    ".",
                    $"DV_MAP_SYNC_PIPE_{targetInstanceId}",
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);

                // ---------- Attempt to connect with timeout ----------
                try
                {
                    await client.ConnectAsync(2000); // 2 seconds timeout
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine($"[PIPE CLIENT] Timeout connecting to instance {targetInstanceId}, skipping.");
                    continue; // Skip this instance
                }

                // ---------- Send message ----------
                using var writer = new StreamWriter(client) { AutoFlush = true };
                var json = JsonSerializer.Serialize(message);
                await writer.WriteLineAsync(json);

                Debug.WriteLine($"[PIPE CLIENT] Message successfully sent to instance {targetInstanceId}");
            }
            catch (Exception ex)
            {
                // ---------- Handle unexpected errors ----------
                Debug.WriteLine($"[PIPE CLIENT] Failed sending to instance {targetInstanceId}: {ex.Message}");
            }
        }
    }

}

    public class MapSyncMessage
    {
        public string SourceInstanceId { get; set; } = string.Empty;
        public long Timestamp { get; set; }

        // Map data
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Scale { get; set; }
        public double Rotation { get; set; }

        // Control flags
        public bool IsControlMessage { get; set; } = false;
        public bool? EnableSync { get; set; }  // null = not a control message
        public bool IsFinal { get; set; } //Control final movement on the map
    }

}
