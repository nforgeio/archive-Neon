//-----------------------------------------------------------------------------
// FILE:	    VolumeListResponse.cs
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
    /// The response from a <see cref="DockerClient.VolumeListAsync"/> command.
    /// </summary>
    public class VolumeListResponse : DockerResponse
    {
        /// <summary>
        /// Constructs the response from a lower-level <see cref="JsonResponse"/>.
        /// </summary>
        /// <param name="response"></param>
        internal VolumeListResponse(JsonResponse response)
            : base(response)
        {
            var volumes = response.AsDynamic().Volumes;

            if (volumes != null)
            {
                foreach (var volume in volumes)
                {
                    this.Volumes.Add(new DockerVolume(volume));
                }
            }
        }

        /// <summary>
        /// Returns the list of volumes returned by the Docker engine.
        /// </summary>
        public List<DockerVolume> Volumes { get; private set; } = new List<DockerVolume>();
    }
}
