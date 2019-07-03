//-----------------------------------------------------------------------------
// FILE:	    DockerNetworkIPAM.cs
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
    /// Describes a Docker network's IPAM configuration.
    /// </summary>
    public class DockerNetworkIpam
    {
        /// <summary>
        /// Constructs an instance from the dynamic network IPAM information
        /// returned by docker.
        /// </summary>
        /// <param name="dynamicIPAM">The IPAM information.</param>
        public DockerNetworkIpam(dynamic dynamicIPAM)
        {
            this.Driver = dynamicIPAM.Driver;
            this.Config = new Dictionary<string, string>();

            foreach (var subConfig in dynamicIPAM.Config)
            {
                foreach (var item in subConfig)
                {
                    Config.Add(item.Name, item.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Returns the IPAM driver.
        /// </summary>
        public string Driver { get; private set; }

        /// <summary>
        /// Returns the IPAM configuration settings.
        /// </summary>
        public Dictionary<string, string> Config { get; private set; }
    }
}
