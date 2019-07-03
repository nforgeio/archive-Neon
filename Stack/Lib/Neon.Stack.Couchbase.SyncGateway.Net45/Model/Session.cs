﻿//-----------------------------------------------------------------------------
// FILE:	    Session.cs
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

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;

namespace Neon.Stack.Couchbase.SyncGateway
{
    /// <summary>
    /// Describes a newly created Couchbase Sync Gateway session.
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Session()
        {
        }

        /// <summary>
        /// The HTTP cookie name to be used for session handling.
        /// </summary>
        [JsonProperty(PropertyName = "cookie_name")]
        public string Cookie { get; set; }

        /// <summary>
        /// The session expiration time (local server time).
        /// </summary>
        [JsonProperty(PropertyName = "expires")]
        public DateTime Expires { get; set; }

        /// <summary>
        /// The session ID.
        /// </summary>
        [JsonProperty(PropertyName = "session_id")]
        public string Id { get; set; }
    }
}
