//-----------------------------------------------------------------------------
// FILE:        DeviceHelpers.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Android.Util;

using Environment = Android.OS.Environment;

#if TODO
using Xamarin.Contacts;
#endif
using Xamarin.Forms;

using XLabs.Forms;
using XLabs.Ioc;
using XLabs.Platform;
using XLabs.Platform.Device;

using Neon.Stack.XamarinExtensions;

namespace Neon.Stack.XamarinExtensions.Droid
{
    /// <summary>
    /// Implements extended device features for Android.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// This class is intended to be instantiated once and then cached for reuse
    /// by the application.
    /// </note>
    /// </remarks>
    public class DeviceHelpers : IDeviceHelpers
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Returns the main Android application activity.
        /// </summary>
        public static XFormsApplicationDroid MainActivity { get; private set; }

        /// <summary>
        /// Android device initialization.
        /// </summary>
        /// <param name="mainActivity">The main application activity.</param>
        /// <remarks>
        /// This method must be called early in an Android application's lifecycle.
        /// </remarks>
        public static void Initialize(XFormsApplicationDroid mainActivity)
        {
            DeviceHelpers.MainActivity = mainActivity;
        }

        //---------------------------------------------------------------------
        // Instance members

        private IDevice         device;
        private IDisplay        display;
        private double          inchToDeviceScale;          // Inch to device scale factor
        private double          deviceToPixelScale;         // Device to pixel scale factor
        private double          positionFudge = 1.0;        // Position/size fudge factor
        private double          strokeFudge   = 1.0;        // Stroke width fudge factor
#if TODO
        private AddressBook     addressBook;                // Cached device address book
#endif
        private Java.IO.File    externalTempFolder;         // The application's external temp folder

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceHelpers()
        {
            device  = Resolver.Resolve<IDevice>();
            display = Resolver.Resolve<IDisplay>();

            deviceToPixelScale = display.Scale;
            inchToDeviceScale  = 160.0;
        }

        /// <inheritdoc/>
        public TargetPlatform Platform
        {
            get { return TargetPlatform.Android; }
        }

        /// <inheritdoc/>
        public bool IsEmulated
        {
            get
            {
                if (string.Equals(device.Manufacturer, "VS Emulator", StringComparison.OrdinalIgnoreCase))
                {
                    // We're running on a Visual Studio Android emulator.

                    return true;
                }

                if (string.Equals(device.Manufacturer, "unknown", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(device.Manufacturer, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    // We're running on an Android SDK emulator.

                    return true;
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public double DipToDevicePosition(double dipMeasure)
        {
            // Android device units are measured at 160 DPI (same as our DIP standard),
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
            if (MainActivity == null)
            {
                throw new InvalidOperationException("DeviceHelper.Initialize() must be called with the application's main activity early in the apps lifecycle.");
            }

            var window = MainActivity.Window;

            if (isVisible)
            {
                window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
                window.ClearFlags(WindowManagerFlags.Fullscreen);
            }
            else
            {
                window.AddFlags(WindowManagerFlags.Fullscreen);
                window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
            }
        }

        /// <inheritdoc/>
        public void CopyToClipboard(string text)
        {
            var clipboard = (Android.Content.ClipboardManager)MainActivity.GetSystemService(XFormsApplicationDroid.ClipboardService);

            clipboard.PrimaryClip = ClipData.NewPlainText(string.Empty, text ?? string.Empty);
        }

        /// <inheritdoc/>
        public global::Xamarin.Forms.Size MeasureText(string text, double fontSize, double width = int.MaxValue, string fontName = null, FontAttributes fontAttributes = FontAttributes.None)
        {
            // Obtain the type face

            var typeface      = Typeface.Default;
            var typeFaceStyle = TypefaceStyle.Normal;

            if (fontAttributes != FontAttributes.None)
            {
                if (fontAttributes == FontAttributes.Bold)
                {
                    typeFaceStyle = TypefaceStyle.Bold;
                }
                else if (fontAttributes == FontAttributes.Italic)
                {
                    typeFaceStyle = TypefaceStyle.Italic;
                }
                else if (fontAttributes == (FontAttributes.Bold | FontAttributes.Italic))
                {
                    typeFaceStyle = TypefaceStyle.BoldItalic;
                }
            }

            if (string.IsNullOrEmpty(fontName) && typeFaceStyle != TypefaceStyle.Normal)
            {
                typeface = Typeface.Create(Typeface.Default, typeFaceStyle);
            }
            else
            {
                typeface = Typeface.Create(fontName, typeFaceStyle);
            }

            // Compute the text size.

            var textView = new TextView(global::Android.App.Application.Context);

            textView.Typeface = typeface;
            textView.SetText(text ?? string.Empty, TextView.BufferType.Normal);

            if (fontSize > 0.0)
            {
                textView.SetTextSize(ComplexUnitType.Px, (float)fontSize);
            }

            int widthSpecification  = Android.Views.View.MeasureSpec.MakeMeasureSpec((int)width, MeasureSpecMode.AtMost);
            int heightSpecification = Android.Views.View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);

            textView.Measure(widthSpecification, heightSpecification);

            return new global::Xamarin.Forms.Size(
                (double)textView.MeasuredWidth,
                (double)textView.MeasuredHeight);
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

                var book = new global::Xamarin.Contacts.AddressBook(DeviceHelpers.MainActivity);

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
            return await Task.FromResult(new Contact[0]);
#endif
        }

        /// <inheritdoc/>
        public bool EmailAttachmentsSupported
        {
            get
            {
                try
                {
                    return Environment.ExternalStorageState == Environment.MediaMounted;
                }
                catch
                {
                    return false; // Handle any unanticipated exceptions just to be safe.
                }
            }
        }

        /// <summary>
        /// Returns the application's external temporary folder or <c>null</c> if the
        /// folder is not accessable.
        /// </summary>
        private Java.IO.File ExternalTempFolder
        {
            get
            {
                if (Environment.ExternalStorageState != Environment.MediaMounted)
                {
                    return null;
                }

                if (externalTempFolder != null)
                {
                    return externalTempFolder;
                }

                return externalTempFolder = new Java.IO.File($"{Environment.ExternalStorageDirectory.AbsolutePath}/{Android.App.Application.Context.ApplicationInfo.ProcessName}/temp");
            }
        }

        /// <inheritdoc/>
        public void PurgeTempFiles()
        {
            try
            {
                if (!ExternalTempFolder.Exists())
                {
                    ExternalTempFolder.Delete();
                }
            }
            catch
            {
                // Eat any exceptions for robustness.
            }
        }

        /// <inheritdoc/>
        public void ShowEmailDraft(string subject, string body, string to, params Attachment[] attachments)
        {
            // Note:
            //
            // The XLabs implementation of this didn't restrict the app choices to
            // just email clients.  It included messaging and other apps.  The code
            // below restricts the choices to just email apps.
            //
            // XLabs was setting the intent type wrong.  It needed to be:
            //
            //      "message/rfc822"

            var intent = new Intent(Intent.ActionSend);

            intent.SetType("message/rfc822");
            intent.PutExtra(Intent.ExtraEmail, new string[] { to });
            intent.PutExtra(Intent.ExtraSubject, subject ?? string.Empty);
            intent.PutExtra(Intent.ExtraText, body);

            if (attachments != null && attachments.Length > 0)
            {
                // Implementation Note:
                //
                // The attachment files need to be located in public external storage so 
                // the email client can load them.  We also can't delete the files here
                // because we won't know exactly when the email client has finished
                // doing its thing.
                //
                // These files may accumulate if we're not careful.  The application
                // should call [PurgeTempFiles()] shortly after launch to mitigate this.

                foreach (var attachment in attachments)
                {
                    var file = new Java.IO.File(ExternalTempFolder.AbsolutePath + "/" + attachment.Name);

                    if (file.Exists())
                    {
                        file.Delete();
                    }
                    else
                    {
                        file.Mkdirs();
                    }

                    var output = new Java.IO.FileOutputStream(file);

                    output.Write(attachment.Data);
                    output.Flush();
                    output.Close();

                    intent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(file));
                }
            }

            var chooserIntent = Intent.CreateChooser(intent, "Choose Email App");

            chooserIntent.SetFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(chooserIntent);
        }

        /// <inheritdoc/>
        public async Task ExperimentalAsync(params object[] args)
        {
            var canSendSmd = Android.Telephony.SmsManager.Default != null;

            Android.Telephony.SmsManager.Default.SendTextMessage("2063561304", null, "Hello World!", null, null);

            await Task.Delay(0);
        }
    }
}
