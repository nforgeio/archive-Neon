//-----------------------------------------------------------------------------
// FILE:	    NetworkCreateResponse.cs
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

namespace Neon.Stack.Docker
{
    /// <summary>
    /// The response from a <see cref="DockerClient.NetworkCreateAsync(DockerNetwork)"/> command.
    /// </summary>
    public class NetworkCreateResponse : DockerResponse
    {
        /// <summary>
        /// Constructs the response from a lower-level <see cref="JsonResponse"/>.
        /// </summary>
        /// <param name="response"></param>
        internal NetworkCreateResponse(JsonResponse response)
            : base(response)
        {
            this.Id = response.AsDynamic().Id;
        }

        /// <summary>
        /// Returns the ID for the created network.
        /// </summary>
        public string Id { get; private set; }
    }
}
