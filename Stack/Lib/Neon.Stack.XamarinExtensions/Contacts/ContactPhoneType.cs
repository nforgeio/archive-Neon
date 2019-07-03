//-----------------------------------------------------------------------------
// FILE:        ContactPhoneType.cs
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
    /// Enumerates the possible types of a contact phone numbers (e.g. mobile, work or home). 
    /// </summary>
    public enum ContactPhoneType
    {
        /// <summary>
        /// The phone number type is not known.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Identifies a home phone number.
        /// </summary>
        Home,

        /// <summary>
        /// Identifies a home fax number.
        /// </summary>
        HomeFax,

        /// <summary>
        /// Identifies a work phone number.
        /// </summary>
        Work,

        /// <summary>
        /// Identifies a work fax number.
        /// </summary>
        WorkFax,

        /// <summary>
        /// Identifies a pager.
        /// </summary>
        Pager,

        /// <summary>
        /// Identifies a mobile phone number.
        /// </summary>
        Mobile
    }
}
