//-----------------------------------------------------------------------------
// FILE:        MeasureRadiusExtension.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements an XAML markup extension that converts device independent units,
    /// inches, and pixels to device specific units to a <see cref="CornerRadius"/>.  
    /// The extension accepts one or four double values with optional unit suffixes
    /// separated by spaces.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// Modern Android and iOS devices measure screen objects at 160 DPI and Windows
    /// Phone devices use 240 DPI.
    /// </note>
    /// <para>
    /// The input may include one or four double input values.  These are floating
    /// point values with an optional unit suffix:
    /// </para>
    /// <list type="table">
    /// <item>
    ///     <term><b>(none)</b> or <b>dip</b></term>
    ///     <description>
    ///     Specifies a measurement in device independent units measured at
    ///     approximately 160 DPI.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>px</b></term>
    ///     <description>
    ///     Specifies a measurment in pixels.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>in</b></term>
    ///     <description>
    ///     Specifies a measurment in inches.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>du</b></term>
    ///     <description>
    ///     Specifies a measurement in device specific units that don't adjust
    ///     for the iOS/Android and Windows Phone scale factor difference.
    ///     </description>
    /// </item>
    /// </list>
    /// <para>
    /// Note that the default without specifiying a unit is device independent units
    /// (<b>dip</b>) since these will be used most often.  This abstracts away the
    /// differences between iOS/Android and Windows Phone.  Note that Android clusters
    /// devices into a handful of display density scale factors, so specifying the
    /// same device independent unit may result in somewhat differing physical sizes
    /// on different devices.
    /// </para>
    /// </remarks>
    [ContentProperty("Value")]
    public class MeasureRadiusExtension : IMarkupExtension
    {
        //---------------------------------------------------------------------
        // Static members

        private static char[] separator = new char[] { ' ' };

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// The input value measured in 160 DPI device independent units.  This may be
        /// a one, two or four double values separated by spaces.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Converts <see cref="Value"/> (the input value) into device units.
        /// </summary>
        /// <param name="serviceProvider">The markup service provider.</param>
        /// <returns>The input device independent (160 DPI) value(s) converted to device specific units.</returns>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            // We're expecting one or four space separated floating point values.

            var value  = Value ?? string.Empty;
            var fields = value.Trim().Split(separator);

            try
            {
                switch (fields.Length)
                {
                    case 1:

                        return new CornerRadius(DeviceHelper.Display.ParseMeasurePosition(fields[0]));

                    case 4:

                        var left   = DeviceHelper.Display.ParseMeasurePosition(fields[0]);
                        var top    = DeviceHelper.Display.ParseMeasurePosition(fields[1]);
                        var right  = DeviceHelper.Display.ParseMeasurePosition(fields[2]);
                        var bottom = DeviceHelper.Display.ParseMeasurePosition(fields[3]);

                        return new CornerRadius(left, top, right, bottom);

                    default:

                        throw new FormatException();
                }
            }
            catch
            {
                throw new FormatException($"[{Value}] is not a valid measure.");
            }
        }
    }
}
