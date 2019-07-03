//-----------------------------------------------------------------------------
// FILE:	    IPerson.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Defines the base properties for a person.
    /// </summary>
    [Entity(Type = EntityTypes.Person)]
    public interface IPerson
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// The person's globally unique ID.
        /// </summary>
        [EntityProperty(Name = "id")]
        string Id { get; set; }

        /// <summary>
        /// Indicates whether the person is currently enabled
        /// in the system.
        /// </summary>
        [EntityProperty(Name = "enabled")]
        bool IsEnabled { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        [EntityProperty(Name = "first")]
        string First { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        [EntityProperty(Name = "last")]
        string Last { get; set; }

        /// <summary>
        /// The person's phone numbers.
        /// </summary>
        [EntityProperty(Name = "phones")]
        IPhone[] Phones { get; set; }

        /// <summary>
        /// The person's email addresses.
        /// </summary>
        [EntityProperty(Name = "emails")]
        IEmail[] Emails { get; set; }
    }
}
