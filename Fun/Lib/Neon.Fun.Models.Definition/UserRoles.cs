//-----------------------------------------------------------------------------
// FILE:	    UserRoles.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Enumerates the possible roles for a user within an organization.
    /// </summary>
    [Include(Namespace = FunConst.Namespace)]
    public enum UserRoles
    {
        /// <summary>
        /// The role is unknown.
        /// </summary>
        [EnumMember(Value = "other")]
        Other = 0,
        
        /// <summary>
        /// Grants limited access.
        /// </summary>
        [EnumMember(Value = "guest")]
        Guest,

        /// <summary>
        /// Grants access for league bowlers, authenticated customers, etc.
        /// </summary>
        [EnumMember(Value = "customer")]
        Customer,

        /// <summary>
        /// Grants basic organization employee access.
        /// </summary>
        [EnumMember(Value = "employee")]
        Employee,

        /// <summary>
        /// Grants organization administration access. 
        /// </summary>
        [EnumMember(Value = "admin")]
        Admin,

        /// <summary>
        /// Grants organization supervisory access.
        /// </summary>
        [EnumMember(Value = "supervisor")]
        Supervisor,

        /// <summary>
        /// Grants organization finance and billing access.
        /// </summary>
        [EnumMember(Value = "finance")]
        Finance,
    }
}
