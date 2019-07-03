//-----------------------------------------------------------------------------
// FILE:        Dialog.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements modal dialog behavior based on a <see cref="EnhancedContentPage"/>.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// The back button is drawn using an icon file named <b>Platform_BackButton.png</b>.  
    /// This file must be included in the application's platform host project in the standard
    /// folders (<b>Resources/drawable*</b> for Android, <b>Resources</b> for iOS, and
    /// <b>Assets</b> for Windows Phone).
    /// </note>
    /// <para>
    /// Use this class to simplify the implementation of modal dialogs based on the
    /// <see cref="EnhancedContentPage"/> class.  Simply have your dialog class
    /// derive from <see cref="Dialog"/> and then call <see cref="ShowModalAsync"/>
    /// to display it.  This method will return after the dialog has been closed, returning
    /// the <see cref="DialogResult"/> indicating the action taken.  Your dialog code will
    /// call <see cref="CloseAsync"/> to close the dialog.
    /// </para>
    /// <para>
    /// The class renders its content within a screen with a title
    /// bar and back button.  The <see cref="Page.Title"/>, <see cref="TitleTextColor"/>, 
    /// <see cref="TitleBackgroundColor"/>, and <see cref="TitlePadding"/> properties control 
    /// how the title is displayed.  The <see cref="DialogContentPadding"/> property specifies the
    /// padding around the <see cref="DialogContent"/>.
    /// </para>
    /// <para>
    /// The dialog instance subscribes to the <see cref="MessageCenter"/>'s <see cref="MessageCenter.IsBusyMessage"/>
    /// to update the <see cref="EnhancedContentPage.IsBusy"/> property so the embedded busy indicator will present itself
    /// as is appropriate.
    /// </para>
    /// <note type="note">
    /// This class implements <see cref="INotifyPropertyChanged"/> to make it easy to
    /// bind the user interface to dialog properties.
    /// </note>
    /// <note type="note">
    /// The <see cref="ShowModalAsync"/> and <see cref="CloseAsync"/> will animate the
    /// activation and dismissal of the dialog if the <see cref="Animate"/> property
    /// is <c>true</c> (the default).
    /// </note>
    /// </remarks>
    [ContentProperty("DialogContent")]
    public abstract class Dialog : EnhancedContentPage, INotifyPropertyChanged
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The dialog animation property.
        /// </summary>
        public static readonly BindableProperty AnimateProperty
            = BindableProperty.Create("Animate", typeof(bool), typeof(Dialog), true);

        /// <summary>
        /// <b>Bindable:</b> Controls whether the dialog is animated when it is activated or dismissed.  Defaults to <c>true</c>.
        /// </summary>
        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        /// <summary>
        /// The title text color property.
        /// </summary>
        public static readonly BindableProperty TitleTextColorProperty
            = BindableProperty.Create("TitleTextColor", typeof(Color), typeof(Dialog), Color.White,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var dialog = (Dialog)bindable;

                    dialog.titleLabel.TextColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The color of the title text.  Defaults to <see cref="Color.White"/>.
        /// </summary>
        public Color TitleTextColor
        {
            get { return (Color)GetValue(TitleTextColorProperty); }
            set { SetValue(TitleTextColorProperty, value); }
        }

        /// <summary>
        /// The title background color property.
        /// </summary>
        public static readonly BindableProperty TitleBackgroundColorProperty
            = BindableProperty.Create("TitleBackgroundColor", typeof(Color), typeof(Dialog), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var dialog = (Dialog)bindable;

                    dialog.titleGrid.BackgroundColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The color of the title background.  Defaults to <see cref="Color.Black"/>.
        /// </summary>
        public Color TitleBackgroundColor
        {
            get { return (Color)GetValue(TitleBackgroundColorProperty); }
            set { SetValue(TitleBackgroundColorProperty, value); }
        }

        /// <summary>
        /// The title padding property.
        /// </summary>
        public static readonly BindableProperty TitlePaddingProperty
            = BindableProperty.Create("TitlePadding", typeof(Thickness), typeof(Dialog), new Thickness(DeviceHelper.Display.DipToDevicePosition(2)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var dialog = (Dialog)bindable;

                    dialog.titleGrid.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The title bar padding.  Defaults to <b>2dip (position)</b>.
        /// </summary>
        public Thickness TitlePadding
        {
            get { return (Thickness)GetValue(TitlePaddingProperty); }
            set { SetValue(TitlePaddingProperty, value); }
        }

        /// <summary>
        /// The dialog content property.
        /// </summary>
        public static readonly BindableProperty DialogContentProperty
            = BindableProperty.Create("DialogContent", typeof(View), typeof(Dialog), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var dialog = (Dialog)bindable;

                    dialog.contentView.Content = (View)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The dialog content.  Defaults to <c>null</c>.
        /// </summary>
        public View DialogContent
        {
            get { return (View)GetValue(DialogContentProperty); }
            set { SetValue(DialogContentProperty, value); }
        }

        /// <summary>
        /// The dialog content padding property.
        /// </summary>
        public static readonly BindableProperty DialogContentPaddingProperty
            = BindableProperty.Create("DialogContentPadding", typeof(Thickness), typeof(Dialog), new Thickness(DeviceHelper.Display.DipToDevicePosition(4)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var dialog = (Dialog)bindable;

                    dialog.contentView.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The padding around the dialog content.  Defaults to <b>4dip (position)</b>.
        /// </summary>
        public Thickness DialogContentPadding
        {
            get { return (Thickness)GetValue(DialogContentPaddingProperty); }
            set { SetValue(DialogContentPaddingProperty, value); }
        }

        /// <summary>
        /// The default command property.
        /// </summary>
        public static readonly BindableProperty DefaultCommandProperty
            = BindableProperty.Create("DefaultCommand", typeof(Command), typeof(Dialog), null);

        /// <summary>
        /// <b>Bindable:</b> The default command property.  This command will be invoked
        /// when the enter or go key is pressed within an <see cref="EnhancedEntry"/> box.
        /// </summary>
        public Command DefaultCommand
        {
            get { return (Command)GetValue(DefaultCommandProperty); }
            set { SetValue(DefaultCommandProperty, value); }
        }

        /// <summary>
        /// The back command property.
        /// </summary>
        public static readonly BindableProperty BackCommandProperty
            = BindableProperty.Create("BackCommand", typeof(Command), typeof(Dialog), null);

        /// <summary>
        /// <b>Bindable:</b> The back command property.  This command will be invoked
        /// when the back button is pressed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The dialog will close itself if this command is set to <c>null</c>.  Set this
        /// to a <see cref="Command"/> instance to override this behavior.
        /// </para>
        /// </remarks>
        public Command BackCommand
        {
            get { return (Command)GetValue(BackCommandProperty); }
            set { SetValue(BackCommandProperty, value); }
        }

        //---------------------------------------------------------------------
        // Static members

        private static ImageSource backButtonSource = ImagePathExtension.Get("Platform_BackButton.png");

        //---------------------------------------------------------------------
        // Instance members

        private TaskCompletionSource<DialogResult>  modalTcs;       // Used to communicate with the async caller
        private Grid                                titleGrid;
        private Label                               titleLabel;
        private Image                               backButton;
        private ContentView                         contentView;
        private Grid                                dialogGrid;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Dialog()
        {
            // Initialize the internal views.

            titleLabel                    = new Label(); 
            titleLabel.FontSize           = Device.GetNamedSize(NamedSize.Medium, titleLabel);
            titleLabel.FontAttributes     = DeviceHelper.Platform == TargetPlatform.Android ? FontAttributes.None : FontAttributes.Bold;
            titleLabel.HorizontalOptions  = LayoutOptions.Center;
            titleLabel.VerticalOptions    = LayoutOptions.Center;
            titleLabel.XAlign             = TextAlignment.Center;
            titleLabel.BackgroundColor    = Color.Transparent;

            backButton                    = new Image();
            backButton.Source             = backButtonSource;
            backButton.HorizontalOptions  = LayoutOptions.Start;
            backButton.VerticalOptions    = LayoutOptions.Center;
            backButton.HeightRequest      = 
            backButton.WidthRequest       = titleLabel.FontSize * 1.5;
            backButton.Aspect             = Aspect.AspectFit;

            titleGrid                     = new Grid();
            titleGrid.HorizontalOptions   = LayoutOptions.FillAndExpand;
            titleGrid.VerticalOptions     = LayoutOptions.FillAndExpand;
            titleGrid.RowSpacing          = 0;
            titleGrid.ColumnSpacing       = 0;
            titleGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
            titleGrid.SetValue(Grid.RowProperty, 0);
            titleGrid.Children.Add(titleLabel);
            titleGrid.Children.Add(backButton);

            contentView                   = new ContentView();
            contentView.HorizontalOptions = LayoutOptions.FillAndExpand;
            contentView.VerticalOptions   = LayoutOptions.FillAndExpand;
            contentView.BackgroundColor   = Color.Transparent;
            contentView.SetValue(Grid.RowProperty, 1);

            dialogGrid                    = new Grid();
            dialogGrid.HorizontalOptions  = LayoutOptions.FillAndExpand;
            dialogGrid.VerticalOptions    = LayoutOptions.FillAndExpand;
            dialogGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            dialogGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
            dialogGrid.Children.Add(titleGrid);
            dialogGrid.Children.Add(contentView);
            
            PageContent    = dialogGrid;
            ContentPadding = FormsHelper.ZeroThickness;

            if (DeviceHelper.Platform == TargetPlatform.iOS && EnhancedNavigationPage.Current != null)
            {
                // For iOS, we need to make sure the device status bar background has the same
                // color as the navigation bar.  We're just going to set the page's background
                // color to accomplish this.

                BackgroundColor = EnhancedNavigationPage.Current.BarBackgroundColor;
            }

            // Initialize the internal control properties.

            titleLabel.Text           = Title;
            titleLabel.TextColor      = TitleTextColor;

            titleGrid.Padding         = TitlePadding;
            titleGrid.BackgroundColor = TitleBackgroundColor;

            contentView.Padding       = DialogContentPadding;

            // We need to intercept changes to the Title property so we can
            // update the label.

            this.PropertyChanged += new PropertyChangedEventHandler(
                (s, a) =>
                {
                    if (a.PropertyName == "Title")
                    {
                        titleLabel.Text = Title;
                    }
                });

            // Add a gesture recognizer to the back button so we can capture user taps.

            var tapRecognizer = new TapGestureRecognizer();

            tapRecognizer.Tapped +=
                async (s, a) =>
                {
                    if (BackCommand == null)
                    {
                        await CloseAsync(DialogResult.Cancel);
                    }
                    else
                    {
                        BackCommand.Execute(null);
                    }
                };

            backButton.GestureRecognizers.Add(tapRecognizer);

            // We need to hook the Completed event of any Entry boxes added
            // to the dialog so we can invoke the default command when RETURN,
            // GO, or DONE is pressed on the keyboard.

            var completedHandler = new EventHandler(
                (s, a) => 
                {
                    var defaultCommand = this.DefaultCommand;

                    if (defaultCommand != null && defaultCommand.CanExecute(null))
                    {
                        defaultCommand.Execute(null);
                    }
                });

            this.DescendantAdded +=
                (s, a) =>
                {
                    var entry = a.Element as Entry;

                    if (entry != null)
                    {
                        entry.Completed += completedHandler;
                    }
                };

            this.DescendantRemoved +=
                (s, a) =>
                {
                    var entry = a.Element as Entry;

                    if (entry != null)
                    {
                        entry.Completed -= completedHandler;
                    }
                };
        }

        /// <summary>
        /// Returns the user action taken.
        /// </summary>
        public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;

        /// <summary>
        /// Shows the forgotten password dialog and submits a reset password request
        /// to the service when requested by the user.
        /// </summary>
        /// <returns>The <see cref="DialogResult"/> indicating the user action taken.</returns>
        /// <remarks>
        /// <note type="note">
        /// The dialog activation will be animated if the <see cref="Animate"/> property
        /// is <c>true</c> (the default).
        /// </note>
        /// </remarks>
        public Task<DialogResult> ShowModalAsync()
        {
            modalTcs = new TaskCompletionSource<DialogResult>();

            Application.Current.MainPage.Navigation.PushModalAsync(this, Animate);

            MessageCenter.Subscribe<bool>(this, MessageCenter.IsBusyMessage,
                isBusy =>
                {
                    this.IsBusy = isBusy;
                });

            return modalTcs.Task;
        }

        /// <summary>
        /// Dismisses the dialog, navigating back to the previous page.
        /// </summary>
        /// <param name="result">The <see cref="DialogResult"/> indicating the user action taken.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// <note type="note">
        /// The dialog dismissal will be animated if the <see cref="Animate"/> property
        /// is <c>true</c> (the default).
        /// </note>
        /// </remarks>
        public async Task CloseAsync(DialogResult result = DialogResult.OK)
        {
            this.DialogResult = result;

            MessageCenter.Unsubscribe<bool>(this, MessageCenter.IsBusyMessage);

            await Application.Current.MainPage.Navigation.PopModalAsync(Animate);
            modalTcs.SetResult(result);
        }

        /// <summary>
        /// Handles the platform back button by invoking  <see cref="BackCommand"/> if present
        /// or closing the dialog.
        /// </summary>
        /// <returns>
        /// <c>true</c> to allow the platform to close the dialog or <c>false</c> 
        /// if the code will handle this.
        /// </returns>
        protected override bool OnBackButtonPressed()
        {
            if (BackCommand == null)
            {
                FormsHelper.FireAndForget(CloseAsync(DialogResult.Cancel));
                return true;
            }
            else
            {
                BackCommand.Execute(null);
                return false;
            }
        }

        /// <summary>
        /// Returns the associated <b>UIViewController</b> on iOS.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// This is a hack intended to be used only for very specialized situations.
        /// </note>
        /// <para>
        /// This property was added so that the ZXing QR/Barcode user scanner could
        /// be made to work when launched from a modal dialog.  The problem was that
        /// there was no easy way to obtain the underlying <b>UIViewController</b>
        /// for the dialog.
        /// </para>
        /// <para>
        /// This property will return the associated view controller on iOS and
        /// <c>null</c> for the other platforms.  The property is set by a custom
        /// iOS renderer.  The <b>UIViewController</b> is not defined for portable
        /// code, so the property is defined as <c>object</c> instead.
        /// </para>
        /// </remarks>
        public object iOSViewController { get; set; }

        //---------------------------------------------------------------------
        // INotifyPropertyChanged implementation

        /// <summary>
        /// Derived classes will call this when an property instance property value has changed.
        /// </summary>
        /// <param name="propertyName">
        /// The optional property name.  This defaults to the name of the caller, typically
        /// the property's setter.
        /// </param>
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
    }
}
