//-----------------------------------------------------------------------------
// FILE:        FormsHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Global Xamarin.Forms related utilities.
    /// </summary>
    public static class FormsHelper
    {
        /// <summary>
        /// Returns a <see cref="GridLength"/> initialized to <b>*</b> (equivalant to <b>1*</b>).
        /// </summary>
        public static GridLength GridLengthStar { get; private set; } = new GridLength(1.0, GridUnitType.Star);

        /// <summary>
        /// Returns a <see cref="Thickness"/> set to zero on all sides.
        /// </summary>
        public static Thickness ZeroThickness { get; private set; } = new Thickness(0);

        /// <summary>
        /// Asynchronously invokes multiple tasks in parallel.
        /// </summary>
        /// <param name="actions">The task actions.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task ParallelInvokeAsync(params Func<Task>[] actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return;
            }

            var tasks = new Task[actions.Length];

            for (int i = 0; i < actions.Length; i++)
            {
                tasks[i] = actions[i]();
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        /// <summary>
        /// Used to indicate that an asynchronous operation is intentionally being
        /// launched without waiting for its completion.
        /// </summary>
        /// <param name="task">The operation <see cref="Task"/>.</param>
        /// <remarks>
        /// This method actually does nothing other than prevent a compiler warning 
        /// and indicate programmer intention.
        /// </remarks>
        public static void FireAndForget(Task task)
        {
        }

        /// <summary>
        /// Used to indicate that an asynchronous operation is intentionally being
        /// launched without waiting for its completion.
        /// </summary>
        /// <typeparam name="TResult">The task result type.</typeparam>
        /// <param name="task">The operation <see cref="Task"/>.</param>
        /// <remarks>
        /// This method actually does nothing other than prevent a compiler warning 
        /// and indicate programmer intention.
        /// </remarks>
        public static void FireAndForget<TResult>(Task<TResult> task)
        {
        }

        /// <summary>
        /// Converts a time from local to UTC.
        /// </summary>
        /// <param name="time">The local time.</param>
        /// <returns>The UTC time.</returns>
        public static DateTime ToUniversal(DateTime time)
        {
            return new DateTime(time.Ticks, DateTimeKind.Local).ToUniversalTime();
        }

        /// <summary>
        /// Converts a time from UTC to local.
        /// </summary>
        /// <param name="time">The UTC time.</param>
        /// <returns>The local time.</returns>
        public static DateTime ToLocal(DateTime time)
        {
            return new DateTime(time.Ticks, DateTimeKind.Utc).ToLocalTime();
        }
    }
}
