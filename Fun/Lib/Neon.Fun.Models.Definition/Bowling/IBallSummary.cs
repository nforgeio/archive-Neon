//-----------------------------------------------------------------------------
// FILE:	    IBallSummary.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Bowling
{
    /// <summary>
    /// High-level bowling ball specifications targeted for bowler applications.
    /// </summary>
    [Entity(Type = EntityTypes.BowlingBallSpecification, Namespace = FunConst.BowlingNamespace)]
    public interface IBallSummary
    {
        /// <summary>
        /// The manufacturer.
        /// </summary>
        [EntityProperty(Name = "manufacturer")]
        string Manufacturer { get; set; }

        /// <summary>
        /// The model name.
        /// </summary>
        [EntityProperty(Name = "model")]
        string Model { get; set; }

        /// <summary>
        /// The release date.
        /// </summary>
        [EntityProperty(Name = "release_date")]
        DateTime ReleaseDate { get; set; }

        /// <summary>
        /// Indicates whether the ball has not been retired.
        /// </summary>
        [EntityProperty(Name = "retired")]
        bool IsRetired { get; set; }

        /// <summary>
        /// Identifies the ball coverstock.
        /// </summary>
        [EntityProperty(Name = "coverstock")]
        string CoverStock { get; set; }

        /// <summary>
        /// Identifies the factory surface finish.
        /// </summary>
        [EntityProperty(Name = "finish")]
        string Finish { get; set; }

        /// <summary>
        /// Identifies the ball color.
        /// </summary>
        [EntityProperty(Name = "color")]
        string Color { get; set; }

        /// <summary>
        /// Identifies the ball fagrance.
        /// </summary>
        [EntityProperty(Name = "color")]
        string Fagrance { get; set; }

        /// <summary>
        /// The available ball weights.
        /// </summary>
        [EntityProperty(Name = "detail")]
        int[] Weights { get; set; }
    }
}
