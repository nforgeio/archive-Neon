//-----------------------------------------------------------------------------
// FILE:        Contact.cs
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
    /// Information about a contact held by the device address book.
    /// </summary>
    public sealed class Contact
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public Contact()
        {
            this.Emails = new List<ContactEmail>();
            this.Phones = new List<ContactPhone>();
        }

        /// <summary>
        /// The contact device ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The contact's display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The contact's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The contact's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Returns the array of email addresses for the contact.
        /// </summary>
        public List<ContactEmail> Emails { get; set; }

        /// <summary>
        /// Returns the array of phone numbers for the contact.
        /// </summary>
        public List<ContactPhone> Phones { get; set; }

        /// <summary>
        /// The value to use when sorting contacts.
        /// </summary>
        public string SortKey { get; set; }

        /// <summary>
        /// Returns the contact email addresses as a comma separated list.
        /// </summary>
        public string EmailList
        {
            get
            {
                // Optimize the common cases.

                if (Emails == null || Emails.Count == 0)
                {
                    return string.Empty;
                }
                else if (Emails.Count == 1)
                {
                    return Emails[0].Address;
                }

                // Generate the list.

                var sb = new StringBuilder();

                foreach (var email in Emails)
                {
                    sb.AppendWithSeparator(email.Address, ", ");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the contact phone numbers as a comma separated list.
        /// </summary>
        public string PhoneList
        {
            get
            {
                // Optimize the common cases.

                if (Phones == null || Phones.Count == 0)
                {
                    return string.Empty;
                }
                else if (Phones.Count == 1)
                {
                    return Phones[0].Number;
                }

                // Generate the list.

                var sb = new StringBuilder();

                foreach (var phone in Phones)
                {
                    sb.AppendWithSeparator(phone.Number, ", ");
                }

                return sb.ToString();
            }
        }
    }
}
