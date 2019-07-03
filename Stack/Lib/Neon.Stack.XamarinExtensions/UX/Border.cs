//-----------------------------------------------------------------------------
// FILE:        Border.cs
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
    /// Displays a rectangle with a border and optionally rounded corners.
    /// </summary>
    public class Border : ContentView
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The border color property.
        /// </summary>
        public static readonly BindableProperty BorderColorProperty
            = BindableProperty.Create("BorderColor", typeof(Color), typeof(Border), Color.Black);

        /// <summary>
        /// <b>Bindable:</b> The border color.  This defaults to <see cref="Color.Black"/>.
        /// </summary>
        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        /// <summary>
        /// The border thickness property.
        /// </summary>
        public static readonly BindableProperty BorderThicknessProperty
            = BindableProperty.Create("BorderThickness", typeof(Thickness), typeof(Border), new Thickness(DeviceHelper.Display.DipToDeviceStroke(2)));

        /// <summary>
        /// <b>Bindable:</b> The border thickness.  This defaults to <b>2dip (stroke)</b>.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// The corner radius property.
        /// </summary>
        public static readonly BindableProperty CornerRadiusProperty
            = BindableProperty.Create("CornerRadius", typeof(CornerRadius), typeof(Border), new CornerRadius(0.0));

        /// <summary>
        /// <b>Bindable:</b> The corner radius.  This defaults to <b>0</b>.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// The background color property.
        /// </summary>
        public static new readonly BindableProperty BackgroundColorProperty
            = BindableProperty.Create("BackgroundColor", typeof(Color), typeof(Border), Color.Transparent);

        /// <summary>
        /// <b>Bindable:</b> The background color.  This defaults to <see cref="Color.Transparent"/>.
        /// </summary>
        public new Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Constructor.
        /// </summary>
        public Border()
        {
        }
    }
}
