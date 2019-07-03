//-----------------------------------------------------------------------------
// FILE:	    NodeProxy.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.IO;
using Neon.Stack.Net;
using Neon.Stack.Retry;
using Neon.Stack.Time;

using ICSharpCode.SharpZipLib.Zip;
using Renci.SshNet;
using Renci.SshNet.Common;

// $todo(jeff.lill):
//
// Have [NodeProxy.Manager] return a healthy manager rather than just the
// first one.

namespace Neon.Cluster
{
    /// <summary>
    /// Remotely manages a NeonCluster host node.
    /// </summary>
    /// <typeparam name="TMetadata">
    /// Defines the metadata type the application wishes to associate with the server.
    /// You may specify <c>object</c> when no additional metadata is required.
    /// </typeparam>
    /// <threadsafety instance="false"/>
    /// <remarks>
    /// <para>
    /// Construct an instance to connect to a specific cluster node.  You may specify
    /// <typeparamref name="TMetadata"/> to associate application specific information
    /// or state with the instance.
    /// </para>
    /// <para>
    /// This class includes methods to invoke Linux commands on the node as well as
    /// methods to issue Docker commands against the local node or the Swarm cluster.
    /// Methods are also provided to upload and download files.
    /// </para>
    /// <para>
    /// Call <see cref="Dispose()"/> or <see cref="Disconnect()"/> to close the connection.
    /// </para>
    /// </remarks>
    public class NodeProxy<TMetadata> : IDisposable
        where TMetadata : class
    {
        private const string Redacted = "!!SECRETS-REDACTED!!";

        private object          syncLock   = new object();
        private bool            isDisposed = false;
        private SshCredentials  credentials;
        private SshClient       sshClient;
        private ScpClient       scpClient;
        private TextWriter      logWriter;
        private bool            isReady;
        private string          status;
        private bool            hasUploadFolder;

        /// <summary>
        /// Constructs a <see cref="NodeProxy{TMetadata}"/>.
        /// </summary>
        /// <param name="name">The display name for the server.</param>
        /// <param name="host">The IP address or FQDN for the server.</param>
        /// <param name="credentials">The credentials to be used for establishing SSH connections.</param>
        /// <param name="logWriter">The optional <see cref="TextWriter"/> where operation logs will be written.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="name"/> or <paramref name="host"/> are <c>null</c> or empty or 
        /// if <paramref name="credentials"/> is <c>null</c>.
        /// </exception>
        public NodeProxy(string name, string host, SshCredentials credentials, TextWriter logWriter = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(host));
            Covenant.Requires<ArgumentNullException>(credentials != null);

            this.Name           = name;
            this.DnsName        = host;
            this.credentials    = credentials;
            this.logWriter      = logWriter;

            this.sshClient      = null;
            this.scpClient      = null;
            this.SshPort        = NetworkPorts.SSH;
            this.Status         = string.Empty;
            this.IsReady        = false;
            this.ConnectTimeout = TimeSpan.FromSeconds(10);
            this.FileTimeout    = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~NodeProxy()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all associated resources (e.g. any open server connections).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all associated resources (e.g. any open server connections).
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (syncLock)
            {
                Disconnect();

                isDisposed = true;
            }
        }

        /// <summary>
        /// Closes any open connections to the Linux server but leaves open the
        /// opportunity to reconnect later.
        /// </summary>
        /// <remarks>
        /// <note>
        /// This is similar to what dispose does <see cref="Dispose()"/> but dispose does
        /// not allow reconnection.
        /// </note>
        /// <para>
        /// This command is useful situations where the client application may temporarily
        /// lose contact with the server if for example, when it is rebooted or the network
        /// configuration changes.
        /// </para>
        /// </remarks>
        public void Disconnect()
        {
            lock (syncLock)
            {
                if (sshClient != null)
                {
                    try
                    {
                        if (sshClient.IsConnected)
                        {
                            sshClient.Dispose();
                        }
                    }
                    finally
                    {
                        sshClient = null;
                    }
                }

                if (scpClient != null)
                {
                    try
                    {
                        if (scpClient.IsConnected)
                        {
                            scpClient.Dispose();
                        }
                    }
                    finally
                    {
                        scpClient = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the display name for the server.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the host IP address or FQDN of the server.
        /// </summary>
        public string DnsName { get; private set; }

        /// <summary>
        /// The SSH port.  This defaults to <b>22</b>.
        /// </summary>
        public int SshPort { get; set; }

        /// <summary>
        /// The connection attempt timeout.  This defaults to <b>10</b> seconds.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// The file operation timeout.  This defaults to <b>60</b> seconds.
        /// </summary>
        public TimeSpan FileTimeout { get; set; }

        /// <summary>
        /// Specifies the default options to be bitwise ORed with any specific
        /// options passed to a run or sudo execution command when the <see cref="RunOptions.Defaults"/> 
        /// flag is specified.  This defaults to <see cref="RunOptions.None"/>.
        /// </summary>
        /// <remarks>
        /// Setting this is a good way to specify a global default for flags like <see cref="RunOptions.FaultOnError"/>.
        /// </remarks>
        public RunOptions DefaultRunOptions { get; set; } = RunOptions.None;

        /// <summary>
        /// The PATH to use on the remote server when executing commands in the
        /// session or <c>null</c>/empty to run commands without a path.  This
        /// defaults to the standard Linux path.
        /// </summary>
        /// <remarks>
        /// <note>
        /// When you modify this, be sure to use a colon (<b>:</b>) to separate 
        /// multiple directories as required.
        /// </note>
        /// </remarks>
        public string RemotePath { get; set; } = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/neontools";

        /// <summary>
        /// The current server status.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is intended to be used by management tools to indicate the state
        /// of the server for UX purposes.  This property will be set by some methods such
        /// as <see cref="WaitForBoot"/> but can also be set explicitly by tools when they
        /// have an operation in progress on the server.
        /// </para>
        /// <note>
        /// This will return <b>*** FAULTED ***</b> if the <see cref="IsFaulted"/>=<c>true</c>.
        /// </note>
        /// </remarks>
        public string Status
        {
            get
            {
                if (IsFaulted)
                {
                    return "*** FAULTED ***";
                }
                else
                {
                    return status;
                }
            }

            set
            {
                if (!string.IsNullOrEmpty(value) && value != status)
                {
                    if (IsFaulted)
                    {
                        LogLine($"*** STATUS[*FAULTED*]");
                    }
                    else
                    {
                        LogLine($"*** STATUS: {value}");
                    }
                }

                status = value;
            }
        }

        /// <summary>
        /// Indicates that the server has completed or has failed the current set of operations.
        /// </summary>
        /// <remarks>
        /// <note>
        /// This will always return <c>false</c> if the server has faulted (<see cref="IsFaulted"/>=<c>true</c>).
        /// </note>
        /// </remarks>
        public bool IsReady
        {
            get
            {
                return IsFaulted || isReady;
            }

            set { isReady = value; }
        }

        /// <summary>
        /// Indicates that the server is in a faulted state because one or more operations
        /// have failed.
        /// </summary>
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// Applications may use this to associate metadata with the instance.
        /// </summary>
        public TMetadata Metadata { get; set; }

        /// <summary>
        /// Attempts to resolve the server's host name into an IP address.
        /// </summary>
        /// <returns>The resolved IP address.</returns>
        /// <exception cref="ClusterDefinitionException">Thrown if the IP address couldn't be resolved.</exception>
        public IPAddress ResolveAddress()
        {
            IPAddress address;

            if (IPAddress.TryParse(DnsName, out address))
            {
                return address;
            }
            else
            {
                try
                {
                    var addresses = Dns.GetHostAddressesAsync(DnsName).Result;

                    if (addresses.Length > 1)
                    {
                        throw new ClusterDefinitionException($"DNS lookup for node [name={Name}] with [host-name={DnsName}] returned more than one address.");
                    }

                    return addresses[0];
                }
                catch (Exception e)
                {
                    throw new ClusterDefinitionException($"DNS lookup for node [name={Name}] with [host-name={DnsName}] failed: {NeonHelper.ExceptionError(e)}");
                }
            }
        }

        /// <summary>
        /// Shutdown the server.
        /// </summary>
        public void Shutdown()
        {
            Status = "shutting down...";

            try
            {
                SudoCommand("shutdown -h 0");
                Disconnect();
            }
            catch (SshConnectionException)
            {
                // Sometimes we "An established connection was aborted by the server."
                // exceptions here, which we'll ignore (because we're shutting down).
            }
            finally
            {
                // Be very sure that the connections are cleared.

                sshClient = null;
                scpClient = null;
            }

            // Give the server a chance to stop.

            Thread.Sleep(TimeSpan.FromSeconds(10));
            Status = "stopped";
        }

        /// <summary>
        /// Reboot the server.
        /// </summary>
        /// <param name="wait">Optionally wait for the server to reboot (defaults to <c>true</c>).</param>
        public void Reboot(bool wait = true)
        {
            Status = "rebooting...";

            try
            {
                SudoCommand("shutdown -r 0");
                Disconnect();
            }
            catch (SshConnectionException)
            {
                // Sometimes we "An established connection was aborted by the server."
                // exceptions here, which we'll ignore (because we're rebooting).
            }
            finally
            {
                // Be very sure that the connections are cleared.

                sshClient = null;
                scpClient = null;
            }

            // Give the server a chance to restart.

            Thread.Sleep(TimeSpan.FromSeconds(10));

            if (wait)
            {
                WaitForBoot();
            }
        }

        /// <summary>
        /// Writes text to the operation log.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Log(string text)
        {
            if (logWriter != null)
            {
                logWriter.Write(text);
            }
        }

        /// <summary>
        /// Writes a line of text to the operation log.
        /// </summary>
        /// <param name="text">The text.</param>
        public void LogLine(string text)
        {
            if (logWriter != null)
            {
                logWriter.WriteLine(text);
                LogFlush();
            }
        }

        /// <summary>
        /// Flushes the log.
        /// </summary>
        public void LogFlush()
        {
            if (logWriter != null)
            {
                logWriter.Flush();
            }
        }

        /// <summary>
        /// Writes exception information to the operation log.
        /// </summary>
        /// <param name="message">The operation details.</param>
        /// <param name="e">The exception.</param>
        public void LogException(string message, Exception e)
        {
            LogLine($"*** ERROR: {message}: {NeonHelper.ExceptionError(e)}");
        }

        /// <summary>
        /// Puts the node proxy into the faulted state.
        /// </summary>
        /// <param name="message">The optional message to be logged.</param>
        public void Fault(string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                LogLine("*** ERROR: " + message);
            }
            else
            {
                LogLine("*** ERROR: Unspecified FAULT");
            }

            IsFaulted = true;
        }

        /// <summary>
        /// Returns the connection information for SSH.NET.
        /// </summary>
        /// <returns>The connection information.</returns>
        private ConnectionInfo GetConnectionInfo()
        {
            return new ConnectionInfo(DnsName, SshPort, credentials.UserName, credentials.AuthenticationMethod)
            {
                Timeout = ConnectTimeout
            };
        }

        /// <summary>
        /// Establishes a connection to the server.
        /// </summary>
        public void Connect()
        {
            // We're only going to make a single connection attempt.

            WaitForBoot(TimeSpan.Zero);
        }

        /// <summary>
        /// Waits for the server to boot by continuously attempting to establish an SSH session.
        /// </summary>
        /// <param name="timeout">The operation timeout (defaults to <b>5min</b>).</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// <para>
        /// The method will attempt to connect to the server every 10 seconds up to the specified
        /// timeout.  If it is unable to connect during this time, the exception thrown by the
        /// SSH client will be rethrown.
        /// </para>
        /// </remarks>
        public void WaitForBoot(TimeSpan? timeout = null)
        {
            Covenant.Requires<ArgumentException>(timeout != null ? timeout >= TimeSpan.Zero : true);

            var operationTimer = new PolledTimer(timeout ?? TimeSpan.FromMinutes(5));

            while (true)
            {
                var sshClient = new SshClient(GetConnectionInfo());

                try
                {
                    sshClient.Connect();
                    break;
                }
                catch (Exception e)
                {
                    if (sshClient.IsConnected)
                    {
                        sshClient.Dispose();
                    }

                    if (operationTimer.HasFired)
                    {
                        throw;
                    }

                    LogException($"*** WARNING: Wait for boot failed", e);

                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }

            Status = "online";
        }

        /// <summary>
        /// Ensures that an SSH connection has been established.
        /// </summary>
        private void EnsureSshConnection()
        {
            lock (syncLock)
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NodeProxy<TMetadata>));
                }

                if (sshClient != null)
                {
                    return;
                }

                sshClient = new SshClient(GetConnectionInfo());

                try
                {
                    sshClient.Connect();
                }
                catch
                {
                    sshClient = null;
                    throw;
                }
            }
        }

        /// <summary>
        /// Ensures that an SCP connection has been established.
        /// </summary>
        private void EnsureScpConnection()
        {
            lock (syncLock)
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NodeProxy<TMetadata>));
                }

                if (scpClient != null)
                {
                    return;
                }

                scpClient = new ScpClient(GetConnectionInfo())
                {
                    OperationTimeout = FileTimeout
                };

                try
                {
                    scpClient.Connect();
                }
                catch
                {
                    scpClient = null;
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns the path to the user's home folder on the server.
        /// </summary>
        public string HomeFolderPath
        {
            get { return $"/home/{credentials.UserName}"; }
        }

        /// <summary>
        /// Returns the path to the user's upload folder on the server.
        /// </summary>
        public string UploadFolderPath
        {
            get { return $"{HomeFolderPath}/.upload"; }
        }

        /// <summary>
        /// Ensures that the [~/upload] folder exists on the server.
        /// </summary>
        private void EnsureUploadFolder()
        {
            if (!hasUploadFolder)
            {
                RunCommand($"mkdir -p {UploadFolderPath}", RunOptions.LogOnErrorOnly | RunOptions.IgnoreRemotePath);

                hasUploadFolder = true;
            }
        }

        /// <summary>
        /// Ensures that the configuration and setup folders required for a Neon host
        /// node exist and have the appropriate permissions.
        /// </summary>
        public void InitializeNeonFolders()
        {
            Status = "prepare: folders";

            SudoCommand($"mkdir -p {NodeHostFolders.Config}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Config}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Secrets}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Secrets}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.State}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.State}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Setup}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Setup}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Tools}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Tools}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Scripts}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Scripts}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Archive}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 600 {NodeHostFolders.Archive}", RunOptions.LogOnErrorOnly);

            SudoCommand($"mkdir -p {NodeHostFolders.Exec}", RunOptions.LogOnErrorOnly);
            SudoCommand($"chmod 777 {NodeHostFolders.Exec}", RunOptions.LogOnErrorOnly);   // Allow non-[sudo] access.
        }

        /// <summary>
        /// Downloads the a file from the Linux server and writes it out a stream.
        /// </summary>
        /// <param name="path">The source path of the file on the Linux server.</param>
        /// <param name="output">The output stream.</param>
        public void Download(string path, Stream output)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));
            Covenant.Requires<ArgumentNullException>(output != null);

            if (IsFaulted)
            {
                return;
            }

            LogLine($"*** Downloading: {path}");

            try
            {
                EnsureScpConnection();

                scpClient.Download(path, output);
            }
            catch (Exception e)
            {
                LogException("*** ERROR Downloading", e);
                throw;
            }
        }

        /// <summary>
        /// Uploads a binary stream to the Linux server and then writes it to the file system.
        /// </summary>
        /// <param name="path">The target path on the Linux server.</param>
        /// <param name="input">The input stream.</param>
        /// <param name="userPermissions">Indicates that the operation should be performed with user-level permissions.</param>
        /// <remarks>
        /// <note>
        /// <para>
        /// <b>Implementation Note:</b> The SSH.NET library we're using does not allow for
        /// files to be uploaded directly to arbitrary file system locations, even if the
        /// logged-in user has admin permissions.  The problem is that SSH.NET does not
        /// provide a way to use <b>sudo</b> to claim these higher permissions.
        /// </para>
        /// <para>
        /// The workaround is to create an upload folder in the user's home directory
        /// called <b>~/upload</b> and upload the file there first and then use SSH
        /// to move the file to its target location under sudo.
        /// </para>
        /// </note>
        /// </remarks>
        public void Upload(string path, Stream input, bool userPermissions = false)
        {
            Covenant.Requires<ArgumentNullException>(input != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            if (IsFaulted)
            {
                return;
            }

            LogLine($"*** Uploading: {path}");

            try
            {
                EnsureUploadFolder();
                EnsureScpConnection();

                var uploadPath = $"{UploadFolderPath}/{LinuxPath.GetFileName(path)}";

                scpClient.Upload(input, uploadPath);

                SudoCommand($"mkdir -p {LinuxPath.GetDirectoryName(path)}", RunOptions.LogOnErrorOnly);

                if (userPermissions)
                {
                    RunCommand($"mv {uploadPath} {path}", RunOptions.LogOnErrorOnly);
                }
                else
                {
                    SudoCommand($"mv {uploadPath} {path}", RunOptions.LogOnErrorOnly);
                }
            }
            catch (Exception e)
            {
                LogException("*** ERROR Uploading", e);
                throw;
            }
        }

        /// <summary>
        /// Uploads a text stream to the Linux server and then writes it to the file system,
        /// converting any CR-LF line endings to the Unix-style LF.
        /// </summary>
        /// <param name="path">The target path on the Linux server.</param>
        /// <param name="textStream">The input stream.</param>
        /// <param name="tabStop">Optionally expands TABs into spaces when non-zero.</param>
        /// <param name="inputEncoding">Optionally specifies the input text encoding (defaults to UTF-8).</param>
        /// <param name="outputEncoding">Optionally specifies the output text encoding (defaults to UTF-8).</param>
        /// <remarks>
        /// <note>
        /// Any Unicode Byte Order Markers (BOM) at start of the input stream will be removed.
        /// </note>
        /// <note>
        /// <para>
        /// <b>Implementation Note:</b> The SSH.NET library we're using does not allow for
        /// files to be uploaded directly to arbitrary file system locations, even if the
        /// logged-in user has admin permissions.  The problem is that SSH.NET does not
        /// provide a way to use <b>sudo</b> to claim these higher permissions.
        /// </para>
        /// <para>
        /// The workaround is to create an upload folder in the user's home directory
        /// called <b>~/upload</b> and upload the file there first and then use SSH
        /// to move the file to its target location under sudo.
        /// </para>
        /// </note>
        /// </remarks>
        public void UploadText(string path, Stream textStream, int tabStop = 0, Encoding inputEncoding = null, Encoding outputEncoding = null)
        {
            Covenant.Requires<ArgumentNullException>(textStream != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            inputEncoding  = inputEncoding ?? Encoding.UTF8;
            outputEncoding = outputEncoding ?? Encoding.UTF8;

            EnsureScpConnection();

            using (var reader = new StreamReader(textStream, inputEncoding))
            {
                using (var binaryStream = new MemoryStream(64 * 1024))
                {
                    foreach (var line in reader.Lines())
                    {
                        var convertedLine = line;

                        if (tabStop > 0)
                        {
                            convertedLine = NeonHelper.ExpandTabs(convertedLine, tabStop: tabStop);
                        }

                        binaryStream.Write(outputEncoding.GetBytes(convertedLine));
                        binaryStream.WriteByte((byte)'\n');
                    }

                    binaryStream.Position = 0;
                    Upload(path, binaryStream);
                }
            }
        }

        /// <summary>
        /// Uploads a text string to the Linux server and then writes it to the file system,
        /// converting any CR-LF line endings to the Unix-style LF.
        /// </summary>
        /// <param name="path">The target path on the Linux server.</param>
        /// <param name="text">The input text.</param>
        /// <param name="tabStop">Optionally expands TABs into spaces when non-zero.</param>
        /// <param name="outputEncoding">Optionally specifies the output text encoding (defaults to UTF-8).</param>
        /// <remarks>
        /// <note>
        /// <para>
        /// <b>Implementation Note:</b> The SSH.NET library we're using does not allow for
        /// files to be uploaded directly to arbitrary file system locations, even if the
        /// logged-in user has admin permissions.  The problem is that SSH.NET does not
        /// provide a way to use <b>sudo</b> to claim these higher permissions.
        /// </para>
        /// <para>
        /// The workaround is to create an upload folder in the user's home directory
        /// called <b>~/upload</b> and upload the file there first and then use SSH
        /// to move the file to its target location under sudo.
        /// </para>
        /// </note>
        /// </remarks>
        public void UploadText(string path, string text, int tabStop = 0, Encoding outputEncoding = null)
        {
            Covenant.Requires<ArgumentNullException>(text != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            using (var textStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                UploadText(path, textStream, tabStop, Encoding.UTF8, outputEncoding);
            }
        }

        /// <summary>
        /// Downloads a file from the remote node to the local file computer, creating
        /// parent folders as necessary.
        /// </summary>
        /// <param name="source">The source path on the Linux server.</param>
        /// <param name="target">The target path on the local computer.</param>
        public void Download(string source, string target)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(source));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(target));

            if (IsFaulted)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target));

            LogLine($"*** Downloading: [{source}] --> [{target}]");

            try
            {
                EnsureScpConnection();

                using (var output = new FileStream(target, FileMode.Create, FileAccess.ReadWrite))
                {
                    scpClient.Download(source, output);
                }
            }
            catch (Exception e)
            {
                LogException("*** ERROR Downloading", e);
                throw;
            }
        }

        /// <summary>
        /// Uploads a Mono compatible executable to the server and generates a Bash script 
        /// that seamlessly executes it.
        /// </summary>
        /// <param name="sourcePath">The path to the source executable on the local machine.</param>
        /// <param name="targetName">The name for the target command on the server (without a folder path or file extension).</param>
        /// <param name="targetFolder">The optional target folder on the server (defaults to <b>/usr/local/bin</b>).</param>
        /// <param name="permissions">
        /// The Linux file permissions.  This defaults to <b>"700"</b> which grants only the current user
        /// read/write/execute permissions.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method does the following:
        /// </para>
        /// <list type="number">
        /// <item>Uploads the executable to the target folder and names it <paramref name="targetName"/><b>.mono</b>.</item>
        /// <item>Creates a bash script in the target folder called <paramref name="targetName"/> that executes the Mono file.</item>
        /// <item>Makes the script executable.</item>
        /// </list>
        /// </remarks>
        public void UploadMonoExecutable(string sourcePath, string targetName, string targetFolder = "/usr/local/bin", string permissions = "700")
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(sourcePath));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(targetName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(targetFolder));

            using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                var binaryPath = LinuxPath.Combine(targetFolder, $"{targetName}.mono");

                Upload(binaryPath, input);

                // Set the permissions on the binary that match those we'll set
                // for the wrapping script, except stripping off the executable
                // flags.

                var binaryPermissions = new LinuxPermissions(permissions);

                binaryPermissions.OwnerExecute = false;
                binaryPermissions.GroupExecute = false;
                binaryPermissions.AllExecute   = false;

                SudoCommand($"chmod {binaryPermissions} {binaryPath}", RunOptions.LogOnErrorOnly);
            }

            var scriptPath = LinuxPath.Combine(targetFolder, targetName);
            var script =
$@"#!/bin/bash
#------------------------------------------------------------------------------
# Seamlessly invokes the [{targetName}.mono] executable using the Mono
# runtime, passing any arguments along.

mono {scriptPath}.mono $@
";

            UploadText(scriptPath, script, tabStop: 4);
            SudoCommand($"chmod {permissions} {scriptPath}", RunOptions.LogOnErrorOnly);
        }

        /// <summary>
        /// Formats a Linux command and argument objects into a form suitable for passing
        /// to the <see cref="RunCommand(string, object[])"/> or <see cref="SudoCommand(string, object[])"/>
        /// methods.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The formatted command string.</returns>
        /// <remarks>
        /// This method to quote arguments with embedded spaces and ignore <c>null</c> arguments.
        /// The method also converts arguments with types like <c>bool</c> into a Bash compatible
        /// form.
        /// </remarks>
        private string FormatCommand(string command, params object[] args)
        {
            var sb = new StringBuilder();

            sb.Append(command);

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg == null)
                    {
                        continue;
                    }

                    sb.Append(' ');

                    if (arg is bool)
                    {
                        sb.Append((bool)arg ? "true" : "false");
                    }
                    else
                    {
                        var argString = arg.ToString();

                        if (string.IsNullOrWhiteSpace(argString))
                        {
                            argString = "-";
                        }
                        else if (argString.Contains(' '))
                        {
                            argString = "\"" + argString + "\"";
                        }

                        sb.Append(argString);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Uploads a command bundle to the server and unpacks it to a temporary folder
        /// in the user's home folder.
        /// </summary>
        /// <param name="bundle">The bundle.</param>
        /// <param name="userPermissions">Indicates whether the upload should be performed with user or root permissions.</param>
        /// <returns>The path to the folder where the bundle was unpacked.</returns>
        private string UploadBundle(CommandBundle bundle, bool userPermissions)
        {
            Covenant.Requires<ArgumentNullException>(bundle != null);

            bundle.Validate();

            var executePermissions = userPermissions ? 707 : 700;

            using (var ms = new MemoryStream())
            {
                using (var zip = ZipFile.Create(ms))
                {
                    zip.BeginUpdate();

                    // Add the bundle files files to the ZIP archive we're going to upload.

                    foreach (var file in bundle)
                    {
                        var data = file.Data;

                        if (data == null && file.Text != null)
                        {
                            data = Encoding.UTF8.GetBytes(file.Text);
                        }

                        zip.Add(new StaticBytesDataSource(data), file.Path);
                    }

                    // Generate the "__run.sh" script file that will set execute permissions and
                    // then execute the bundle command.

                    var sb = new StringBuilder();

                    sb.AppendLineLinux("#!/bin/sh");
                    sb.AppendLineLinux();

                    foreach (var file in bundle.Where(f => f.IsExecutable))
                    {
                        if (file.Path.Contains(' '))
                        {
                            sb.AppendLineLinux($"chmod {executePermissions} \"{file.Path}\"");
                        }
                        else
                        {
                            sb.AppendLineLinux($"chmod {executePermissions} {file.Path}");
                        }
                    }

                    sb.AppendLineLinux(FormatCommand(bundle.Command, bundle.Args));

                    zip.Add(new StaticStringDataSource(sb.ToString()), "__run.sh");

                    // Commit the changes to the ZIP stream.

                    zip.CommitUpdate();
                }

                // Upload the ZIP file to a temporary folder.

                var bundleFolder = $"{NodeHostFolders.Exec}/{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss.fff")}";
                var zipPath = LinuxPath.Combine(bundleFolder, "__bundle.zip");

                SudoCommand($"mkdir -p", RunOptions.LogOnErrorOnly, bundleFolder);
                SudoCommand($"chmod 777", RunOptions.LogOnErrorOnly, bundleFolder);

                ms.Position = 0;
                Upload(zipPath, ms, userPermissions: true);

                // Unzip the bundle. 

                RunCommand($"unzip {zipPath} -d {bundleFolder}", RunOptions.LogOnErrorOnly);

                // Make [__run.sh] executable.

                SudoCommand($"chmod {executePermissions}", RunOptions.LogOnErrorOnly, LinuxPath.Combine(bundleFolder, "__run.sh"));

                return bundleFolder;
            }
        }

        /// <summary>
        /// Runs a shell command on the Linux server.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="args">The optional command arguments.</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="DefaultRunOptions"/> when executing the command.
        /// </para>
        /// <para>
        /// You can override this behavior by passing an <see cref="RunOptions"/> to
        /// the <see cref="RunCommand(string, RunOptions, object[])"/> override.
        /// </para>
        /// <note>
        /// Any <c>null</c> arguments will be ignored.
        /// </note>
        /// </remarks>
        public CommandResponse RunCommand(string command, params object[] args)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(command));

            return RunCommand(command, DefaultRunOptions, args);
        }

        /// <summary>
        /// Runs a shell command on the Linux server with <see cref="RunOptions"/>s.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="options">The execution options.</param>
        /// <param name="args">The optional command arguments.</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="options"/> flags control how this command functions.
        /// If <see cref="RunOptions.FaultOnError"/> is set, then commands that return
        /// a non-zero exit code will put the server into the faulted state by setting
        /// <see cref="IsFaulted"/>=<c>true</c>.  This means that <see cref="IsReady"/> will 
        /// always return <c>false</c> afterwards and subsequent calls to <see cref="RunCommand(string, object[])"/>
        /// and <see cref="SudoCommand(string, object[])"/> will be ignored unless 
        /// <see cref="RunOptions.RunWhenFaulted"/> is passed with the future command. 
        /// <see cref="RunOptions.LogOnErrorOnly"/> indicates that command output should
        /// be logged only for non-zero exit codes.
        /// </para>
        /// <note>
        /// Any <c>null</c> arguments will be ignored.
        /// </note>
        /// </remarks>
        public CommandResponse RunCommand(string command, RunOptions options, params object[] args)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(command));

            var startLogged = false;

            command = FormatCommand(command, args);

            if (!string.IsNullOrWhiteSpace(RemotePath) && (options & RunOptions.IgnoreRemotePath) == 0)
            {
                command = $"export PATH={RemotePath} && {command}";
            }

            if ((options & RunOptions.Defaults) != 0)
            {
                options |= DefaultRunOptions;
            }

            var runWhenFaulted = (options & RunOptions.RunWhenFaulted) != 0;
            var logOnErrorOnly = (options & RunOptions.LogOnErrorOnly) != 0 && (options & RunOptions.LogOutput) == 0;
            var faultOnError   = (options & RunOptions.FaultOnError) != 0;
            var binaryOutput   = (options & RunOptions.BinaryOutput) != 0;
            var isClassified   = (options & RunOptions.Classified) != 0;
            var logBundle      = (options & RunOptions.LogBundle) != 0;

            if (IsFaulted && !runWhenFaulted)
            {
                return new CommandResponse()
                {
                    Command        = command,
                    ExitCode       = 1,
                    ProxyIsFaulted = true,
                    ErrorText      = "** proxy is faulted **"
                };
            }

            EnsureSshConnection();

            // Generate the command string we'll log by stripping out the 
            // remote PATH statement, if there is one.

            var commandToLog = command.Replace($"export PATH={RemotePath} && ", string.Empty);

            if (isClassified)
            {
                // Redact everything after the commmand word.

                var posEnd = commandToLog.IndexOf(' ');

                if (posEnd != -1)
                {
                    commandToLog = commandToLog.Substring(0, posEnd + 1) + Redacted;
                }
            }

            if (logBundle)
            {
                startLogged = true;
            }
            else if (!logOnErrorOnly)
            {
                LogLine($"START: {commandToLog}");

                startLogged = true;
            }

            SshCommand          result;
            CommandResponse     commandResult;

            if (binaryOutput)
            {
                // We're going to pipe the standard output of the command to a temporary
                // file on the remote server and then download it.

                var remoteOutputFile = $"{NodeHostFolders.Exec}/{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss.fff")}" + ".output";

                result = sshClient.RunCommand($"{command} 1> {remoteOutputFile}");

                try
                {
                    EnsureScpConnection();

                    using (var ms = new MemoryStream())
                    {
                        commandResult = new CommandResponse()
                        {
                            Command   = command,
                            ExitCode  = result.ExitStatus,
                            ErrorText = result.Error
                        };

                        scpClient.Download(remoteOutputFile, ms);
                        commandResult.OutputBinary = ms.ToArray();
                    }
                }
                finally
                {
                    sshClient.RunCommand($"rm {remoteOutputFile}");
                }
            }
            else
            {
                // Text output.

                result        = sshClient.RunCommand(command);
                commandResult = new CommandResponse()
                {
                    Command    = command,
                    ExitCode   = result.ExitStatus,
                    OutputText = result.Result,
                    ErrorText  = result.Error
                };
            }

            var logEnabled = result.ExitStatus != 0 || !logOnErrorOnly;

            if ((result.ExitStatus != 0 && logOnErrorOnly) || (options & RunOptions.LogOutput) != 0)
            {
                if (!startLogged)
                {
                    LogLine($"START: {commandToLog}");
                }

                if ((options & RunOptions.LogOutput) != 0)
                {
                    if (binaryOutput)
                    {
                        LogLine($"    BINARY OUTPUT [length={commandResult.OutputBinary.Length}]");
                    }
                    else
                    {
                        if (isClassified)
                        {
                            LogLine("    " + Redacted);
                        }
                        else
                        {
                            using (var reader = new StringReader(commandResult.OutputText))
                            {
                                foreach (var line in reader.Lines())
                                {
                                    LogLine("    " + line);
                                }
                            }
                        }
                    }
                }
            }

            if (result.ExitStatus != 0 || !logOnErrorOnly || (options & RunOptions.LogOutput) != 0)
            {
                if (isClassified)
                {
                    LogLine("STDERR");
                    LogLine("    " + Redacted);
                }
                else
                {
                    using (var reader = new StringReader(commandResult.ErrorText))
                    {
                        var extendedWritten = false;

                        foreach (var line in reader.Lines())
                        {
                            if (!extendedWritten)
                            {
                                LogLine("STDERR");
                                extendedWritten = true;
                            }

                            LogLine("    " + line);
                        }
                    }
                }

                if (result.ExitStatus == 0)
                {
                    LogLine("END [OK]");
                }
                else
                {
                    LogLine($"END [ERROR={result.ExitStatus}]");
                }

                if (result.ExitStatus != 0)
                {
                    if (faultOnError)
                    {
                        Status    = $"ERROR[{result.ExitStatus}]";
                        IsFaulted = true;
                    }
                    else
                    {
                        Status = $"WARN[{result.ExitStatus}]";
                    }
                }
            }

            return commandResult;
        }

        /// <summary>
        /// Runs a <see cref="CommandBundle"/> with user permissioins on the remote machine.
        /// </summary>
        /// <param name="bundle">The bundle.</param>
        /// <param name="options">The execution options (defaults to <see cref="RunOptions.Defaults"/>).</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended for situations where one or more files need to be uploaded to a NeonCluster host node 
        /// and then be used when a command is executed.
        /// </para>
        /// <para>
        /// A good example of this is performing a <b>docker stack</b> command on the cluster.  In this case, we need to
        /// upload the DAB file along with any files it references and then we we'll want to execute the the Docker client.
        /// </para>
        /// <para>
        /// To use this class, construct an instance passing the command and arguments to be executed.  The command be 
        /// an absolute reference to an executable in folders such as <b>/bin</b> or <b>/usr/local/bin</b>, an executable
        /// somewhere on the current PATH, or relative to the files unpacked from the bundle.  The current working directory
        /// will be set to the folder where the bundle was unpacked, so you can reference local executables like
        /// <b>./MyExecutable</b>.
        /// </para>
        /// <para>
        /// Once a bundle is constructed, you will add <see cref="CommandFile"/> instances specifying the
        /// file data you want to include.  These include the relative path to the file to be uploaded as well
        /// as its text or binary data.  You may also indicate whether each file is to be marked as executable.
        /// </para>
        /// <note>
        /// <paramref name="options"/> is set to <see cref="RunOptions.Defaults"/> by default.  This means
        /// that the flags specified by <see cref="DefaultRunOptions"/> will be be used.  This is a 
        /// good way to specify a global default for flags like <see cref="RunOptions.FaultOnError"/>.
        /// </note>
        /// <note>
        /// This command requires that the <b>unzip</b> package be installed on the host.
        /// </note>
        /// </remarks>
        public CommandResponse RunCommand(CommandBundle bundle, RunOptions options = RunOptions.Defaults)
        {
            Covenant.Requires<ArgumentNullException>(bundle != null);

            // Write the START log line here so we can log the actual command being
            // executed and then disable this at the lower level, which would have 
            // logged the execution of the "__run.sh" script.

            if ((options & RunOptions.Classified) != 0)
            {
                LogLine($"START-BUNDLE: {Redacted}");
            }
            else
            {
                LogLine($"START-BUNDLE: {bundle}");
            }

            // Upload and extract the bundle and then run the "__run.sh" script.

            var bundleFolder = UploadBundle(bundle, userPermissions: true);
            var result       = RunCommand($"cd {bundleFolder} && ./__run.sh", options | RunOptions.LogBundle);

            // Remove the bundle files.

            RunCommand("rm -rf", RunOptions.RunWhenFaulted, RunOptions.LogOnErrorOnly, bundleFolder);

            return result;
        }

        /// <summary>
        /// Runs a shell command on the Linux server under <b>sudo</b>.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="args">The optional command arguments.</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <note>
        /// <paramref name="command"/> may not include single quotes.  For more complex
        /// command, try uploading and executing a <see cref="CommandBundle"/> instead.
        /// </note>
        /// <para>
        /// This method uses the <see cref="DefaultRunOptions"/> when executing the command.
        /// </para>
        /// <para>
        /// You can override this behavior by passing an <see cref="RunOptions"/> to
        /// the <see cref="RunCommand(string, RunOptions, object[])"/> override.
        /// </para>
        /// <note>
        /// Any <c>null</c> arguments will be ignored.
        /// </note>
        /// </remarks>
        public CommandResponse SudoCommand(string command, params object[] args)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(command));

            return SudoCommand(command, DefaultRunOptions, args);
        }

        /// <summary>
        /// Runs a shell command on the Linux server under <b>sudo</b> with <see cref="RunOptions"/>s.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="options">The execution options.</param>
        /// <param name="args">The optional command arguments.</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <note>
        /// <paramref name="command"/> may not include single quotes.  For more complex
        /// command, try uploading and executing a <see cref="CommandBundle"/> instead.
        /// </note>
        /// <para>
        /// The <paramref name="options"/> flags control how this command functions.
        /// If <see cref="RunOptions.FaultOnError"/> is set, then commands that return
        /// a non-zero exit code will put the server into the faulted state by setting
        /// <see cref="IsFaulted"/>=<c>true</c>.  This means that <see cref="IsReady"/> will 
        /// always return <c>false</c> afterwards and subsequent command executions will be 
        /// ignored unless  <see cref="RunOptions.RunWhenFaulted"/> is specified for the 
        /// future command.
        /// </para>
        /// <para>
        /// <see cref="RunOptions.LogOnErrorOnly"/> indicates that command output should
        /// be logged only for non-zero exit codes.
        /// </para>
        /// <note>
        /// Any <c>null</c> arguments will be ignored.
        /// </note>
        /// </remarks>
        public CommandResponse SudoCommand(string command, RunOptions options, params object[] args)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(command));

            if (command.Contains('\''))
            {
                throw new ArgumentException($"[{nameof(SudoCommand)}(command,...)] does not support single quotes.  Upload and run a [{nameof(CommandBundle)}] instead.");
            }

            command = FormatCommand(command, args);

            if (!string.IsNullOrWhiteSpace(RemotePath) && (options & RunOptions.IgnoreRemotePath) == 0)
            {
                command = $"export PATH={RemotePath} && {command}";
            }

            // $todo(jeff.lill):
            //
            // Should I be escaping any single quotes in the command?  I'm not
            // sure that this is possible.

            return RunCommand($"sudo bash -c '{command}'", options | RunOptions.IgnoreRemotePath);
        }

        /// <summary>
        /// Runs a <see cref="CommandBundle"/> under <b>sudo</b> on the remote machine.
        /// </summary>
        /// <param name="bundle">The bundle.</param>
        /// <param name="options">The execution options (defaults to <see cref="RunOptions.Defaults"/>).</param>
        /// <returns>The <see cref="CommandResponse"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended for situations where one or more files need to be uploaded to a NeonCluster host node 
        /// and then be used when a command is executed.
        /// </para>
        /// <para>
        /// A good example of this is performing a <b>docker stack</b> command on the cluster.  In this case, we need to
        /// upload the DAB file along with any files it references and then we we'll want to execute the the Docker 
        /// client.
        /// </para>
        /// <para>
        /// To use this class, construct an instance passing the command and arguments to be executed.  The command be 
        /// an absolute reference to an executable in folders such as <b>/bin</b> or <b>/usr/local/bin</b>, an executable
        /// somewhere on the current PATH, or relative to the files unpacked from the bundle.  The current working directory
        /// will be set to the folder where the bundle was unpacked, so you can reference local executables like
        /// <b>./MyExecutable</b>.
        /// </para>
        /// <para>
        /// Once a bundle is constructed, you will add <see cref="CommandFile"/> instances specifying the
        /// file data you want to include.  These include the relative path to the file to be uploaded as well
        /// as its text or binary data.  You may also indicate whether each file is to be marked as executable.
        /// </para>
        /// <note>
        /// <paramref name="options"/> is set to <see cref="RunOptions.Defaults"/> by default.  This means
        /// that the flags specified by <see cref="DefaultRunOptions"/> will be be used.  This is a 
        /// good way to specify a global default for flags like <see cref="RunOptions.FaultOnError"/>.
        /// </note>
        /// <note>
        /// This command requires that the <b>unzip</b> package be installed on the host.
        /// </note>
        /// <note>
        /// Any <c>null</c> arguments will be ignored.
        /// </note>
        /// </remarks>
        public CommandResponse SudoCommand(CommandBundle bundle, RunOptions options = RunOptions.Defaults)
        {
            Covenant.Requires<ArgumentNullException>(bundle != null);

            // Write the START log line here so we can log the actual command being
            // executed and then disable this at the lower level, which would have 
            // logged the execution of the "__run.sh" script.

            if ((options & RunOptions.Classified) != 0)
            {
                LogLine($"START-BUNDLE: {Redacted}");
            }
            else
            {
                LogLine($"START-BUNDLE: {bundle}");
            }

            // Upload and extract the bundle and then run the "__run.sh" script.

            var bundleFolder = UploadBundle(bundle, userPermissions: false);
            var result       = SudoCommand($"cd {bundleFolder} && /bin/bash ./__run.sh", options | RunOptions.LogBundle);

            // Remove the bundle files.

            SudoCommand("rm -rf", RunOptions.LogOnErrorOnly, bundleFolder);

            return result;
        }

        /// <summary>
        /// Runs a Docker command on the node under <b>sudo</b> while attempting to handle
        /// transient errors.
        /// </summary>
        /// <param name="command">The Linux command.</param>
        /// <param name="args">The command arguments.</param>
        /// <remarks>
        /// <para>
        /// This method attempts to retry transient Docker client errors (e.g. when an
        /// image pull fails for some reason).  Using this will be more reliable than
        /// executing the command directly, especially on large clusters.
        /// </para>
        /// <note>
        /// You'll need to passes the full Docker command, including the leading
        /// <b>docker</b> client program name.
        /// </note>
        /// </remarks>
        public CommandResponse DockerCommand(string command, params object[] args)
        {
            // $todo(jeff.lill): Hardcoding the transient handling for now.

            CommandResponse response    = null;
            int             attempt     = 0;
            int             maxAttempts = 10;
            TimeSpan        delay       = TimeSpan.FromSeconds(15);
            string          orgStatus   = Status;

            while (attempt++ < maxAttempts)
            {
                response = SudoCommand(command, RunOptions.LogOutput, args);

                if (response.ExitCode == 0)
                {
                    return response;
                }

                // Simple transitent error detection.

                if (response.ErrorText.Contains("i/o timeout") || response.ErrorText.Contains("Client.Timeout"))
                {
                    Status = $"[retry:{attempt}/{maxAttempts}]: {orgStatus}";
                    Log($"*** Waiting [{delay}] before retrying after a possible transient error.");

                    Thread.Sleep(delay);
                }
                else
                {
                    // Looks like a hard error.

                    Fault();
                    return response;
                }
            }

            Log($"*** Operation failed after retrying [{maxAttempts}] times.");
            Fault();

            return response;
        }

        /// <summary>
        /// Verifies a TLS/SSL certificate.
        /// </summary>
        /// <param name="name">The certificate name (included in errors).</param>
        /// <param name="certificate">The certificate being tested or <c>null</c>.</param>
        /// <param name="hostName">The host name to be secured by the certificate.</param>
        /// <returns>The command result.</returns>
        /// <remarks>
        /// You may pass <paramref name="certificate"/> as <c>null</c> to indicate that no 
        /// checking is to be performed as a convienence.
        /// </remarks>
        public CommandResponse VerifyCertificate(string name, TlsCertificate certificate, string hostName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));

            if (certificate == null)
            {
                return new CommandResponse() { ExitCode = 0 };
            }

            Status = $"verifying: [{name}] certificate";

            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentException($"No host name is specified for the [{name}] certificate test.");
            }

            // Verify that the private key looks reasonable.

            if (!certificate.Key.StartsWith("-----BEGIN PRIVATE KEY-----"))
            {
                throw new FormatException($"The [{name}] certificate's private key is not PEM encoded.");
            }

            // Verify the certificate.

            if (!certificate.Cert.StartsWith("-----BEGIN CERTIFICATE-----"))
            {
                throw new ArgumentException($"The [{name}] certificate is not PEM encoded.");
            }

            // We're going to split the certificate into two files, the issued
            // certificate and the certificate authority's certificate chain
            // (AKA the CA bundle).
            //
            // Then we're going to upload these to [/tmp/cert.crt] and [/tmp/cert.ca]
            // and then use the [openssl] command to verify it.

            var pos = certificate.Cert.IndexOf("-----END CERTIFICATE-----");

            if (pos == -1)
            {
                throw new ArgumentNullException($"The [{name}] certificate is not formatted properly.");
            }

            pos = certificate.Cert.IndexOf("-----BEGIN CERTIFICATE-----", pos);

            var issuedCert = certificate.Cert.Substring(0, pos);
            var caBundle   = certificate.Cert.Substring(pos);

            try
            {
                UploadText("/tmp/cert.crt", issuedCert);
                UploadText("/tmp/cert.ca", caBundle);

                return SudoCommand(
                    "openssl verify",
                    RunOptions.FaultOnError,
                    "-verify_hostname", hostName,
                    "-purpose", "sslserver",
                    "-CAfile", "/tmp/cert.ca",
                    "/tmp/cert.crt");
            }
            finally
            {
                SudoCommand("rm -f /tmp/cert.*", RunOptions.LogOnErrorOnly);
            }
        }

        /// <summary>
        /// Creates an interactive shell.
        /// </summary>
        /// <returns>A <see cref="ShellStream"/>.</returns>
        public ShellStream CreateShell()
        {
            EnsureSshConnection();

            return sshClient.CreateShellStream("dumb", 80, 24, 800, 600, 1024);
        }

        /// <summary>
        /// Creates an interactive shell for running with <b>sudo</b> permissions. 
        /// </summary>
        /// <returns>A <see cref="ShellStream"/>.</returns>
        public ShellStream CreateSudoShell()
        {
            var shell = CreateShell();

            shell.WriteLine("sudo");

            return shell;
        }
    }
}
