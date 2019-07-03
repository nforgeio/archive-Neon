//-----------------------------------------------------------------------------
// FILE:        MathExtension.cs
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

// $todo(jeff.lill): Complete this

#if TODO

namespace Neon.Stack.Xam
{
    /// <summary>
    /// Implements basic math operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension is useful for performing basic math operations in markup, like multiplying
    /// a font size by a number.  The default <see cref="Value"/> property should be set to the
    /// <c>int</c>, <c>long</c>, <c>double</c>, or <c>float</c> value to be manipulated, <see cref="Operation"/>
    /// needs to be specified as one of <b>Add</b>, <b>Subtract</b>, <b>Multiply</b>, or <b>Divide</b>
    /// and <see cref="Arg0"/> through <see cref="Arg9"/> specify the operation arguments.
    /// </para>
    /// <para>
    /// The extension computes the result by repeating the operation for each of the arguments
    /// specified in the markup.  For example:
    /// </para>
    /// <para>
    /// <b>{c:Math {c:Static FontHelper.Small}, Operation=Add, Arg0=10, Arg1=20}</b>
    /// </para>
    /// <para>
    /// will add obtain the size of the small font, add 10 and 20 to it and then return the result.
    /// </para>
    /// <para>
    /// The markup extension properties listed below support XAML assignment via nested markup extensions.
    /// These all are <c>string</c> values that are parsed internally by the <see cref="ProvideValue"/>
    /// methods.  See <see cref="MarkupPropertyParser"/> for more information on how this works.
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="Value"/></item>
    ///     <item><see cref="Arg0"/></item>
    ///     <item><see cref="Arg1"/></item>
    ///     <item><see cref="Arg2"/></item>
    ///     <item><see cref="Arg3"/></item>
    ///     <item><see cref="Arg4"/></item>
    ///     <item><see cref="Arg5"/></item>
    ///     <item><see cref="Arg6"/></item>
    ///     <item><see cref="Arg7"/></item>
    ///     <item><see cref="Arg8"/></item>
    ///     <item><see cref="Arg9"/></item>
    /// </list>
    /// </remarks>
    [ContentProperty("Value")]
    public class MathExtension : IMarkupExtension
    {
        /// <summary>
        /// <para>
        /// The text to be measured.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the font name.  Set <c>null</c> or the empty string for
        /// the default device font.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Font { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the font size.  This defaults to <b>0.0</b> indicating that
        /// a reasonable default size will be used.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string FontSize { get; set; } = "0.0";

        /// <summary>
        /// Specifies the font attributes.
        /// </summary>
        public FontAttributes FontAttributes { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the number of lines of text to be measured.  Defaults to <b>0</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string LineCount { get; set; } = "1";

        /// <summary>
        /// <para>
        /// Specifies the device specific spacing between the lines of text being measured if 
        /// <see cref="LineCount"/> is specified.  Defaults to <b>0</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string LineSpacing { get; set; } = "0";

        /// <summary>
        /// <para>
        /// Specifies the width constraint to measure the height of word-wrapped text.
        /// This defaults to measuring only a single line of text.
        /// </para>
        /// </summary>
        public string WidthConstraint { get; set; } = int.MaxValue.ToString();

        /// <summary>
        /// <para>
        /// Specifies any padding to be included in the measurement.  This defaults
        /// to <b>0</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Padding { get; set; } = "0";

        /// <summary>
        /// Measures the height of the <see cref="Text"/> in device units.
        /// </summary>
        /// <param name="serviceProvider">The markup service provider.</param>
        /// <returns>The height of the <see cref="Text"/> in device units.</returns>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var text            = (string)MarkupPropertyParser.Parse<string>(this.Text, serviceProvider) ?? string.Empty;
            var font            = (string)MarkupPropertyParser.Parse<string>(this.Font, serviceProvider);
            var fontSize        = (double)MarkupPropertyParser.Parse<double>(this.FontSize, serviceProvider);
            var lineCount       = (int)MarkupPropertyParser.Parse<int>(this.LineCount, serviceProvider);
            var lineSpacing     = (int)MarkupPropertyParser.Parse<int>(this.LineSpacing, serviceProvider);
            var widthConstraint = (double)MarkupPropertyParser.Parse<double>(this.WidthConstraint, serviceProvider);
            var padding         = (Thickness)MarkupPropertyParser.Parse<Thickness>(this.Padding, serviceProvider);

            if (lineSpacing > 0)
            {
                var lineHeight = FontHelper.MeasureText(" ", fontSize, int.MaxValue, font, this.FontAttributes).Height;

                return padding.Top + 
                       lineHeight * lineCount + 
                       lineSpacing * (lineCount - 1) +
                       padding.Bottom;
            }
            else
            {
                return FontHelper.MeasureText(text, fontSize, widthConstraint, font, this.FontAttributes).Height * lineCount + padding.Top + padding.Bottom;
            }
        }
    }
}

#endif