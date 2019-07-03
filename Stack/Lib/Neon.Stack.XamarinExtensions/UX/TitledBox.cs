//-----------------------------------------------------------------------------
// FILE:        TitledBox.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using XLabs.Forms.Controls;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Implements a box that includes a textual title, a border, and a content area.
    /// </summary>
    [ContentProperty("Content")]
    public class TitledBox : Grid
    {
        //---------------------------------------------------------------------
        // Bindable properties

        /// <summary>
        /// The title text property.
        /// </summary>
        public static readonly BindableProperty TitleProperty
            = BindableProperty.Create("Title", typeof(string), typeof(TitledBox), null, 
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.titleLabel.Text = (string)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The title text.
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// The title text color property.
        /// </summary>
        public static readonly BindableProperty TitleTextColorProperty
            = BindableProperty.Create("TitleTextColor", typeof(Color), typeof(TitledBox), Color.White,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.titleLabel.TextColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The title text color.  This defaults to <see cref="Color.White"/>.
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
            = BindableProperty.Create("TitleBackgroundColor", typeof(Color), typeof(TitledBox), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.titleGrid.BackgroundColor = (Color)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The title background color.  This defaults to <see cref="Color.Black"/>.
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
            = BindableProperty.Create("TitlePadding", typeof(Thickness), typeof(TitledBox), new Thickness(DeviceHelper.Display.DipToDevicePosition(2)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.titleGrid.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The title padding.  This defaults to <b>2dip (position)</b>.
        /// </summary>
        public Thickness TitlePadding
        {
            get { return (Thickness)GetValue(TitlePaddingProperty); }
            set { SetValue(TitlePaddingProperty, value); }
        }

        /// <summary>
        /// The border color property.
        /// </summary>
        public static readonly BindableProperty BorderColorProperty
            = BindableProperty.Create("BorderColor", typeof(Color), typeof(TitledBox), Color.Black,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.border.BorderColor = (Color)newValue;
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
        /// The border thickness property.
        /// </summary>
        public static readonly BindableProperty BorderThicknessProperty
            = BindableProperty.Create("BorderThickness", typeof(Thickness), typeof(TitledBox), new Thickness(DeviceHelper.Display.DipToDeviceStroke(2)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.border.BorderThickness = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The border thickness.  This defaults to <b>2dip (stroke)</b>.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// The content padding property.
        /// </summary>
        public static readonly BindableProperty ContentPaddingProperty
            = BindableProperty.Create("ContentPadding", typeof(Thickness), typeof(TitledBox), new Thickness(DeviceHelper.Display.DipToDevicePosition(5)),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.contentView.Padding = (Thickness)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The content padding.  This defaults to <b>5dip (position)</b>.
        /// </summary>
        public Thickness ContentPadding
        {
            get { return (Thickness)GetValue(ContentPaddingProperty); }
            set { SetValue(ContentPaddingProperty, value); }
        }

        /// <summary>
        /// The box content property.
        /// </summary>
        public static readonly BindableProperty ContentProperty
            = BindableProperty.Create("Content", typeof(View), typeof(TitledBox), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var box = (TitledBox)bindable;

                    box.contentView.Content = (View)newValue;
                });

        /// <summary>
        /// <b>Bindable:</b> The box content.
        /// </summary>
        public View Content
        {
            get { return (View)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        //---------------------------------------------------------------------
        // Implementation

        private Label           titleLabel;
        private Grid            titleGrid;
        private StackLayout     titleButtonsLayout;
        private Grid            boxGrid;
        private ContentView     contentView;
        private Border          border;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TitledBox()
        {
            // Initialize the subviews.

            titleLabel                           = new Label(); 
            titleLabel.FontSize                  = Device.GetNamedSize(NamedSize.Medium, titleLabel);
            titleLabel.FontAttributes            = FontAttributes.Bold;
            titleLabel.HorizontalOptions         = LayoutOptions.Center;
            titleLabel.VerticalOptions           = LayoutOptions.Center;
            titleLabel.XAlign                    = TextAlignment.Center;
            titleLabel.BackgroundColor           = Color.Transparent;
            titleLabel.Text                      = Title;
            titleLabel.TextColor                 = TitleTextColor;

            titleButtonsLayout                   = new StackLayout();
            titleButtonsLayout.Orientation       = StackOrientation.Horizontal;
            titleButtonsLayout.Padding           = FormsHelper.ZeroThickness;
            titleButtonsLayout.HorizontalOptions = LayoutOptions.End;
            titleButtonsLayout.VerticalOptions   = LayoutOptions.Center;

            titleGrid                            = new Grid();
            titleGrid.HorizontalOptions          = LayoutOptions.FillAndExpand;
            titleGrid.VerticalOptions            = LayoutOptions.FillAndExpand;
            titleGrid.BackgroundColor            = TitleBackgroundColor;
            titleGrid.Padding                    = TitlePadding;
            titleGrid.RowSpacing                 = 0;
            titleGrid.ColumnSpacing              = 0;
            titleGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
            titleGrid.SetValue(Grid.RowProperty, 0);
            titleGrid.Children.Add(titleLabel);
            titleGrid.Children.Add(titleButtonsLayout);

            contentView                          = new ContentView();
            contentView.HorizontalOptions        = LayoutOptions.FillAndExpand;
            contentView.VerticalOptions          = LayoutOptions.FillAndExpand;
            contentView.BackgroundColor          = Color.Transparent;
            contentView.Padding                  = ContentPadding;
            contentView.SetValue(Grid.RowProperty, 1);

            boxGrid                              = new Grid();
            boxGrid.HorizontalOptions            = LayoutOptions.FillAndExpand;
            boxGrid.VerticalOptions              = LayoutOptions.FillAndExpand;
            boxGrid.BackgroundColor              = Color.Transparent;
            boxGrid.RowSpacing                   = 0;
            boxGrid.ColumnSpacing                = 0;
            boxGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            boxGrid.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
            boxGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
            boxGrid.Children.Add(titleGrid);
            boxGrid.Children.Add(contentView);

            border                               = new Border();
            border.HorizontalOptions             = LayoutOptions.FillAndExpand;
            border.VerticalOptions               = LayoutOptions.FillAndExpand;
            border.BackgroundColor               = Color.Transparent;
            border.BorderColor                   = BorderColor;
            border.BorderThickness               = BorderThickness;
            border.Padding                       = FormsHelper.ZeroThickness;

            // Initialize the base grid.

            this.BackgroundColor               = BackgroundColor;
            this.RowSpacing                    = 0;
            this.ColumnSpacing                 = 0;
            this.RowDefinitions.Add(new RowDefinition() { Height = FormsHelper.GridLengthStar });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = FormsHelper.GridLengthStar });
            this.Children.Add(border);
            this.Children.Add(boxGrid);

            // Initialize the title bar commands collection.

            TitleBarButtons = new ObservableCollection<IconButton>();

            TitleBarButtons.CollectionChanged +=
                (s, a) =>
                {
                    titleButtonsLayout.Children.Clear();

                    foreach (var button in TitleBarButtons)
                    {
                        titleButtonsLayout.Children.Add(button);
                    }
                };
        }

        /// <summary>
        /// The collection of title bar buttons.
        /// </summary>
        public ObservableCollection<IconButton> TitleBarButtons { get; private set; }
    }
}
