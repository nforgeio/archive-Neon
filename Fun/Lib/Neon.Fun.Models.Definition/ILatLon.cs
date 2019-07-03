//-----------------------------------------------------------------------------
// FILE:	    ILatLon.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// The lat/lon coordinates of a location on the globe.
    /// </summary>
    [Entity(Type = EntityTypes.LatLon, Namespace = FunConst.Namespace)]
    public interface ILatLon
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// Latitute coordinate.
        /// </summary>
        double Lat { get; set; }

        /// <summary>
        /// Longitude coordinate.
        /// </summary>
        double Lon { get; set; }
    }
}
