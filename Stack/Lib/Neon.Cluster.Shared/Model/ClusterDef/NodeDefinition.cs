//-----------------------------------------------------------------------------
// FILE:	    NodeDefinition.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes a Neon Docker host node.
    /// </summary>
    public class NodeDefinition
    {
        private string      name;
        private string      dnsName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NodeDefinition()
        {
            Labels = new NodeLabels(this);
        }

        /// <summary>
        /// Uniquely identifies the node within the cluster.
        /// </summary>
        /// <remarks>
        /// <note>
        /// The name may include only letters, numbers, periods, dashes, and underscores and
        /// also that all names will be converted to lower case.
        /// </note>
        /// </remarks>
        [JsonProperty(PropertyName = "name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Name
        {
            get { return name; }

            set
            {
                if (value != null)
                {
                    name = value.ToLowerInvariant();
                }
                else
                {
                    name = null;
                }
            }
        }

        /// <summary>
        /// The IP address or fully qualified domain name for the node.
        /// </summary>
        /// <remarks>
        /// <note>
        /// The name must be a valid DNS host name or IP address.
        /// </note>
        /// </remarks>
        [JsonProperty(PropertyName = "dns_name", Required = Required.Always)]
        public string DnsName
        {
            get { return dnsName; }

            set
            {
                if (value != null)
                {
                    dnsName = value.ToLowerInvariant();
                }
                else
                {
                    dnsName = null;
                }
            }
        }

        /// <summary>
        /// The node's IP address.  This will be derived from the <see cref="DnsName"/>
        /// via a DNS lookup or by parsing it as an IP address.
        /// </summary>
        [JsonIgnore]
        public IPAddress Address { get; set; } = null;

        /// <summary>
        /// Indicates that the node will act as a management node (defaults to <c>false</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Management nodes are reponsible for managing service discovery and coordinating 
        /// container deployment across the cluster.  Neon uses <b>Consul</b> (https://www.consul.io/) 
        /// for service discovery and <b>Docker Swarm</b> (https://docs.docker.com/swarm/) for
        /// container orchestration.  These services will be deployed to management nodes.
        /// </para>
        /// <para>
        /// An odd number of management nodes must be deployed in a cluster (to help prevent
        /// split-brain).  One management node may be deployed for non-production environments,
        /// but to enable high-availability, three or five management nodes may be deployed.
        /// </para>
        /// <note>
        /// Consul documentation recommends no more than 5 nodes be deployed per cluster to
        /// prevent floods of network traffic from the internal gossip discovery protocol.
        /// Swarm does not have this limitation but to keep things simple, Neon is going 
        /// to standardize on a single management node concept.
        /// </note>
        /// </remarks>
        [JsonProperty(PropertyName = "manager", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool Manager { get; set; } = false;

        /// <summary>
        /// Returns <c>true</c> for worker nodes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Worker nodes within a cluster are where application containers will be deployed.
        /// Any node that is not a <see cref="Manager"/> is considered to be a worker.
        /// </para>
        /// <note>
        /// Set <see cref="Manager"/>=<c>false</c> to identify a worker node.
        /// </note>
        /// </remarks>
        [JsonIgnore]
        public bool Worker
        {
            get { return !Manager; }
        }

        /// <summary>
        /// Returns the node's <see cref="NodeRole"/> (currently <see cref="NodeRole.Manager"/> 
        /// or <see cref="NodeRole.Worker"/>).
        /// </summary>
        [JsonIgnore]
        public string Role
        {
            get { return Manager ? NodeRole.Manager : NodeRole.Worker; }
        }

        /// <summary>
        /// Allow the node operating system to swap RAM to the file system.  This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "swapping", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool Swapping { get; set; } = false;

        /// <summary>
        /// Specifies the Docker labels to be assigned to the host node.  These can provide
        /// detailed information such as the host CPU, RAM, storage, etc.  <see cref="NodeLabels"/>
        /// for more information.
        /// </summary>
        [JsonProperty(PropertyName = "labels")]
        public NodeLabels Labels { get; set; }

        /// <summary>
        /// Set to the server's SSH server's RSA key fingerprint when the parent <see cref="ClusterDefinition"/> 
        /// is persisted within a <see cref="ClusterSecrets"/> instance.  This is an MD5 hash encoded as
        /// hex bytes separated by colons.
        /// </summary>
        [JsonProperty(PropertyName = "ssh_key_fingerprint")]
        public string SshKeyFingerprint { get; set; }

        /// <summary>
        /// Returns a clone of the current instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public NodeDefinition Clone()
        {
            var clone = new NodeDefinition();

            this.CopyTo(clone);

            return clone;
        }

        /// <summary>
        /// Performs a deep copy of the current cluster node to another instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        internal void CopyTo(NodeDefinition target)
        {
            Covenant.Requires<ArgumentNullException>(target != null);
            
            target.Name     = this.Name;
            target.DnsName  = this.DnsName;
            target.Address  = this.Address;
            target.Manager  = this.Manager;
            target.Swapping = this.Swapping;
            target.Labels   = this.Labels.Clone(target);
        }

        /// <summary>
        /// Validates the node definition.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ArgumentException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            Labels = Labels ?? new NodeLabels(this);

            if (Name == null)
            {
                throw new ClusterDefinitionException($"The [{nameof(Name)}] property is required.");
            }

            if (!ClusterDefinition.IsValidName(Name))
            {
                throw new ClusterDefinitionException($"The [{nameof(Name)}={Name}] property is not valid.  Only letters, numbers, periods, dashes, and underscores are allowed.");
            }

            if (DnsName == null)
            {
                throw new ClusterDefinitionException($"The [{nameof(DnsName)}] property is required.");
            }

            if (DnsName.Length > 255)
            {
                throw new ClusterDefinitionException($"The [{nameof(DnsName)}={DnsName}] length exceeds 255 characters.");
            }

            IPAddress ip;

            if (!IPAddress.TryParse(DnsName, out ip) && !ClusterDefinition.DnsHostRegex.IsMatch(DnsName))
            {
                throw new ClusterDefinitionException($"The [{nameof(DnsName)}={DnsName}] is not a valid DNS host or IP address.");
            }

            Labels.Validate(clusterDefinition);
        }
    }
}
