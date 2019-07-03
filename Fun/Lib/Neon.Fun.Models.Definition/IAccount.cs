//-----------------------------------------------------------------------------
// FILE:	    IAccount.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes an organization user account.
    /// </summary>
    [Entity(Type = EntityTypes.Account, Namespace = FunConst.Namespace)]
    public interface IAccount
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// The globally unique document ID of the person associated with this account.
        /// </summary>
        [EntityProperty(Name = "@person", IsLink = true)]
        IPerson Person { get; set; }

        /// <summary>
        /// Indicates whether the user is currently enabled.
        /// </summary>
        [EntityProperty(Name = "enabled")]
        bool IsEnabled { get; set; }

        /// <summary>
        /// Identifies the user's roles with the organization.
        /// </summary>
        [EntityProperty(Name = "roles")]
        string[] Roles { get; set; }
    }
}
