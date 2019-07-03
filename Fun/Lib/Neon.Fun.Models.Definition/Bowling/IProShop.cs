//-----------------------------------------------------------------------------
// FILE:	    IProShop.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Bowling
{
    /// <summary>
    /// Describes a bowling ProShop organization.
    /// </summary>
    [Entity(Type = EntityTypes.BowlingProShop, Namespace = FunConst.BowlingNamespace)]
    public interface IProShop : IOrganization
    {
        /// <summary>
        /// The user accounts associated with the ProShop.
        /// </summary>
        IAccount[] Accounts { get; set; }
    }
}
