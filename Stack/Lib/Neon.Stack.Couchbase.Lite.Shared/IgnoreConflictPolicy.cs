﻿//-----------------------------------------------------------------------------
// FILE:	    IgnoreConflictPolicy.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Couchbase.Lite
{
    /// <summary>
    /// This policy quietly persists conflicts to the database, leaving Couchbase
    /// Lite to automatically choose the winning revision.  Advanced applications
    /// may go back and resolve the conflict differently sometime in the future.
    /// </summary>
    public sealed class IgnoreConflictPolicy : ConflictPolicy
    {
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal IgnoreConflictPolicy()
        {
        }

        /// <inheritdoc/>
        public override ConflictPolicyType Type
        {
            get { return ConflictPolicyType.Ignore; }
        }

        /// <inheritdoc/>
        public override void Resolve(ConflictDetails details)
        {
            throw new NotImplementedException();
        }
    }
}
