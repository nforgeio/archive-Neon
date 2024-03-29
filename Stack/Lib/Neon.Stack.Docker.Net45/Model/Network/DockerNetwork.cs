﻿//-----------------------------------------------------------------------------
// FILE:	    DockerNetwork.cs
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
    /// Describes a Docker network.
    /// </summary>
    public class DockerNetwork
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DockerNetwork()
        {
            this.Containers = new List<DockerNetworkContainer>();
            this.Options    = new Dictionary<string, string>();
            this.Labels     = new Dictionary<string, string>();
        }

        /// <summary>
        /// Constructs an instance from the dynamic network information returned by
        /// the Docker engine.
        /// </summary>
        /// <param name="dynamicNetwork">The network information.</param>
        internal DockerNetwork(dynamic dynamicNetwork)
            : this()
        {
            this.Name       = dynamicNetwork.Name;
            this.Id         = dynamicNetwork.Id;
            this.Scope      = dynamicNetwork.Scope;
            this.Driver     = dynamicNetwork.Driver;
            this.EnableIPv6 = dynamicNetwork.EnableIPv6;
            this.Internal   = dynamicNetwork.Internal;
            this.Ipam       = new DockerNetworkIpam(dynamicNetwork.IPAM);

            foreach (var item in dynamicNetwork.Containers)
            {
                Containers.Add(new DockerNetworkContainer(item));
            }

            foreach (var item in dynamicNetwork.Options)
            {
                Options.Add(item.Name, item.Value.ToString());
            }

            foreach (var item in dynamicNetwork.Labels)
            {
                Labels.Add(item.Name, item.Value.ToString());
            }
        }

        /// <summary>
        /// The network name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Used when creating a network to have the Docker Engine verify that
        /// network does not already exist.  This defaults to <c>false</c>.
        /// </summary>
        public bool CheckDuplicate { get; set; }

        /// <summary>
        /// Returns the network ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Returns the network scope.
        /// </summary>
        public string Scope { get; private set; }

        /// <summary>
        /// The network driver.
        /// </summary>
        public string Driver { get; set; }

        /// <summary>
        /// Indicates if the network is IPv6 enabled.
        /// </summary>
        public bool EnableIPv6 { get; set; }

        /// <summary>
        /// Indicates if the network is internal.
        /// </summary>
        public bool Internal { get; set; }

        /// <summary>
        /// The network's IPAM configuration.
        /// </summary>
        public DockerNetworkIpam Ipam { get; private set; }

        /// <summary>
        /// Lists the containers attached to the network.
        /// </summary>
        public List<DockerNetworkContainer> Containers { get; private set; }

        /// <summary>
        /// Lists the network options.
        /// </summary>
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        /// Lists the network labels.
        /// </summary>
        public Dictionary<string, string> Labels { get; private set; }
    }
}
