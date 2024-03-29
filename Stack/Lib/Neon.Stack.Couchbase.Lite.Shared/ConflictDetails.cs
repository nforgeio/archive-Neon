﻿//-----------------------------------------------------------------------------
// FILE:	    ConflictDetails.cs
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
    /// Holds the information required so that a <see cref="ConflictPolicy"/> can resolve
    /// a document conflict.
    /// </summary>
    public class ConflictDetails
    {
        /// <summary>
        /// Returns the low-level Couchbase document.
        /// </summary>
        public Document Document { get; internal set; }

        /// <summary>
        /// Returns the revision being saved.
        /// </summary>
        public UnsavedRevision UnsavedRevision { get; internal set; }

        /// <summary>
        /// Returns the conflicting revisions sorted in descending order by ID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The revisions are sorted in decending order by revision ID such that the
        /// most recent revisions will tend to be located at the top of the list.
        /// </para>
        /// <para>
        /// This fact can be used by <see cref="ConflictPolicy"/> implementations
        /// to help choose one revision over others.
        /// </para>
        /// </remarks>
        public SavedRevision[] ConflictingRevisions { get; internal set; }

        /// <summary>
        /// <para>
        /// Returns the entity document.
        /// </para>
        /// <note>
        /// You can use <see cref="IEntityDocument.EntityType"/> to discover the document
        /// content type and then cast this into the corresponding generic document.
        /// </note>
        /// </summary>
        public IEntityDocument EntityDocument { get; internal set; }

        /// <summary>
        /// Conflict policies will set this to the saved revision if the policy was
        /// able to successfully resolve the conflict.  This will be <c>null</c> if
        /// the conflict could not be resolved.
        /// </summary>
        public SavedRevision SavedRevision { get; set; }
    }
}
