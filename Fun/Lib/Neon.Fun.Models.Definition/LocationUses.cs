//-----------------------------------------------------------------------------
// FILE:	    LocationUses.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Enumerates the possible purposes for a location.
    /// </summary>
    [Include(Namespace = FunConst.Namespace)]
    public enum LocationUses
    {
        /// <summary>
        /// The mail address type could not be determined.
        /// </summary>
        [EnumMember(Value = "other")]
        Other = 0,

        /// <summary>
        /// The primary mailing address.
        /// </summary>
        [EnumMember(Value = "mailing")]
        Mailing,

        /// <summary>
        /// The billing address.
        /// </summary>
        [EnumMember(Value = "billing")]
        Billing,

        /// <summary>
        /// The corporate address.
        /// </summary>
        [EnumMember(Value = "corporate")]
        Corporate,

        /// <summary>
        /// The retail address.
        /// </summary>
        [EnumMember(Value = "retail")]
        Retail
    }
}
