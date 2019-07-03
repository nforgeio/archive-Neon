//-----------------------------------------------------------------------------
// FILE:        Lib.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XLabs.Forms;
using XLabs.Forms.Services;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Mvvm;
using XLabs.Platform.Services;
using XLabs.Platform.Services.Email;
using XLabs.Platform.Services.Media;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Handles library initialization.
    /// </summary>
    public static class Lib
    {
        /// <summary>
        /// Called by the platform specific common library's initialization method 
        /// to initialize the common library.
        /// </summary>
        public static void Initialize()
        {
            DeviceHelper.Initialize();
        }
    }
}
