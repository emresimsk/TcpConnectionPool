using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpConnection.Pool
{
    public abstract class TcpPool
    {
        protected int MaxConnectionCount { get; set; }
        protected int MinConnectionCount { get; set; }
        protected int IdleTimeout { get; set; }
        protected bool IsDebug { get; set; }

        private volatile int _poolSize;

        private ConcurrentQueue<Tuple<long, TcpClient>> _pool = new ConcurrentQueue<Tuple<long, TcpClient>>();

        protected TcpPool()
        {
            NewConnection().ConfigureAwait(false);
        }

        protected async Task<TResult> ProcessRequest<TResult>(Func<TcpClient, Task<TResult>> clientHandler)
        {
            var (key,client) = await Acquire();

            try
            {
                var result = await clientHandler(client).ConfigureAwait(false);
                return result;
            }
            finally
            {
                await Release(key,client);
            }
        } 


        private async Task<(long,TcpClient)> Acquire()
        {
            return await Task.Run(async () =>
            {
                Tuple<long, TcpClient> clientTuple;

                while (!_pool.TryDequeue(out clientTuple))
                {
                    if (Interlocked.Decrement(ref _poolSize) < MinConnectionCount)
                    {
                        await NewConnection();
                    }
                }

                Interlocked.Decrement(ref _poolSize);

                if (IsDebug)
                {
                    Console.WriteLine($"Connection count:{_poolSize}");
                }

                return (clientTuple.Item1, clientTuple.Item2);
            });
        }

        private async Task Release(long key,TcpClient client)
        {
           await Task.Run( async () =>
            {
                if (Validate(key))
                {
                    if (Interlocked.Increment(ref _poolSize) <= MaxConnectionCount)
                    {
                        if (IsDebug)
                        {
                            Console.WriteLine($"Connection count:{_poolSize}");
                        }

                        _pool.Enqueue(new Tuple<long, TcpClient>(DateTime.Now.Ticks, client));
                    }
                    else
                    {
                        Interlocked.Decrement(ref _poolSize);
                    }
                }
                else
                {
                    if (IsDebug)
                    {
                        Console.WriteLine($"Connection count:{_poolSize}");
                    }
                }
            });
        }

        private async Task NewConnection()
        {
            while (Interlocked.Increment(ref _poolSize) <= MaxConnectionCount)
            {
                try
                {
                    var client = InitializeConnection();

                    _pool.Enqueue(new Tuple<long, TcpClient>(DateTime.Now.Ticks, client));

                    if (IsDebug)
                    {
                        Console.WriteLine($"New connection created. Connection count:{_poolSize}");
                    }
                }
                catch (Exception e)
                {
                    Interlocked.Decrement(ref _poolSize);
                }
            }

            Interlocked.Decrement(ref _poolSize);

        }

        private bool Validate(long key)
        {
            TimeSpan duration = new TimeSpan(DateTime.Now.Ticks - key);

            return duration.Seconds <= IdleTimeout;
        }


        protected abstract TcpClient InitializeConnection();
    }
}
