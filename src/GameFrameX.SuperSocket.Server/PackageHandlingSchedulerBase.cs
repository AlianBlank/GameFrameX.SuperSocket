using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    public abstract class PackageHandlingSchedulerBase<TPackageInfo> : IPackageHandlingScheduler<TPackageInfo>
    {
        public IPackageHandler<TPackageInfo> PackageHandler { get; private set; }

        public Func<IAppSession, PackageHandlingException<TPackageInfo>, ValueTask<bool>> ErrorHandler { get; private set; }

        public abstract ValueTask HandlePackage(IAppSession session, TPackageInfo package, CancellationToken cancellationToken);

        public virtual void Initialize(IPackageHandler<TPackageInfo> packageHandler, Func<IAppSession, PackageHandlingException<TPackageInfo>, ValueTask<bool>> errorHandler)
        {
            PackageHandler = packageHandler;
            ErrorHandler = errorHandler;
        }

        protected async ValueTask HandlePackageInternal(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
        {
            var packageHandler = PackageHandler;
            var errorHandler = ErrorHandler;

            try
            {
                if (packageHandler != null)
                    await packageHandler.Handle(session, package, cancellationToken);
            }
            catch (Exception e)
            {
                var toClose = await errorHandler(session, new PackageHandlingException<TPackageInfo>($"Session {session.SessionID} got an error when handle a package.", package, e));

                if (toClose)
                {
                    session.CloseAsync(CloseReason.ApplicationError).DoNotAwait();
                }
            }
        }
    }
}