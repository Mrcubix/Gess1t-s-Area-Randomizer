using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Area_Randomizer
{
    public class Client
    {
        public string pipename;
        public NamedPipeClientStream client;
        public JsonRpc rpc;
        public Client(string pipename)
        {
            this.pipename = pipename;
        }
        public async Task StartAsync()
        {
            client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
            Log.Debug("AreaVisualizer", $"Waiting for connections...");
            await client.ConnectAsync();
            Log.Debug("AreaVisualizer", $"Connected = {client.IsConnected}");
            rpc = JsonRpc.Attach(client);

            rpc.Disconnected += (_, _) =>
            {
                Log.Debug("AreaVisualizer", $"Server disconnected.");
                client.Dispose();
                rpc.Dispose();
                _ = StartAsync();
            };

            Log.Debug("AreaVisualizer", $"Now listening to {pipename}");
        }
        public void Dispose()
        {
            client.Dispose();
            rpc.Dispose();
        }
    }
}