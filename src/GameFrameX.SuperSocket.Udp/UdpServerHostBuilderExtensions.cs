using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Host;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameFrameX.SuperSocket.Udp
{
    public static class UdpServerHostBuilderExtensions
    {
        public static ISuperSocketHostBuilder UseUdp(this ISuperSocketHostBuilder hostBuilder)
        {
            return (hostBuilder.ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConnectionListenerFactory, UdpConnectionListenerFactory>();
                    services.AddSingleton<IConnectionFactoryBuilder, UdpConnectionFactoryBuilder>();
                }) as ISuperSocketHostBuilder)
                .ConfigureSupplementServices((context, services) =>
                {
                    if (!services.Any(s => s.ServiceType == typeof(IUdpSessionIdentifierProvider)))
                    {
                        services.AddSingleton<IUdpSessionIdentifierProvider, IPAddressUdpSessionIdentifierProvider>();
                    }

                    if (!services.Any(s => s.ServiceType == typeof(IAsyncSessionContainer)))
                    {
                        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, InProcSessionContainerMiddleware>(s => s.GetRequiredService<InProcSessionContainerMiddleware>()));
                        services.AddSingleton<InProcSessionContainerMiddleware>();
                        services.AddSingleton<ISessionContainer>((s) => s.GetRequiredService<InProcSessionContainerMiddleware>());
                        services.AddSingleton<IAsyncSessionContainer>((s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
                    }
                });
        }

        public static ISuperSocketHostBuilder<TReceivePackage> UseUdp<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseUdp() as ISuperSocketHostBuilder<TReceivePackage>;
        }
    }
}