//-----------------------------------------------------------------------------
// FILE:	    DatabaseConfiguration.cs
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
    /// Configuration information for a Sync Gateway database.
    /// </summary>
    public class DatabaseConfiguration
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DatabaseConfiguration()
        {
        }

        /// <summary>
        /// The database name.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The database server REST URI including the port (typically 8091).
        /// </summary>
        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }

        /// <summary>
        /// The data bucket name.
        /// </summary>
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        /// <summary>
        /// The Javascript <b>sync</b> function code.
        /// </summary>
        [JsonProperty(PropertyName = "sync")]
        public string Sync { get; set; }
    }
}
