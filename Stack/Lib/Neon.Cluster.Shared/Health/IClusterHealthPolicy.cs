//-----------------------------------------------------------------------------
// FILE:	    IClusterHealthPolicy.cs
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
    /// Defines how the health of a NeonCluster and its nodes is evaluated.
    /// </summary>
    [ContractClass(typeof(IClusterHealthPolicyContract))]
    public interface IClusterHealthPolicy
    {
        /// <summary>
        /// Updates the health related properties for a cluster.
        /// </summary>
        /// <param name="clusterState">The cluster state.</param>
        /// <remarks>
        /// <para>
        /// This method updates the following node state properties.
        /// </para>
        /// <list type="bullet">
        /// <item><see cref="ClusterState.HealthStatus"/></item>
        /// <item><see cref="ClusterState.HealthSummary"/></item>
        /// <item><see cref="ClusterState.HealthDetails"/></item>
        /// </list>
        /// </remarks>
        void UpdateClusterHealth(ClusterState clusterState);

       /// <summary>
        /// Updates the health related properties for a node.
        /// </summary>
        /// <param name="nodeState">The node state.</param>
        /// <remarks>
        /// <para>
        /// This method updates the following node state properties.
        /// </para>
        /// <list type="bullet">
        /// <item><see cref="NodeState.HealthStatus"/></item>
        /// <item><see cref="NodeState.HealthSummary"/></item>
        /// <item><see cref="NodeState.HealthDetails"/></item>
        /// </list>
        /// </remarks>
        void UpdateNodeHealth(NodeState nodeState);
    }

    [ContractClassFor(typeof(IClusterHealthPolicy))]
    internal abstract class IClusterHealthPolicyContract : IClusterHealthPolicy
    {
        public void UpdateClusterHealth(ClusterState clusterState)
        {
        }

        public void UpdateNodeHealth(NodeState nodeState)
        {
        }
    }
}

#endif // NETCORE