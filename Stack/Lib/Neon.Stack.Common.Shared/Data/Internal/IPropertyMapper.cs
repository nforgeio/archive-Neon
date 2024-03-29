﻿//-----------------------------------------------------------------------------
// FILE:	    IPropertyMapper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json.Linq;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Stack.Data.Internal
{
    /// <summary>
    /// <b>Platform use only:</b> Defines common members implemented by entity
    /// property mapper classes.
    /// </summary>
    /// <remarks>
    /// <note>
    /// This class is intended for use only by classes generated by the 
    /// <b>entity-gen</b>.
    /// </note>
    /// </remarks>
    public interface IPropertyMapper
    {
        /// <summary>
        /// Returns the JSON property name..
        /// </summary>
        string JsonName { get; }

        /// <summary>
        /// Returns the entity property name.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// Maps or re-maps a <see cref="JProperty"/>. 
        /// </summary>
        /// <param name="newProperty">The property.</param>
        /// <param name="reload">Optionally specifies that the model is being reloaded.</param>
        /// <remarks>
        /// <returns>
        /// <c>true</c> if the new property was different from the existing one 
        /// and updates were applied.
        /// </returns>
        /// <note>
        /// Pass <paramref name="reload"/>=<c>true</c> to reload data from a new 
        /// <see cref="JObject"/> into the model.  In this case, the implementation
        /// must ensure that all appropriate property and collection change notifications 
        /// are raised to ensure that any listening UX elements will be updated.
        /// </note>
        /// </remarks>
        bool Load(JProperty newProperty, bool reload = false);
    }
}
