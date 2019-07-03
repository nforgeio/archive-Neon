﻿//-----------------------------------------------------------------------------
// FILE:	    DockerSettings.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Net;
using Neon.Stack.Retry;

namespace Neon.Stack.Docker
{
    /// <summary>
    /// Specifies the configuration settings for a <see cref="DockerClient"/>.
    /// </summary>
    public class DockerSettings
    {
        /// <summary>
        /// Constructs settings using a DNS host name for the Docker engine.
        /// </summary>
        /// <param name="host">Engine host name.</param>
        /// <param name="port">Optional TCP port (defaults to <see cref="NetworkPorts.Docker"/> [<b>2375</b>]).</param>
        /// <param name="secure">Optionally specifies that the connection will be secured via TLS (defaults to <c>false</c>).</param>
        public DockerSettings(string host, int port = NetworkPorts.Docker, bool secure = false)
        {
            var scheme = secure ? "https" : "http";

            this.Uri = $"{scheme}://{host}:{port}";
        }

        /// <summary>
        /// Constructs settings using an <see cref="IPAddress"/> for the Docker engine.
        /// </summary>
        /// <param name="address">The engine IP address.</param>
        /// <param name="port">Optional TCP port (defaults to <see cref="NetworkPorts.Docker"/> [<b>2375</b>]).</param>
        /// <param name="secure">Optionally specifies that the connection will be secured via TLS (defaults to <c>false</c>).</param>
        public DockerSettings(IPAddress address, int port = NetworkPorts.Docker, bool secure = false)
            : this(address.ToString(), port, secure)
        {
            this.RetryPolicy = new ExponentialRetryPolicy(TransientDetector.NetworkAndHttp);
        }

        /// <summary>
        /// Returns the target engine's base URI.
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// The <see cref="IRetryPolicy"/> to be used when submitting requests to docker.
        /// This defaults to a reasonable <see cref="ExponentialRetryPolicy"/> using the
        /// <see cref="TransientDetector.NetworkAndHttp(Exception)"/> transient detector.
        /// </summary>
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Creates a <see cref="DockerClient"/> using the settings.
        /// </summary>
        /// <returns>The created <see cref="DockerClient"/>.</returns>
        public DockerClient CreateClient()
        {
            return new DockerClient(this);
        }
    }
}
