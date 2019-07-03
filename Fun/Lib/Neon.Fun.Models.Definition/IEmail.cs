//-----------------------------------------------------------------------------
// FILE:	    IEmail.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes an email address.
    /// </summary>
    [Entity(Type = EntityTypes.Email, Namespace = FunConst.Namespace)]
    public interface IEmail
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// Describes the email address use.
        /// </summary>
        [EntityProperty(Name = "use")]
        EmailUses Use { get; set; }

        /// <summary>
        /// The email address.
        /// </summary>
        [EntityProperty(Name = "address")]
        string Address { get; set; }
    }
}
