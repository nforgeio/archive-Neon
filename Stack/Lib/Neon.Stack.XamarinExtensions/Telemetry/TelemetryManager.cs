//-----------------------------------------------------------------------------
// FILE:        TelemetryManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

using Xamarin.Forms;

// $todo(jeff.lill): This is currently stubbed

namespace Neon.Stack.XamarinExtensions
{
	/// <summary>
	/// This class exposes functions to track different types of telemetry data.
	/// </summary>
	public static class TelemetryManager
	{
        /// <inherit/>
        public static void TrackEvent(string eventName)
		{
		}

        /// <inherit/>
		public static void TrackEvent(string eventName, Dictionary<string, string> properties)
		{
		}

        /// <inherit/>
		public static void TrackTrace(string message)
		{
		}

        /// <inherit/>
		public static void TrackTrace(string message, Dictionary<string, string> properties)
		{
		}

        /// <inherit/>
		public static void TrackMetric(string metricName, double value)
		{
		}

        /// <inherit/>
		public static void TrackMetric(string metricName, double value, Dictionary<string, string> properties)
		{
		}

        /// <inherit/>
		public static void TrackPageView(string pageName)
		{
		}

        /// <inherit/>
		public static void TrackPageView(string pageName, int duration)
		{
		}

        /// <inherit/>
		public static void TrackPageView(string pageName, int duration, Dictionary<string, string> properties)
		{
		}

        /// <inherit/>
		public static void TrackManagedException(Exception exception, bool handled = true)
		{
		}
	}
}
