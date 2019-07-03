//-----------------------------------------------------------------------------
// FILE:	    PerfectClusterHealthPolicy.cs
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
    /// Implements a cluster health policy that requires all components
    /// to be in perfect health.
    /// </summary>
    public class PerfectClusterHealthPolicy : IClusterHealthPolicy
    {
        /// <inheritdoc/>
        public void UpdateClusterHealth(ClusterState clusterState)
        {
        }

        /// <inheritdoc/>
        public void UpdateNodeHealth(NodeState nodeState)
        {
        }
    }
}

#endif // NETCORE