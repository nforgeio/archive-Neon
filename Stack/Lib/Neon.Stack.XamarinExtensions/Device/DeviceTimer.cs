//-----------------------------------------------------------------------------
// FILE:        DeviceTimer.cs
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
    /// Implements a Xamarin device timer that periodically invokes an action and
    /// is also disposable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses the Xamarin <see cref="Device.StartTimer(TimeSpan, Func{bool})"/> method to start
    /// the timer.  The action will be invoked the first time after the timer interval has elapsed and then
    /// it will be invoked again each time the interval elapses.  The timer will continue to run until
    /// disposed.
    /// </para>
    /// <para>
    /// This class provides for cleaner usage patterns.
    /// </para>
    /// </remarks>
    public sealed class DeviceTimer : IDisposable
    {
        private Action action;      // The action to be performed or NULL if the timer is disposed.

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interval">The interval that the action will be performed.</param>
        /// <param name="action">The action.</param>
        /// <remarks>
        /// <note>
        /// The action will be invoked for the first time after waiting for the
        /// <paramref name="interval"/> to elapse.
        /// </note>
        /// <note>
        /// The timer will log any exceptions thrown by the action and then
        /// continue running.
        /// </note>
        /// </remarks>
        public DeviceTimer(TimeSpan interval, Action action)
        {
            this.action = action;

            Device.StartTimer(interval,
                () =>
                {
                    var currentAction = this.action;

                    if (currentAction == null)
                    {
                        return false;
                    }

                    try
                    {
                        currentAction();
                    }
                    catch (Exception e)
                    {
                        TelemetryManager.TrackManagedException(e);
                    }

                    return true;
                });
        }

        /// <summary>
        /// Stops and releases the timer.
        /// </summary>
        public void Dispose()
        {
            action = null;
        }
    }
}
