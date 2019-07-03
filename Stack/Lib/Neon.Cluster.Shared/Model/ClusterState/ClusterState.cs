//-----------------------------------------------------------------------------
// FILE:	    ClusterState.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

#if !NETCORE

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes the current state of a NeonCluster.
    /// </summary>
    public class ClusterState : ClusterDefinition
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Queries a NeonCluster for its status.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition file.</param>
        /// <param name="queryFlags">Flags controlling how much cluster status to query (defaults to <see cref="ClusterStateQueryFlags.All"/>).</param>
        /// <param name="credentials">The admin credentials to be used to connect to the node.</param>
        /// <param name="healthPolicy">The policy to be used to evaluate node and cluster health (defaults to <see cref="PerfectClusterHealthPolicy"/>).</param>
        /// <returns>The captured <see cref="ClusterState"/>.</returns>
        public static ClusterState Query(ClusterDefinition clusterDefinition, SshCredentials credentials,
                                             ClusterStateQueryFlags queryFlags = ClusterStateQueryFlags.All,
                                             IClusterHealthPolicy healthPolicy = null)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(credentials != null);

            // This call ensures that all objects and fields are properly initialized 
            // and validated.

            clusterDefinition.Validate();

            // Initialize the state objects from the cluster definition.

            var clusterState = new ClusterState()
            {
                QueryFlags = queryFlags
            };

            clusterDefinition.CopyTo(clusterState);

            foreach (var nodeDefinition in clusterDefinition.NodeDefinitions.Values)
            {
                var nodeState = new NodeState();

                nodeDefinition.CopyTo(nodeState);
                clusterState.Nodes.Add(nodeState.Name, nodeState);
            }

            // Determine which nodes we will query for status.

            var nodes = new List<NodeState>();

            foreach (var nodeDefinition in clusterDefinition.Nodes)
            {
                if (nodeDefinition.Manager)
                {
                    if ((queryFlags & ClusterStateQueryFlags.Managers) != 0)
                    {
                        nodes.Add(clusterState.Nodes[nodeDefinition.Name]);
                    }
                }
                else if ((queryFlags & ClusterStateQueryFlags.Nodes) != 0)
                {
                    nodes.Add(clusterState.Nodes[nodeDefinition.Name]);
                }
            }

            // Query the servers in parallel on separate threads.

            var finishedEvent = new ManualResetEvent(false);
            var activeCount   = nodes.Count;

            foreach (var node in nodes)
            {
                NeonHelper.ThreadRun(
                    () =>
                    {
                        try
                        {
                            using (var server = new NodeProxy<object>(node.Name ?? node.DnsName, node.DnsName, credentials))
                            {
                                if (node.Manager)
                                {
                                    if ((queryFlags & ClusterStateQueryFlags.Consul) != 0)
                                    {
                                        // Determine whether the manager node is currently acting as the 
                                        // Consul leader.
                                        //
                                        // We're going to accomplish this by examining the results of
                                        // the following command:
                                        //
                                        //      consul info

                                        var consulResult = server.RunCommand("consul info", RunOptions.RunWhenFaulted);

                                        if (!consulResult.Success)
                                        {
                                            throw new NeonClusterException(consulResult.ErrorSummary);
                                        }

                                        var consulInfo = new ConsulInfo(consulResult.OutputText);

                                        node.IsConsulLeader = consulInfo["consul.leader"].Equals("true", StringComparison.OrdinalIgnoreCase);
                                        node.IsConsulServer = consulInfo["consul.server"].Equals("true", StringComparison.OrdinalIgnoreCase);
                                        node.ConsulServerCount = int.Parse(consulInfo["raft.num_peers"]);
                                    }

                                    if ((queryFlags & ClusterStateQueryFlags.Swarm) != 0)
                                    {
                                        // $todo(jeff.lill): Implement this.
                                    }
                                }
                            }

                            node.IsValid = true;
                        }
                        catch (Exception e)
                        {
                            node.CaptureError = NeonHelper.ExceptionError(e);
                            node.IsValid = false;
                        }

                        if (Interlocked.Decrement(ref activeCount) <= 0)
                        {
                            finishedEvent.Set();
                        }
                    });
            }

            finishedEvent.WaitOne(TimeSpan.FromSeconds(30));

            return clusterState;
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClusterState()
        {
            this.Nodes = new Dictionary<string, NodeState>();
            this.UnexpectedNodes = new List<NodeState>();
        }

        /// <summary>
        /// State information for the cluster nodes.
        /// </summary>
        [JsonProperty(PropertyName = "nodes")]
        public new Dictionary<string, NodeState> Nodes { get; set; }

        /// <summary>
        /// List of unxepected cluster nodes.  These are nodes that we're discovered in
        /// the live environment that were not in the cluster definition.
        /// </summary>
        [JsonProperty(PropertyName = "unexpected_nodes")]
        public List<NodeState> UnexpectedNodes { get; set; }

        /// <summary>
        /// Evaluates the node and cluster health using a specified <see cref="IClusterHealthPolicy"/>.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="healthPolicy">The policy to be used to evaluate node and cluster health (defaults to <see cref="PerfectClusterHealthPolicy"/>).</param>
        public void EvaluteHealth(ClusterDefinition clusterDefinition, IClusterHealthPolicy healthPolicy = null)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            var policy = healthPolicy ?? new PerfectClusterHealthPolicy();
        }

        /// <summary>
        /// Returns the flags used to control how much cluster status was obtained.
        /// </summary>
        [JsonProperty(PropertyName = "query_flags")]
        public ClusterStateQueryFlags QueryFlags { get; private set; }

        /// <summary>
        /// Indicates the overall health state for the cluster.
        /// </summary>
        [JsonProperty(PropertyName = "health_status")]
        public HealthStatus HealthStatus { get; set; }

        /// <summary>
        /// A terse one line string describing the cluster health.
        /// </summary>
        [JsonProperty(PropertyName = "health_summary")]
        public string HealthSummary { get; set; }

        /// <summary>
        /// A more detailed description of any cluster health issues. 
        /// </summary>
        [JsonProperty(PropertyName = "health_details")]
        public string HealthDetails { get; set; }
    }
}

#endif // NETCORE
