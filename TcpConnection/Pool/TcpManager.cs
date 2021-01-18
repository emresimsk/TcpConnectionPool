using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpConnection.Pool
{
    public class TcpManager : TcpPool
    {
        public TcpManager()
        {
            base.IsDebug = true;
            base.MaxConnectionCount = 50;
            base.MinConnectionCount = 20;
            base.IdleTimeout = 200;
        }

        protected override TcpClient InitializeConnection()
        {
            TcpClient client = new TcpClient();
            client.Connect("localhost", 80);
            return client;
        }

        public async Task<string> RequestSend()
        {
            var response = await ProcessRequest<string>(async (client) =>
            {
                var networkStream = client.GetStream();

                var writeByte = Encoding.ASCII.GetBytes("request body");

                networkStream.Write(writeByte, 0, writeByte.Length);

                writeByte = new byte[512];

                var bytes = networkStream.Read(writeByte, 0, writeByte.Length);

                var responseData = Encoding.ASCII.GetString(writeByte, 0, bytes);
                
                return responseData;
            });
        }
    }
}