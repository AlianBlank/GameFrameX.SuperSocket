using System.IO.Compression;
using System.Security.Authentication;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class DefaultConnectionStreamInitializersFactory : IConnectionStreamInitializersFactory
    {
        public CompressionLevel CompressionLevel { get; }

        private IEnumerable<IConnectionStreamInitializer> _empty = new IConnectionStreamInitializer[0];

        public DefaultConnectionStreamInitializersFactory()
            : this(CompressionLevel.NoCompression)
        {
        }

        public DefaultConnectionStreamInitializersFactory(CompressionLevel compressionLevel)
        {
            CompressionLevel = compressionLevel;
        }

        public virtual IEnumerable<IConnectionStreamInitializer> Create(ListenOptions listenOptions)
        {
            var connectionStreamInitializers = new List<IConnectionStreamInitializer>();

            if (listenOptions.Security != SslProtocols.None)
            {
                connectionStreamInitializers.Add(new NetworkStreamInitializer());
                connectionStreamInitializers.Add(new SslStreamInitializer());
            }

            if (CompressionLevel != CompressionLevel.NoCompression)
            {
                if (!connectionStreamInitializers.Any())
                {
                    connectionStreamInitializers.Add(new NetworkStreamInitializer());
                }

                connectionStreamInitializers.Add(new GZipStreamInitializer());
            }

            connectionStreamInitializers.ForEach(initializer => initializer.Setup(listenOptions));

            return connectionStreamInitializers;
        }
    }
}