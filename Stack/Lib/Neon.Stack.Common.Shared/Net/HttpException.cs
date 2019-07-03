//-----------------------------------------------------------------------------
// FILE:	    HttpException.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.Net
{
    /// <summary>
    /// An extension of <see cref="HttpRequestException"/> that includes the response
    /// <see cref="StatusCode"/> and <see cref="ReasonPhrase"/>.
    /// </summary>
    public class HttpException : HttpRequestException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="statusCode">The HTTP response status code.</param>
        /// <param name="reasonPhrase">The HTTP response peason phrase (or <c>null</c>).</param>
        public HttpException(HttpStatusCode statusCode, string reasonPhrase)
            : base($"[status={(int)statusCode}, reason={reasonPhrase}]: {statusCode}")
        {
            this.StatusCode   = statusCode;
            this.ReasonPhrase = reasonPhrase ?? string.Empty;
        }

        /// <summary>
        /// Returns the HTTP response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Returns the HTTP response status message.
        /// </summary>
        public string ReasonPhrase { get; private set; }
    }
}
