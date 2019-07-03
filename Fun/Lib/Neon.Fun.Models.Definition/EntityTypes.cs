//-----------------------------------------------------------------------------
// FILE:	    EntityTypes.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Runtime.Serialization;

using Neon.Stack.Common;
using Neon.Stack.Data;

using Neon.Fun.Models.Bowling;
using Neon.Fun.Models.Signage;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Uniquely identifies Neon Fun entity and document types within the scope of
    /// all Neon services and applications.  The <b>nf.*</b> prefix (for <b>Neon Fun</b>)
    /// is reserved for this purpose.
    /// </summary>
    [Include(Namespace = FunConst.Namespace)]
    public enum EntityTypes
    {
        //---------------------------------------------------------------------
        // Generic types

        /// <summary>
        /// Maps to <see cref="IEmail"/>.
        /// </summary>
        [EnumMember(Value = "nf.email")]
        Email,

        /// <summary>
        /// Maps to <see cref="IPerson"/>.
        /// </summary>
        [EnumMember(Value = "nf.person")]
        Person,

        /// <summary>
        /// Maps to <see cref="IPhone"/>.
        /// </summary>
        [EnumMember(Value = "nf.phone")]
        Phone,

        /// <summary>
        /// Maps to <see cref="ILocation"/>.
        /// </summary>
        [EnumMember(Value = "nf.location")]
        Location,

        /// <summary>
        /// Maps to <see cref="ILatLon"/>.
        /// </summary>
        [EnumMember(Value = "nf.lat_lon")]
        LatLon,

        /// <summary>
        /// Maps to <see cref="IAccount"/>.
        /// </summary>
        [EnumMember(Value = "nf.account")]
        Account,

        /// <summary>
        /// Maps to <see cref="IEmail"/>.
        /// </summary>
        [EnumMember(Value = "nf.employee")]
        Employee,

        /// <summary>
        /// Maps to <see cref="ICustomer"/>.
        /// </summary>
        [EnumMember(Value = "nf.customer")]
        Customer,

        /// <summary>
        /// Maps to <see cref="IOrganization"/>.
        /// </summary>
        [EnumMember(Value = "nf.org")]
        Organization,

        /// <summary>
        /// Maps to <see cref="ICasino"/>.
        /// </summary>
        [EnumMember(Value = "nf.casino")]
        Casino,

        /// <summary>
        /// Maps to <see cref="Restaurant"/>.
        /// </summary>
        [EnumMember(Value = "nf.restaurant")]
        Restaurant,

        /// <summary>
        /// Maps to <see cref="ITask"/>.
        /// </summary>
        [EnumMember(Value = "nf.task")]
        Task,

        //---------------------------------------------------------------------
        // Bowling-specific types

        /// <summary>
        /// Maps to <see cref="ILeague"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.league")]
        BowlingLeague,

        /// <summary>
        /// Maps to <see cref="IProShop"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.proshop")]
        BowlingProShop,

        /// <summary>
        /// Maps to <see cref="IBowlingCenter"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.center")]
        BowlingCenter,

        /// <summary>
        /// Maps to <see cref="ILane"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.lane")]
        BowlingLane,

        /// <summary>
        /// Maps to <see cref="IBallInfo"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.ballspec")]
        BowlingBallSpecification,

        /// <summary>
        /// Maps to <see cref="IBallDetails"/>.
        /// </summary>
        [EnumMember(Value = "nf.bowl.balldetail")]
        BowlingBallDetails,

        //---------------------------------------------------------------------
        // Digital signage specific types
        
        /// <summary>
        /// Maps to <see cref="ISignageContentProperties"/> which describes the properties
        /// of a content package document.
        /// </summary>
        [EnumMember(Value = "nf.sign.content.properties")]
        SignageContentProperties,

        /// <summary>
        /// Maps to <see cref="ISignageContentPlayList"/> which holds the content to
        /// be played a signage player for a timeslot.
        /// </summary>
        [EnumMember(Value = "nf.sign.content.playlist")]
        SignageContentPlayList,
    }
}
