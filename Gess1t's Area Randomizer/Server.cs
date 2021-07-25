using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Area_Randomizer
{
    public class Server
    {
        public string pipename;
        private JsonRpc rpc;
        private NamedPipeServerStream server;
        private object objectToAttach;
        private bool running = true;
        public Server(string pipename, object objectToAttach) 
        {
            this.pipename = pipename;
            this.objectToAttach = objectToAttach;
        }
        public async Task StartAsync()
        {
            while (running)
            {
                server = new NamedPipeServerStream(pipename, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Log.Debug($"{pipename}", "Waiting for connection...");
                await server.WaitForConnectionAsync();
                Log.Debug($"{pipename}", "Connected");
                rpc = JsonRpc.Attach(server, objectToAttach);
                Log.Debug($"{pipename}", "Listening to request...");
                await rpc.Completion;
                Log.Debug($"{pipename}", "Client Disconnected, Disposing and restarting...");
                rpc.Dispose();
                await server.DisposeAsync();
            }
        }
        public void Dispose()
        {
            running = false;
            rpc.Dispose();
            server.Dispose();
        }
    }
}