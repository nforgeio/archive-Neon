//-----------------------------------------------------------------------------
// FILE:	    ProxyTcpFrontend.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
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
    /// Describes a TCP proxy frontend.
    /// </summary>
    public class ProxyTcpFrontend
    {
        /// <summary>
        /// The TCP port where inbound TCP traffic will be received by the proxy.
        /// </summary>
        [JsonProperty(PropertyName = "port", Required = Required.Always)]
        public int Port { get; set; }

        /// <summary>
        /// Validates the frontend.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <param name="route">The parent route.</param>
        public void Validate(ProxyValidationContext context, ProxyTcpRoute route)
        {
            if (Port < context.Settings.FirstTcpPort || context.Settings.LastPort < Port)
            {
                context.Error($"Route [{route.Name}] assigns [{nameof(Port)}={Port}] which is outside the range of valid frontend TCP ports for this proxy.");
            }
        }
    }
}
