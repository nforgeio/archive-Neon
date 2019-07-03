//-----------------------------------------------------------------------------
// FILE:	    AlwaysHealthyClusterHealthPolicy.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

#if !NETCORE

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Stack.Common;

using Renci.SshNet;

namespace Neon.Cluster
{
    /// <summary>
    /// Implements a cluster health policy that doesn't actually evaluate 
    /// health and assumes that everything is healthy.
    /// </summary>
    public class AlwaysHealthyClusterHealthPolicy : IClusterHealthPolicy
    {
        /// <inheritdoc/>
        public void UpdateClusterHealth(ClusterState clusterState)
        {
            clusterState.HealthStatus  = HealthStatus.Healthy;
            clusterState.HealthDetails = "Healthy";
            clusterState.HealthDetails = string.Empty;
        }

        /// <inheritdoc/>
        public void UpdateNodeHealth(NodeState nodeState)
        {
            nodeState.HealthStatus  = HealthStatus.Healthy;
            nodeState.HealthDetails = "Healthy";
            nodeState.HealthDetails = string.Empty;
        }
    }
}

#endif // NETCORE