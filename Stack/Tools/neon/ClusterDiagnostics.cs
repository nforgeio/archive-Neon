﻿//-----------------------------------------------------------------------------
// FILE:	    ClusterDiagnostics.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using EtcdNet;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Net;

// $todo(jeff.lill): Verify that there are no unexpected nodes in the cluster.

namespace NeonCluster
{
    /// <summary>
    /// Methods to verify that cluster nodes are configured and functioning properly.
    /// </summary>
    public static class ClusterDiagnostics
    {
        /// <summary>
        /// Verifies that a cluster manager node is healthy.
        /// </summary>
        /// <param name="node">The manager node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        public static void CheckClusterManager(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentException>(node.Metadata.Manager);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            if (!node.IsFaulted)
            {
                CheckManagerNtp(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckDocker(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckConsul(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckVault(node, clusterDefinition);
            }

            node.Status = "healthy";
        }

        /// <summary>
        /// Verifies that a cluster worker nodes is healthy.
        /// </summary>
        /// <param name="node">The server node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        public static void CheckClusterWorker(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentException>(node.Metadata.Worker);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            if (!node.IsFaulted)
            {
                CheckWorkerNtp(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckDocker(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckConsul(node, clusterDefinition);
            }

            if (!node.IsFaulted)
            {
                CheckVault(node, clusterDefinition);
            }

            node.Status = "healthy";
        }

        /// <summary>
        /// Verifies the cluster log service health.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        public static void CheckLogServices(ClusterProxy cluster)
        {
            if (!cluster.Definition.Log.Enabled)
            {
                return;
            }

            CheckLogEsDataService(cluster);
            CheckLogCollectorService(cluster);
            CheckLogKibanaService(cluster);
        }

        /// <summary>
        /// Verifies the log collector service health.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        private static void CheckLogCollectorService(ClusterProxy cluster)
        {
            // $todo(jeff.lill): Implement this.
        }

        /// <summary>
        /// Verifies the log Elasticsearch cluster health.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        private static void CheckLogEsDataService(ClusterProxy cluster)
        {
            // $todo(jeff.lill): Implement this.
        }

        /// <summary>
        /// Verifies the log Kibana service health.
        /// </summary>
        /// <param name="cluster">The cluster proxy.</param>
        private static void CheckLogKibanaService(ClusterProxy cluster)
        {
            // $todo(jeff.lill): Implement this.
        }

        /// <summary>
        /// Verifies that a manager node's NTP health.
        /// </summary>
        /// <param name="node">The manager node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckManagerNtp(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            // We're going to use [ntpq -p] to query the configured time sources.
            // We should get something back that looks like
            //
            //      remote           refid      st t when poll reach   delay   offset  jitter
            //      ==============================================================================
            //       LOCAL(0).LOCL.          10 l  45m   64    0    0.000    0.000   0.000
            //      * clock.xmission. .GPS.            1 u  134  256  377   48.939 - 0.549  18.357
            //      + 173.44.32.10    18.26.4.105      2 u  200  256  377   96.981 - 0.623   3.284
            //      + pacific.latt.ne 44.24.199.34     3 u  243  256  377   41.457 - 8.929   8.497
            //
            // For manager nodes, we're simply going to verify that we have at least one external 
            // time source answering.

            node.Status = "checking: NTP";

            var retryDelay = TimeSpan.FromSeconds(30);
            var fault      = (string)null;

            for (int tryCount = 0; tryCount < 6; tryCount++)
            {
                var response = node.SudoCommand("/usr/bin/ntpq -pw", RunOptions.LogOutput);

                if (response.ExitCode != 0)
                {
                    Thread.Sleep(retryDelay);
                    continue;
                }

                using (var reader = response.OpenOutputTextReader())
                {
                    string line;

                    // Column header and table bar lines.

                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        fault = "NTP: Invalid [ntpq -p] response.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line[0] != '=')
                    {
                        fault = "NTP: Invalid [ntpq -p] response.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Count the lines starting that don't include [*.LOCL.*], 
                    // the local clock.

                    var sourceCount = 0;

                    for (line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        if (line.Length > 0 && !line.Contains(".LOCL."))
                        {
                            sourceCount++;
                        }
                    }

                    if (sourceCount == 0)
                    {
                        fault = "NTP: No external sources are answering.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Everything looks good.

                    break;
                }
            }

            if (fault != null)
            {
                node.Fault(fault);
            }
        }

        /// <summary>
        /// Verifies that a worker node's NTP health.
        /// </summary>
        /// <param name="node">The manager node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckWorkerNtp(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            // We're going to use [ntpq -p] to query the configured time sources.
            // We should get something back that looks like
            //
            //           remote           refid      st t when poll reach   delay   offset  jitter
            //           ==============================================================================
            //            LOCAL(0).LOCL.          10 l  45m   64    0    0.000    0.000   0.000
            //           * 10.0.1.5        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //           + 10.0.1.7        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //           + 10.0.1.7        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //
            //
            // For worker nodes, we need to verify that each of the managers are answering
            // by confirming that their IP addresses are present.

            node.Status = "checking: NTP";

            var retryDelay = TimeSpan.FromSeconds(30);
            var fault      = (string)null;

            for (var tries = 0; tries < 6; tries++)
            {
                var output = node.SudoCommand("/usr/bin/ntpq -pw", RunOptions.LogOutput).OutputText;

                foreach (var manager in clusterDefinition.SortedManagers)
                {
                    // We're going to check the for presence of the manager's IP address
                    // or its name, the latter because [ntpq] appears to attempt a reverse
                    // IP address lookup which will resolve into one of the DNS names defined
                    // in the local [/etc/hosts] file.

                    if (!output.Contains(manager.Address.ToString()) && !output.Contains(manager.Name.ToLower()))
                    {
                        fault = $"NTP: Manager [{manager.Name}/{manager.Address}] is not answering.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Everything looks OK.

                    break;
                }
            }

            if (fault != null)
            {
                node.Fault(fault);
            }
        }

        /// <summary>
        /// Verifies Docker health.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckDocker(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            node.Status = "checking: Docker";

            // This is a super simple ping to verify that Docker appears to be running.

            var response = node.SudoCommand("docker info");

            if (response.ExitCode != 0)
            {
                node.Fault($"Docker: {response.AllText}");
            }
        }

        /// <summary>
        /// Verifies Consul health.
        /// </summary>
        /// <param name="node">The manager node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckConsul(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            node.Status = "checking: Consul";

            // Verify that the daemon is running.

            switch (Program.ServiceManager)
            {
                case ServiceManager.Systemd:

                    {
                        var output = node.SudoCommand("systemctl status consul", RunOptions.LogOutput).OutputText;

                        if (!output.Contains("Active: active (running)"))
                        {
                            node.Fault($"Consul deamon is not running.");
                            return;
                        }
                    }
                    break;

                default:

                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Verifies that Etcd is function properly at the specified location.
        /// </summary>
        /// <param name="url"></param>
        /// <returns><c>true</c> on success.</returns>
        private static bool CheckEtc(string url)
        {
            // This works by setting and then deleting a unique node 
            // named:
            //
            //      /neon/health-check/GUID.

            var options = new EtcdClientOpitions()
            {
                Urls = new string[] { url }
            };

            var client = new EtcdClient(options);

            try
            {
                Task.Run(
                    async () =>
                    {
                        var key = $"/neon/health-check/{Guid.NewGuid()}";

                        var response = await client.CreateNodeAsync(key, "test", 10);

                        if (response.Node.Key != key || response.Node.Value != "test")
                        {
                            throw new Exception();
                        }

                        response = await client.GetNodeAsync(key);

                        if (response.Node.Key != key || response.Node.Value != "test")
                        {
                            throw new Exception();
                        }

                        await client.DeleteNodeAsync(key);
                        return true;

                    }).Wait();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies Vault health for a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckVault(NodeProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            // $todo(jeff.lill): Implement this.

            return;

            node.Status = "checking: Vault";

            // This is a minimal health test that just verifies that Vault
            // is listening for requests.  We're going to ping the local
            // Vault instance at [/v1/sys/health].
            //
            // Note that this should return a 500 status code with some
            // JSON content.  The reason for this is because we have not
            // yet initialized and unsealed the vault.

            var targetUrl = $"http://{node.Metadata.Address}:{clusterDefinition.Vault.Port}/v1/sys/health?standbycode=200";

            using (var client = new HttpClient())
            {
                try
                {
                    var response = client.GetAsync(targetUrl).Result;

                    if (response.StatusCode != HttpStatusCode.OK && 
                        response.StatusCode != HttpStatusCode.InternalServerError)
                    {
                        node.Fault($"Vault: Unexpected HTTP response status [{(int) response.StatusCode}={response.StatusCode}]");
                        return;
                    }

                    if (!response.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        node.Fault($"Vault: Unexpected content type [{response.Content.Headers.ContentType.MediaType}]");
                        return;
                    }
                }
                catch (Exception e)
                {
                    node.Fault($"Vault: {NeonHelper.ExceptionError(e)}");
                }
            }
        }
    }
}