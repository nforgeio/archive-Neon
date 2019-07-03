﻿//-----------------------------------------------------------------------------
// FILE:	    DockerClient.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Net;
using Neon.Stack.Retry;

namespace Neon.Stack.Docker
{
    /// <summary>
    /// Implements a client that can submit commands to a Docker engine via the Docker Remote API.
    /// </summary>
    public partial class DockerClient : IDisposable
    {
        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="settings">The settings</param>
        internal DockerClient(DockerSettings settings = null)
        {
            var handler = new HttpClientHandler()
            {
                 AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            this.Settings   = settings;
            this.JsonClient = new JsonClient(handler, disposeHandler: true)
            {
                SafeRetryPolicy = settings.RetryPolicy
            };
        }

        /// <summary>
        /// Releases all resources associated with the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all associated resources.
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            JsonClient.Dispose();
        }

        /// <summary>
        /// Returns the version of the Docker Remote API implemented by this class.
        /// </summary>
        public Version ApiVersion { get; private set; } = new Version("1.23");

        /// <summary>
        /// Returns the <see cref="DockerSettings"/>.
        /// </summary>
        public DockerSettings Settings { get; private set; }

        /// <summary>
        /// Returns the underlying <see cref="JsonClient"/>.
        /// </summary>
        public JsonClient JsonClient { get; private set; }

        /// <summary>
        /// Returns the URI for a specific command.
        /// </summary>
        /// <param name="command">The command name.</param>
        /// <param name="item">The optionak sub item.</param>
        /// <returns>The command URI.</returns>
        private string GetUri(string command, string item = null)
        {
            if (string.IsNullOrEmpty(item))
            {
                return $"{Settings.Uri}/{command}";
            }
            else
            {
                return $"{Settings.Uri}/{command}/{item}";
            }
        }

        /// <summary>
        /// Pinge the remote Docker engine to verify that it's ready.
        /// </summary>
        /// <returns><c>true</c> if ready.</returns>
        /// <remarks>
        /// <note>
        /// This method does not use a <see cref="IRetryPolicy"/>.
        /// </note>
        /// </remarks>
        public async Task<bool> PingAsync()
        {
            try
            {
                var httpResponse = await JsonClient.HttpClient.GetAsync(GetUri("_ping"));

                return httpResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for the Docker engine or Swarm manager to be ready to accept 
        /// requests.
        /// </summary>
        /// <param name="timeout">The maximum timne to wait (defaults to 120 seconds).</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// <para>
        /// The Swarm Manager can return unexpected HTTP response codes when it is
        /// not ready to accept requests.  For example, a request to <b>/volumes</b>
        /// may return a <b>404: Not Found</b> response rather than the <b>503: Service Unavailable</b>
        /// that one would expect.   The server can return this even when <see cref="PingAsync"/>
        /// return successfully.
        /// </para>
        /// <para>
        /// This method attempts to ensure that the server is really ready.
        /// </para>
        /// </remarks>
        public async Task WaitUntilReadyAsync(TimeSpan? timeout = null)
        {
            Covenant.Requires<ArgumentException>(timeout == null || timeout >= TimeSpan.Zero);

            // Create a transient detector that extends [TransientDetector.Network] to
            // consider HTTP 404 (not found) as transient too.

            Func<Exception, bool> transientDetector =
                e =>
                {
                    if (TransientDetector.NetworkAndHttp(e))
                    {
                        return true;
                    }

                    var httpException = e as HttpException;

                    if (httpException != null)
                    {
                        return httpException.StatusCode == HttpStatusCode.NotFound;
                    }

                    return false;
                };

            timeout = timeout ?? TimeSpan.FromSeconds(120);

            IRetryPolicy retryPolicy;

            if (timeout == TimeSpan.Zero)
            {
                retryPolicy = NoRetryPolicy.Instance;
            }
            else
            {
                // We're going to use a [LinearRetryPolicy] that pings the server every
                // two seconds for the duration of the requested timeout period.

                var retryInterval = TimeSpan.FromSeconds(2);

                retryPolicy = new LinearRetryPolicy(transientDetector, maxAttempts: (int)(timeout.Value.TotalSeconds / retryInterval.TotalSeconds), retryInterval: retryInterval);
            }

            await JsonClient.GetAsync(retryPolicy, GetUri("info"));

            // $hack(jeff.lill):
            //
            // At this point, the server should be ready but I'm still seeing 500 errors
            // when listing Docker volumes.  I'm going to add an additional request to
            // list the volumes and not return until at least one volume is present.

            await retryPolicy.InvokeAsync(
                async () =>
                {
                    var volumesResponse = new VolumeListResponse(await JsonClient.GetAsync(NoRetryPolicy.Instance, GetUri("volumes")));

                    if (volumesResponse.Volumes.Count == 0)
                    {
                        throw new TransientException("Docker node reports no volumes.");
                    }
                });
        }
    }
}
