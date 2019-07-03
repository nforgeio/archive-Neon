//-----------------------------------------------------------------------------
// FILE:	    HealthState.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Stack.Common;

using Renci.SshNet;

namespace Neon.Cluster
{
    /// <summary>
    /// Enumerates the possible health states of an entity in a NeonCluster.
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// The entity health status is not known.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown,

        /// <summary>
        /// The entity is healthy.
        /// </summary>
        [EnumMember(Value = "healthy")]
        Healthy,

        /// <summary>
        /// The entity health is somewhat impaired but is within functional limits.
        /// </summary>
        [EnumMember(Value = "impaired")]
        Impaired,

        /// <summary>
        /// The entity health is not within functional limits.
        /// </summary>
        [EnumMember(Value = "faulted")]
        Faulted
    }
}
