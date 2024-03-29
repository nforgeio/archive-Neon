﻿//-----------------------------------------------------------------------------
// FILE:	    DocLinkMapper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Stack.Data.Internal
{
    /// <summary>
    /// <b>Platform use only:</b> Used by <see cref="IEntity"/> implementations to 
    /// map a property to a linked <see cref="IEntity"/> instance.
    /// </summary>
    /// <typeparam name="TDocument">The property value type.</typeparam>
    /// <remarks>
    /// <note>
    /// This class is intended for use only by classes generated by the 
    /// <b>entity-gen</b> build tool.
    /// </note>
    /// <para>
    /// This class is used to link a <see cref="JProperty"/> value to an external
    /// entity document.  The property value will act as the document link and the
    /// <see cref="IEntityContext"/> passed to the constructor (if any) will be
    /// used to dereference the link and load the document.
    /// </para>
    /// <para>
    /// Linked documents are loaded on demand and cached when the <see cref="Value"/> 
    /// getter is called.  Subsequent calls to the getter will return the cached
    /// value.  The getter will return <c>null</c> if the link is null or if the
    /// referenced document doesn't exist.
    /// </para>
    /// <note>
    /// This class will simply return <c>null</c> if no <see cref="IEntityContext"/> 
    /// is present.
    /// </note>
    /// </remarks>
    /// <threadsafety instance="false"/>
    public struct DocLinkMapper<TDocument> : IPropertyMapper
        where TDocument : class, IDocument
    {
        private IEntity             parentEntity;
        private IEntityContext      context;
        private JProperty           property;
        private IDocument           documentValue;
        private Func<bool>          isDeletedFunc;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentEntity">The <see cref="IEntity"/> that owns this mapper.</param>
        /// <param name="jsonName">The JSON property name.</param>
        /// <param name="propertyName">The entity property name.</param>
        /// <param name="context">The <see cref="IEntityContext"/> or <c>null</c>.</param>
        public DocLinkMapper(IEntity parentEntity, string jsonName, string propertyName, IEntityContext context)
        {
            Covenant.Requires<ArgumentNullException>(parentEntity != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(jsonName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(propertyName));

            this.parentEntity  = parentEntity;
            this.context       = context;
            this.JsonName      = jsonName;
            this.PropertyName  = propertyName;
            this.property      = null;
            this.documentValue = null;
            this.isDeletedFunc = null;
        }

        /// <inheritdoc/>
        public string JsonName { get; private set; }

        /// <inheritdoc/>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Returns the document link or <c>null</c>.
        /// </summary>
        public string Link
        {
            get
            {
                switch (property.Value.Type)
                {
                    case JTokenType.String:

                        // This is the preferred property value type.

                        return (string)property.Value.ToString();

                    case JTokenType.Bytes:
                    case JTokenType.Float:
                    case JTokenType.Guid:
                    case JTokenType.Integer:
                    case JTokenType.Uri:

                        // These will work too.

                        return property.Value.ToString();

                    default:

                        // The remaining types indicate null or don't really
                        // make sense, so we'll treat them as null.

                        return null;
                }
            }
        }

        /// <summary>
        /// Returns the link string for a document or <c>null</c>.
        /// </summary>
        /// <param name="document">The document or <c>null</c>.</param>
        /// <returns>The entity link or <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the value being saved cannot be linked.</exception>
        /// <remarks>
        /// This method returns <c>null</c> when <paramref name="document"/>=<c>null</c>, otherwise
        /// it returns the document's link.  A non-<c>null</c> document must be linkable.
        /// </remarks>
        private static string GetLink(TDocument document)
        {
            if (document == null)
            {
                return null;
            }

            var link = document._GetLink();

            if (link == null)
            {
                throw new ArgumentException($"The [{nameof(TDocument)}] instance cannot be linked.  For Couchbase scenarios, be sure the document is persisted to a database.");
            }

            return link;
        }

        /// <summary>
        /// The current property value.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the value being saved cannot be linked.</exception>
        public TDocument Value
        {
            get
            {
                if (context == null || Link == null)
                {
                    return null;
                }

                if (documentValue != null)
                {
                    // We have a cached document.  Return it unless it 
                    // has been deleted.

                    if (!isDeletedFunc())
                    {
                        return (TDocument)documentValue;
                    }

                    // The document has been deleted.  We'll clear the cache and
                    // then drop through to the code below on the off-chance that
                    // we'll be able to load it again.

                    documentValue = null;
                    isDeletedFunc = null;
                }

                documentValue = context.LoadDocument<TDocument>(Link, out isDeletedFunc);

                return (TDocument)documentValue;
            }

            set
            {
                if (context == null)
                {
                    return;
                }

                if (value == null)
                {
                    property.Value = null;
                }
                else
                {
                    property.Value = GetLink(value);
                }

                // Purge any cached info.

                documentValue = null;
                isDeletedFunc = null;
            }
        }

        /// <inheritdoc/>
        public bool Load(JProperty newProperty, bool reload = false)
        {
            Covenant.Requires<ArgumentNullException>(newProperty != null);

            var changed = !NeonHelper.JTokenEquals(property, newProperty);

            this.property      = newProperty;
            this.documentValue = null;      // Purge any cached entity info
            this.isDeletedFunc = null;

            if (reload && changed)
            {
                parentEntity._OnPropertyChanged(PropertyName);
            }

            return changed;
        }
    }
}
