//-----------------------------------------------------------------------------
// FILE:	    PhoneUses.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Enumerates the uses for a phone number.
    /// </summary>
    [Include(Namespace = FunConst.Namespace)]
    public enum PhoneUses
    {
        /// <summary>
        /// The phone number type is not known.
        /// </summary>
        [EnumMember(Value = "other")]
        Other = 0,

        /// <summary>
        /// Identifies a home phone number.
        /// </summary>
        [EnumMember(Value = "home")]
        Home,

        /// <summary>
        /// Identifies a home fax number.
        /// </summary>
        [EnumMember(Value = "homefax")]
        HomeFax,

        /// <summary>
        /// Identifies a work phone number.
        /// </summary>
        [EnumMember(Value = "work")]
        Work,

        /// <summary>
        /// Identifies a work fax number.
        /// </summary>
        [EnumMember(Value = "workfax")]
        WorkFax,

        /// <summary>
        /// Identifies a pager.
        /// </summary>
        [EnumMember(Value = "pager")]
        Pager
    }
}
