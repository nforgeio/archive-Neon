//-----------------------------------------------------------------------------
// FILE:	    LogManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.Diagnostics
{
    /// <summary>
    /// Global class used to manage application logging.
    /// </summary>
    public static class LogManager
    {
        private static bool         initialized = false;
        private static LogLevel     logLevel;

        /// <summary>
        /// Initializes the manager.
        /// </summary>
        private static void Initialize()
        {
            if (!Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL"), true, out logLevel))
            {
                logLevel = LogLevel.Info;
            }

            initialized = true;
        }

        /// <summary>
        /// Specifies the level of events to be actually recorded.
        /// </summary>
        public static LogLevel LogLevel
        {
            get
            {
                if (!initialized)
                {
                    Initialize();
                }

                return logLevel;
            }

            set
            {
                logLevel    = value;
                initialized = true;
            }
        }

        /// <summary>
        /// Controls whether timestamps are emitted.  This defaults to <c>true</c>.
        /// </summary>
        public static bool EmitTimestamp { get; set; } = true;

        /// <summary>
        /// Returns a named logger.
        /// </summary>
        /// <param name="name">The logger name.</param>
        /// <returns>The <see cref="ILog"/> instance.</returns>
        public static ILog GetLogger(string name)
        {
            return new Logger(name ?? string.Empty);
        }

        /// <summary>
        /// Returns a logger for a specific type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="ILog"/> instance.</returns>
        public static ILog GetLogger(Type type)
        {
            return new Logger(type.FullName);
        }
    }
}
