//-----------------------------------------------------------------------------
// FILE:	    EnvironmentType.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

namespace Neon.Cluster
{
    /// <summary>
    /// Enumerates the types of cluster operating environments.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        [EnumMember(Value = "other")]
        Other = 0,

        /// <summary>
        /// Development environment.
        /// </summary>
        [EnumMember(Value = "dev")]
        Dev,

        /// <summary>
        /// Test environment.
        /// </summary>
        [EnumMember(Value = "test")]
        Test,

        /// <summary>
        /// Staging environment.
        /// </summary>
        [EnumMember(Value = "stage")]
        Stage,

        /// <summary>
        /// Production environment.
        /// </summary>
        [EnumMember(Value = "prod")]
        Prod
    }
}
