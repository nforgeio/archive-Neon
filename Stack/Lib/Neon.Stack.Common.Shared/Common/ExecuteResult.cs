//-----------------------------------------------------------------------------
// FILE:        ExecuteResult.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Diagnostics;

namespace Neon.Stack.Common
{
    /// <summary>
    /// Holds the process exit code and captured standard output from a process
    /// launched by <see cref="NeonHelper.ExecuteCaptureStreams(string, string, TimeSpan?, Process)"/>.
    /// </summary>
    public class ExecuteResult
    {
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal ExecuteResult()
        {
        }

        /// <summary>
        /// Returns the process exit code.
        /// </summary>
        public int ExitCode { get; internal set; }

        /// <summary>
        /// Returns the captured standard output stream from the process.
        /// </summary>
        public string StandardOutput { get; internal set; }

        /// <summary>
        /// Returns the captured standard error stream from the process.
        /// </summary>
        public string StandardError { get; internal set; }
    }
}
