//-----------------------------------------------------------------------------
// FILE:	    IPhone.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes a phone number.
    /// </summary>
    [Entity(Type = EntityTypes.Phone, Namespace = FunConst.Namespace)]
    public interface IPhone
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// Describes the phone number's use.
        /// </summary>
        [EntityProperty(Name = "use")]
        PhoneUses Use { get; set; }

        /// <summary>
        /// The phone number.
        /// </summary>
        [EntityProperty(Name = "number")]
        string Number { get; set; }

        /// <summary>
        /// Indicates if the number is to a mobile phone vs. a landline.
        /// </summary>
        [EntityProperty(Name = "mobile")]
        bool IsMobile { get; set; }
    }
}
