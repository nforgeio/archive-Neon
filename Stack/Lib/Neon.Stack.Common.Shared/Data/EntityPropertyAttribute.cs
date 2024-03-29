﻿//-----------------------------------------------------------------------------
// FILE:	    EntityPropertyAttribute.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Data;
using Neon.Stack.Data.Internal;
using System.Runtime.Serialization;

namespace Neon.Stack.Data
{
    /// <summary>
    /// Used to customize code generation for a property definition within an
    /// <c>interface</c> tagged by <see cref="EntityAttribute"/> by the
    /// <b>entity-gen</b> build tool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute provides the <see cref="Name"/> property which allows for
    /// the customization of the string used when the property is serialized to
    /// JSON.  This allows for the decoupling of entity property names from
    /// the database.
    /// </para>
    /// <note>
    /// You need to be sure that all of the property names are unique within a 
    /// entity interface and any derived interfaces.
    /// </note>
    /// <para>
    /// The <see cref="IsTypeProperty"/> may be assigned <c>true</c> to generate a read-only
    /// property that returns the value specified by the <see cref="EntityAttribute.Type"/>
    /// value used to tag the interface.
    /// </para>
    /// <note>
    /// <see cref="IsTypeProperty"/>=<c>true</c> must be set for the base interface
    /// of a derived interface heirarchy.
    /// </note>
    /// <note>
    /// <see cref="Name"/> defaults to <see cref="Entity.EntityTypeName"/> if <see cref="IsTypeProperty"/>=<c>true</c>
    /// rather than the actual property name to standardize the serialized type property
    /// name on a compact string.  Applications can override this behavior by explicitly
    /// setting <see cref="Name"/>.
    /// </note>
    /// <para>
    /// <see cref="IsLink"/> may be set to <c>true</c> to have the entity perist a string
    /// reference to another entity, rather than the entity itself.  This is useful for Couchbase
    /// Lite scenarios, where you'd like an entity to reference another its document ID,
    /// but have that entity be accessable as a parent entity property.  This will simplify
    /// UX data binding, etc.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityPropertyAttribute : Attribute
    {
        /// <summary>
        /// The optional name to use when serializing the property.  This defaults
        /// to the defined property name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>
        /// Optionally indicates that the property will be used to serialize the
        /// entity type.  The tagged property's type must be either a <c>string</c> 
        /// or <c>enum</c> type.
        /// </para>
        /// <note>
        /// Entity type properties must define a getter but not a setter and may only
        /// be specified for root entity interfaces (not derived ones).
        /// </note>
        /// <note>
        /// You may not combine this with <see cref="Name"/> or <see cref="IsLink"/>=<c>true</c>.
        /// </note>
        /// <para>
        /// The <b>entity-gen</b> code generator will generate this as a read-only property
        /// that returns the value specified by the <see cref="EntityAttribute.Type"/>
        /// value used to tag the interface.
        /// </para>
        /// </summary>
        public bool IsTypeProperty { get; set; }

        /// <summary>
        /// Optionally indicates that the property is a reference to another entity rather
        /// than holding the entity itself.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Reference properties will persist a string value that identifies the specific 
        /// entity being referenced (e.g. a Couchbase Lite document ID or perhaps a file
        /// system path).  Advanced implementations can implement <see cref="IEntityContext"/> 
        /// to provide a way for entities to resolve the string reference into 
        /// the referenced entity instance.
        /// </para>
        /// <note>
        /// You may not combine this with <see cref="IsTypeProperty"/>=<c>true</c>.
        /// </note>
        /// </remarks>
        public bool IsLink { get; set; }
    }
}
