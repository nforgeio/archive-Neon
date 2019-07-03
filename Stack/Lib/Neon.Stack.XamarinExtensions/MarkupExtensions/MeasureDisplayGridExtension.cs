//-----------------------------------------------------------------------------
// FILE:        MeasureDisplayGridExtension.cs
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
    /// Implements a XAML markup extension that computes a grid measurement based on
    /// the dimensions of the device's display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension is useful for situations where you need to base the size of
    /// an display element on the size of the device display.  For example, you 
    /// may need to create a grid row of three buttons with each button sized at 
    /// approximately 1/3 of the display width.
    /// </para>
    /// <para>
    /// The <see cref="Dimension"/> property specifies which dimension you wish
    /// to measure, <see cref="Units"/> specifies the desired output units,
    /// <see cref="Slices"/> specifies the number of slices to cut the dimension,
    /// <see cref="Spacing"/> is the spacing between each slice, 
    /// <see cref="Padding"/> specifies the padding that will be deduced from 
    /// the display dimension before the the slice size is computed, and
    /// <see cref="Portion"/> is a double typically between 0.0 and 1.0 specifying 
    /// the portion of the display dimension to measure after removing the
    /// padding.
    /// </para>
    /// <para>
    /// This extension works by:
    /// </para>
    /// <list type="number">
    ///     <item>Querying the device for the actual display dimension in pixels.</item>
    ///     <item>Converting the dimension to device independent units.</item>
    ///     <item>Subtracting any <see cref="Padding"/>.</item>
    ///     <item>Multiplying the result by <see cref="Portion"/>.</item>
    ///     <item>Using <see cref="Slices"/> and <see cref="Spacing"/> to compute the size of each slice.</item>
    ///     <item>Returning the computed slice size in the desired units.</item>
    /// </list>
    /// <para>
    /// The markup extension properties listed below support XAML assignment via nested markup extensions.
    /// These all are <c>string</c> values that are parsed internally by the <see cref="ProvideValue"/>
    /// methods.  See <see cref="MarkupPropertyParser"/> for more information on how this works.
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="Portion"/></item>
    ///     <item><see cref="Slices"/></item>
    ///     <item><see cref="Spacing"/></item>
    ///     <item><see cref="Padding"/></item>
    /// </list>
    /// </remarks>
    [ContentProperty("Dimension")]
    public class MeasureDisplayGridExtension : IMarkupExtension
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MeasureDisplayGridExtension()
        {
        }

        /// <summary>
        /// Specifies the dimension of the device display to be measured.  This defaults
        /// to <see cref="MeasureDisplayExtension.MeasureDimension.PortraitWidth"/>.
        /// </summary>
        public MeasureDisplayExtension.MeasureDimension Dimension { get; set; } = MeasureDisplayExtension.MeasureDimension.PortraitWidth;

        /// <summary>
        /// Specifies the units to be used to make the measurement.  This defaults
        /// to <see cref="MeasureDisplayExtension.MeasureUnits.Du"/>.
        /// </summary>
        public MeasureDisplayExtension.MeasureUnits Units { get; set; } = MeasureDisplayExtension.MeasureUnits.Du;

        /// <summary>
        /// <para>
        /// Specifies the number of sections the dimension is to be split into with the
        /// measurement returned being the size of a single slice.  This defaults to <b>1</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Slices { get; set; } = "1";

        /// <summary>
        /// <para>
        /// Specifies the spacing between the slices in device specific units, potentially 
        /// reducing the size of each slice.  This defaults to <b>0.0</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Spacing { get; set; } = "0.0";

        /// <summary>
        /// <para>
        /// Specifies the portion of the display to be measured after removing any
        /// <see cref="Padding"/>.  This defaults to <b>1.0</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Portion { get; set; } = "1.0";

        /// <summary>
        /// <para>
        /// Specifies the padding that will be deduced from the display dimension before
        /// computing the slice size.  This defaults to <b>zero</b>.
        /// </para>
        /// <note type="note">
        /// This property supports XAML assignment via nested markup extensions.
        /// See <see cref="MarkupPropertyParser"/> for more information.
        /// </note>
        /// </summary>
        public string Padding { get; set; } = "0";

        /// <summary>
        /// Converts the input value into device units.
        /// </summary>
        /// <param name="serviceProvider">The markup service provider.</param>
        /// <returns>The input device independent (160 DPI) value(s) converted to device specific units.</returns>
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            // We're simply going to instantiate a [MeasureDisplayExtension] instance,
            // copy the property values and then use it to obtain the display
            // dimension and then return it as a [GridLength].

            var measureDisplay = new MeasureDisplayExtension()
            {
                Dimension = this.Dimension,
                Padding   = this.Padding,
                Portion   = this.Portion,
                Slices    = this.Slices,
                Spacing   = this.Spacing,
                Units     = this.Units
            };

            return new GridLength((double)measureDisplay.ProvideValue(serviceProvider));
        }
    }
}
