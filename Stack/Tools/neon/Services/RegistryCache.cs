//-----------------------------------------------------------------------------
// FILE:	    RegistryCache.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.IO;
using Neon.Stack.Net;
using Neon.Stack.Retry;
using Neon.Stack.Time;

// $todo(jeff.lill): This could be parallelized better for large clusters.

namespace NeonCluster
{
    /// <summary>
    /// Handles the provisioning of cluster Docker Registry cache services.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RegistryCache
    {
        private ClusterProxy cluster;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        public RegistryCache(ClusterProxy cluster)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);

            this.cluster = cluster;
        }

        /// <summary>
        /// Configures the cluster registry cache related services.
        /// </summary>
        public void Configure()
        {
            if (!cluster.Definition.Docker.RegistryCache)
            {
                return;
            }

            //---------------------------------------------
            // Generate self-signed certificates and private keys for the registry
            // caches to be hosted on each of the managers.  Note that these
            // certs will expire in about 1,000 years, so they're effectively
            // permanent.

            var managerNameToCert = new Dictionary<string, string>();
            var managerNameToKey  = new Dictionary<string, string>();

            foreach (var manager in cluster.Definition.SortedManagers)
            {
                var certificate = TlsCertificate.CreateSelfSigned(GetCacheHost(manager), 4096, 365000);

                try
                {
                    managerNameToCert.Add(manager.Name, certificate.Cert);
                    managerNameToKey.Add(manager.Name, certificate.Key);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** ERROR: Could not generate registry cache TLS certificate.");
                    Console.WriteLine(e.Message);
                    Program.Exit(1);
                }
            }

            //---------------------------------------------
            // Configure the nodes.

            var steps = new ConfigStepList();

            // Add the [<manager>.neon-registry-cache.cluster] DNS mappings
            // to the [/etc/hosts] file on all nodes.

            var sb = new StringBuilder();

            sb.AppendLineLinux();
            sb.AppendLineLinux("# Map the registry cache instances running on the managers.");
            sb.AppendLineLinux();

            if (cluster.Definition.Docker.RegistryCache)
            {
                foreach (var manager in cluster.Definition.SortedManagers)
                {
                    sb.AppendLineLinux($"{manager.Address} {GetCacheHost(manager)}");
                }
            }
            
            var hostMappings = sb.ToString();

            foreach (var node in cluster.Nodes)
            {
                var bundleStep = CommandStep.CreateSudo(node.Name, "cat mappings.txt >> /etc/hosts");

                bundleStep.AddFile("mappings.txt", hostMappings);
                steps.Add(bundleStep);
            }

            // Upload each cache's certificate and private key up to
            // its manager to [cache.crt] and [cache.key] at
            // [/etc/neon-registry-cache/].  This directory will be 
            // mapped into the cache container.
            //
            // Then create the cache's data volume and start the manager's 
            // Registry cache container.

            foreach (var manager in cluster.Definition.SortedManagers)
            {
                // Copy the cert and key.

                var copyCommand = CommandStep.CreateSudo(manager.Name, "./registry-cache-server-certs.sh");
                var sbScript    = new StringBuilder();

                sbScript.AppendLine("mkdir -p /etc/neon-registry-cache");

                copyCommand.AddFile($"cache.crt", managerNameToCert[manager.Name]);
                copyCommand.AddFile($"cache.key", managerNameToKey[manager.Name]);

                sbScript.AppendLine($"cp cache.crt /etc/neon-registry-cache/cache.crt");
                sbScript.AppendLine($"cp cache.key /etc/neon-registry-cache/cache.key");

                copyCommand.AddFile("registry-cache-server-certs.sh", sbScript.ToString(), isExecutable: true);

                steps.Add(copyCommand);

                // Create the data volume.

                steps.Add(new VolumeCreateStep(manager.Name, "neon-registry-cache"));

                // Start the Registry cache.

                // $todo(jeff.lill): Set LOG_LEVEL=info and remove [--log-driver].

                var runCommand = CommandStep.CreateSudo(manager.Name,
                    "docker run",
                    "--name", "neon-registry-cache",
                    "--detach",
                    "--restart", "always",
                    "--publish", $"{NeonHostPorts.RegistryCache}:5000",
                    "--volume", "/etc/neon-registry-cache:/etc/neon-registry-cache:ro",
                    "--volume", "neon-registry-cache:/var/lib/neon-registry-cache",
                    "--env", $"HOSTNAME={manager.Name}.{NeonHosts.RegistryCache}",
                    "--env", $"REGISTRY={cluster.Definition.Docker.Registry}",
                    "--env", $"USERNAME={cluster.Definition.Docker.RegistryUserName}",
                    "--env", $"PASSWORD={cluster.Definition.Docker.RegistryPassword}",
                    "--env", "LOG_LEVEL=debug",
                    "--log-driver", "json-file",
                    "neoncluster/neon-registry-cache");

                steps.Add(runCommand);
            }

            // Upload the cache certificates to every cluster node at:
            //
            //      /etc/docker/certs.d/<hostname>:{NeonHostPorts.RegistryCache}/ca.crt
            //      /usr/local/share/ca-certificates/<hostname>.crt
            //
            // and then have Linux update its known certificates.

            foreach (var node in cluster.Definition.SortedNodes)
            {
                var copyCommand = CommandStep.CreateSudo(node.Name, "./registry-cache-client-certs.sh");
                var sbScript    = new StringBuilder();

                sbScript.AppendLine("mkdir -p /etc/docker/certs.d");
                sbScript.AppendLine("mkdir -p /usr/local/share/ca-certificates");

                foreach (var manager in cluster.Definition.SortedManagers)
                {
                    copyCommand.AddFile($"{manager.Name}.crt", managerNameToCert[manager.Name]);

                    var cacheHostName = GetCacheHost(manager);

                    sbScript.AppendLine($"mkdir -p /etc/docker/certs.d/{cacheHostName}:{NeonHostPorts.RegistryCache}");
                    sbScript.AppendLine($"cp {manager.Name}.crt /etc/docker/certs.d/{cacheHostName}:{NeonHostPorts.RegistryCache}/ca.crt");
                    sbScript.AppendLine($"cp {manager.Name}.crt /usr/local/share/ca-certificates/{cacheHostName}.crt");
                }

                sbScript.AppendLineLinux();
                sbScript.AppendLineLinux("update-ca-certificates");

                copyCommand.AddFile("registry-cache-client-certs.sh", sbScript.ToString(), isExecutable: true);
                steps.Add(copyCommand);
            }

            cluster.Configure(steps);
        }

        /// <summary>
        /// Returns the host name for a Registry cache instance hosted on a manager node.
        /// </summary>
        /// <param name="manager">The manager node.</param>
        /// <returns>The hostname.</returns>
        private string GetCacheHost(NodeDefinition manager)
        {
            return $"{manager.Name}.{NeonHosts.RegistryCache}";
        }
    }
}
