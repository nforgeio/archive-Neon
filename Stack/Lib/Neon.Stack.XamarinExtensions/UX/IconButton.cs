//-----------------------------------------------------------------------------
// FILE:        IconButton.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

// $todo(jeff.lill): Implement command enable/disable with disabled icons.

// $todo(jeff.lill): Add a border.

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements a simple button that displays an image.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see cref="Icon"/> property to specify the icon source and <see cref="IconHeight"/>
    /// and <see cref="IconWidth"/> to control its displayed dimension.  <see cref="Command"/>
    /// and <see cref="CommandParameter"/> are used to bind the button to a <see cref="Xamarin.Forms.Command"/>
    /// and <see cref="TouchFeedback"/> and <see cref="FeedbackColor"/> determine how the control
    /// provides visual feedback to the user.
    /// </para>
    /// <note type="note">
    /// The icon will be displayed centered within the extent of the control as determined by the
    /// <see cref="Layout.Padding"/>,  <see cref="View.HeightRequest"/> and <see cref="View.WidthRequest"/> 
    /// and/or the <see cref="View.HorizontalOptions"/> and <see cref="View.VerticalOptions"/> layout options.
    /// </note>
    /// </remarks>
    public class IconButton : ContentView
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The command property.
        /// </summary>
        public static readonly BindableProperty CommandProperty
            = BindableProperty.Create("Command", typeof(Command), typeof(IconButton), null);

        /// <summary>
        /// <b>Bindable:</b> The command to be executed when the button is pressed.
        /// </summary>
        public Command Command
        {
            get { return (Command)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// The command parameter property.
        /// </summary>
        public static readonly BindableProperty CommandParameterProperty
            = BindableProperty.Create("CommandParameter", typeof(object), typeof(IconButton), null);

        /// <summary>
        /// <b>Bindable:</b> The parameter to pass to the command when the button is pressed.
        /// </summary>
        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// The icon source property.
        /// </summary>
        public static readonly BindableProperty IconProperty
            = BindableProperty.Create("Icon", typeof(FileImageSource), typeof(IconButton), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconButton)bindable;

                    button.image.Source = (FileImageSource)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The command to be executed when the button is pressed.
        /// </summary>
        public FileImageSource Icon
        {
            get { return (FileImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// The icon height property.
        /// </summary>
        public static readonly BindableProperty IconHeightProperty
            = BindableProperty.Create("IconHeight", typeof(double), typeof(IconButton), 50.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconButton)bindable;

                    button.SetDimensions();
                });

        /// <summary>
        /// <b>Bindable:</b> The icon height in device specific units.  This defaults to <b>50</b>.
        /// </summary>
        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        /// <summary>
        /// The icon width property.
        /// </summary>
        public static readonly BindableProperty IconWidthProperty
            = BindableProperty.Create("IconWidth", typeof(double), typeof(IconButton), 50.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconButton)bindable;

                    button.SetDimensions();
                });

        /// <summary>
        /// <b>Bindable:</b> The icon width in device specific units.  This defaults to <b>50</b>.
        /// </summary>
        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        /// <summary>
        /// The feedback color property.
        /// </summary>
        public static readonly BindableProperty FeedbackColorProperty
            = BindableProperty.Create("FeedbackColor", typeof(Color), typeof(IconButton), Color.FromHex("#FFCCCCCC"),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconButton)bindable;

                    button.feedbackBox.Color = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The background color to be displayed briefly to provide
        /// feedback when the user selects the button.  This defaults to a light gray.
        /// </summary>
        public Color FeedbackColor
        {
            get { return (Color)GetValue(FeedbackColorProperty); }
            set { SetValue(FeedbackColorProperty, value); }
        }

        /// <summary>
        /// The touch feedback property.
        /// </summary>
        public static readonly BindableProperty TouchFeedbackProperty
            = BindableProperty.Create("TouchFeedback", typeof(bool), typeof(IconButton), true);

        /// <summary>
        /// <b>Bindable:</b> Controls whether the button displays feedback when touched.
        /// This defaults to <c>true</c>.
        /// </summary>
        public bool TouchFeedback
        {
            get { return (bool)GetValue(TouchFeedbackProperty); }
            set { SetValue(TouchFeedbackProperty, value); }
        }

        //---------------------------------------------------------------------
        // Implementation

        private Grid        grid;
        private BoxView     feedbackBox;
        private Image       image;

        /// <summary>
        /// Constructor.
        /// </summary>
        public IconButton()
        {
            grid = new Grid()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill,
                BackgroundColor   = Color.Transparent
            };

            feedbackBox = new BoxView()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill,
                Color             = FeedbackColor,
                BackgroundColor   = Color.Transparent,
                Opacity           = 0.0
            };

            image = new Image()
            {
                Source            = Icon,
                Aspect            = Aspect.Fill,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            };

            // Layer the feedback box behind the image so we can animate
            // its color to highlight the image.

            grid.Children.Add(feedbackBox);
            grid.Children.Add(image);

            BackgroundColor = Color.Transparent;
            Content         = grid;

            SetDimensions();

            var tapGestureRecognizer = new TapGestureRecognizer();

            tapGestureRecognizer.Command = new Command(
                async arg =>
                {
                    if (IsEnabled && Command != null && Command.CanExecute(arg))
                    {
                        Command.Execute(CommandParameter);

                        if (TouchFeedback)
                        {
                            // Run the feedback animation.

                            var orgBackgroundColor = BackgroundColor;

                            BackgroundColor = Color.Transparent;

                            feedbackBox.Opacity = 1.0;
                            await feedbackBox.FadeTo(0.0, 250, Easing.Linear);

                            BackgroundColor = orgBackgroundColor;
                        }
                    }
                },
                arg => IsEnabled && (Command == null ? false : Command.CanExecute(arg)));

            grid.GestureRecognizers.Add(tapGestureRecognizer);
        }

        /// <summary>
        /// Sets the size related properties from the bindable properties above.
        /// </summary>
        private void SetDimensions()
        {
            image.WidthRequest  = (int)Math.Round(IconWidth);
            image.HeightRequest = (int)Math.Round(IconHeight);

            WidthRequest        = (int)Math.Round(Padding.Left + IconWidth + Padding.Right);
            HeightRequest       = (int)Math.Round(Padding.Top + IconHeight + Padding.Bottom);
        }

        /// <summary>
        /// Called when a property value has changed. 
        /// </summary>
        /// <param name="propertyName">Names the changed property.</param>
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == "Padding")
            {
                SetDimensions();
            }
        }
    }
}
