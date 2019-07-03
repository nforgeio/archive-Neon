//-----------------------------------------------------------------------------
// FILE:        DeviceHelpers.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Foundation;
using MessageUI;
using UIKit;

#if TODO
using Xamarin.Contacts;
#endif
using Xamarin.Forms;

using XLabs.Ioc;
using XLabs.Platform.Device;

using Neon.Stack.XamarinExtensions;

namespace Neon.Stack.XamarinExtensions.iOS
{
    /// <summary>
    /// Implements extended device features for iOS.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// This class is intended to be instantiated once and then cached for reuse
    /// by the application.
    /// </note>
    /// </remarks>
    public class DeviceHelpers : IDeviceHelpers
    {
        private IDevice                                 device;
        private IDisplay                                display;
        private double                                  inchToDeviceScale;          // Inch to device scale factor
        private double                                  deviceToPixelScale;         // Device to pixel scale factor
        private double                                  positionFudge = 1.0;        // Position/size fudge factor
        private double                                  strokeFudge   = 1.0;        // Stroke width fudge factor
#if TODO
        private global::Xamarin.Contacts.AddressBook    addressBook;                // Cached device address book
#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceHelpers()
        {
            device  = Resolver.Resolve<IDevice>();
            display = Resolver.Resolve<IDisplay>();

            // $hack(jeff.lill):
            //
            // This is a bit of a hack that will require some tweaking
            // if Apple ever increases their screen density again.

            // $hack(jeff.lill):
            //
            // Actual iOS and WinPhone devices seem to have issues with stroke widths (iOS is a bit 
            // too thick compared to Android and WinPhone is way too thick).  I manually eyeballed 
            // adjustments for iOS and WinPhone against Android which looks pretty reasonable.

            if (display.Xdpi < 400)
            {
                deviceToPixelScale = display.Scale;
                strokeFudge        = 0.325;
            }
            else
            {
                deviceToPixelScale = 4.0;
                strokeFudge        = 0.65;
            }

            inchToDeviceScale = 160.0;
        }

        /// <inheritdoc/>
        public TargetPlatform Platform
        {
            get { return TargetPlatform.iOS; }
        }

        /// <inheritdoc/>
        public bool IsEmulated
        {
            get { return string.Equals(device.HardwareVersion, "Simulator", StringComparison.OrdinalIgnoreCase); }
        }

        /// <inheritdoc/>
        public double DipToDevicePosition(double dipMeasure)
        {
            // iOS device units are measured at 160 DPI (same as our DIP standard),
            // so no conversion is necessary.

            return dipMeasure * positionFudge;
        }

        /// <inheritdoc/>
        public double DipToDeviceStroke(double dipMeasure)
        {
            return dipMeasure * strokeFudge;
        }

        /// <inheritdoc/>
        public double InchToDevicePosition(double inches)
        {
            return inches * inchToDeviceScale * positionFudge;
        }

        /// <inheritdoc/>
        public double InchToDeviceStroke(double inches)
        {
            return inches * inchToDeviceScale * strokeFudge;
        }

        /// <inheritdoc/>
        public double PixelToDevicePosition(double pixels)
        {
            return pixels / deviceToPixelScale;
        }

        /// <inheritdoc/>
        public double PixelToDeviceStroke(double pixels)
        {
            return pixels / deviceToPixelScale;
        }

        /// <inheritdoc/>
        public void SetStatusVisibility(bool isVisible)
        {
            UIApplication.SharedApplication.SetStatusBarHidden(!isVisible, true);
        }

        /// <inheritdoc/>
        public void CopyToClipboard(string text)
        {
            var clipboard = UIPasteboard.General;

            clipboard.String = text;
        }

        /// <inheritdoc/>
        public global::Xamarin.Forms.Size MeasureText(string text, double fontSize, double width = int.MaxValue, string fontName = null, FontAttributes fontAttributes = FontAttributes.None)
        {
            var nsText    = new NSString(text ?? string.Empty);
            var boundSize = new SizeF((float)width, float.MaxValue);
            var options   = NSStringDrawingOptions.UsesFontLeading |
                            NSStringDrawingOptions.UsesLineFragmentOrigin;

            if (fontSize == 0.0)
            {
                fontSize = UIFont.LabelFontSize;
            }

            // Obtain the string drawing attributes (aka font details).

            var attributes = new UIStringAttributes();

            if (string.IsNullOrEmpty(fontName))
            {
                if (fontAttributes == FontAttributes.None)
                {
                    attributes.Font = UIFont.SystemFontOfSize((nfloat)fontSize);
                }
                else if (fontAttributes == FontAttributes.Bold)
                {
                    attributes.Font = UIFont.BoldSystemFontOfSize((nfloat)fontSize);
                }
                else if (fontAttributes == FontAttributes.Italic)
                {
                    attributes.Font = UIFont.ItalicSystemFontOfSize((nfloat)fontSize);
                }
                else if (fontAttributes == (FontAttributes.Bold | FontAttributes.Italic))
                {
                    attributes.Font = UIFont.FromName(UIFont.SystemFontOfSize(UIFont.LabelFontSize).Name + "Bold Italic", (nfloat)fontSize);
                }
            }
            else
            {
                if (fontAttributes == FontAttributes.None)
                {
                    attributes.Font = UIFont.FromName(fontName, (nfloat)fontSize);
                }
                else if (fontAttributes == FontAttributes.Bold)
                {
                    attributes.Font = UIFont.FromName(fontName + " Bold", (nfloat)fontSize);
                }
                else if (fontAttributes == FontAttributes.Italic)
                {
                    attributes.Font = UIFont.FromName(fontName + " Italic", (nfloat)fontSize);
                }
                else if (fontAttributes == (FontAttributes.Bold | FontAttributes.Italic))
                {
                    attributes.Font = UIFont.FromName(fontName + " Bold Italic", (nfloat)fontSize);
                }
            }

            var sizeF = nsText.GetBoundingRect(boundSize, options, attributes, null).Size;

            return new global::Xamarin.Forms.Size((double)sizeF.Width, 
                                                  (double)sizeF.Height);
        }

#if TODO
        /// <summary>
        /// Converts a device contact into a common contact.
        /// </summary>
        /// <param name="deviceContact">The device contact to be converted.</param>
        /// <returns>The converted contact.</returns>
        private Neon.Stack.Xam.Contact ConvertDeviceContact(global::Xamarin.Contacts.Contact deviceContact)
        {
            // Note: This code should be kept in-sync across all all common
            //       platform specific libraries.  If you make a change here,
            //       please update the other libraries as well.

            if (deviceContact.DisplayName == null)
            {
                // Make sure we have a display name.

                if (!string.IsNullOrEmpty(deviceContact.FirstName) && !string.IsNullOrEmpty(deviceContact.LastName))
                {
                    deviceContact.DisplayName = $"{deviceContact.FirstName} {deviceContact.LastName}";
                }
                else if (!string.IsNullOrEmpty(deviceContact.FirstName))
                {
                    deviceContact.DisplayName = deviceContact.FirstName;
                }
                else if (!string.IsNullOrEmpty(deviceContact.LastName))
                {
                    deviceContact.DisplayName = deviceContact.LastName;
                }
                else
                {
                    var email = deviceContact.Emails.FirstOrDefault();

                    if (email != null)
                    {
                        deviceContact.DisplayName = email.Address;
                    }
                    else
                    {
                        var website = deviceContact.Websites.FirstOrDefault();

                        if (website != null)
                        {
                            deviceContact.DisplayName = website.Address;
                        }
                        else
                        {
                            deviceContact.DisplayName = "-na-";
                        }
                    }
                }
            }

            var contact = new Neon.Stack.Xam.Contact()
            {
                Id          = deviceContact.Id,
                DisplayName = deviceContact.DisplayName,
                FirstName   = deviceContact.FirstName,
                LastName    = deviceContact.LastName,
                SortKey     = deviceContact.DisplayName.ToUpper()
            };

            foreach (var deviceEmail in deviceContact.Emails)
            {
                var email = new ContactEmail()
                {
                    Address = deviceEmail.Address
                };

                switch (deviceEmail.Type)
                {
                    default:
                    case EmailType.Other:

                        email.Type = ContactEmailType.Other;
                        break;

                    case EmailType.Home:

                        email.Type = ContactEmailType.Home;
                        break;

                    case EmailType.Work:

                        email.Type = ContactEmailType.Work;
                        break;
                }

                contact.Emails.Add(email);
            }

            foreach (var devicePhone in deviceContact.Phones)
            {
                var phone = new ContactPhone()
                {
                    Number = devicePhone.Number
                };

                switch (devicePhone.Type)
                {
                    default:
                    case PhoneType.Other:

                        phone.Type = ContactPhoneType.Other;
                        break;

                    case PhoneType.Home:

                        phone.Type = ContactPhoneType.Home;
                        break;

                    case PhoneType.HomeFax:

                        phone.Type = ContactPhoneType.HomeFax;
                        break;

                    case PhoneType.Work:

                        phone.Type = ContactPhoneType.Work;
                        break;

                    case PhoneType.WorkFax:

                        phone.Type = ContactPhoneType.WorkFax;
                        break;

                    case PhoneType.Pager:

                        phone.Type = ContactPhoneType.Pager;
                        break;

                    case PhoneType.Mobile:

                        phone.Type = ContactPhoneType.Mobile;
                        break;
                }

                contact.Phones.Add(phone);
            }

            return contact;
        }
#endif

        /// <inheritdoc/>
        public async Task<IEnumerable<Neon.Stack.XamarinExtensions.Contact>> GetContactsAsync()
        {
#if TODO
            // Note: This code should be kept in-sync across all all common
            //       platform specific libraries.  If you make a change here,
            //       please update the other libraries as well.

            if (addressBook == null)
            {
                // The address book isn't cached yet.

                var book = new global::Xamarin.Contacts.AddressBook();

                if (!await book.RequestPermission())
                {
                    return null;
                }

                addressBook = book;
            }

            var contacts = new List<Neon.Stack.Xam.Contact>(500);

            foreach (var deviceContact in addressBook)
            {
                contacts.Add(ConvertDeviceContact(deviceContact));
            }

            return contacts;
#else
            await Task.Delay(0);

            return new Neon.Stack.XamarinExtensions.Contact[0];
#endif
        }

        /// <inheritdoc/>
        public void PurgeTempFiles()
        {
            // This is currently a NOP for iOS because we didn't need to
            // create actual files for email attachments below.
        }

        /// <inheritdoc/>
        public bool EmailAttachmentsSupported
        {
            get { return MFMailComposeViewController.CanSendMail; }
        }

        /// <inheritdoc/>
        public void ShowEmailDraft(string subject, string body, string to, params Attachment[] attachments)
        {
            var mailer = new MFMailComposeViewController();

            mailer.SetMessageBody(body ?? string.Empty, false);
            mailer.SetSubject(subject ?? string.Empty);
            mailer.SetToRecipients(new string[] { to });
            mailer.Finished +=
                (s, e) =>
                {
                    ((MFMailComposeViewController)s).DismissViewController(true, () => { });
                };

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    mailer.AddAttachmentData(NSData.FromArray(attachment.Data), attachment.MimeType, attachment.Name);
                }
            }

            var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;

            while (vc.PresentedViewController != null)
            {
                vc = vc.PresentedViewController;
            }

            vc.PresentViewController(mailer, true, null);
        }

        /// <inheritdoc/>
        public async Task ExperimentalAsync(params object[] args)
        {
            await Task.Delay(0);
        }
    }
}
