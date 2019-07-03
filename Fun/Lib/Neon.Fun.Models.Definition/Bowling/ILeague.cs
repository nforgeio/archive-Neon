//-----------------------------------------------------------------------------
// FILE:	    ILeague.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Bowling
{
    /// <summary>
    /// Describes a bowling league organization.
    /// </summary>
    [Entity(Type = EntityTypes.BowlingLeague, Namespace = FunConst.BowlingNamespace)]
    public interface ILeague : IOrganization
    {
    }
}
