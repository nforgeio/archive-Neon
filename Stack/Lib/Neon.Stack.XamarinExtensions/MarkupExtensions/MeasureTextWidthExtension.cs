//-----------------------------------------------------------------------------
// FILE:        MeasureTextWidthExtension.cs
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
    /// Measures the width of the default <see cref="Text"/> property in device specific units.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension is useful for situations where you need to base one element's dimensions on 
    /// the text to be displayed within (e.g. for a Syncfusion grid column width or row height).
    /// The default font may be overridden via the <see cref="Font"/>, <see cref="FontSize"/>, 
    /// and <see cref="FontAttributes"/> properties.
    /// </para>
    /// <para>
    /// By default, only one line of text will be measured.  Use the <see cref="WidthConstraint"/>
    /// property to specify the maximum bounding width to be measured.  The extension will word-wrap
    /// the text within this constraint and measure the height of lines of text that will fit the
    /// width.
    /// </para>
    /// <para>
    /// Use the <see cref="Padding"/> property to add additional padding to the measurement returned.
    /// </para>
    /// <para>
    /// </para>
    /// <para>
    /// The markup extension properties listed below support XAML assignment via nested markup extensions.
    /// These all are <c>string</c> values that are parsed internally by the <see cref="ProvideValue"/>
    /// methods.  See <see cref="MarkupPropertyParser"/> for more information on how this works.
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="Text"/></item>
    ///     <item><see cref="Font"/></item>
    ///     <item><see cref="FontSize"/></item>
    ///     <item><see cref="WidthConstraint"/></item>
    ///     <item><see cref="Padding"/></item>
    /// </list>
    /// </remarks>
    [ContentProperty("Text")]
    public class MeasureTextWidthExtension : IMarkupExtension
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
        /// <remarks>
        /// If <see cref="MeasureMax"/> is <c>true</c> then this property may specify multiple
        /// strings separated by tilda (<b>~</b>) characters.  Each string is to be measured
        /// separately with the extension to return the result for the widest measured
        /// string.
        /// </remarks>
        public string Text { get; set; }

        /// <summary>
        /// Specifies whether the maximum width of a set of strings is to be measured.
        /// </summary>
        /// <remarks>
        /// Set this to <c>true</c> if the <see cref="Text"/> property may specify multiple
        /// strings separated by tilda (<b>~</b>) characters.  Each string is to be measured
        /// separately with the extension to return the result for the widest measured
        /// string.
        /// </remarks>
        public bool MeasureMax { get; set; }

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
        /// Specifies the width constraint to measure the height of word-wrapped text.
        /// This defaults to measuring only a single line of text.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
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
        /// Measures the width of the <see cref="Text"/> in device units.
        /// </summary>
        /// <param name="serviceProvider">The markup service provider.</param>
        /// <returns>The width of the <see cref="Text"/> in device units.</returns>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var text            = (string)MarkupPropertyParser.Parse<string>(this.Text, serviceProvider) ?? string.Empty;
            var font            = (string)MarkupPropertyParser.Parse<string>(this.Font, serviceProvider);
            var fontSize        = (double)MarkupPropertyParser.Parse<double>(this.FontSize, serviceProvider);
            var widthConstraint = (double)MarkupPropertyParser.Parse<double>(this.WidthConstraint, serviceProvider);
            var padding         = (Thickness)MarkupPropertyParser.Parse<Thickness>(this.Padding, serviceProvider);

            if (MeasureMax)
            {
                var textStrings = text.Split('~');

                if (textStrings.Length > 1)
                {
                    var maxWidth = 0.0;

                    foreach (var s in textStrings)
                    {
                        maxWidth = Math.Max(maxWidth, FontHelper.MeasureText(s, fontSize, widthConstraint, font, this.FontAttributes).Width);
                    }
                    return padding.Left + maxWidth + padding.Right;
                }
            }

            return padding.Left +
                   FontHelper.MeasureText(text, fontSize, widthConstraint, font, this.FontAttributes).Width +
                   padding.Right;
        }
    }
}
