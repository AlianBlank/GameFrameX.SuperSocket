﻿using System;
using System.Threading;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Command
{
    public struct CommandExecutingContext
    {
        /// <summary>
        /// Gets the session.
        /// </summary>
        public IAppSession Session { get; set; }

        /// <summary>
        /// Gets the request info.
        /// </summary>
        public object Package { get; set; }

        /// <summary>
        /// Gets the current command.
        /// </summary>
        public ICommand CurrentCommand { get; set; }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}