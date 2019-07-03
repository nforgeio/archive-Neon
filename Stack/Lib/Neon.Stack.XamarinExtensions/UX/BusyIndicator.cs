//-----------------------------------------------------------------------------
// FILE:        BusyIndicator.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using Syncfusion.SfBusyIndicator.XForms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements a custom busy indicator intended to overlay the current page, present
    /// an animated busy indication as well as to intercept any user gestures made while
    /// the application is busy.
    /// </summary>
    public class BusyIndicator : Grid
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The forground color property.
        /// </summary>
        public static readonly BindableProperty ColorProperty 
            = BindableProperty.Create("Color", typeof(Color), typeof(BusyIndicator), Color.White,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var indicator = (BusyIndicator)bindable;

                    indicator.sfBusyIndicator.TextColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The foreground color.  This defaults to <see cref="Color.White"/>.
        /// </summary>
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// The indicator box background color property.
        /// </summary>
        public static readonly BindableProperty BoxColorProperty
            = BindableProperty.Create("BoxColor", typeof(Color), typeof(BusyIndicator), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var indicator = (BusyIndicator)bindable;

                    indicator.border.BackgroundColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The indicator box background color.  This defaults to <see cref="Color.Black"/>.
        /// </summary>
        public Color BoxColor
        {
            get { return (Color)GetValue(BoxColorProperty); }
            set { SetValue(BoxColorProperty, value); }
        }

        /// <summary>
        /// The screen overlay color property.
        /// </summary>
        public static readonly BindableProperty OverlayColorProperty
            = BindableProperty.Create("OverlayColor", typeof(Color), typeof(BusyIndicator), Color.Transparent,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var indicator = (BusyIndicator)bindable;

                    indicator.BackgroundColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The screen overlay color typically used to dim the screen behind the busy
        /// indicator using the alpha channel.  This defaults to <see cref="Color.Transparent"/>.
        /// </summary>
        public Color OverlayColor
        {
            get { return (Color)GetValue(OverlayColorProperty); }
            set { SetValue(OverlayColorProperty, value); }
        }

        /// <summary>
        /// The size property: width and height of the indicator.
        /// </summary>
        public static readonly BindableProperty SizeProperty
            = BindableProperty.Create("Size", typeof(double), typeof(BusyIndicator), DeviceHelper.Display.InchToDevicePosition(0.5),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var indicator = (BusyIndicator)bindable;

                    indicator.SetSize((double)newValue);
                });

        /// <summary>
        /// <b>Bindable:</b> Specifies the width and height of the indicator (defaults to 0.5in).
        /// </summary>
        public double Size
        {
            get { return (double)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        /// <summary>
        /// The busy indication property.
        /// </summary>
        public static readonly BindableProperty IsBusyProperty 
            = BindableProperty.Create("IsBusy", typeof(bool), typeof(BusyIndicator), false,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var indicator = (BusyIndicator)bindable;
                    var isBusy    = (bool)newValue;

                    indicator.IsVisible = isBusy;

                    if (DeviceHelper.Platform == TargetPlatform.iOS)
                    {
                        // $hack(jeff.lill):
                        //
                        // Without this, sometimes the busy indicator won't render.  I'm not
                        // sure why we need this.

                        indicator.sfBusyIndicator.IsVisible = false;
                        indicator.sfBusyIndicator.IsBusy    = false;
                    }

                    indicator.sfBusyIndicator.IsVisible = isBusy;
                    indicator.sfBusyIndicator.IsBusy    = isBusy;
                });

        /// <summary>
        /// <b>Bindable:</b> Indicates whether the busy indicator is to be displayed.
        /// </summary>
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        //---------------------------------------------------------------------
        // Implementation

        private Border              border;
        private SfBusyIndicator     sfBusyIndicator;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BusyIndicator()
        {
            // We need to intercept any user touches while the control is busy.

            this.GestureRecognizers.Add(new TapGestureRecognizer());

            // Arrange the SyncFusion busy indicator above the border in the Z-order.
            // The busy indicator will have a transparent background and the border
            // will use the control's background color.

            border = new Border()
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
                BackgroundColor   = this.BoxColor,
                BorderThickness   = FormsHelper.ZeroThickness
            };

            sfBusyIndicator = new SfBusyIndicator()
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
                AnimationType     = AnimationTypes.SingleCircle,
                TextColor         = this.Color,
                BackgroundColor   = Color.Transparent
            };

            SetSize(Size);
            Children.Add(border);
            Children.Add(sfBusyIndicator);

            IsVisible     = IsBusy;
            RowSpacing    = 0;
            ColumnSpacing = 0;
        }

        /// <summary>
        /// Sets the size of the indicator.
        /// </summary>
        /// <param name="size">The new height and width of the indicator.</param>
        private void SetSize(double size)
        {
            border.HeightRequest = size;
            border.WidthRequest  = size;
            border.CornerRadius  = new CornerRadius(size * 0.25);

            // The underlying Syncfusion control appears to be implemented
            // somewhat differently on each platform so we need to adjust the
            // view box.

            double viewSize = size;

            switch (DeviceHelper.Platform)
            {
                case TargetPlatform.Android:

                    viewSize = size * 1.33333;
                    break;

                case TargetPlatform.iOS:

                    viewSize = size * 1.5;
                    break;

                default:
                case TargetPlatform.WinPhone:

                    viewSize = size * 0.75;
                    break;
            }

            sfBusyIndicator.ViewBoxHeight =
            sfBusyIndicator.ViewBoxWidth  = (int)viewSize;
        }
    }
}
