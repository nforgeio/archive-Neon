//-----------------------------------------------------------------------------
// FILE:	    ISignageContentProperties.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Signage
{
    /// <summary>
    /// The root content entity for a <see cref="ISignageContentDocument"/>  that describes
    /// a digital signage content package.
    /// </summary>
    [Entity(Type = EntityTypes.SignageContentProperties, Namespace = FunConst.SignageNamespace)]
    public interface ISignageContentProperties
    {
        /// <summary>
        /// Indicates whether the content is enabled for playing.
        /// </summary>
        [EntityProperty(Name = "enabled")]
        bool IsEnabled { get; set; }

        /// <summary>
        /// Indicates the minimum version of the player that can play this content.
        /// </summary>
        [EntityProperty(Name = "min_player")]
        string MinimumPlayerVersion { get; set; }

        /// <summary>
        /// Identifies the content type.
        /// </summary>
        [EntityProperty(Name = "content_type")]
        SignageContentType ContentType { get; set; }

        /// <summary>
        /// The content run time.
        /// </summary>
        [EntityProperty(Name = "run_time")]
        TimeSpan RunTime { get; set; }
    }
}
