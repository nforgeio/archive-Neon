//-----------------------------------------------------------------------------
// FILE:        EnhancedEntryRenderer.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreGraphics;
using UIKit;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Neon.Stack.XamarinExtensions;
using Neon.Stack.XamarinExtensions.iOS;

[assembly: ExportRenderer(typeof(EnhancedEntry), typeof(EnhancedEntryRenderer))]

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Implements the iOS renderer for the <see cref="EnhancedEntry"/> control.
    /// </summary>
    public class EnhancedEntryRenderer : EntryRenderer
    {
        /// <summary>
        /// Returns the associated portable element instance.
        /// </summary>
        public new EnhancedEntry Element
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
        /// Configures the iOS keyboard settings for the current Xamarin.Forms keyboard,
        /// password and autosuggest.
        /// </summary>
        private void ConfigureKeyboard()
        {
            if (Element.Keyboard == null || Element.Keyboard == Keyboard.Text || Element.Keyboard == Keyboard.Default)
            {
                Control.KeyboardType = UIKeyboardType.Default;
            }
            else if (Element.Keyboard == Keyboard.Chat)
            {
                Control.KeyboardType = UIKeyboardType.Default;
            }
            else if (Element.Keyboard == Keyboard.Email)
            {
                Control.KeyboardType = UIKeyboardType.EmailAddress;
            }
            else if (Element.Keyboard == Keyboard.Numeric)
            {
                Control.KeyboardType = UIKeyboardType.NumberPad;
            }
            else if (Element.Keyboard == Keyboard.Telephone)
            {
                Control.KeyboardType = UIKeyboardType.PhonePad;
            }
            else if (Element.Keyboard == Keyboard.Text || Element.Keyboard == Keyboard.Default)
            {
                Control.KeyboardType = UIKeyboardType.Default;
            }
            else if (Element.Keyboard == Keyboard.Url)
            {
                Control.KeyboardType = UIKeyboardType.Url;
            }

            if (Element.IsPassword)
            {
                Control.SecureTextEntry        = true;
                Control.AutocapitalizationType = UITextAutocapitalizationType.None;
                Control.AutocorrectionType     = UITextAutocorrectionType.No;
            }
            else
            {
                Control.SecureTextEntry        = false;
                Control.AutocapitalizationType = Element.AutoSuggest ? UITextAutocapitalizationType.Sentences : UITextAutocapitalizationType.None;
                Control.AutocorrectionType     = Element.AutoSuggest ? UITextAutocorrectionType.Yes : UITextAutocorrectionType.No;
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
                    Control.BackgroundColor = ConvertToPlatform.UIColor(Element.BackgroundColor);
                    Control.TextColor       = ConvertToPlatform.UIColor(Element.TextColor);
                    return true;

                case "BorderColor":
                case "BorderWidth":

                    ConfigureBorder();
                    return false;

                case "TextColor":

                    Control.TextColor = ConvertToPlatform.UIColor(Element.TextColor);
                    return false;

                case "BackgroundColor":

                    Control.BackgroundColor = ConvertToPlatform.UIColor(Element.BackgroundColor);
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
        /// Configures the border related properties.
        /// </summary>
        private void ConfigureBorder()
        {
            Control.Layer.CornerRadius  = 0;
            Control.Layer.MasksToBounds = true;
            Control.Layer.BorderColor   = ConvertToPlatform.CGColor(Element.BorderColor);
            Control.Layer.BorderWidth   = (nfloat)Element.BorderWidth;
        }
    }
}