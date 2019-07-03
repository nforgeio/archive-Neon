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

namespace Neon.Stack.XamarinExtensions.Droid
{
    /// <summary>
    /// Handles Android common library initialization and global state.
    /// </summary>
    public static class Lib
    {
        /// <summary>
        /// Called by platform host applications during startup to initialize
        /// the library.
        /// </summary>
        /// <param name="hostApp">The parent android application.</param>
        public static void Initialize(XFormsApplicationDroid hostApp)
        {
            // Initialize the XLabs IoC container and components.

            if (!Resolver.IsSet)
            {
                var resolverContainer = new SimpleContainer();
                var app               = new XFormsAppDroid();

                app.Init(hostApp);

                resolverContainer
                    .Register<IDeviceHelpers>(t => new DeviceHelpers())
                    .Register<IDevice>(t => AndroidDevice.CurrentDevice)
                    .Register<IDisplay>(t => t.Resolve<IDevice>().Display)
                    .Register<IEmailService, EmailService>()
                    .Register<IPhoneService, PhoneService>()
                    .Register<IMediaPicker, MediaPicker>()
                    .Register<ITextToSpeechService, TextToSpeechService>()
                    .Register<IDependencyContainer>(resolverContainer)
                    .Register<IXFormsApp>(app)
                    .Register<ISecureStorage>(t => new KeyVaultStorage(t.Resolve<IDevice>().Id.ToCharArray()));

                Resolver.SetResolver(resolverContainer.GetResolver());
            }
            else
            {
                var app = Resolver.Resolve<IXFormsApp>() as IXFormsApp<XFormsApplicationDroid>;

                if (app != null)
                {
                    app.AppContext = hostApp;
                }
            }

            // Initialize the common PCL.

            global::Neon.Stack.XamarinExtensions.Lib.Initialize();
        }
    }
}
