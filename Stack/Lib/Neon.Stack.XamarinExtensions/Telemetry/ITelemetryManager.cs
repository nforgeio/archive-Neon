//-----------------------------------------------------------------------------
// FILE:        ITelemetryManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// This interface defines methods to track different types of telemetry data.
    /// </summary>
    public interface ITelemetryManager
	{
        /// <summary>
        /// Tracks a custom event object
        /// </summary>
        /// <param name="eventName">The name of the custom event.</param>
        void TrackEvent(string eventName);

        /// <summary>
        /// Tracks a custom event object
        /// </summary>
        /// <param name="eventName">The name of the custom event.</param>
        /// <param name="properties">Custom properties that should be added to this event.</param>
        void TrackEvent(string eventName, Dictionary<string, string> properties);

        /// <summary>
        /// Tracks a custom message
        /// </summary>
        /// <param name="message">The message to be tracked.</param>
        void TrackTrace(string message);

        /// <summary>
        /// Tracks a custom message
        /// </summary>
        /// <param name="message">The message to be tracked.</param>
        /// <param name="properties">Custom properties that should be added to this message.</param>
        void TrackTrace(string message, Dictionary<string, string> properties);

        /// <summary>
        /// Tracks a custom metric.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="value">The numeric metric value.</param>
        void TrackMetric(string metricName, double value);

        /// <summary>
        /// Tracks a custom metric.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="value">The numeric metric value.</param>
        /// <param name="properties">Custom properties that should be added to this metric.</param>
        void TrackMetric(string metricName, double value, Dictionary<string, string> properties);

        /// <summary>
        /// Tracks a page view.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        void TrackPageView(string pageName);

        /// <summary>
        /// Tracks a page view.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="duration">The time the page was visible to the user</param>
        void TrackPageView(string pageName, int duration);

        /// <summary>
        /// Tracks a page view.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="duration">The time the page was visible to the user</param>
        /// <param name="properties">Custom properties that should be added to this page view object.</param>
        void TrackPageView(string pageName, int duration, Dictionary<string, string> properties);

        /// <summary>
        /// Tracks a managed handled/unhandled exception
        /// </summary>
        /// <param name="exception">The exception object that should be tracked</param>
        /// <param name="handled">If set to <c>true</c> (the default) the exception has been handled by the app.</param>
        void TrackManagedException(Exception exception, bool handled = true);
    }
}