﻿//-----------------------------------------------------------------------------
// FILE:	    KeepOtherConflictPolicy.cs
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
    /// This policy resolves conflicts by retaining the existing revision in
    /// database over the revision being saved.
    /// </summary>
    public sealed class KeepOtherConflictPolicy : ConflictPolicy
    {
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal KeepOtherConflictPolicy()
        {
        }

        /// <inheritdoc/>
        public override ConflictPolicyType Type
        {
            get { return ConflictPolicyType.KeepOther; }
        }

        /// <inheritdoc/>
        public override void Resolve(ConflictDetails details)
        {
#if NETCORE
            // $todo(jeff.lill): 
            //
            // Get rid of this once CouchbaseException no longer derives from ApplicationException.

            throw new NotImplementedException();
#else
            var currentRevision    = details.Document.CurrentRevision;
            var unsavedRevision    = currentRevision.CreateRevision();
            var mostRecentConflict = details.ConflictingRevisions.First();

            unsavedRevision.SetProperties(mostRecentConflict.Properties);
            unsavedRevision.ReplaceAttachmentsFrom(mostRecentConflict);

            details.SavedRevision = unsavedRevision.Save();
#endif
        }
    }
}
