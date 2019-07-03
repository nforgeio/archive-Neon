﻿//-----------------------------------------------------------------------------
// FILE:	    RoleProperties.cs
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
    /// Describes a Couchbase Sync Gateway role.
    /// </summary>
    public class RoleProperties
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RoleProperties()
        {
        }

        /// <summary>
        /// The role name within the database.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Lists the channels explicitly made accessable to members of the role.
        /// </summary>
        [JsonProperty(PropertyName = "admin_channels")]
        public List<string> AdminChannels { get; set; } = new List<string>();

        /// <summary>
        /// Lists all of the channels accessable to members of the role.  This includes
        /// the channels explicitly assigned to the role as well as channels granted
        /// special access by the Sync Gateway sync function.
        /// </summary>
        [JsonProperty(PropertyName = "all_channels")]
        public List<string> AllChannels { get; set; } = new List<string>();
    }
}
