﻿//-----------------------------------------------------------------------------
// FILE:	    LinkListMapper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    /// <b>Platform use only:</b> Used by <see cref="IEntity"/> implementations 
    /// to manage a list of entity links.
    /// </summary>
    /// <typeparam name="TEntity">The list element type (implementing <see cref="IEntity"/>).</typeparam>
    /// <remarks>
    /// <note>
    /// This class is intended for use only by classes generated by the 
    /// <b>entity-gen</b> build tool.
    /// </note>
    /// <para>
    /// The <see cref="ListMapper{T}"/>'s primary responsibility is to listen 
    /// collection change events from the <see cref="JArray"/> and relay these to
    /// the parent entity.
    /// </para>
    /// </remarks>
    /// <threadsafety instance="false"/>
    public struct LinkListMapper<TEntity> : IPropertyMapper
        where TEntity : class, IEntity, new()
    {
        private IEntity                     parentEntity;
        private IEntityContext              context;
        private JProperty                   property;
        private LinkListWrapper<TEntity>    listWrapper;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentEntity">The <see cref="IEntity"/> that owns this mapper.</param>
        /// <param name="jsonName">The JSON property name.</param>
        /// <param name="propertyName">The entity property name.</param>
        /// <param name="context">The <see cref="IEntityContext"/> or <c>null</c>.</param>
        public LinkListMapper(IEntity parentEntity, string jsonName, string propertyName, IEntityContext context)
        {
            Covenant.Requires<ArgumentNullException>(parentEntity != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(jsonName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(propertyName));

            this.parentEntity = parentEntity;
            this.context      = context;
            this.JsonName     = jsonName;
            this.PropertyName = propertyName;
            this.property     = null;
            this.listWrapper  = null;
        }

        /// <inheritdoc/>
        public string JsonName { get; private set; }

        /// <inheritdoc/>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Returns the current property value.
        /// </summary>
        public LinkListWrapper<TEntity> Value
        {
            get { return listWrapper; }
        }

        /// <summary>
        /// Sets the collection items.
        /// </summary>
        /// <param name="items">The collection items or <c>null</c>.</param>
        public void Set(IEnumerable<TEntity> items)
        {
            if (items == null)
            {
                if (listWrapper != null)
                {
                    listWrapper.Detach();
                }

                property.Value = null;
                listWrapper    = null;
                return;
            }

            var jArray = new JArray();

            property.Value = jArray;
            listWrapper    = new LinkListWrapper<TEntity>(parentEntity, context, jArray, items);
        }

        /// <inheritdoc/>
        public bool Load(JProperty newProperty, bool reload = false)
        {
            Covenant.Requires<ArgumentNullException>(newProperty != null);

            var changed = !NeonHelper.JTokenEquals(property, newProperty);

            property = newProperty;

            if (property.Value == null || property.Value.Type == JTokenType.Null)
            {
                listWrapper = null;
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                listWrapper = new LinkListWrapper<TEntity>(parentEntity, context, (JArray)property.Value, null);
            }
            else
            {
                listWrapper = null;
            }

            if (reload && changed)
            {
                parentEntity._OnPropertyChanged(PropertyName);
            }

            return changed;
        }
    }
}
