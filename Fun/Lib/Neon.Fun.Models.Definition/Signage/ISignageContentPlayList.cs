//-----------------------------------------------------------------------------
// FILE:	    ISignageContentPlayList.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Signage
{
    /// <summary>
    /// Identifies content to be displayed by a signage player within a timeslot.
    /// </summary>
    [Entity(Type = EntityTypes.SignageContentPlayList, Namespace = FunConst.SignageNamespace)]
    public interface ISignageContentPlayList
    {
        /// <summary>
        /// Identifies the time (UTC) when the timeslot starts (inclusive).
        /// </summary>
        [EntityProperty(Name = "start")]
        DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Identifies the time (UTC) when the timeslot ends (exclusives).
        /// </summary>
        [EntityProperty(Name = "end")]
        DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// References the content to be displayed within the timeslot.
        /// </summary>
        [EntityProperty(Name = "list")]
        ISignageContentDocument[] List { get; set; }
    }
}
