//-----------------------------------------------------------------------------
// FILE:        FontHelper.cs
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
    /// Device font related helpers.
    /// </summary>
    public static class FontHelper
    {
        /// <summary>
        /// Returns the standard extra huge font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XHuge}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XHuge { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label)) * 2.5;

        /// <summary>
        /// Returns the standard huge font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.Huge}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double Huge { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label)) * 2.0;

        /// <summary>
        /// Returns the standard extra large font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XXXLarge}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XXXLarge { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label)) * 1.75;

        /// <summary>
        /// Returns the standard extra large font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XXLarge}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XXLarge { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label)) * 1.50;

        /// <summary>
        /// Returns the standard extra large font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XLarge}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XLarge { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label)) * 1.25;

        /// <summary>
        /// Returns the standard large font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.Large}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double Large { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Large, typeof(Label));

        /// <summary>
        /// Returns the standard medium font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.Medium}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double Medium { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Medium, typeof(Label));

        /// <summary>
        /// Returns a font size between medium and small for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.MediumSmall}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double MediumSmall
        {
            get
            {
                return (Medium + Small) / 2.0;
            }
        }

        /// <summary>
        /// Returns the standard small font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.Small}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double Small { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Small, typeof(Label));

        /// <summary>
        /// Returns the standard extra small font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XSmall}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XSmall { get; private set; }
            = (global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Small, typeof(Label)) + global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Micro, typeof(Label))) * 0.6666667;

        /// <summary>
        /// Returns the standard extra extra small font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.XXSmall}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double XXSmall { get; private set; }
            = (global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Small, typeof(Label)) + global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Micro, typeof(Label))) * 0.3333333;

        /// <summary>
        /// Returns the standard micro font size for the current device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The static font size properties are designed to return consistent
        /// font sizes based on the font displayed by a <see cref="Label"/> element.
        /// The <see cref="Device.GetNamedSize(NamedSize, Element)"/> related
        /// methods may return different sizes based on the element type.
        /// </para>
        /// <para>
        /// Use the <b>{Static FontHelper.Micro}</b> markup extension to use one of these values
        /// in your XAML markup.
        /// </para>
        /// <note type="note">
        /// The value returned appears to be in units that vary by the device
        /// platform.  For Android, the units appear to be pixels.  I suspect that
        /// Windows Phone and iOS will return device specific units.
        /// </note>
        /// </remarks>
        public static double Micro { get; private set; }
            = global::Xamarin.Forms.Device.GetNamedSize(NamedSize.Micro, typeof(Label));

        /// <summary>
        /// Measures the dimensions of a string as it will be rendered using the specified font
        /// and size.
        /// </summary>
        /// <param name="text">The string to be measured.</param>
        /// <param name="fontSize">The optional font size in device specific units.</param>
        /// <param name="width">The optional width to constrain the text to measure with word wrapping.</param>
        /// <param name="fontName">The optional font name to override the default.</param>
        /// <param name="fontAttributes">The optional font attributes.;</param>
        /// <returns>The <see cref="Size"/> specifying the text height and width in device specific units.</returns>
        /// <remarks>
        /// <para>
        /// Pass the string you want to measure as <paramref name="text"/> and the font
        /// size as <paramref name="fontSize"/>.  The result will describe the height and 
        /// width of the text using the device's default font.  You can specify a custom
        /// font using <paramref name="fontName"/>.
        /// </para>
        /// <para>
        /// By default, the height returned will be for one line of text.  Use <paramref name="width"/>
        /// to constrain the width of the text rendered to word-wrap the text.  In this case,
        /// the height returned will account for the number of text lines required to render
        /// the string.
        /// </para>
        /// </remarks>
        public static Size MeasureText(string text, double fontSize = 0.0, double width = int.MaxValue, string fontName = null, FontAttributes fontAttributes = FontAttributes.None)
        {
            return DeviceHelper.Implementation.MeasureText(text, fontSize, width, fontName, fontAttributes);
        }
    }
}
