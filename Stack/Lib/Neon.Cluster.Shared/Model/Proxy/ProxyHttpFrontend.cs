//-----------------------------------------------------------------------------
// FILE:	    ProxyHttpFrontend.cs
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
using Neon.Stack.Cryptography;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes an HTTP/HTTPS proxy frontend.
    /// </summary>
    public class ProxyHttpFrontend
    {
        /// <summary>
        /// The host name to be matched for this frontend.
        /// </summary>
        [JsonProperty(PropertyName = "host", Required = Required.Always)]
        public string Host { get; set; }

        /// <summary>
        /// Optionally names the TLS certificate to be used to secure requests to the frontend.
        /// </summary>
        [JsonProperty(PropertyName = "cert_name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string CertName { get; set; } = null;

        /// <summary>
        /// The optional port number.  This defaults to the proxy's default HTTPS port a certificate name
        /// is specified or the default HTTP port otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "port", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(0)]
        public int Port { get; set; } = 0;

        /// <summary>
        /// Returns <c>true</c> if the frontend is to be secured via TLS.
        /// </summary>
        [JsonIgnore]
        public bool Tls
        {
            get { return !string.IsNullOrEmpty(CertName); }
        }

        /// <summary>
        /// Validates the frontend.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <param name="route">The parent route.</param>
        public void Validate(ProxyValidationContext context, ProxyHttpRoute route)
        {
            if (string.IsNullOrEmpty(Host) ||
                !ClusterDefinition.DnsHostRegex.IsMatch(Host))
            {
                context.Error($"Route [{route.Name}] defines the invalid hostname [{Host}].");
            }

            if (CertName != null)
            {
                TlsCertificate certificate;

                if (!context.Certificates.TryGetValue(CertName, out certificate))
                {
                    context.Error($"Route [{route.Name}] references certificate [{CertName}] that does not exist.");
                }
                else
                {
                    if (!certificate.IsValidHost(Host))
                    {
                        context.Error($"Route [{route.Name}] references certificate [{CertName}] which does not cover host [{Host}].");
                    }

                    if (!certificate.IsValidDate(DateTime.UtcNow))
                    {
                        context.Error($"Route [{route.Name}] references certificate [{CertName}] which expired on [{certificate.ValidUntil}].");
                    }
                }
            }

            if (Port != 0)
            {
                if (Port < context.Settings.FirstPort || context.Settings.LastPort < Port)
                {
                    context.Error($"Route [{route.Name}] assigns [{nameof(Port)}={Port}] which is outside the range of valid frontend ports for this proxy.");
                }
            }
            else
            {
                if (CertName == null)
                {
                    Port = context.Settings.DefaultHttpPort;
                }
                else
                {
                    Port = context.Settings.DefaultHttpsPort;
                }
            }
        }
    }
}
