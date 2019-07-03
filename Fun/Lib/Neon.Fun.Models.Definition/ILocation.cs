//-----------------------------------------------------------------------------
// FILE:	    ILocation.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes a physical mailing address, patterned after:
    /// <a href="https://schema.org/PostalAddress">https://schema.org/PostalAddress</a>
    /// combined with optional lat/lon coordinates.
    /// </summary>
    [Entity(Type = EntityTypes.Location, Namespace = FunConst.Namespace)]
    public interface ILocation
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// Identifies the address uses.
        /// </summary>
        [EntityProperty(Name = "uses")]
        LocationUses[] Uses { get; set; }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/ISO_3166-1">ISO 3166-1 Alpha-2</a> 
        /// two-character country code.
        /// </summary>
        [EntityProperty(Name = "country_code")]
        string CountryCode { get; set; }

        /// <summary>
        /// The region (e.g. State).
        /// </summary>
        [EntityProperty(Name = "region")]
        string Region { get; set; }

        /// <summary>
        /// The locality (e.g. City).
        /// </summary>
        [EntityProperty(Name = "locality")]
        string Locality { get; set; }

        /// <summary>
        /// The postal box number.
        /// </summary>
        [EntityProperty(Name = "box_number")]
        string BoxNumber { get; set; }

        /// <summary>
        /// First line of a street address.
        /// </summary>
        [EntityProperty(Name = "street1")]
        string Street1 { get; set; }

        /// <summary>
        /// Second line of a street address (e.g. Building, Floor, Suite,...)
        /// </summary>
        [EntityProperty(Name = "street2")]
        string Street2 { get; set; }

        /// <summary>
        /// Postal code.
        /// </summary>
        [EntityProperty(Name = "postal_code")]
        string PostalCode { get; set; }

        /// <summary>
        /// Optional lat/lon coordinates.
        /// </summary>
        [EntityProperty(Name = "coordinates")]
        ILatLon Coordinates { get; set; }
    }
}
