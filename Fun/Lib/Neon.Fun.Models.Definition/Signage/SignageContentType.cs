//-----------------------------------------------------------------------------
// FILE:	    SignageContentType.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models.Signage
{
    /// <summary>
    /// Enumerates the known digital signage content types.
    /// </summary>
    [Include(Namespace = FunConst.SignageNamespace)]
    public enum SignageContentType
    {
        /// <summary>
        /// The content type couild not be identified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A slideshow consisting of one or more static images.
        /// </summary>
        Slideshow,
    }
}
