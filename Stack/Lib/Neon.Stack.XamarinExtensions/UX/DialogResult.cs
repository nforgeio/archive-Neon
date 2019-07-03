//-----------------------------------------------------------------------------
// FILE:        DialogAction.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Enumerates common user dialog actions.
    /// </summary>
    public enum DialogResult
    {
        /// <summary>
        /// The user pressed <b>Cancel</b>.
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// The user pressed <b>OK</b>.
        /// </summary>
        OK,

        /// <summary>
        /// The user pressed <b>Yes</b>.
        /// </summary>
        Yes,

        /// <summary>
        /// The user pressed <b>No</b>.
        /// </summary>
        No,

        /// <summary>
        /// The user pressed <b>Create</b>.
        /// </summary>
        Create,

        /// <summary>
        /// The user pressed <b>Delete</b>.
        /// </summary>
        Delete
    }
}
