//-----------------------------------------------------------------------------
// FILE:        CornerRadiusConverter.cs
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
    /// Handles conversion between a string and a <see cref="CornerRadius"/> instance
    /// while parsing XAML.
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
    public class CornerRadiusTypeConverter : TypeConverter
    {
        //---------------------------------------------------------------------
        // Static members

        private static char[] separator = new char[] { ' ' };

        /// <summary>
        /// Parses an input field.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The parsed value.</returns>
        private static double Parse(string input)
        {
            return DeviceHelper.Display.ParseMeasurePosition(input);
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Indicates whether the converter can convert from a given type.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <returns><c>true</c> if the conversion is supported.</returns>
        public override bool CanConvertFrom(Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Converts the value passed into a <see cref="CornerRadius"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="value">The string input value.</param>
        /// <returns>The parsed <see cref="CornerRadius"/>.</returns>
        public override object ConvertFrom(CultureInfo culture, object value)
        {
            // We're expecting one or four space separated floating point values.

            var input  = (string)value;
            var fields = input.Split(separator);

            try
            {
                switch (fields.Length)
                {
                    case 1:

                        return new CornerRadius(Parse(fields[0]));

                    case 4:

                        var left   = Parse(fields[0]);
                        var top    = Parse(fields[1]);
                        var right  = Parse(fields[2]);
                        var bottom = Parse(fields[3]);

                        return new CornerRadius(left, top, right, bottom);

                    default:

                        throw new FormatException();
                }
            }
            catch
            {
                throw new FormatException($"[{input}] is not a valid corner radius.");
            }
        }
    }
}
