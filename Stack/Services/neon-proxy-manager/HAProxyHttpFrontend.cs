﻿//-----------------------------------------------------------------------------
// FILE:	    HAProxyHttpFrontend.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack.Common;
using Neon.Stack.Cryptography;
using Neon.Stack.Diagnostics;

namespace NeonProxyManager
{
    /// <summary>
    /// Holds information about an HAProxy HTTP frontend being generated.
    /// </summary>
    public class HAProxyHttpFrontend
    {
        /// <summary>
        /// Retrurns the HAProxy frontend name.
        /// </summary>
        public string Name
        {
            get
            {
                var scheme = Tls ? "https" : "http";

                return $"{scheme}:port-{Port}";
            }
        }

        /// <summary>
        /// The TCP port to be bound.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// A dictionary of the referenced certificates keyed by name.
        /// </summary>
        public Dictionary<string, TlsCertificate> Certificates { get; private set; } = new Dictionary<string, TlsCertificate>();

        /// <summary>
        /// A dictionary that maps host names to HAProxy backend names.
        /// </summary>
        public Dictionary<string, string> HostMappings { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Returns <c>true</c> for TLS frontends.
        /// </summary>
        public bool Tls
        {
            get { return Certificates.Count > 0; }
        }

        /// <summary>
        /// Indicates that logging is enabled for the frontend.
        /// </summary>
        public bool Log { get; set; }
    }
}
