//-----------------------------------------------------------------------------
// FILE:	    DockerNetworkContainer.cs
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
    /// Describes a container attached to a Docker network.
    /// </summary>
    public class DockerNetworkContainer
    {
        /// <summary>
        /// Constructs an instance from the dynamic attached container information
        /// returned by docker.
        /// </summary>
        /// <param name="dynamicContainer">The container information.</param>
        public DockerNetworkContainer(dynamic dynamicContainer)
        {
            this.Id = dynamicContainer.Name;

            var properties   = dynamicContainer.Value;

            this.EndpointId  = properties.EndpointID;
            this.MacAddress  = properties.MacAddress;
            this.IPv4Address = properties.IPv4Address;
            this.IPv6Address = properties.IPv6Address;
        }

        /// <summary>
        /// Returns the container's ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Returns the container's endpoint ID.
        /// </summary>
        public string EndpointId { get; private set; }

        /// <summary>
        /// Returns the container's MAC address.
        /// </summary>
        public string MacAddress { get; private set; }

        /// <summary>
        /// Returns the container's IPv4 address.
        /// </summary>
        public string IPv4Address { get; private set; }

        /// <summary>
        /// Returns the container's IPv6 address.
        /// </summary>
        public string IPv6Address { get; private set; }
    }
}
