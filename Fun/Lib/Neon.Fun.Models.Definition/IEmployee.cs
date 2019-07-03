//-----------------------------------------------------------------------------
// FILE:	    IEmployee.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes an organization's employee.
    /// </summary>
    [Entity(Type = EntityTypes.Employee, Namespace = FunConst.Namespace)]
    public interface IEmployee : IAccount
    {
    }
}
