//-----------------------------------------------------------------------------
// FILE:        ContactEmailType.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using XLabs.Forms.Services;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services;
using XLabs.Platform.Services.Email;
using XLabs.Platform.Services.Media;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Enumerates the possible types of a contact email addresses (e.g. work or home). 
    /// </summary>
    public enum ContactEmailType
    {
        /// <summary>
        /// The email type is not known.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Identifies a home email address.
        /// </summary>
        Home,

        /// <summary>
        /// Identifies a work email address.
        /// </summary>
        Work
    }
}
