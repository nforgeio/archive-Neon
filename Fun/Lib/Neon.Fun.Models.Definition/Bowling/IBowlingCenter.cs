//-----------------------------------------------------------------------------
// FILE:	    IBowlingCenter.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Bowling
{
    /// <summary>
    /// Describes a bowling center organization.
    /// </summary>
    [Entity(Type = EntityTypes.BowlingCenter, Namespace = FunConst.BowlingNamespace)]
    public interface IBowlingCenter : IOrganization
    {
        /// <summary>
        /// The bowling lanes.
        /// </summary>
        [EntityProperty(Name = "lanes")]
        ILane[] Lanes { get; set; }
    }
}
