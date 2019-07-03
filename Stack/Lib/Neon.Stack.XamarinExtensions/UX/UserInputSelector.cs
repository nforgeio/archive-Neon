//-----------------------------------------------------------------------------
// FILE:        UserInputSelector.cs
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
    /// Used to display summary information about user required input in a dialog
    /// or page along with the ability to invoke a command when the user touches
    /// the view.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// The selection button is drawn using an icon file named <b>Platform_AngleRight.png</b>.  
    /// This file must be included in the application's platform host project in the standard
    /// folders (<b>Resources/drawable*</b> for Android, <b>Resources</b> for iOS, and
    /// <b>Assets</b> for Windows Phone).
    /// </note>
    /// <para>
    /// This view is pretty simple: It's designed to display a horizontal row of custom
    /// content on the left with a right-angle image on the right indicating to the user
    /// that touching the control will display a page or dialog with the input controls.
    /// By default, the control will expand to full the horizontal extent of its parent.
    /// </para>
    /// <para>
    /// Applications are responsible displaying any custom prompts or content using the
    /// <see cref="Content"/> property and wiring the <see cref="Command"/> and <see cref="CommandParameter"/>
    /// properties to the <see cref="Xamarin.Forms.Command"/> to be invoked when the
    /// user touches the view.
    /// </para>
    /// <note type="note">
    /// The view creates a gesture recognizer that will intercept all user touches 
    /// within the view.  This means that the application will not be able to react
    /// to touch itself.
    /// </note>
    /// <note type="note">
    /// The view does not render any borders or separators.  Applications will need
    /// to add these as required.
    /// </note>
    /// </remarks>
    [ContentProperty("Content")]
    public class UserInputSelector : ContentView
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The content property.
        /// </summary>
        public new static readonly BindableProperty ContentProperty
            = BindableProperty.Create("Content", typeof(View), typeof(UserInputSelector), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var selector = (UserInputSelector)bindable;

                    selector.contentView.Content = (View)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The view to be displayed as the selector's content.
        /// </summary>
        public new View Content
        {
            get { return (View)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// The command property.
        /// </summary>
        public static readonly BindableProperty CommandProperty
            = BindableProperty.Create("Command", typeof(Command), typeof(UserInputSelector), null);

        /// <summary>
        /// <b>Bindable:</b> The <see cref="Xamarin.Forms.Command"/> to invoke when the
        /// user touches the selector.
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
            = BindableProperty.Create("CommandParameter", typeof(object), typeof(UserInputSelector), null);

        /// <summary>
        /// <b>Bindable:</b> The custom parameter to be passed when the <see cref="Xamarin.Forms.Command"/>
        /// is invoked.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// The padding property.
        /// </summary>
        public new static readonly BindableProperty PaddingProperty
            = BindableProperty.Create("Padding", typeof(Thickness), typeof(UserInputSelector), new Thickness(DeviceHelper.Display.DipToDevicePosition(3)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var selector = (UserInputSelector)bindable;

                    selector.rowGrid.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The padding around the content and right-angle icon within the row.
        /// This defaults to <b>3dip</b>.
        /// </summary>
        public new Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// The spacing property.
        /// </summary>
        public static readonly BindableProperty SpacingProperty
            = BindableProperty.Create("Spacing", typeof(double), typeof(UserInputSelector), DeviceHelper.Display.DipToDevicePosition(6),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var selector = (UserInputSelector)bindable;

                    selector.rowGrid.ColumnSpacing = (double)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The horizontal spacing between the right edge of the 
        /// content area and the left side of the right-angle icon.  This defaults
        /// to <b>6dip</b>.
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// The feedback color property.
        /// </summary>
        public static readonly BindableProperty FeedbackColorProperty
            = BindableProperty.Create("FeedbackColor", typeof(Color), typeof(IconButton), Color.FromHex("#FFCCCCCC"),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var selector = (UserInputSelector)bindable;

                    selector.feedbackBox.Color = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The background color to be displayed briefly to provide
        /// feedback when the user selects the control.  This defaults to a light gray.
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
        /// <b>Bindable:</b> Controls whether the control displays feedback when touched.
        /// This defaults to <c>true</c>.
        /// </summary>
        public bool TouchFeedback
        {
            get { return (bool)GetValue(TouchFeedbackProperty); }
            set { SetValue(TouchFeedbackProperty, value); }
        }

        //---------------------------------------------------------------------
        // Static methods

        private static ImageSource rightAngleIconSource = ImagePathExtension.Get("Platform_AngleRight.png");

        //---------------------------------------------------------------------
        // Implementation

        // Note:
        //
        // I'm going to implement this as a grid with a single row and two columns.  The first column
        // will host the content view (stretched to fit) and the second column will host the right-angle
        // icon.  The grid's horizontal spacing will be mapped to the view's spacing property.  We'll
        // add the gesture recognizer to the grid and map the view's padding to the grid as well.
        //
        // A box view will also be created and will behind all of the other items in the grid in the 
        // Z-order will be configured to span all of grid cells.  The box view will used to animate
        // any touch feedback.

        private BoxView         feedbackBox;
        private ContentView     contentView;
        private Image           rightAngleImage;
        private Grid            rowGrid;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserInputSelector()
        {
            feedbackBox = new BoxView();
            feedbackBox.HorizontalOptions = LayoutOptions.Fill;
            feedbackBox.VerticalOptions   = LayoutOptions.Fill;
            feedbackBox.Color             = FeedbackColor;
            feedbackBox.BackgroundColor   = Color.Transparent;
            feedbackBox.Opacity           = 0.0;
            feedbackBox.SetValue(Grid.RowProperty, 0);
            feedbackBox.SetValue(Grid.ColumnProperty, 0);
            feedbackBox.SetValue(Grid.ColumnSpanProperty, 2);

            contentView                   = new ContentView();
            contentView.Padding           = FormsHelper.ZeroThickness;
            contentView.HorizontalOptions = LayoutOptions.Fill;
            contentView.VerticalOptions   = LayoutOptions.Center;
            contentView.Content           = this.Content;
            contentView.BackgroundColor   = Color.Transparent;
            contentView.SetValue(Grid.ColumnProperty, 0);

            rightAngleImage               = new Image();
            rightAngleImage.WidthRequest  = 
            rightAngleImage.HeightRequest = DeviceHelper.Display.DipToDevicePosition(18);
            rightAngleImage.Source        = rightAngleIconSource;
            rightAngleImage.SetValue(Grid.ColumnProperty, 1);

            rowGrid                       = new Grid();
            rowGrid.BackgroundColor       = Color.Transparent;
            rowGrid.Padding               = this.Padding;
            rowGrid.RowSpacing            = 0;
            rowGrid.ColumnSpacing         = this.Spacing;
            rowGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

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

                            feedbackBox.Opacity = 1.0;

                            if (DeviceHelper.Platform == TargetPlatform.Android)
                            {
                                // $todo(jeff.lill): 
                                //
                                // For some weird reason, [FadeTo()] doesn't work in this
                                // situation on Android.  We'll just do a simple animation
                                // instead.

                                await Task.Delay(250);

                                feedbackBox.Opacity = 0.0;
                            }
                            else
                            {
                                await feedbackBox.FadeTo(0.0, 250, Easing.Linear);
                            }
                        }
                    }
                },
                arg => IsEnabled && (Command == null ? false : Command.CanExecute(arg)));

            rowGrid.GestureRecognizers.Add(tapGestureRecognizer);

            rowGrid.Children.Add(feedbackBox);
            rowGrid.Children.Add(contentView);
            rowGrid.Children.Add(rightAngleImage);

            base.Content = rowGrid;
        }
    }
}
