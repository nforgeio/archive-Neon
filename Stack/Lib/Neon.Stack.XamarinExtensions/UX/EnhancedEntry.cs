//-----------------------------------------------------------------------------
// FILE:        EnhancedEntry.cs
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
    /// Enhances the Xamarin.Forms <see cref="Entry"/> control with additional properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The base Xamarin <see cref="Entry"/> control has a few issues:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///     The border color and width is platform specific and cannot be customized by
    ///     the application.
    ///     </item>
    ///     <item>
    ///     The keyboard spell-checking and auto-suggest is enabled by default and cannot
    ///     be easily disabled in XAML or cleanly in code.
    ///     </item>
    ///     <item>
    ///     The background color does not work on all platforms.
    ///     </item>
    /// </list>
    /// <para>
    /// The control and the associated platform specific renderers add the <see cref="BorderColor"/>
    /// and <see cref="BorderWidth"/> properties to manage the border, the <see cref="AutoSuggest"/>
    /// property to enable spellcheck, autosuggest, and sentence capitalization and code to ensure
    /// that the background color is honored.
    /// </para>
    /// <para>
    /// This control sets the default <see cref="Entry.TextColor"/> to <see cref="Color.Black"/>.
    /// </para>
    /// </remarks>
    public class EnhancedEntry : Entry
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The border color property.
        /// </summary>
        public static readonly BindableProperty BorderColorProperty
            = BindableProperty.Create("BorderColor", typeof(Color), typeof(EnhancedEntry), Color.Black);

        /// <summary>
        /// <b>Bindable:</b> Specifies the border color.  This defaults to <see cref="Color.Black"/>.
        /// </summary>
        public Color BorderColor 
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        /// <summary>
        /// The border width property.
        /// </summary>
        public static readonly BindableProperty BorderWidthProperty
            = BindableProperty.Create("BorderWidth", typeof(double), typeof(EnhancedEntry), DeviceHelper.Display.DipToDevicePosition(2));

        /// <summary>
        /// <b>Bindable:</b> Specifies the border width.  This defaults to <b>2dip</b>.
        /// </summary>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        /// <summary>
        /// The autosuggest property.
        /// </summary>
        public static readonly BindableProperty AutoSuggestProperty
            = BindableProperty.Create("AutoSuggest", typeof(bool), typeof(EnhancedEntry), false);

        /// <summary>
        /// <b>Bindable:</b> Specifies whether the device keyboard will capitalize the first word of sentences,
        /// spellcheck, and enable autosuggest.  This defaults to <c>false</c>.
        /// </summary>
        public bool AutoSuggest
        {
            get { return (bool)GetValue(AutoSuggestProperty); }
            set { SetValue(AutoSuggestProperty, value); }
        }

        //---------------------------------------------------------------------
        // Implementation

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnhancedEntry()
        {
            TextColor = Color.Black;
        }
    }
}
