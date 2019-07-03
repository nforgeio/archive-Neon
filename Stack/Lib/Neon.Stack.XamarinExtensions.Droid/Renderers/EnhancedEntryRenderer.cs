//-----------------------------------------------------------------------------
// FILE:        EnhancedEntryRenderer.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

using AndroidView = Android.Views.View;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Neon.Stack.XamarinExtensions;
using Neon.Stack.XamarinExtensions.Droid;

[assembly: ExportRenderer(typeof(EnhancedEntry), typeof(EnhancedEntryRenderer))]

namespace Neon.Stack.XamarinExtensions.Droid
{
    /// <summary>
    /// Implements the Android renderer for the <see cref="EnhancedEntry"/> control.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// We're going to implement this as a [EditText] control within
    /// a [FrameLayout].
    /// </note>
    /// </remarks>
    public class EnhancedEntryRenderer : EntryRenderer
    {
        /// <summary>
        /// Returns the underlying <see cref="Android.Widget.EditText"/> control.
        /// </summary>
        private new EditText Control
        {
            get { return (EditText)base.Control; }
        }

        /// <summary>
        /// Returns the <see cref="EnhancedEntry"/> element.
        /// </summary>
        private new EnhancedEntry Element
        {
            get { return (EnhancedEntry)base.Element; }
        }

        /// <summary>
        /// Called when the attached element instance changes.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected override void OnElementChanged(ElementChangedEventArgs<Entry> args)
        {
            base.OnElementChanged(args);

            if (Control != null && args.NewElement != null)
            {
                UpdateProperty();
            }
        }

        /// <summary>
        /// Called when an element property changes.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event arguments.</param>
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (UpdateProperty(args.PropertyName))
            {
                base.OnElementPropertyChanged(sender, args);
            }
        }

        /// <summary>
        /// Update a control property.
        /// </summary>
        /// <param name="propertyName">The changed property name  or <c>null</c> to update all properties.</param>
        /// <returns><c>true</c> if the property change should be handled by the base class.</returns>
        private bool UpdateProperty(string propertyName = null)
        {
            switch (propertyName)
            {
                case null:

                    ConfigureKeyboard();
                    ConfigureBorder();

                    Control.SetTextColor(ConvertToPlatform.Color(Element.TextColor));
                    return true;

                case "BorderColor":
                case "BorderWidth":

                    ConfigureBorder();
                    return false;

                case "TextColor":

                    Control.SetTextColor(ConvertToPlatform.Color(Element.TextColor));
                    return false;

                case "AutoSuggest":
                case "Keyboard":
                case "IsPassword":

                    ConfigureKeyboard();
                    return false;

                default:

                    return true;
            }
        }

        /// <summary>
        /// Configures the Android keyboard settings for the current Xamarin.Forms keyboard,
        /// password and autosuggest.
        /// </summary>
        private void ConfigureKeyboard()
        {
            var type = InputTypes.ClassText | InputTypes.TextVariationNormal;

            if (Element.Keyboard == null || Element.Keyboard == Keyboard.Text || Element.Keyboard == Keyboard.Default)
            {
                type = InputTypes.ClassText | InputTypes.TextVariationNormal;
            }
            else if (Element.Keyboard == Keyboard.Chat)
            {
                type = InputTypes.ClassText | InputTypes.TextVariationShortMessage;
            }
            else if (Element.Keyboard == Keyboard.Email)
            {
                type = InputTypes.ClassText | InputTypes.TextVariationEmailAddress;
            }
            else if (Element.Keyboard == Keyboard.Numeric)
            {
                type = InputTypes.ClassNumber | InputTypes.NumberFlagDecimal;
            }
            else if (Element.Keyboard == Keyboard.Telephone)
            {
                type = InputTypes.ClassPhone;
            }
            else if (Element.Keyboard == Keyboard.Url)
            {
                type = InputTypes.ClassText | InputTypes.TextVariationUri;
            }

            if (Element.IsPassword)
            {
                type |= InputTypes.TextVariationPassword | InputTypes.TextFlagNoSuggestions;
            }
            else if (Element.AutoSuggest)
            {
                type |= InputTypes.TextFlagAutoComplete | InputTypes.TextFlagAutoCorrect | InputTypes.TextFlagCapSentences;
            }

            Control.InputType = type;
        }

        /// <summary>
        /// Configures the border related properties.
        /// </summary>
        private void ConfigureBorder()
        {
            var drawable = new GradientDrawable();

            drawable.SetCornerRadius(0);
            drawable.SetColor(ConvertToPlatform.Color(Element.BackgroundColor));
            drawable.SetStroke((int)Element.BorderWidth, ConvertToPlatform.Color(Element.BorderColor));

            Control.SetBackgroundDrawable(drawable);
        }
    }
}