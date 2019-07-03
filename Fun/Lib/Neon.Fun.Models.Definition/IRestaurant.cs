//-----------------------------------------------------------------------------
// FILE:	    IRestaurant.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes a restaurant organization.
    /// </summary>
    [Entity(Type = EntityTypes.Restaurant, Namespace = FunConst.Namespace)]
    public interface IRestaurant : IOrganization
    {
    }
}
