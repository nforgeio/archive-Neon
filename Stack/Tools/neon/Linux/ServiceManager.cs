//-----------------------------------------------------------------------------
// FILE:	    ServiceManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

namespace NeonCluster
{
    /// <summary>
    /// Identifies the service manager configured for a Linux node.
    /// </summary>
    public enum ServiceManager
    {
        /// <summary>
        /// Systemd
        /// </summary>
        Systemd
    }
}
