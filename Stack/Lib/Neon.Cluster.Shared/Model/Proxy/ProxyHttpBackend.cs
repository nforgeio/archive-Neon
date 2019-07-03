﻿//-----------------------------------------------------------------------------
// FILE:	    ProxyHttpBackend.cs
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
    /// Describes an HTTP/HTTPS proxy backend.
    /// </summary>
    public class ProxyHttpBackend
    {
        /// <summary>
        /// The optional server backend server name.  The <b>neon-proxy-manmager</b> will
        /// generate a unique name within the route if this isn't specified.
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Name { get; set; } = null;

        /// <summary>
        /// The IP address or DNS name of the backend server where traffic to be forwarded.
        /// </summary>
        [JsonProperty(PropertyName = "server", Required = Required.Always)]
        public string Server { get; set; }

        /// <summary>
        /// The TCP port on the backend server where the traffic is to be forwarded.
        /// </summary>
        [JsonProperty(PropertyName = "port", Required = Required.Always)]
        public int Port { get; set; }

        /// <summary>
        /// Forward the request to this backend using TLS (defaults to <c>false</c>).
        /// </summary>
        [JsonProperty(PropertyName = "tls", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool Tls { get; set; } = false;

        /// <summary>
        /// Validates the backend.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <param name="route">The parent route.</param>
        public void Validate(ProxyValidationContext context, ProxyHttpRoute route)
        {
            IPAddress address;

            if (!string.IsNullOrEmpty(Name) && !ClusterDefinition.IsValidName(Name))
            {
                context.Error($"Route [{route.Name}] has backend server with invalid [{nameof(Name)}={Server}].");
            }

            if (string.IsNullOrEmpty(Server) ||
                (!IPAddress.TryParse(Server, out address) && !ClusterDefinition.DnsHostRegex.IsMatch(Server)))
            {
                context.Error($"Route [{route.Name}] has backend server with invalid [{nameof(Server)}={Server}].  A DNS name or IP address was expected.");
            }

            if (Port <= 0 || ushort.MaxValue < Port)
            {
                context.Error($"Route [{route.Name}] has backend server with invalid [{nameof(Port)}={Port}] which is outside the range of valid TCP ports.");
            }
        }
    }
}
