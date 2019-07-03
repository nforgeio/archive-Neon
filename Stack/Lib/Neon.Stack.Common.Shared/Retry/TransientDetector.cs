﻿//-----------------------------------------------------------------------------
// FILE:	    TransientDetector.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Net;

namespace Neon.Stack.Retry
{
    /// <summary>
    /// Provides some common transient error detection functions for use by
    /// <see cref="IRetryPolicy"/> implementations.
    /// </summary>
    public static class TransientDetector
    {
        /// <summary>
        /// Always determines that the exception is transient.
        /// </summary>
        /// <param name="e">The potential transient exception.</param>
        /// <returns><c>true</c></returns>
        public static bool Always(Exception e)
        {
            Covenant.Requires<ArgumentException>(e != null);

            return true;
        }

        /// <summary>
        /// Never determines that the exception is transient.
        /// </summary>
        /// <param name="e">The potential transient exception.</param>
        /// <returns><c>false</c></returns>
        public static bool Never(Exception e)
        {
            Covenant.Requires<ArgumentException>(e != null);

            return false;
        }

        /// <summary>
        /// Considers <see cref="SocketException"/> as possible transient errors as well as these
        /// exceptions nested within an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="e">The potential transient exception.</param>
        /// <returns><c>true</c> if the exception is to be considered as transient.</returns>
        /// <remarks>
        /// <note>
        /// <see cref="TransientException"/> is always considered to be a transient exception.
        /// </note>
        /// </remarks>
        public static bool Network(Exception e)
        {
            Covenant.Requires<ArgumentException>(e != null);

            var transientException = e as TransientException;

            if (transientException != null)
            {
                return true;
            }

            var aggregateException = e as AggregateException;

            if (aggregateException != null)
            {
                e = aggregateException.InnerException;
            }

            var socketException = e as SocketException;

            if (socketException != null)
            {
                switch (socketException.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionRefused:
                    case SocketError.ConnectionReset:
                    case SocketError.HostDown:
                    case SocketError.HostNotFound:
                    case SocketError.HostUnreachable:
                    case SocketError.Interrupted:
                    case SocketError.NotConnected:
                    case SocketError.NetworkDown:
                    case SocketError.NetworkReset:
                    case SocketError.NetworkUnreachable:
                    case SocketError.TimedOut:

                        return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Considers <see cref="HttpException"/> as possible transient errors as well as this
        /// exception nested within an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="e">The potential transient exception.</param>
        /// <returns><c>true</c> if the exception is to be considered as transient.</returns>
        /// <remarks>
        /// <note>
        /// <see cref="TransientException"/> is always considered to be a transient exception.
        /// </note>
        /// </remarks>
        public static bool Http(Exception e)
        {
            Covenant.Requires<ArgumentException>(e != null);

            var transientException = e as TransientException;

            if (transientException != null)
            {
                return true;
            }

            var aggregateException = e as AggregateException;

            if (aggregateException != null)
            {
                e = aggregateException.InnerException;
            }

            var httpException = e as HttpException;

            if (httpException != null)
            {
                if ((int)httpException.StatusCode < 400)
                {
                    return true;
                }

                switch (httpException.StatusCode)
                {
                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.InternalServerError:
                    case HttpStatusCode.ServiceUnavailable:
                    case (HttpStatusCode)429: // To many requests

                        return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Considers <see cref="SocketException"/> and <see cref="HttpRequestException"/> as possible
        /// transient errors as well as these exceptions nested within an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="e">The potential transient exception.</param>
        /// <returns><c>true</c> if the exception is to be considered as transient.</returns>
        /// <remarks>
        /// <note>
        /// <see cref="TransientException"/> is always considered to be a transient exception.
        /// </note>
        /// </remarks>
        public static bool NetworkAndHttp(Exception e)
        {
            Covenant.Requires<ArgumentException>(e != null);

            return Network(e) || Http(e);
        }
    }
}
