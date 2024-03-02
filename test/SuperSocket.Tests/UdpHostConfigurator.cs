using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Host;
using SuperSocket.Client;
using SuperSocket.ProtoBase;

namespace SuperSocket.Tests
{
    public class UdpHostConfigurator : IHostConfigurator
    {
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
        
        public string WebSocketSchema => "ws";

        public bool IsSecure => false;

        public ListenOptions Listener { get; private set; }

        private static Random _rd = new Random();

        public void Configure(ISuperSocketHostBuilder hostBuilder)
        {
            hostBuilder
                .UseUdp()
                .ConfigureServices((ctx, services) =>
                    {
                        services.Configure<ServerOptions>((options) =>
                        {
                            var listener = options.Listeners[0];
                            Listener = listener;
                        });
                    }
                );
        }

        public IEasyClient<TPackageInfo> ConfigureEasyClient<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter, ConnectionOptions options)
            where TPackageInfo : class
        {
            return new EasyClient<TPackageInfo>(pipelineFilter, options);
        }

        public Socket CreateClient()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var retry = 0;

            while (retry < 10)
            {
                try
                {
                    var localPort = _rd.Next(40000, 50000);
                    var localEndPoint = new IPEndPoint(IPAddress.Loopback, localPort);
                    socket.Bind(localEndPoint);
                    return socket;
                }
                catch
                {
                    retry++;
                }
            }

            throw new Exception("Cannot find an available port for udp client binding.");
        }

        private async Task UdpReceive(Socket socket, IVirtualConnection connection)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 4040);

            while (true)
            {
                var buffer = _bufferPool.Rent(1024 * 5);

                try
                {
                    var result = await socket
                        .ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, remoteEndPoint)
                        .ConfigureAwait(false);

                    await connection.WritePipeDataAsync((new ArraySegment<byte>(buffer, 0, result.ReceivedBytes)).AsMemory(), CancellationToken.None);
                }
                catch (NullReferenceException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                finally
                {
                    _bufferPool.Return(buffer);
                }
            }
        }

        public ValueTask<Stream> GetClientStream(Socket socket)
        {
            var channel = new UdpPipeConnection<TextPackageInfo>(socket, new TerminatorPipelineFilter<TextPackageInfo>(new[] { (byte)'\r', (byte)'\n'})
                {
                    Decoder = new UdpPackageDecoder()
                },
                new ConnectionOptions(), new IPEndPoint(IPAddress.Loopback, Listener.GetListenEndPoint().Port));

            channel.Start();

            UdpReceive(socket, channel).DoNotAwait();

            return new ValueTask<Stream>(new UdpChannelStream(channel, socket));
        }

        public TextReader GetStreamReader(Stream stream, Encoding encoding)
        {
            var connection = (stream as UdpChannelStream).Connection;
            return new UdpTextReader(connection);
        }

        class UdpPackageDecoder : IPackageDecoder<TextPackageInfo>
        {
            public TextPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context)
            {
                return new TextPackageInfo { Text = buffer.GetString(Encoding.UTF8) };
            }
        }

        public async ValueTask KeepSequence()
        {
            await Task.Delay(200);
        }

    }
}