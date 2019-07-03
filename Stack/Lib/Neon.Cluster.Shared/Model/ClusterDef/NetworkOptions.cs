﻿//-----------------------------------------------------------------------------
// FILE:	    NetworkOptions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Net;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes the network options for a NeonCluster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// NeonClusters are provisioned with two standard overlay networks: <b>neon-cluster-public</b> and <b>neon-cluster-private</b>.
    /// </para>
    /// <para>
    /// <b>neon-cluster-public</b> is configured by default on the <b>10.249.0.0/16</b> subnet and is intended to
    /// host public facing service endpoints to be served by the <b>neon-proxy-public</b> proxy service.
    /// </para>
    /// <para>
    /// <b>neon-cluster-private</b> is configured by default on the <b>10.248.0.0/16</b> subnet and is intended to
    /// host internal service endpoints to be served by the <b>neon-proxy-private</b> proxy service.
    /// </para>
    /// </remarks>
    public class NetworkOptions
    {
        private const string defaultPublicSubnet  = "10.249.0.0/16";
        private const string defaultPrivateSubnet = "10.248.0.0/16";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NetworkOptions()
        {
        }

        /// <summary>
        /// The subnet to be assigned to the built-in <b>neon-cluster-public</b> overlay network.  This defaults to <b>10.249.0.0/16</b>.
        /// </summary>
        [JsonProperty(PropertyName = "public_subnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultPublicSubnet)]
        public string PublicSubnet { get; set; } = defaultPublicSubnet;

        /// <summary>
        /// Allow non-Docker swarm mode service containers to attach to the built-in <b>neon-cluster-public</b> cluster 
        /// overlay network.  This defaults to <b>true</b> for flexibility but you may consider disabling this for
        /// better security.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The advantage of enabling is is that any container will be able to connect to the default network
        /// and access swarm mode services.  The downside is that this makes it possible for a bad guy who
        /// gains root access to a single node could potentially deploy a malicious container that could also
        /// join the network.  With this disabled, the bad guy would need to gain access to one of the manager
        /// nodes to deploy a malicious service.
        /// </para>
        /// <para>
        /// Unforunately, it's not currently possible to change this setting after a cluster is deployed.
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "public_attachable", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool PublicAttachable { get; set; } = true;

        /// <summary>
        /// The subnet to be assigned to the built-in <b>neon-cluster-public</b> overlay network.  This defaults to <b>10.248.0.0/16</b>.
        /// </summary>
        [JsonProperty(PropertyName = "private_subnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultPrivateSubnet)]
        public string PrivateSubnet { get; set; } = defaultPrivateSubnet;

        /// <summary>
        /// Allow non-Docker swarm mode service containers to attach to the built-in <b>neon-cluster-private</b> cluster 
        /// overlay network.  This defaults to <b>true</b> for flexibility but you may consider disabling this for
        /// better security.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The advantage of enabling is is that any container will be able to connect to the default network
        /// and access swarm mode services.  The downside is that this makes it possible for a bad guy who
        /// gains root access to a single node could potentially deploy a malicious container that could also
        /// join the network.  With this disabled, the bad guy would need to gain access to one of the manager
        /// nodes to deploy a malicious service.
        /// </para>
        /// <para>
        /// Unforunately, it's not currently possible to change this setting after a cluster is deployed.
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "private_attachable", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool PrivateAttachable { get; set; } = true;

        /// <summary>
        /// The IP addresses of the upstream DNS nameservers to be used by the cluster.  This defaults to the 
        /// Google Public DNS servers: <b>[ "8.8.8.8", "8.8.4.4" ]</b> when the property is <c>null</c> or empty.
        /// </summary>
        /// <remarks>
        /// <para>
        /// NeonClusters configure the Consul servers running on the manager nodes to handle the DNS requests
        /// from the cluster host nodes and containers by default.  This enables the registration of services
        /// with Consul that will be resolved to specific IP addresses.  This is used by the <b>proxy-manager</b>
        /// to support stateful services deployed as multiple containers and may also be used in other future
        /// scenarios.
        /// </para>
        /// <para>
        /// NeonCluster Consul DNS servers answer requests for names with the <b>cluster</b> top-level domain.
        /// Other requests will be handled recursively by forwarding the request to one of the IP addresses
        /// specified here.
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "nameservers", Required = Required.AllowNull)]
        public string[] Nameservers { get; set; } = null;

        /// <summary>
        /// Validates the options definition and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ClusterDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            NetworkCidr cidr;

            if (!NetworkCidr.TryParse(PublicSubnet, out cidr))
            {
                throw new ClusterDefinitionException($"Invalid [{nameof(PublicSubnet)}={PublicSubnet}].");
            }

            if (!NetworkCidr.TryParse(PrivateSubnet, out cidr))
            {
                throw new ClusterDefinitionException($"Invalid [{nameof(PrivateSubnet)}={PrivateSubnet}].");
            }

            if (PublicSubnet == PrivateSubnet)
            {
                throw new ClusterDefinitionException($"[{nameof(PublicSubnet)}] cannot be the same as [{nameof(PrivateSubnet)}] .");
            }

            if (Nameservers == null || Nameservers.Length == 0)
            {
                Nameservers = new string[] { "8.8.8.8", "8.8.4.4" };
            }

            foreach (var nameserver in Nameservers)
            {
                IPAddress address;

                if (!IPAddress.TryParse(nameserver, out address))
                {
                    throw new ClusterDefinitionException($"[{nameserver}] is not a valid [{nameof(NetworkOptions)}.{nameof(Nameservers)}] IP address.");
                }
            }
        }

        /// <summary>
        /// Returns a deep clone of the current instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public NetworkOptions Clone()
        {
            return new NetworkOptions()
            {
                PublicSubnet      = this.PublicSubnet,
                PublicAttachable  = this.PublicAttachable,
                PrivateSubnet     = this.PrivateSubnet,
                PrivateAttachable = this.PrivateAttachable
            };
        }
    }
}
