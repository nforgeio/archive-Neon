//-----------------------------------------------------------------------------
// FILE:	    DockerVolume.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;

namespace Neon.Stack.Docker
{
    /// <summary>
    /// Describes a Docker volume.
    /// </summary>
    public class DockerVolume
    {
        /// <summary>
        /// Constructs an instance from the dynamic volume information returned by
        /// the Docker engine.
        /// </summary>
        /// <param name="dynamicVolume">The volume information.</param>
        internal DockerVolume(dynamic dynamicVolume)
        {
            this.Name       = dynamicVolume.Name;
            this.Driver     = dynamicVolume.Driver;
            this.Mountpoint = dynamicVolume.Mountpoint;
        }

        /// <summary>
        /// Returns the volume name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the volume driver.
        /// </summary>
        public string Driver { get; private set; }

        /// <summary>
        /// Returns the volume mount point on the host node.
        /// </summary>
        public string Mountpoint { get; private set; }
    }
}
