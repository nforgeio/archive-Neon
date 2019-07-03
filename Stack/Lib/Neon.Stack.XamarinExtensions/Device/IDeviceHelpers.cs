//-----------------------------------------------------------------------------
// FILE:        IExtendedDeviceInfo.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Describes the cross platform implementation of a class to obtain extended
    /// device information and features.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// This interface is not intended to be access directly by applications.  Use
    /// the static <see cref="DeviceHelper"/> class instead.
    /// </note>
    /// <note type="note">
    /// Implementations of this interface are intended to be instantiated once and 
    /// then cached for reuse by the application.
    /// </note>
    /// </remarks>
    public interface IDeviceHelpers
    {
        /// <summary>
        /// Identifies the platform system powering the device.
        /// </summary>
        TargetPlatform Platform { get; }

        /// <summary>
        /// Returns <c>true</c> if the current device is emulated.
        /// </summary>
        bool IsEmulated { get; }

        /// <summary>
        /// Converts a platform independent value measured at 160 DPI to be used 
        /// for positioning or sizing a view.
        /// </summary>
        /// <param name="dipMeasure">The device independent input value measured at 160 DPI.</param>
        /// <returns>The device measurement.</returns>
        double DipToDevicePosition(double dipMeasure);

        /// <summary>
        /// Converts a platform independent value measured at 160 DPI to device
        /// to be used for drawing a stroke.
        /// </summary>
        /// <param name="dipMeasure">The device independent input value measured at 160 DPI.</param>
        /// <returns>The device measurement.</returns>
        double DipToDeviceStroke(double dipMeasure);

        /// <summary>
        /// Converts a measurement in inches to be used for positioning or sizing a view.
        /// </summary>
        /// <param name="inches">The measurement in inches.</param>
        /// <returns>The device measurement.</returns>
        double InchToDevicePosition(double inches);

        /// <summary>
        /// Converts a measurement in inches to be used for drawing a stroke.
        /// </summary>
        /// <param name="inches">The measurement in inches.</param>
        /// <returns>The device measurement.</returns>
        double InchToDeviceStroke(double inches);

        /// <summary>
        /// Converts a measurement in pixels to be used for positioning or sizing a view.
        /// </summary>
        /// <param name="pixels">The measurment in pixels.</param>
        /// <returns>The device specific measurement.</returns>
        double PixelToDevicePosition(double pixels);

        /// <summary>
        /// Converts a measurement in pixels to be used drawing a stroke.
        /// </summary>
        /// <param name="pixels">The measurment in pixels.</param>
        /// <returns>The device specific measurement.</returns>
        double PixelToDeviceStroke(double pixels);

        /// <summary>
        /// Controls whether the device status bar is visible.
        /// </summary>
        /// <param name="isVisible">Specifies whether the status bar should be visible or hidden.</param>
        void SetStatusVisibility(bool isVisible);

        /// <summary>
        /// Copies text to the device clipboard.
        /// </summary>
        /// <param name="text">The text to copied to the clipboard.</param>
        void CopyToClipboard(string text);

        /// <summary>
        /// Measures the dimensions of a string as it will be rendered using the specified font
        /// and size.
        /// </summary>
        /// <param name="text">The string to be measured.</param>
        /// <param name="fontSize">The optional font size in device specific units.</param>
        /// <param name="width">The optional width to constrain the text to measure with word wrapping.</param>
        /// <param name="fontName">The optional font name to override the default.</param>
        /// <param name="fontAttributes">The optional font attributes.;</param>
        /// <returns>The <see cref="Size"/> specifying the text height and width in device specific units.</returns>
        /// <remarks>
        /// <para>
        /// Pass the string you want to measure as <paramref name="text"/> and the font
        /// size as <paramref name="fontSize"/>.  The result will describe the height and 
        /// width of the text using the device's default font.  You can specify a custom
        /// font using <paramref name="fontName"/>.
        /// </para>
        /// <para>
        /// By default, the height returned will be for one line of text.  Use <paramref name="width"/>
        /// to constrain the width of the text rendered to word-wrap the text.  In this case,
        /// the height returned will account for the number of text lines required to render
        /// the string.
        /// </para>
        /// </remarks>
        Size MeasureText(string text, double fontSize = 0.0, double width = int.MaxValue, string fontName = null, FontAttributes fontAttributes = FontAttributes.None);

        /// <summary>
        /// Attempts to return the contacts from the device address book.
        /// </summary>
        /// <returns>
        /// The array of contacts or <c>null</c> if the device or user prevented address book access.
        /// </returns>
        /// <remarks>
        /// <note type="note">
        /// The contacts are not returned in any particular order.
        /// </note>
        /// </remarks>
        Task<IEnumerable<Contact>> GetContactsAsync();

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
        bool EmailAttachmentsSupported { get; }

        /// <summary>
        /// Presents the draft of an email message so the user can decide whether to
        /// send the message or not.
        /// </summary>
        /// <param name="subject">The message subject.</param>
        /// <param name="body">The message body in plain text.</param>
        /// <param name="to">The message recipient.</param>
        /// <param name="attachments">The optional attachments.</param>
        /// <remarks>
        /// <note type="note">
        /// Attachments may be written to the device file system (depending on the platform).
        /// To ensure that these don't accumulate and fill up storage, you should call
        /// <see cref="PurgeTempFiles"/> at strategic times (like shortly after application
        /// launch).
        /// </note>
        /// </remarks>
        void ShowEmailDraft(string subject, string body, string to, params Attachment[] attachments);

        /// <summary>
        /// Deletes any temporary files created by the application.
        /// </summary>
        void PurgeTempFiles();

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
        Task ExperimentalAsync(params object[] args);
    }
}
