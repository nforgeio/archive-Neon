﻿//-----------------------------------------------------------------------------
// FILE:	    EntityAttribute.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Stack.Data
{
    /// <summary>
    /// Used to tag an <c>interface</c> such that the <b>entity-gen</b> tool will 
    /// automatically generate equivalent data model classes derived from
    /// <see cref="Entity"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Optional name for the generated class; otherwise the name will
        /// default to the interface name with the leading "I" character
        /// removed (if present).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional namespace for the generated class; otherwise the namespace
        /// will default to the namespace of the tagged interface.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Optionally indicates that the generated class will be declared as <c>internal</c>
        /// rather than <c>public</c>, the default.
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// <para>
        /// Optional entity type identifier.  This can be set to a <c>string</c> or <c>enum</c> 
        /// value uniquely identifying the entity type within the application domain.
        /// </para>
        /// <note>
        /// This property must be set for derived <c>interface</c> definitions.
        /// </note>
        /// </summary>
        public object Type { get; set; }
    }
}
