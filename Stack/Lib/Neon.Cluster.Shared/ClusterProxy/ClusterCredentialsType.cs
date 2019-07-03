﻿//-----------------------------------------------------------------------------
// FILE:	    ClusterCredentialsType.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Cluster
{
    /// <summary>
    /// 
    /// </summary>
    public enum ClusterCredentialsType
    {
        /// <summary>
        /// The credential type is not known.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        /// <summary>
        /// The credential is a Vault token.
        /// </summary>
        [EnumMember(Value = "vault-token")]
        VaultToken,

        /// <summary>
        /// The credentials is a Vault AppRole.
        /// </summary>
        [EnumMember(Value = "vault-approle")]
        VaultAppRole
    }
}
