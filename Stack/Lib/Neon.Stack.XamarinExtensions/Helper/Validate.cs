//-----------------------------------------------------------------------------
// FILE:        Validate.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Validates user entered values.
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Validates an email address.
        /// </summary>
        /// <param name="email">The string to be validated.</param>
        /// <exception cref="FormatException">Thrown if the string is not valid.</exception>
        public static void Email(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new FormatException("Please enter an email address.");
            }

            // Just doing a cursory check.  Note that the local part of an email address
            // can tehnically be nearly anything (including having '@' characters).  I could
            // try to validate the host part but I'm not going to bother and let the service
            // handle this instead.

            var atPos = email.LastIndexOf('@');

            if (atPos < 0)
            {
                throw new FormatException("Email addresses must include an '@'.");
            }

            var namePart = email.Substring(0, atPos);
            var hostPart = email.Substring(atPos + 1);

            if (string.IsNullOrWhiteSpace(namePart) || string.IsNullOrWhiteSpace(hostPart))
            {
                throw new FormatException("Please enter a valid email address.");
            }
        }

        /// <summary>
        /// Validates a password's suitability.
        /// </summary>
        /// <param name="password">The string to be validated.</param>
        /// <exception cref="FormatException">Thrown if the string is not valid.</exception>
        public static void Password(string password)
        {
            // I'm just going to verify that the password includes at least 8 characters.
            // This matches what the server requires.  

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new FormatException("Please enter your password.");
            }

            if (password.Length < 8)
            {
                throw new FormatException("Passwords require at least 8 characters.");
            }
        }

        /// <summary>
        /// Validates a 10-digit phone number.
        /// </summary>
        /// <param name="phone">The phone number to validate.</param>
        /// <returns>The phone number with all characters but the digits and plus (signs) removed.</returns>
        /// <exception cref="FormatException">Thrown if the string is not valid.</exception>
        /// <remarks>
        /// <note type="note">
        /// I'm going to allow plus (+) as the first character to support international 
        /// numbers and I'm not going to enforce a specific number of digits to support
        /// potentially weird dial plans.
        /// </note>
        /// </remarks>
        public static string Phone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new FormatException("Phone number cannot be blank.");
            }

            var sb = new StringBuilder();

            foreach (var ch in phone)
            {
                if (char.IsDigit(ch) || (sb.Length == 0 && ch == '+'))
                {
                    sb.Append(ch);
                }
                else
                {
                    switch (ch)
                    {
                        // These characters are allowed but ignored.

                        case '(':
                        case ')':
                        case '-':
                        case '.':
                        case ' ':

                            break;

                        default:

                            throw new FormatException("Invalid character in phone number.");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
