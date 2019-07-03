//-----------------------------------------------------------------------------
// FILE:	    NodeState.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
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
    /// Describes the current state of a NeonCluster node.
    /// </summary>
    public class NodeState : NodeDefinition
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NodeState()
        {
        }

        /// <summary>
        /// Indicates that the node was able to be contacted and its
        /// state was successfully obtained.
        /// </summary>
        [JsonProperty(PropertyName = "valid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// Indicates whether the manager node has been elected as the Consul leader.
        /// </summary>
        [JsonProperty(PropertyName = "consul_leader")]
        public bool IsConsulLeader { get; set; }

        /// <summary>
        /// Indicates whether the manager node is configured as a Consul server.
        /// </summary>
        [JsonProperty(PropertyName = "consul_server")]
        public bool IsConsulServer { get; set; }

        /// <summary>
        /// Returns the number of Consul servers the current manager node believes
        /// are active.
        /// </summary>
        [JsonProperty(PropertyName = "consul_server_count")]
        public int ConsulServerCount { get; set; }

        /// <summary>
        /// Indicates that Consul reported that the node is currently a member of the cluster.
        /// </summary>
        [JsonProperty(PropertyName = "consul_member")]
        public bool IsConsulMember { get; set; }

        /// <summary>
        /// Indicates whether the manager node is currently responsible for Swarm orchestration.
        /// </summary>
        [JsonProperty(PropertyName = "swarm_manager")]
        public bool IsSwarmManager { get; set; }

        /// <summary>
        /// Indicates the overall health state for the node.
        /// </summary>
        [JsonProperty(PropertyName = "health_status")]
        public HealthStatus HealthStatus { get; set; }

        /// <summary>
        /// A terse one line string describing the node health.
        /// </summary>
        [JsonProperty(PropertyName = "health_summary")]
        public string HealthSummary { get; set; }

        /// <summary>
        /// A more detailed description of any cluster node issues. 
        /// </summary>
        [JsonProperty(PropertyName = "health_details")]
        public string HealthDetails { get; set; }

        /// <summary>
        /// Set to an error message if an error was detected while querying the node state.
        /// </summary>
        [JsonProperty(PropertyName = "capture_error")]
        public string CaptureError { get; set; }
    }
}
