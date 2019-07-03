//-----------------------------------------------------------------------------
// FILE:	    NeonClusterHelpers.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Consul;
using Newtonsoft.Json;

using Neon.Stack.Common;
using Neon.Stack.Diagnostics;

namespace Neon.Cluster
{
    /// <summary>
    /// NeonCluster related utilties.
    /// </summary>
    public static class NeonClusterHelper
    {
        private static ILog                         log = LogManager.GetLogger(typeof(NeonClusterHelper));
        private static Dictionary<string, string>   secrets;

#if NETCORE
        /// <summary>
        /// Encrypts a file or directory when supported by the underlying operating system
        /// and file system.  Currently, this only works on non-HOME versions of Windows
        /// and NTFS file systems.  This fails silently.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns><c>true</c> if the operation was successful.</returns>
        private static bool EncryptFile(string path)
        {
            // This is a NOP for non-Windows environments.  The assumption is that
            // Linux/OSX is encrypting user home directories.

            return false;
        }
#else
        private static class Windows
        {
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EncryptFile(string filename);
        }

        /// <summary>
        /// Encrypts a file or directory when supported by the underlying operating system
        /// and file system.  Currently, this only works on non-HOME versions of Windows
        /// and NTFS file systems.  This fails silently.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns><c>true</c> if the operation was successful.</returns>
        private static bool EncryptFile(string path)
        {
            return Windows.EncryptFile(path);
        }
#endif

        /// <summary>
        /// Returns the path the folder containing various cluster secrets.
        /// </summary>
        /// <returns>The folder path.</returns>
        /// <remarks>
        /// <note>
        /// This folder contains sensitive information and will be encrypted with the user's
        /// Windows credentials (if possible).
        /// </note>
        /// </remarks>
        public static string GetClusterRootFolder()
        {
            if (NeonHelper.IsWindows)
            {
                var path = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Neon Research", "neon-cluster");

                Directory.CreateDirectory(path);

                try
                {
                    EncryptFile(path);
                }
                catch
                {
                    // Encryption is not available on all platforms (e.g. Windows Home, or non-NTFS
                    // file systems).  The secrets won't be encrypted for these situations.
                }

                return path;
            }
            else
            {
                // $todo(jeff.lill): Implement this.

                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the path the folder containing secrets for the known clusters, creating
        /// the folder if it doesn't already exist.
        /// </summary>
        /// <returns>The folder path.</returns>
        /// <remarks>
        /// <para>
        /// This folder will exist on developer/operator workstations that have used the <b>neon.exe</b>
        /// tool to deploy and manage NeonClusters.  Each known cluster will have a JSON file named
        /// <b><i>cluster-name</i>.json</b> holding the serialized <see cref="Cluster.ClusterSecrets"/> for
        /// the cluster.
        /// </para>
        /// <para>
        /// The <b>.current</b> file (if present) specifies the name of the cluster to be considered
        /// to be currently logged in.
        /// </para>
        /// </remarks>
        public static string GetClusterSecretsFolder()
        {
            if (NeonHelper.IsWindows)
            {
                var path = Path.Combine(GetClusterRootFolder(), "clusters");

                Directory.CreateDirectory(path);

                return path;
            }
            else
            {
                // $todo(jeff.lill): Implement this.

                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Loads the information for a cluster being managed, performing any necessary decryption.
        /// </summary>
        /// <param name="clusterName">The name of the target cluster.</param>
        /// <returns>The <see cref="Cluster.ClusterSecrets"/>.</returns>
        public static ClusterSecrets LoadClusterSecrets(string clusterName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(clusterName));

            return NeonHelper.JsonDeserialize<ClusterSecrets>(File.ReadAllText(Path.Combine(GetClusterSecretsFolder(), $"{clusterName}.json")));
        }

        /// <summary>
        /// Validates a certificate name and returns the full path of its Vault key.
        /// </summary>
        /// <param name="name">The certificate name.</param>
        /// <returns>The fully qualified certificate key.</returns>
        /// <exception cref="ArgumentException">Thrown if the certificate name is not valid.</exception>
        /// <remarks>
        /// Reports and exits the application for invalid certificate names.
        /// </remarks>
        public static string GetVaultCertificateKey(string name)
        {
            if (!ClusterDefinition.IsValidName(name))
            {
                throw new ArgumentException($"[{name}] is not a valid certificate name.  Only letters, numbers, periods, dashes, and underscores are allowed.");
            }

            return "neon-secret/cert/" + name;
        }

        /// <summary>
        /// Returns the cluster's Vault URI.
        /// </summary>
        public static Uri VaultUri
        {
            get { return new Uri(Environment.GetEnvironmentVariable("VAULT_ADDR")); }
        }

        /// <summary>
        /// Returns the cluster's Consul URI.
        /// </summary>
        public static Uri ConsulUri
        {
            get { return new Uri(Environment.GetEnvironmentVariable("CONSUL_HTTP_FULLADDR")); }
        }

        /// <summary>
        /// Indicates whether the application is running outside of a Docker container
        /// but we're going to try to simulate the environment such that the application
        /// believe it is running in a container within a Docker cluster.  See 
        /// <see cref="ConnectCluster(DebugSecrets, string)"/> for more information.
        /// </summary>
        public static bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Returns the <see cref="Cluster.ClusterSecrets"/> for the current cluster if we're running in debug mode. 
        /// </summary>
        public static ClusterSecrets ClusterSecrets { get; private set; } = null;

        /// <summary>
        /// Returns the <see cref="ClusterProxy"/> for the current cluster if we're running in debug mode.
        /// </summary>
        public static ClusterProxy Cluster { get; private set; } = null;

        /// <summary>
        /// Attempts to simulate running the current application within a NeonCluster
        /// container for external tools as well as for development and debugging purposes.
        /// </summary>
        /// <param name="secrets">Optional emulated Docker secrets.</param>
        /// <param name="clusterName">
        /// Name of the target cluster or <c>null</c> to read this from the
        /// <b>NR_DEBUG_CLUSTER</b> environment variable.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if a cluster is already connected.</exception>
        /// <remarks>
        /// <note>
        /// This method requres elevated administrative permissions for the local operating
        /// system.
        /// </note>
        /// <note>
        /// Take care to call <see cref="DisconnectCluster()"/> just before your application
        /// exits to reset any temporary settings like the DNS resolver <b>hosts</b> file.
        /// </note>
        /// <para>
        /// Pass <paramref name="clusterName"/> as the name of the deployed cluster to be targeted
        /// or <c>null</c> to read this from the <b>NR_DEBUG_CLUSTER</b> environment variable.
        /// This name will be used to load the cluster secrets from the local workstation from
        /// their standard location (as managed by the <b>neon.exe</b> tool).
        /// </para>
        /// <note>
        /// This method currently simulates running the application on a cluster manager 
        /// node.  In the future, we may provide a way to specific a particular node.
        /// </note>
        /// <para>
        /// In an ideal world, Microsoft/Docker would provide a way to deploy, run and
        /// debug applications into an existing Docker cluster as a container or swarm
        /// mode service.  At this time, there are baby steps in this direction: it's
        /// possible to F5 an application into a standalone container but this method
        /// currently supports running the application directly on Windows while trying
        /// to emulate some of the cluster environment.  Eventually, it will be possible
        /// to do the same in a local Docker container.
        /// </para>
        /// <para>
        /// This method provides a somewhat primitive simulation of running within a
        /// cluster by:
        /// </para>
        /// <list type="bullet">
        ///     <item>
        ///     Simulating the presence of a mounted and executed <b>/etc/neoncluster/env-host</b> script.
        ///     </item>
        ///     <item>
        ///     Simulated mounted Docker secrets.
        ///     </item>
        ///     <item>
        ///     Temporarily modifying the local <b>hosts</b> file to add host entries
        ///     for local services like Vault and Consul.
        ///     </item>
        ///     <item>
        ///     Emulating Docker secret delivery specified using and <paramref name="secrets"/> 
        ///     and using <see cref="GetSecret(string)"/>.
        ///     </item>
        /// </list>
        /// <note>
        /// This is also useful for external tools that are not executed on a cluster node
        /// (such as <b>neon.exe</b>).  For example, class configures the local <n>hosts</n>
        /// file such that we'll be able to access the Cluster Vault and Consul servers over
        /// TLS.
        /// </note>
        /// <para>
        /// Pass a <see cref="DebugSecrets"/> instance as <paramref name="secrets"/> to emulate 
        /// the Docker secrets feature.  <see cref="DebugSecrets"/> may specify secrets as simple
        /// name/value pairs or may specify more complex Vault or Consul credentials.
        /// </para>
        /// <note>
        /// Applications may wish to use <see cref="NeonHelper.IsDevWorkstation"/> to detect when
        /// the application is running outside of a production environment and call this method
        /// early during application initialization.  <see cref="NeonHelper.IsDevWorkstation"/>
        /// uses the presence of the <b>DEV_WORKSTATION</b> environment variable to determine 
        /// this.
        /// </note>
        /// </remarks>
        public static void ConnectCluster(DebugSecrets secrets = null, string clusterName = null)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Already connected to a cluster.");
            }

            // Load the cluster secrets.

            if (string.IsNullOrEmpty(clusterName))
            {
                clusterName = Environment.GetEnvironmentVariable("NR_DEBUG_CLUSTER");

                if (string.IsNullOrEmpty(clusterName))
                {
                    throw new ArgumentException("A valid cluster name must be passed as a parameter or be available in the NR_DEBUG_CLUSTER environment variable.");
                }
            }

            log.Info(() => $"Emulating connection to cluster [{clusterName}].");

            if (!File.Exists(Path.Combine(GetClusterSecretsFolder(), $"{clusterName}.json")))
            {
                throw new ArgumentException($"Secrets for the cluster [{clusterName}] could not be located.  Use the [neon.exe] tool to create or add this cluster.");
            }

            ClusterSecrets = LoadClusterSecrets(clusterName);

            ConnectCluster(
                new Cluster.ClusterProxy(ClusterSecrets,
                    (host, name) =>
                    {
                        var proxy = new NodeProxy<NodeDefinition>(name ?? host, host, ClusterSecrets.GetSshCredentials(), null);

                        proxy.RemotePath += $":{NodeHostFolders.Setup}";
                        proxy.RemotePath += $":{NodeHostFolders.Tools}";

                        return proxy;
                    }));

            // Support emulated secrets too.

            secrets?.Realize(Cluster, ClusterSecrets);

            NeonClusterHelper.secrets = secrets;
        }

        /// <summary>
        /// Connects to a cluster using a <see cref="ClusterProxy"/>.  Note that this version does not
        /// fully initialize the <see cref="ClusterSecrets"/> property.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        /// <exception cref="InvalidOperationException">Thrown if a cluster is already connected.</exception>
        public static void ConnectCluster(ClusterProxy cluster)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);

            if (IsConnected)
            {
                throw new InvalidOperationException("Already connected to a cluster.");
            }

            IsConnected = true;

            // It looks like .NET 4.5 defaults to just SSL 3.0.  This prevents us from connecting
            // to more advanced services (like HashiCorp Vault) that disable older insecure encryption
            // protocols.  We're going to enable all known protocols here.
#if !NETCORE
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
            Cluster = cluster;

            if (ClusterSecrets == null)
            {
                ClusterSecrets =
                    new ClusterSecrets()
                    {
                        Definition = cluster.Definition
                    };
            }

            // Initialize some properties.

            var clusterDefinition = Cluster.Definition;
            var node              = Cluster.Manager;

            // Simulate the environment variables initialized by a mounted [env-host] script.

            Environment.SetEnvironmentVariable("NEON_CLUSTER", clusterDefinition.Name);
            Environment.SetEnvironmentVariable("NEON_DATACENTER", clusterDefinition.Datacenter);
            Environment.SetEnvironmentVariable("NEON_ENVIRONMENT", clusterDefinition.Environment.ToString());
            Environment.SetEnvironmentVariable("NEON_NODE_NAME", node.Name);
            Environment.SetEnvironmentVariable("NEON_NODE_ROLE", node.Metadata.Role);
            Environment.SetEnvironmentVariable("NEON_NODE_DNSNAME", node.DnsName);
            Environment.SetEnvironmentVariable("NEON_NODE_IP", node.Metadata.Address.ToString());
            Environment.SetEnvironmentVariable("NEON_NODE_SSD", node.Metadata.Labels.StorageSSD ? "true" : "false");
            Environment.SetEnvironmentVariable("NEON_APT_CACHE", clusterDefinition.PackageCache);
            Environment.SetEnvironmentVariable("VAULT_ADDR", $"{clusterDefinition.Vault.GetDirectUri(Cluster.Manager.Name)}");
            Environment.SetEnvironmentVariable("CONSUL_HTTP_ADDR", $"{NeonHosts.Consul}:{clusterDefinition.Consul.Port}");
            Environment.SetEnvironmentVariable("CONSUL_HTTP_FULLADDR", $"http://{NeonHosts.Consul}:{clusterDefinition.Consul.Port}");

            // Modify the DNS resolver hosts file.

            var hosts = new Dictionary<string, IPAddress>();

            hosts.Add(NeonHosts.Consul, node.Metadata.Address);
            hosts.Add(NeonHosts.Vault, node.Metadata.Address);
            hosts.Add($"{node.Name}.{NeonHosts.Vault}", node.Metadata.Address);
            hosts.Add(NeonHosts.LogEsData, node.Metadata.Address);

            NeonHelper.ModifyHostsFile(hosts);

            NeonClusterHelper.secrets = new Dictionary<string, string>();
        }

        /// <summary>
        /// Resets any temporary cponfigurations made by <see cref="ConnectCluster(DebugSecrets, string)"/>
        /// such as the modifications to the DNS resolver <b>hosts</b> file.  This should be called just
        /// before the application exits.
        /// </summary>
        public static void DisconnectCluster()
        {
            if (!IsConnected)
            {
                return;
            }

            IsConnected = false;

            log.Info("Emulating cluster disconnect.");

            NeonHelper.ModifyHostsFile();
        }

        /// <summary>
        /// Returns the value of a named secret.
        /// </summary>
        /// <param name="name">The secret name.</param>
        /// <returns>The secret value or <c>null</c> if the secret doesn't exist.</returns>
        /// <remarks>
        /// <para>
        /// This method can be used to retrieve a secret provisioned to a container via the
        /// Docker secrets feature or a secret provided to <see cref="ConnectCluster(DebugSecrets, string)"/> 
        /// when we're emulating running the application as a cluster container.
        /// </para>
        /// <para>
        /// Docker provisions secrets by mounting a <b>tmpfs</b> file system at <b>/var/run/secrets</b>
        /// and writing the secrets there as text files with the file name identifying the secret.
        /// When the application is not running in debug mode, this method simply attempts to read
        /// the requested secret from the named file in this folder.
        /// </para>
        /// </remarks>
        public static string GetSecret(string name)
        {
            if (IsConnected)
            {
                if (secrets == null)
                {
                    return null;
                }

                string secret;

                if (secrets.TryGetValue(name, out secret))
                {
                    return secret;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    return File.ReadAllText(Path.Combine(NodeHostFolders.DockerSecrets, name));
                }
                catch (IOException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Opens the cluster Consul service.
        /// </summary>
        /// <returns>The <see cref="ConsulClient"/>.</returns>
        public static ConsulClient OpenConsul()
        {
            return new ConsulClient(
                config =>
                {
                    config.Address = ConsulUri;
                });
        }

        /// <summary>
        /// Opens the cluster Vault secret management service using a Vault token.
        /// </summary>
        /// <param name="token">The Vault token.</param>
        /// <returns>The opened <see cref="VaultClient"/>.</returns>
        public static VaultClient OpenVault(string token)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(token));

            return VaultClient.OpenWithToken(VaultUri, token);
        }

        /// <summary>
        /// Opens the cluster Vault secret management service using the specified credentials.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns>The opened <see cref="VaultClient"/>.</returns>
        public static VaultClient OpenVault(ClusterCredentials credentials)
        {
            Covenant.Requires<ArgumentNullException>(credentials != null);

            credentials.Validate();

            switch (credentials.Type)
            {
                case ClusterCredentialsType.VaultAppRole:

                    return VaultClient.OpenWithAppRole(VaultUri, credentials.VaultRoleId, credentials.VaultSecretId);

                case ClusterCredentialsType.VaultToken:

                    return VaultClient.OpenWithToken(VaultUri, credentials.VaultToken);

                default:

                    throw new NotSupportedException($"Vault cannot be opened using [{credentials.Type}] credentisls.");
            }
        }

        /// <summary>
        /// Returns the HAProxy log format string for the named proxy.
        /// </summary>
        /// <param name="proxyName">The proxy Docker service name.</param>
        /// <param name="tcp">Pass <c>true</c> for TCP proxies, <c>false</c> for HTTP.</param>
        /// <returns>The HAProxy log format string.</returns>
        /// <remarks>
        /// <para>
        /// The log format consists fields separated by the <b>» (0xbb)</b>character.  None of the values 
        /// should include this so quoting or escaping are not required.  The tables below describe the 
        /// fields and include the HAProxy log format codes.  See the
        /// <a href="http://cbonte.github.io/haproxy-dconv/1.7/configuration.html#8.2.4">HAPoxy Documentation</a> 
        /// for more information.
        /// </para>
        /// <para>
        /// The first field is hardcoded to be <b>traffic</b> to so the log pipeline can distinguish between
        /// proxy traffic and status messages.  The second field specifies the type of record and the format
        /// version number.  This will be <b>tcp-v1</b> for TCP traffic and <b>http-v1</b> for HTTP traffic.
        /// </para>
        /// <para>
        /// The traffic specific fields follow the <b>traffic</b> and <b>type/version</b> fields.  Note that
        /// the HTTP format is a strict superset of the TCP format, with the additional HTTP related fields 
        /// appearing after the common TCP fields.
        /// </para>
        /// <para><b>Common TCP and HTTP Fields</b></para>
        /// <para>
        /// Here are the TCP log fields in order they appear in the message.
        /// </para>
        /// <list type="table">
        /// <item>
        ///     <term><b>Service</b></term>
        ///     <description>
        ///     The proxy service name (e.g. <b>neon-proxy-public</b>).
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Timestamp (TCP=%t, HTTP=%tr)</b></term>
        ///     <description>
        ///     Event time, for TCP this is usually when the connection is closed.  For HTTP, this
        ///     is the request time.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Client IP (%ci)</b></term>
        ///     <description>
        ///     Client IP address.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Backend Name (%b)</b></term>
        ///     <description>
        ///     Identifies the proxy backend.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Server Name (%s)</b></term>
        ///     <description>
        ///     Identifies the backend server name.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Server IP (%si)</b></term>
        ///     <description>
        ///     The backend server IP address
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Server Port (%sp)</b></term>
        ///     <description>
        ///     The backend server port.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>TLS/SSL Version (%sslv)</b></term>
        ///     <description>
        ///     The TLS/SSL version or a dash (<b>-</b>) if the connection is not secured.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>TLS/SSL Cypher (%sslc)</b></term>
        ///     <description>
        ///     The TLS/SSL cypher (<b>-</b>) if the connection is not secured.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Bytes Received (%U)</b></term>
        ///     <description>
        ///     Bytes send from client to proxy.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Bytes Sent (%B)</b></term>
        ///     <description>
        ///     Bytes sent from proxy to client.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Queue Time (%Tw)</b></term>
        ///     <description>
        ///     Milliseconds waiting in queues for a connection slot or -1 if the connection
        ///     was terminated before reaching a queue.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Connect Time (%Tc)</b></term>
        ///     <description>
        ///     Milliseconds waiting to establish a connection between the backend and the server 
        ///     or -1 if a connection was never established.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Session Time (%Tt)</b></term>
        ///     <description>
        ///    Total milliseconds between the time the proxy accepted the connection and the
        ///    time the connection was closed.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Termination Flags (%ts)</b></term>
        ///     <description>
        ///     Describes how the connection was terminated.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Proxy Connections (%ac)</b></term>
        ///     <description>
        ///     Number of active connections currently managed by the proxy.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Frontend Connections (%fc)</b></term>
        ///     <description>
        ///     Number of active proxy frontend connections.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Backend Connections (%bc)</b></term>
        ///     <description>
        ///     Number of active proxy backend connections.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Server Connections (%sc)</b></term>
        ///     <description>
        ///     Number of active proxy server connections
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Retries (%rc)</b></term>
        ///     <description>
        ///     Number of retries.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Server Queue (%sq)</b></term>
        ///     <description>
        ///     Server queue length.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Backend Queue (%bq)</b></term>
        ///     <description>
        ///     Backend queue length.
        ///     </description>
        /// </item>
        /// </list>
        /// <para><b>The Extended HTTP Related Fields</b></para>
        /// <para>
        /// Here are the HTTP log fields in the order they appear in the message.
        /// </para>
        /// <list type="table">
        /// <item>
        ///     <term><b>Activity ID (%ID)</b></term>
        ///     <description>
        ///     The globally unique request activity ID (from the <b>X-Activity-ID header</b>).
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Idle Time (%Ti)</b></term>
        ///     <description>
        ///     Milliseconds waiting idle before the first byte of the HTTP request was received.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Request Time (%TR)</b></term>
        ///     <description>
        ///     Milliseconds to receive the full HTTP request from the first byte.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Response Time (%Tr)</b></term>
        ///     <description>
        ///     Milliseconds the server took to process the request and return the full status
        ///     line and HTTP headers.  This does not include the network overhead for delivering
        ///     the data.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Active Time (%Ta)</b></term>
        ///     <description>
        ///     Milliseconds from the <b>Request Time</b> until the response was transmitted.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>HTTP Method (%HM)</b></term>
        ///     <description>
        ///     HTTP request method.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>URI (%HP)</b></term>
        ///     <description>
        ///     Partial HTTP relative URI that excludes query strings.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>URI Query string (%HU)</b></term>
        ///     <description>
        ///     HTTP URI query string.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>HTTP Version (%HV)</b></term>
        ///     <description>
        ///     The HTTP version (e.g. <b>HTTP/1.1</b>).
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>HTTP Status (%ST)</b></term>
        ///     <description>
        ///     The HTTP response status code.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Request Headers (%hr)</b></term>
        ///     <description>
        ///     The captured HTTP request headers within <b>{...}</b> and separated by pipe 
        ///     (|) characters.  Currently, only the the <b>Host</b> and <b>User-Agent</b> 
        ///     headers are captured, in that order.
        ///     </description>
        /// </item>
        /// </list>
        /// </remarks>
        public static string GetProxyLogFormat(string proxyName, bool tcp)
        {
            if (tcp)
            {
                return $"traffic»tcp-v1»{proxyName}»%t»%ci»%b»%s»%si»%sp»%sslv»%sslc»%U»%B»%Tw»%Tc»%Tt»%ts»%ac»%fc»%bc»%sc»%rc»%sq»%bq";
            }
            else
            {
               return $"traffic»http-v1»{proxyName}»%tr»%ci»%b»%s»%si»%sp»%sslv»%sslc»%U»%B»%Tw»%Tc»%Tt»%ts»%ac»%fc»%bc»%sc»%rc»%sq»%bq»%ID»%Ti»%TR»%Tr»%Ta»%HM»%HP»%HQ»%HV»%ST»%hr";
            }
        }
    }
}
