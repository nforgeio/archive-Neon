﻿//-----------------------------------------------------------------------------
// FILE:	    TargetOS.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Neon.Cluster;
using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Enumerates the possible target operating systems.
    /// </summary>
    public enum TargetOS
    {
        /// <summary>
        /// Ubuntu 16.04 LTS.
        /// </summary>
        [EnumMember(Value = "ubuntu-16.04")]
        Ubuntu_16_04
    }
}
