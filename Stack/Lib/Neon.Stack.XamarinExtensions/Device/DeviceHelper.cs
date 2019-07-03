//-----------------------------------------------------------------------------
// FILE:        DeviceHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

using XLabs.Forms.Services;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services;
using XLabs.Platform.Services.Email;
using XLabs.Platform.Services.Media;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Provides extended device specific information and utilities.
    /// </summary>
    public static class DeviceHelper
    {
        //---------------------------------------------------------------------
        // Local types

        /// <summary>
        /// Display related information.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// The XLabs <see cref="IDisplay"/> implementation for iOS doesn't compute the 
        /// <see cref="IDisplay.Scale"/> factor properly (it doesn't account for the 3x
        /// DPI of modern phones).  This class corrects that problem and also uses the
        /// opportunity to rename some members to clarify their meanings.
        /// </note>
        /// </remarks>
        public sealed class DisplayDetails
        {
            /// <summary>
            /// Local constructor.
            /// </summary>
            internal DisplayDetails()
            {
                if (DeviceHelper.Platform == TargetPlatform.iOS)
                {
                    // $hack(jeff.lill):
                    //
                    // This is a bit of a hack that will require some tweaking
                    // if Apple ever increases their screen density again.

                    if (Xdpi < 400)
                    {
                        Scale = DeviceHelper.internalDisplay.Scale;
                    }
                    else
                    {
                        Scale = 3.0;
                    }
                }
                else
                {
                    Scale = DeviceHelper.internalDisplay.Scale;
                }
            }

            /// <summary>
            /// Returns the height of the display in pixels.
            /// </summary>
            public int PixelHeight
            {
                get { return DeviceHelper.internalDisplay.Height; }
            }

            /// <summary>
            /// Returns the width of the display in pixels.
            /// </summary>
            public int PixelWidth
            {
                get { return DeviceHelper.internalDisplay.Width; }
            }

            /// <summary>
            /// Returns the screen horizontal pixels per inch.
            /// </summary>
            public double Xdpi
            {
                get { return DeviceHelper.internalDisplay.Xdpi; }
            }

            /// <summary>
            /// Returns the screen vertical pixels per inch.
            /// </summary>
            public double Ydpi
            {
                get { return DeviceHelper.internalDisplay.Ydpi; }
            }

            /// <summary>
            /// Returns the scale factor to use when converting device units to pixels.
            /// </summary>
            /// <remarks>
            /// <note type="note">
            /// The scale factor returned is not exact and will depend on the actual device.
            /// Android organizes devices into loose groups based on screen density and then
            /// assigns a single scale factor to all of the devices in each group, even if
            /// their densities vary somewhat.
            /// </note>
            /// </remarks>
            public double Scale { get; private set; }

            //---------------------------------------------------------------------
            // Display unit conversion and parsing.

            /// <summary>
            /// Converts a platform independent value measured at 160 DPI to be used 
            /// into a device specific unit for positioning or sizing a view.
            /// </summary>
            /// <param name="dipMeasure">The device independent input value measured at 160 DPI.</param>
            /// <returns>The device specific measurement.</returns>
            public double DipToDevicePosition(double dipMeasure)
            {
                return deviceHelpers.DipToDevicePosition(dipMeasure);
            }

            /// <summary>
            /// Converts a platform independent value measured at 160 DPI to a device
            /// specific unit to be used for drawing a stroke.
            /// </summary>
            /// <param name="dipMeasure">The device independent input value measured at 160 DPI.</param>
            /// <returns>The device specific measurement.</returns>
            public double DipToDeviceStroke(double dipMeasure)
            {
                return deviceHelpers.DipToDeviceStroke(dipMeasure);
            }

            /// <summary>
            /// Converts a measurement in inches to a device specific unit
            /// to be used for positioning or sizing a view.
            /// </summary>
            /// <param name="inches">The measurement in inches.</param>
            /// <returns>The device specific measurement.</returns>
            public double InchToDevicePosition(double inches)
            {
                return deviceHelpers.InchToDevicePosition(inches);
            }

            /// <summary>
            /// Converts a measurement in inches to a device specific unit
            /// to be used for drawing a stroke.
            /// </summary>
            /// <param name="inches">The measurement in inches.</param>
            /// <returns>The device specific measurement.</returns>
            public double InchToDeviceStroke(double inches)
            {
                return deviceHelpers.InchToDevicePosition(inches);
            }

            /// <summary>
            /// Converts a measurement in pixels to a device specific unit
            /// to be used for positioning or sizing a view.
            /// </summary>
            /// <param name="pixels">The measurment in pixels.</param>
            /// <returns>The device specific measurement.</returns>
            public double PixelToDevicePosition(double pixels)
            {
                return deviceHelpers.PixelToDevicePosition(pixels);
            }

            /// <summary>
            /// Converts a measurement in pixels to a device specific unit
            /// to be used drawing a stroke.
            /// </summary>
            /// <param name="pixels">The measurment in pixels.</param>
            /// <returns>The device specific measurement.</returns>
            public double PixelToDeviceStroke(double pixels)
            {
                return deviceHelpers.PixelToDeviceStroke(pixels);
            }

            /// <summary>
            /// Converts a measurement in device specific units to device
            /// independent units to be used for positioning or sizing a view.
            /// </summary>
            /// <param name="device">The device specific units.</param>
            /// <returns>The device independent measurement.</returns>
            public double DeviceToDipPosition(double device)
            {
                return device / deviceUnitsPerDip;
            }

            /// <summary>
            /// Converts a measurement in device specific units to inches
            /// to be used for positioning or sizing a view.
            /// </summary>
            /// <param name="device">The device specific units.</param>
            /// <returns>The inch measurement.</returns>
            public double DeviceToInchPosition(double device)
            {
                return device / deviceUnitsPerInch;
            }

            /// <summary>
            /// Converts a measurement in device specific units to pixels
            /// to be used for positioning or sizing a view.
            /// </summary>
            /// <param name="device">The device specific units.</param>
            /// <returns>The pixel measurement.</returns>
            public double DeviceToPixelPosition(double device)
            {
                return device / deviceUnitsPerPixel;
            }

            /// <summary>
            /// Parses a single measurement and converts it to the device specific unit
            /// to be used for sizing or positioning a view.
            /// </summary>
            /// <param name="input">The input value.</param>
            /// <returns>The device specific unit.</returns>
            /// <remarks>
            /// <note type="note">
            /// Modern Android and iOS devices measure screen objects at 160 DPI and Windows
            /// Phone devices use 240 DPI.
            /// </note>
            /// <para>
            /// The input may include one or four double input values.  These are floating
            /// point values with an optional unit suffix:
            /// </para>
            /// <list type="table">
            /// <item>
            ///     <term><b>(none)</b> or <b>dip</b></term>
            ///     <description>
            ///     Specifies a measurement in device independent units measured at
            ///     approximately 160 DPI.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>px</b></term>
            ///     <description>
            ///     Specifies a measurment in pixels.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>in</b></term>
            ///     <description>
            ///     Specifies a measurment in inches.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>du</b></term>
            ///     <description>
            ///     Specifies a measurement in device specific units that don't adjust
            ///     for the iOS/Android and Windows Phone scale factor difference.
            ///     </description>
            /// </item>
            /// </list>
            /// <para>
            /// Note that the default without specifiying a unit is device independent units
            /// (<b>dip</b>) since these will be used most often.  This abstracts away the
            /// differences between iOS/Android and Windows Phone.  Note that Android clusters
            /// devices into a handful of display density scale factors, so specifying the
            /// same device independent unit may result in different physical sizes on
            /// different devices.
            /// </para>
            /// </remarks>
            internal double ParseMeasurePosition(string input)
            {
                input = input.Trim();

                if (input.EndsWith("px", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    return PixelToDevicePosition(double.Parse(input, CultureInfo.InvariantCulture));
                }
                else if (input.EndsWith("in", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    return InchToDevicePosition(double.Parse(input, CultureInfo.InvariantCulture));
                }
                else if (input.EndsWith("du", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    double.Parse(input, CultureInfo.InvariantCulture);
                }

                if (input.EndsWith("dip", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 3);
                }

                return DipToDevicePosition(double.Parse(input, CultureInfo.InvariantCulture));
            }

            /// <summary>
            /// Parses a single measurement and converts it to the device specific unit
            /// to be used for setting a stroke width.
            /// </summary>
            /// <param name="input">The input value.</param>
            /// <returns>The device specific unit.</returns>
            /// <remarks>
            /// <note type="note">
            /// Modern Android and iOS devices measure screen objects at 160 DPI and Windows
            /// Phone devices use 240 DPI.
            /// </note>
            /// <para>
            /// The input may include one or four double input values.  These are floating
            /// point values with an optional unit suffix:
            /// </para>
            /// <list type="table">
            /// <item>
            ///     <term><b>(none)</b>/<b>dip</b></term>
            ///     <description>
            ///     Specifies a measurement in device independent units measured at
            ///     approximately 160 DPI.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>px</b></term>
            ///     <description>
            ///     Specifies a measurment in pixels.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>in</b></term>
            ///     <description>
            ///     Specifies a measurment in inches.
            ///     </description>
            /// </item>
            /// <item>
            ///     <term><b>du</b></term>
            ///     <description>
            ///     Specifies a measurement in device specific units that don't adjust
            ///     for the iOS/Android and Windows Phone scale factor difference.
            ///     </description>
            /// </item>
            /// </list>
            /// <para>
            /// Note that the default without specifiying a unit is device independent units
            /// (<b>dip</b>) since these will be used most often.  This abstracts away the
            /// differences between iOS/Android and Windows Phone.  Note that Android clusters
            /// devices into a handful of display density scale factors, so specifying the
            /// same device independent unit may result in different physical sizes on
            /// different devices.
            /// </para>
            /// </remarks>
            internal double ParseMeasureStroke(string input)
            {
                input = input.Trim();

                if (input.EndsWith("px", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    return PixelToDeviceStroke(double.Parse(input, CultureInfo.InvariantCulture));
                }
                else if (input.EndsWith("in", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    return InchToDeviceStroke(double.Parse(input, CultureInfo.InvariantCulture));
                }
                else if (input.EndsWith("du", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 2);

                    return double.Parse(input, CultureInfo.InvariantCulture);
                }

                if (input.EndsWith("dip", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 3);
                }

                return DipToDeviceStroke(double.Parse(input, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Email related helpers.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// The XLabs <see cref="IEmailService"/> didn't work as well as I would have 
        /// liked so I'm providing my own implementation.
        /// </note>
        /// </remarks>
        public sealed class EmailHelper
        {
            /// <summary>
            /// Local constructor.
            /// </summary>
            internal EmailHelper()
            {
            }

            /// <summary>
            /// Returns <c>true</c> if the device is capable of sending email.
            /// </summary>
            public bool CanSend
            {
                get { return DeviceHelper.emailService.CanSend; }
            }

            /// <summary>
            /// Returns <c>true</c> if the device currently supports email attachments.
            /// </summary>
            /// <remarks>
            /// <note type="note">
            /// iOS always supports attachments.  Android devices support attachments if an
            /// external SD card is mounted and the application has read/write access.  Windows 
            /// Phone devices do not support attachments at all.
            /// </note>
            /// </remarks>
            public bool AttachmentsSupported
            {
                get { return DeviceHelper.deviceHelpers.EmailAttachmentsSupported; }
            }

            /// <summary>
            /// Presents the draft of an email message so the user can decide whether to
            /// send the message or not.
            /// </summary>
            /// <param name="subject">The message subject.</param>
            /// <param name="body">The message body in plain text.</param>
            /// <param name="to">The message recipient.</param>
            /// <param name="attachments">The optional attachments.</param>
            public void ShowDraft(string subject, string body, string to, params Attachment[] attachments)
            {
                DeviceHelper.deviceHelpers.ShowEmailDraft(subject, body, to, attachments);
            }
        }

        //---------------------------------------------------------------------
        // Implementation

        private const double DipDpi = 160.0;

        private static IDeviceHelpers           deviceHelpers;
        private static IDisplay                 internalDisplay;
        private static EmailHelper              emailHelper;
        private static ISecureStorage           secureStorage;
        private static IEmailService            emailService;
        private static IPhoneService            phoneService;
        private static IMediaPicker             mediaPicker;
        private static ITextToSpeechService     textToSpeech;
        private static double                   deviceUnitsPerDip;
        private static double                   deviceUnitsPerInch;
        private static double                   deviceUnitsPerPixel;

        /// <summary>
        /// Initializes the device helper.
        /// </summary>
        internal static void Initialize()
        {
            deviceHelpers       = Resolver.Resolve<IDeviceHelpers>();
            Device              = Resolver.Resolve<IDevice>();
            internalDisplay     = Resolver.Resolve<IDisplay>();
            Display             = new DisplayDetails();
            deviceUnitsPerDip   = Display.DipToDevicePosition(1);
            deviceUnitsPerInch  = Display.InchToDevicePosition(1);
            deviceUnitsPerPixel = Display.PixelToDevicePosition(1);
        }

        /// <summary>
        /// Returns the device-specific <see cref="IDeviceHelpers"/> implementation.
        /// </summary>
        internal static IDeviceHelpers Implementation
        {
            get { return deviceHelpers; }
        }

        /// <summary>
        /// Identifies the platform powering the device.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// The Xamarin.Forms <see cref="global::Xamarin.Forms.Device.OS"/> property does not
        /// always return the correct operating system code (e.g. before Xamarin forms is
        /// initialized).  Use this property instead.
        /// </note>
        /// </remarks>
        public static TargetPlatform Platform
        {
            get { return deviceHelpers.Platform; }
        }

        /// <summary>
        /// Returns <c>true</c> if the current device is emulated.
        /// </summary>
        public static bool IsEmulated
        {
            get { return deviceHelpers.IsEmulated; }
        }

        /// <summary>
        /// Provides detailed device information as well as access to advanced device
        /// features such as the gyroscope, accelerometer, network, microphone, phone
        /// calls, etc.
        /// </summary>
        public static IDevice Device { get; private set; }

        /// <summary>
        /// Returns details of the current device's display.
        /// </summary>
        public static DisplayDetails Display { get; private set; }

        /// <summary>
        /// Provides local secure storage service.
        /// </summary>
        public static ISecureStorage SecureStorage
        {
            get
            {
                if (secureStorage != null)
                {
                    return secureStorage;
                }

                return secureStorage = Resolver.Resolve<ISecureStorage>();
            }
        }

        /// <summary>
        /// Provides email services.
        /// </summary>
        public static EmailHelper Email
        {
            get
            {
                if (emailService == null)
                {
                    emailService = Resolver.Resolve<IEmailService>();
                }

                if (emailHelper == null)
                {
                    emailHelper = new EmailHelper();
                }

                return emailHelper;
            }
        }

        /// <summary>
        /// Provides phone and SMS services.
        /// </summary>
        public static IPhoneService Phone
        {
            get
            {
                if (phoneService != null)
                {
                    return phoneService;
                }

                return phoneService = Resolver.Resolve<IPhoneService>();
            }
        }

        /// <summary>
        /// Provides text-to-speech services.
        /// </summary>
        public static ITextToSpeechService TextToSpeech
        {
            get
            {
                if (textToSpeech != null)
                {
                    return textToSpeech;
                }

                return textToSpeech = Resolver.Resolve<ITextToSpeechService>();
            }
        }

        /// <summary>
        /// Provides access to media saved on the device.
        /// </summary>
        public static IMediaPicker MediaPicker
        {
            get
            {
                if (mediaPicker != null)
                {
                    return mediaPicker;
                }

                return mediaPicker = Resolver.Resolve<IMediaPicker>();
            }
        }

        /// <summary>
        /// Controls whether the device status bar is visible.
        /// </summary>
        /// <param name="isVisible">Specifies whether the status bar should be visible or hidden.</param>
        /// <remarks>
        /// <note type="note">
        /// This doesn't currently work for Windows Phone devices.
        /// </note>
        /// </remarks>
        public static void SetStatusVisibility(bool isVisible)
        {
            deviceHelpers.SetStatusVisibility(isVisible);
        }

        /// <summary>
        /// Copies text to the device clipboard.
        /// </summary>
        /// <param name="value">The object to copied to the clipboard.</param>
        public static void CopyToClipboard(object value)
        {
            value = value ?? string.Empty;

            System.Diagnostics.Debug.WriteLine($"Clipboard Copy: {value}");
            deviceHelpers.CopyToClipboard(value.ToString());
        }

        /// <summary>
        /// Uses the device's camera to scan a QR-Code.
        /// </summary>
        /// <returns>The barcode text or <c>null</c> if the scan was cancelled or the App doesn't have access to the camera.</returns>
        /// <remarks>
        /// <note type="note">
        /// The application must be configured with permission to access the camera.
        /// </note>
        /// </remarks>
        public static async Task<string> ScanCameraQRCodeAsync()
        {
            var scanner = DependencyService.Get<IQRCodeScanner>();

            return await scanner.ScanCameraAsync();
        }

        /// <summary>
        /// Scans a QR-Code from <see cref="ImageSource"/> data.
        /// </summary>
        /// <param name="imageStream">The <see cref="Stream"/> with the image data.</param>
        /// <returns>The barcode text or <c>null</c> if a QR-Code could not be located.</returns>
        public static async Task<string> ScanImageQRCodeAsync(Stream imageStream)
        {
            var scanner = DependencyService.Get<IQRCodeScanner>();

            return await scanner.ScanImageAsync(imageStream);
        }

        /// <summary>
        /// Attempts to return the contacts from the device address book.
        /// </summary>
        /// <returns>
        /// The enumerable set of contacts or <c>null</c> if the device or user 
        /// prevented address book access.
        /// </returns>
        /// <remarks>
        /// <note type="note">
        /// The contacts are not returned in any particular order.
        /// </note>
        /// </remarks>
        public static async Task<IEnumerable<Contact>> GetContactsAsync()
        {
            return await deviceHelpers.GetContactsAsync();
        }

        /// <summary>
        /// Deletes any temporary files created by the application.
        /// </summary>
        /// <param name="delay">The optional time to delay before actually deleting the files.</param>
        /// <remarks>
        /// This method deletes the files on a background thread.  The optional delay can 
        /// be used to delay the operation until sometime in the near future.  Applications
        /// may want to do this when purging files during application launch, to minimize
        /// the impact on launch performance.
        /// </remarks>
        public static void PurgeTempFiles(TimeSpan? delay = null)
        {
            Task.Run(
                async () =>
                {
                    if (delay.HasValue)
                    {
                        await Task.Delay(delay.Value);
                    }

                    deviceHelpers.PurgeTempFiles();
                });
        }

        /// <summary>
        /// <b>DEVELOPMENT ONLY:</b> Used for development purposes to test platform specific 
        /// functionality before this can be made into production quality code.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// <note type="note">
        /// This method should <b>never</b> be called by production code.
        /// </note>
        /// </remarks>
        public static async Task ExperimentalAsync(params object[] args)
        {
            await deviceHelpers.ExperimentalAsync(args);
        }
    }
}
