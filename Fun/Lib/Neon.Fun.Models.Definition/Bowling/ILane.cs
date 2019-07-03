//-----------------------------------------------------------------------------
// FILE:	    ILane.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Bowling
{
    /// <summary>
    /// Describes a bowling lane.
    /// </summary>
    [Entity(Type = EntityTypes.BowlingLane, Namespace = FunConst.BowlingNamespace)]
    public interface ILane
    {
        /// <summary>
        /// The lane number.
        /// </summary>
        [EntityProperty(Name = "number")]
        int Number { get; set; }
    }
}
