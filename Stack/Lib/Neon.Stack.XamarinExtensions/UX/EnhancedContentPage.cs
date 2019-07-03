//-----------------------------------------------------------------------------
// FILE:        EnhancedContentPage.cs
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

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Extends the Xamarin.Forms <see cref="ContentPage"/> by adding a <see cref="BusyIndicator"/>
    /// that overlays the entire page when active and some content related properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You'll use this class to implement your primary application pages.  This provides a
    /// busy indicator that will overlay the entire page when active.  Simply set the
    /// <see cref="IsBusy"/> property to <c>true</c> to display the busy indicator or better
    /// yet, bind it to a property in your view model that indicates that the application
    /// is busy.
    /// </para>
    /// <para>
    /// The page also adds some content related properties including <see cref="PageContent"/>,
    /// <see cref="ContentBackgroundColor"/>, and <see cref="ContentPadding"/>.
    /// </para>
    /// </remarks>
    [ContentProperty("PageContent")]
    public partial class EnhancedContentPage : ContentPage
    {
        //---------------------------------------------------------------------
        // Implementation Notes:
        //
        // Xamarin doesn't support control templates so I need to wireup the the 
        // page Content property and the busy indicator with a child grid in in
        // node.  I'm going to create and style the child grid along with the busy
        // indicator in the constructor and then set the content in the bindable
        // property implementation, taking care to ensure that the busy indicator
        // will always be on top of the content in the Z-order.
        //
        // The page will consist of a grid with two rows.  For iOS dialogs, the first 
        // row will have height=20 to account for the iOS status bar at the top of the
        // screen.  For Windows Phone non-dialog pages, we're going to display the title
        // in row 0.  For Android (and all other circumstances, the first row will have
        // height=0.
        //
        // The page grid will have two children located in the second row.  The first
        // child (lowest in the z-order) will be a ContentView used to host the page
        // content.  The second (highest in the z-order) will be the busy indicator.
        //
        // The background color property for the page will be wired up to modify the
        // background of the content view so that page background will remain 
        // transparent.

        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The page content property.
        /// </summary>
        public static readonly BindableProperty PageContentProperty
            = BindableProperty.Create("PageContent", typeof(View), typeof(EnhancedContentPage), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var page = (EnhancedContentPage)bindable;

                    page.pageContent.Content = (View)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> Specifies the page content.  Note that this property is bindable
        /// where as the base <see cref="ContentPage.Content"/> property is not.
        /// </summary>
        public View PageContent
        {
            get { return (View)GetValue(PageContentProperty); }
            set { SetValue(PageContentProperty, value); }
        }

        /// <summary>
        /// The content background color property.
        /// </summary>
        public static readonly BindableProperty ContentBackgroundColorProperty
            = BindableProperty.Create("ContentBackgroundColor", typeof(Color), typeof(EnhancedContentPage), Color.White,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var page = (EnhancedContentPage)bindable;

                    page.pageContent.BackgroundColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> Specifies the content background color.  This defaults to <see cref="Color.White"/>.
        /// </summary>
        public Color ContentBackgroundColor
        {
            get { return (Color)GetValue(ContentBackgroundColorProperty); }
            set { SetValue(ContentBackgroundColorProperty, value); }
        }

        /// <summary>
        /// The content padding property.
        /// </summary>
        public static readonly BindableProperty ContentPaddingProperty
            = BindableProperty.Create("ContentPadding", typeof(Thickness), typeof(EnhancedContentPage), new Thickness(DeviceHelper.Display.DipToDevicePosition(2)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var page = (EnhancedContentPage)bindable;

                    page.pageContent.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The padding around the content.  Defaults to <b>2dip (position)</b>.
        /// </summary>
        public Thickness ContentPadding
        {
            get { return (Thickness)GetValue(ContentPaddingProperty); }
            set { SetValue(ContentPaddingProperty, value); }
        }

        /// <summary>
        /// The busy indicator property.
        /// </summary>
        public new static readonly BindableProperty IsBusyProperty
            = BindableProperty.Create("IsBusy", typeof(bool), typeof(EnhancedContentPage), false,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var page = (EnhancedContentPage)bindable;

                    page.busyIndicator.IsBusy = (bool)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> Controls the visibility of the busy indicator.  Defaults to <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// This property overrides the base Xamarin page's <see cref="Page.IsBusy"/> to prevent the
        /// display of the device specific busy indicator.
        /// </note>
        /// </remarks>
        public new bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        //---------------------------------------------------------------------
        // Implementation

        private Grid            pageGrid;
        private ContentView     pageContent;
        private BusyIndicator   busyIndicator;
        private Grid            titleGrid;          // These next two are set for non-dialog
        private Label           titleLabel;         // pages on Windows Phone.

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnhancedContentPage()
        {
            this.BindingContext = this;

            // Construct the busy indicator: This will always be the last grid child view.
            // Note also that we're updating the busy indicator's properties in code
            // because the page's binding context may not always be set to the
            // view model (e.g. for dialogs).

            object      busyStyleObject;
            Style       busyStyle     = null;

            busyIndicator = new BusyIndicator();

            if (Application.Current.Resources.TryGetValue("BusyStyle", out busyStyleObject))
            {
                busyStyle = busyStyleObject as Style;
            }

            if (busyStyle == null)
            {
                busyStyle = new Style(typeof(BusyIndicator));

                busyStyle.Setters.Add(BusyIndicator.HorizontalOptionsProperty, LayoutOptions.FillAndExpand);
                busyStyle.Setters.Add(BusyIndicator.VerticalOptionsProperty, LayoutOptions.FillAndExpand);
            }

            busyIndicator.Style  = busyStyle;
            busyIndicator.IsBusy = IsBusy;

            // Construct the page content.

            pageContent                 = new ContentView();
            pageContent.BackgroundColor = ContentBackgroundColor;
            pageContent.Padding         = ContentPadding;

            // Construct the page grid and locate the content and busy indicator in the second row
            // and with the correct z-order (busy indicator on top).

            pageGrid                   = new Grid();
            pageGrid.HorizontalOptions = LayoutOptions.Fill;
            pageGrid.VerticalOptions   = LayoutOptions.Fill;
            pageGrid.BackgroundColor   = Color.Transparent;
            pageGrid.RowSpacing        = 0;
            pageGrid.ColumnSpacing     = 0;

            // Initialize row 0 (see the note above).

            var isDialog = this is Dialog;

            if (DeviceHelper.Platform == TargetPlatform.iOS && isDialog)
            {
                pageGrid.RowDefinitions.Add(new RowDefinition() { Height = 20 });
            }
            else if (DeviceHelper.Platform == TargetPlatform.WinPhone && !isDialog && EnhancedNavigationPage.Current != null)
            {
                // $todo(jeff.lill): Consider adding a back button.

                pageGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                titleLabel                   = new Label(); 
                titleLabel.FontSize          = Device.GetNamedSize(NamedSize.Medium, titleLabel);
                titleLabel.FontAttributes    = FontAttributes.Bold;
                titleLabel.HorizontalOptions = LayoutOptions.Center;
                titleLabel.VerticalOptions   = LayoutOptions.Center;
                titleLabel.XAlign            = TextAlignment.Center;
                titleLabel.TextColor         = EnhancedNavigationPage.Current.BarTextColor;         // $hack(jeff.lill): This color won't be updated the navigation page propery changes.
                titleLabel.BackgroundColor   = Color.Transparent;
                titleLabel.Text              = Title;

                titleGrid                    = new Grid();
                titleGrid.HorizontalOptions  = LayoutOptions.FillAndExpand;
                titleGrid.VerticalOptions    = LayoutOptions.FillAndExpand;
                titleGrid.RowSpacing         = 0;
                titleGrid.ColumnSpacing      = 0;
                titleGrid.BackgroundColor    = EnhancedNavigationPage.Current.BarBackgroundColor;   // $hack(jeff.lill): This color won't be updated the navigation page propert changes.
                titleGrid.Padding            = new Thickness(DeviceHelper.Display.DipToDevicePosition(2));
                titleGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
                titleGrid.Children.Add(titleLabel);
            }
            else
            {
                pageGrid.RowDefinitions.Add(new RowDefinition() { Height = 0 });
            }

            pageGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });

            if (titleGrid != null)
            {
                titleGrid.SetValue(Grid.RowProperty, 0);
                pageGrid.Children.Add(titleGrid);
            }

            pageContent.SetValue(Grid.RowProperty, 1);
            pageGrid.Children.Add(pageContent);

            busyIndicator.SetValue(Grid.RowProperty, 1);
            pageGrid.Children.Add(busyIndicator);

            Content = pageGrid;
        }

        /// <summary>
        /// Called when an instance property value has changed.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == "Title" && titleLabel != null)
            {
                titleLabel.Text = Title;
            }
        }
    }
}
