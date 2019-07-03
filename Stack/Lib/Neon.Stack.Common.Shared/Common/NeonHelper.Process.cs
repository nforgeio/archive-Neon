﻿//-----------------------------------------------------------------------------
// FILE:	    NeonHelper.Process.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Stack.Common
{
    public static partial class NeonHelper
    {
        /// <summary>
        /// Starts a process to run an executable file and waits for the process to terminate.
        /// </summary>
        /// <param name="path">Path to the executable file.</param>
        /// <param name="args">Command line arguments (or <c>null</c>).</param>
        /// <param name="timeout">
        /// Optional maximum time to wait for the process to complete or <c>null</c> to wait
        /// indefinitely.
        /// </param>
        /// <param name="process">
        /// The optional <see cref="Process"/> instance to use to launch the process.
        /// </param>
        /// <returns>The process exit code.</returns>
        /// <exception cref="TimeoutException">Thrown if the process did not exit within the <paramref name="timeout"/> limit.</exception>
        /// <remarks>
        /// <note>
        /// If <paramref name="timeout"/> is and execution has not commpleted in time then
        /// a <see cref="TimeoutException"/> will be thrown and the process will be killed
        /// if it was created by this method.  Process instances passed via the <paramref name="process"/>
        /// parameter will not be killed in this case.
        /// </note>
        /// </remarks>
        public static int Execute(string path, string args, TimeSpan? timeout = null, Process process = null)
        {
            var processInfo   = new ProcessStartInfo(path, args != null ? args : string.Empty);
            var killOnTimeout = process == null;

            if (process == null)
            {
                process = new Process();
            }

            try
            {
                processInfo.UseShellExecute        = false;
                processInfo.RedirectStandardError  = false;
                processInfo.RedirectStandardOutput = false;
                processInfo.CreateNoWindow         = true;
                process.StartInfo                  = processInfo;
                process.EnableRaisingEvents        = true;

                process.Start();

                if (!timeout.HasValue || timeout.Value >= TimeSpan.FromDays(1))
                    process.WaitForExit();
                else
                {
                    process.WaitForExit((int)timeout.Value.TotalMilliseconds);

                    if (!process.HasExited)
                    {
                        if (killOnTimeout)
                        {
                            process.Kill();
                        }

                        throw new TimeoutException(string.Format("Process [{0}] execute has timed out.", path));
                    }
                }

                return process.ExitCode;
            }
            finally
            {
                process.Dispose();
            }
        }

        /// <summary>
        /// Asyncrhonously starts a process to run an executable file and waits for the process to terminate.
        /// </summary>
        /// <param name="path">Path to the executable file.</param>
        /// <param name="args">Command line arguments (or <c>null</c>).</param>
        /// <param name="timeout">
        /// Optional maximum time to wait for the process to complete or <c>null</c> to wait
        /// indefinitely.
        /// </param>
        /// <param name="process">
        /// The optional <see cref="Process"/> instance to use to launch the process.
        /// </param>
        /// <returns>The process exit code.</returns>
        /// <exception cref="TimeoutException">Thrown if the process did not exit within the <paramref name="timeout"/> limit.</exception>
        /// <remarks>
        /// <note>
        /// If <paramref name="timeout"/> is and execution has not commpleted in time then
        /// a <see cref="TimeoutException"/> will be thrown and the process will be killed
        /// if it was created by this method.  Process instances passed via the <paramref name="process"/>
        /// parameter will not be killed in this case.
        /// </note>
        /// </remarks>
        public static async Task<int> ExecuteAsync(string path, string args, TimeSpan? timeout = null, Process process = null)
        {
            return await Task.Run(() => Execute(path, args, timeout, process));
        }

        /// <summary>
        /// Used by <see cref="ExecuteCaptureStreams(string, string, TimeSpan?, Process)"/> to redirect process output streams.
        /// </summary>
        private sealed class StreamRedirect
        {
            private object          syncLock       = new object();
            public StringBuilder    sbOutput       = new StringBuilder();
            public StringBuilder    sbError        = new StringBuilder();
            public bool             isOutputClosed = false;
            public bool             isErrorClosed  = false;

            public void OnOutput(object sendingProcess, DataReceivedEventArgs args)
            {
                lock (syncLock)
                {
                    if (string.IsNullOrWhiteSpace(args.Data))
                    {
                        isOutputClosed = true;
                    }
                    else
                    {
                        sbOutput.AppendLine(args.Data);
                    }
                }
            }

            public void OnError(object sendingProcess, DataReceivedEventArgs args)
            {
                lock (syncLock)
                {
                    if (string.IsNullOrWhiteSpace(args.Data))
                    {
                        isErrorClosed = true;
                    }
                    else
                    {
                        sbError.AppendLine(args.Data);
                    }
                }
            }

            public void Wait()
            {
                while (!isOutputClosed || !isErrorClosed)
                {
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Starts a process to run an executable file and waits for the process to terminate
        /// while capturing any output written to the standard output and error streams.
        /// </summary>
        /// <param name="path">Path to the executable file.</param>
        /// <param name="args">Command line arguments (or <c>null</c>).</param>
        /// <param name="timeout">
        /// Optional maximum time to wait for the process to complete or <c>null</c> to wait
        /// indefinitely.
        /// </param>
        /// <param name="process">
        /// The optional <see cref="Process"/> instance to use to launch the process.
        /// </param>
        /// <returns>
        /// The <see cref="ExecuteResult"/> including the process exit code and capture 
        /// standard output and error streams.
        /// </returns>
        /// <exception cref="TimeoutException">Thrown if the process did not exit within the <paramref name="timeout"/> limit.</exception>
        /// <remarks>
        /// <note>
        /// If <paramref name="timeout"/> is and execution has not commpleted in time then
        /// a <see cref="TimeoutException"/> will be thrown and the process will be killed
        /// if it was created by this method.  Process instances passed via the <paramref name="process"/>
        /// parameter will not be killed in this case.
        /// </note>
        /// </remarks>
        public static ExecuteResult ExecuteCaptureStreams(string path, string args, TimeSpan? timeout = null, Process process = null)
        {
            var processInfo     = new ProcessStartInfo(path, args != null ? args : string.Empty);
            var redirect        = new StreamRedirect();
            var externalProcess = process != null;

            if (process == null)
            {
                process = new Process();
            }

            try
            {
                processInfo.UseShellExecute        = false;
                processInfo.RedirectStandardError  = true;
                processInfo.RedirectStandardOutput = true;
                processInfo.CreateNoWindow         = true;
                process.StartInfo                  = processInfo;
                process.OutputDataReceived        += new DataReceivedEventHandler(redirect.OnOutput);
                process.ErrorDataReceived         += new DataReceivedEventHandler(redirect.OnError);
                process.EnableRaisingEvents        = true;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!timeout.HasValue || timeout.Value >= TimeSpan.FromDays(1))
                {
                    process.WaitForExit();
                }
                else
                {
                    process.WaitForExit((int)timeout.Value.TotalMilliseconds);

                    if (!process.HasExited)
                    {
                        if (!externalProcess)
                        {
                            process.Kill();
                        }

                        throw new TimeoutException(string.Format("Process [{0}] execute has timed out.", path));
                    }
                }

                redirect.Wait();    // Wait for the standard output/error streams
                                    // to receive all the data

                return new ExecuteResult()
                    {
                        ExitCode       = process.ExitCode,
                        StandardOutput = redirect.sbOutput.ToString(),
                        StandardError  = redirect.sbError.ToString()
                    };
            }
            finally
            {
                if (!externalProcess)
                {
                    process.Dispose();
                }
            }
        }

        /// <summary>
        /// Asynchronously starts a process to run an executable file and waits for the process to terminate
        /// while capturing any output written to the standard output and error streams.
        /// </summary>
        /// <param name="path">Path to the executable file.</param>
        /// <param name="args">Command line arguments (or <c>null</c>).</param>
        /// <param name="timeout">
        /// Maximum time to wait for the process to complete or <c>null</c> to wait
        /// indefinitely.
        /// </param>
        /// <param name="process">
        /// The optional <see cref="Process"/> instance to use to launch the process.
        /// </param>
        /// <returns>
        /// The <see cref="ExecuteResult"/> including the process exit code and capture 
        /// standard output and error streams.
        /// </returns>
        /// <exception cref="TimeoutException">Thrown if the process did not exit within the <paramref name="timeout"/> limit.</exception>
        /// <remarks>
        /// <note>
        /// If <paramref name="timeout"/> is and execution has not commpleted in time then
        /// a <see cref="TimeoutException"/> will be thrown and the process will be killed
        /// if it was created by this method.  Process instances passed via the <paramref name="process"/>
        /// parameter will not be killed in this case.
        /// </note>
        /// </remarks>
        public static async Task<ExecuteResult> ExecuteCaptureStreamsAsync(string path, string args, 
                                                                           TimeSpan? timeout = null, Process process = null)
        {
            return await Task.Run(() => ExecuteCaptureStreams(path, args, timeout, process));
        }

        /// <summary>
        /// Starts a process for an <see cref="Assembly" /> by calling the assembly's <b>main()</b>
        /// entry point method. 
        /// </summary>
        /// <param name="assembly">The assembly to be started.</param>
        /// <param name="args">The command line arguments (or <c>null</c>).</param>
        /// <returns>The process started.</returns>
        /// <remarks>
        /// <note>
        /// This method works only for executable assemblies with
        /// an appropriate <b>main</b> entry point that reside on the
        /// local file system.
        /// </note>
        /// </remarks>
        public static Process StartProcess(Assembly assembly, string args)
        {
            string path = assembly.CodeBase;

            if (!path.StartsWith("file://"))
            {
                throw new ArgumentException("Assembly must reside on the local file system.", "assembly");
            }

            return Process.Start(NeonHelper.StripFileScheme(path), args != null ? args : string.Empty);
        }
    }
}
