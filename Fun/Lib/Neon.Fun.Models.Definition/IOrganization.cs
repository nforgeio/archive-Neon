//-----------------------------------------------------------------------------
// FILE:	    IOrganization.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes a base organization.
    /// </summary>
    [Entity(Type = EntityTypes.Organization, Namespace = FunConst.Namespace)]
    public interface IOrganization
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// The organization's globally unique ID.
        /// </summary>
        [EntityProperty(Name = "id")]
        string Id { get; set; }

        /// <summary>
        /// Indicates whether the organization is currently enabled
        /// in the system.
        /// </summary>
        [EntityProperty(Name = "enabled")]
        bool IsEnabled { get; set; }

        /// <summary>
        /// The organization name.
        /// </summary>
        [EntityProperty(Name = "name")]
        string Name { get; set; }

        /// <summary>
        /// The organization's <see cref="ILocation"/>es.
        /// </summary>
        [EntityProperty(Name = "addresses")]
        ILocation[] Addresses { get; set; }

        /// <summary>
        /// The organization's <see cref="IAccount"/>s.
        /// </summary>
        [EntityProperty(Name = "@users", IsLink = true)]
        IAccount[] Users { get; set; }

        /// <summary>
        /// Links to any parent organizations.
        /// </summary>
        [EntityProperty(Name = "@parents", IsLink = true)]
        IOrganization[] Parents { get; set; }

        /// <summary>
        /// Links to any child organizations.
        /// </summary>
        [EntityProperty(Name = "@children", IsLink = true)]
        IOrganization[] Children { get; set; }
    }
}
