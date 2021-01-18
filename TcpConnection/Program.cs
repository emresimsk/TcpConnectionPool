using System;
using System.Threading.Tasks;
using TcpConnection.Pool;

namespace TcpConnection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TcpManager manager = new TcpManager();

            var response = await manager.RequestSend();

            Console.ReadKey();
        }
    }
}
