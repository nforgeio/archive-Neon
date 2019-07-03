//-----------------------------------------------------------------------------
// FILE:        ContactEmail.cs
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
    /// Information about a <see cref="Contact"/>'s email address from the
    /// device address book.
    /// </summary>
    public struct ContactEmail
    {
        /// <summary>
        /// The email address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The email address type.
        /// </summary>
        public ContactEmailType Type { get; set; }
    }
}
