//-----------------------------------------------------------------------------
// FILE:	    EmailUses.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Enumerates the uses for an email address.
    /// </summary>
    [Include(Namespace = FunConst.Namespace)]
    public enum EmailUses
    {
        /// <summary>
        /// The email use is not known.
        /// </summary>
        [EnumMember(Value = "other")]
        Other = 0,

        /// <summary>
        /// Identifies a home email address.
        /// </summary>
        [EnumMember(Value = "home")]
        Home,

        /// <summary>
        /// Identifies a work email address.
        /// </summary>
        [EnumMember(Value = "work")]
        Work
    }
}
