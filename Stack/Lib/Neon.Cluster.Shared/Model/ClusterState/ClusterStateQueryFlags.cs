//-----------------------------------------------------------------------------
// FILE:	    ClusterStateQueryFlags.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

namespace Neon.Cluster
{
    /// <summary>
    /// Flags used to control what state <see cref="ClusterState.Query"/> attempts
    /// to capture from the cluster.
    /// </summary>
    [Flags]
    public enum ClusterStateQueryFlags
    {
        /// <summary>
        /// Queries each manager node.
        /// </summary>
        [EnumMember(Value = "managers")]
        Managers = 0x00000001,

        /// <summary>
        /// Queries the Consul status for each manager node (this implies <see cref="Managers"/>).
        /// </summary>
        [EnumMember(Value = "consul")]
        Consul = 0x00000002 | Managers,

        /// <summary>
        /// Queries the Swarm status for each manager node (this implies <see cref="Managers"/>).
        /// </summary>
        [EnumMember(Value = "swarm")]
        Swarm = 0x00000004 | Managers,

        /// <summary>
        /// Queries the status for all nodes.
        /// </summary>
        [EnumMember(Value = "nodes")]
        Nodes = 0x0001000 | Managers,

        /// <summary>
        /// Queries for all status.
        /// </summary>
        [EnumMember(Value = "all")]
        All = -1
    }
}
