//-----------------------------------------------------------------------------
// FILE:        IconTextButton.cs
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

// $todo(jeff.lill): This is currently hardcoded to display the text beneath the icon.  It would
//              be useful to add a property that specifies how the icon and text are positioned.

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements a button that displays an image as well as some text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see cref="Icon"/> property to specify the icon source and <see cref="IconHeight"/>
    /// and <see cref="IconWidth"/> to control its displayed dimension.  The control text is controlled
    /// by the <see cref="Text"/>, <see cref="TextColor"/>, <see cref="FontSize"/>, and <see cref="FontAttributes"/>
    /// properties.  The border is configured using <see cref="BorderWidth"/> and <see cref="BorderColor"/>.
    /// <see cref="Command"/> nd <see cref="CommandParameter"/> are used to bind the button to a <see cref="Xamarin.Forms.Command"/>
    /// </para>
    /// <para>
    /// <see cref="TouchFeedback"/> and <see cref="FeedbackColor"/> determine how the control
    /// provides visual feedback to the user.
    /// </para>
    /// </remarks>
    public class IconTextButton : ContentView
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The command property.
        /// </summary>
        public static readonly BindableProperty CommandProperty
            = BindableProperty.Create("Command", typeof(Command), typeof(IconTextButton), null);

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
            = BindableProperty.Create("CommandParameter", typeof(object), typeof(IconTextButton), null);

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
            = BindableProperty.Create("Icon", typeof(FileImageSource), typeof(IconTextButton), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

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
            = BindableProperty.Create("IconHeight", typeof(double), typeof(IconTextButton), 50.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

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
            = BindableProperty.Create("IconWidth", typeof(double), typeof(IconTextButton), 50.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

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
        /// The text property.
        /// </summary>
        public static readonly BindableProperty TextProperty
            = BindableProperty.Create("Text", typeof(string), typeof(IconTextButton), string.Empty,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.label.Text = (string)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The button text.  This defaults to the empty string.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// The text color property.
        /// </summary>
        public static readonly BindableProperty TextColorProperty
            = BindableProperty.Create("TextColor", typeof(Color), typeof(IconTextButton), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.label.TextColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The button text color.  This defaults to <see cref="Color.Black"/>.
        /// </summary>
        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        /// <summary>
        /// The font size property.
        /// </summary>
        public static readonly BindableProperty FontSizeProperty
            = BindableProperty.Create("FontSize", typeof(double), typeof(IconTextButton), FontHelper.XSmall,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.label.FontSize = (double)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The font size.  This defaults to <see cref="FontHelper.XSmall"/>.
        /// </summary>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// The font attributes property.
        /// </summary>
        public static readonly BindableProperty FontAttributesProperty
            = BindableProperty.Create("FontAttributes", typeof(FontAttributes), typeof(IconTextButton), FontAttributes.Bold,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.label.FontAttributes = (FontAttributes)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The font attributes.  This defaults to <see cref="FontAttributes.Bold"/>.
        /// </summary>
        public FontAttributes FontAttributes
        {
            get { return (FontAttributes)GetValue(FontAttributesProperty); }
            set { SetValue(FontAttributesProperty, value); }
        }

        /// <summary>
        /// The spacing property.
        /// </summary>
        public static readonly BindableProperty SpacingProperty
            = BindableProperty.Create("Spacing", typeof(double), typeof(IconTextButton), DeviceHelper.Display.DipToDevicePosition(5),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.outerGrid.RowSpacing = (double)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The spacing between the icon and the text.  This defaults to <c>5dip</c>.
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// The border color property.
        /// </summary>
        public static readonly BindableProperty BorderColorProperty
            = BindableProperty.Create("BorderColor", typeof(Color), typeof(IconTextButton), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.border.BorderColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The border color.  This defaults to <see cref="Color.Black"/>.
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
            = BindableProperty.Create("BorderWidth", typeof(double), typeof(IconTextButton), DeviceHelper.Display.DipToDevicePosition(0),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

                    button.border.BorderThickness = new Thickness((double)newValue);
                });

        /// <summary>
        /// <b>Bindable:</b> The border width.  This defaults to <b>0</b>.
        /// </summary>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        /// <summary>
        /// The feedback color property.
        /// </summary>
        public static readonly BindableProperty FeedbackColorProperty
            = BindableProperty.Create("FeedbackColor", typeof(Color), typeof(IconTextButton), Color.FromHex("#FFCCCCCC"),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var button = (IconTextButton)bindable;

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
            = BindableProperty.Create("TouchFeedback", typeof(bool), typeof(IconTextButton), true);

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

        private Grid        outerGrid;
        private Grid        innerGrid;
        private BoxView     feedbackBox;
        private Border      border;
        private Image       image;
        private Label       label;

        /// <summary>
        /// Constructor.
        /// </summary>
        public IconTextButton()
        {
            outerGrid = new Grid()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill,
                BackgroundColor   = Color.Transparent,
                RowSpacing        = Spacing
            };

            feedbackBox = new BoxView()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill,
                Color             = FeedbackColor,
                BackgroundColor   = Color.Transparent,
                Opacity           = 0.0
            };

            Grid.SetRow(feedbackBox, 0);

            border = new Border()
            {
                BorderColor     = BorderColor,
                BorderThickness = new Thickness(BorderWidth),
                BackgroundColor = Color.Transparent
            };

            Grid.SetRow(border, 0);

            innerGrid = new Grid()
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
                BackgroundColor   = Color.Transparent,
                RowSpacing        = Spacing
            };

            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            image = new Image()
            {
                Source            = Icon,
                Aspect            = Aspect.Fill,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            };

            Grid.SetRow(image, 0);

            label = new Label()
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Start,
                Text              = Text,
                FontSize          = FontSize,
                FontAttributes    = FontAttributes
            };

            Grid.SetRow(label, 1);

            innerGrid.Children.Add(image);
            innerGrid.Children.Add(label);

            // Layer the feedback box behind the border, image and text so we can animate
            // its color to highlight the button.

            outerGrid.Children.Add(feedbackBox);
            outerGrid.Children.Add(border);
            outerGrid.Children.Add(innerGrid);

            Content = outerGrid;

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

            outerGrid.GestureRecognizers.Add(tapGestureRecognizer);
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
