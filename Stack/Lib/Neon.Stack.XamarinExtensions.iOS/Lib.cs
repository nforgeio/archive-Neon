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

using Neon.Stack.XamarinExtensions;

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Handles iOS common library initialization and global state.
    /// </summary>
    public static class Lib
    {
        /// <summary>
        /// Called by platform host applications during startup to initialize
        /// the library.
        /// </summary>
        /// <param name="appDelegate">The host application delegate.</param>
        public static void Initialize(XFormsApplicationDelegate appDelegate)
        {
            // Initialize the XLabs IoC container and components.

            var resolverContainer = new SimpleContainer();
            var app               = new XFormsAppiOS();

            app.Init(appDelegate);

            resolverContainer
                .Register<IDeviceHelpers>(t => new DeviceHelpers())
                .Register<IDevice>(t => AppleDevice.CurrentDevice)
                .Register<IDisplay>(t => t.Resolve<IDevice>().Display)
                .Register<ITextToSpeechService, TextToSpeechService>()
                .Register<IEmailService, EmailService>()
                .Register<IPhoneService, PhoneService>()
                .Register<IMediaPicker, MediaPicker>()
                .Register<IXFormsApp>(app)
                .Register<ISecureStorage, SecureStorage>()
                .Register<IDependencyContainer>(t => resolverContainer);

            Resolver.SetResolver(resolverContainer.GetResolver());

            // Initialize the common PCL.

            global::Neon.Stack.XamarinExtensions.Lib.Initialize();
        }
    }
}
