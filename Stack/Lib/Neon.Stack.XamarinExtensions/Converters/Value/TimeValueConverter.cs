//-----------------------------------------------------------------------------
// FILE:        TimeValueConverter.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Handles <see cref="DateTime"/> conversions between device local and UTC time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pass <b>ConverterParameter=ToUTC</b> to convert the <see cref="DateTime"/> input from
    /// local time to UTC or <b>ConverterParameter=ToLocal</b> to convert from UTC to local
    /// time.  <b>ToLocal</b> will be assumed if no converter parameter is specified or if the
    /// value passed is not valid.
    /// </para>
    /// <note>
    /// This class only converts <see cref="DateTime"/> values.  All other value types will
    /// be returned without change.
    /// </note>
    /// </remarks>
    public class TimeValueConverter : IValueConverter
    {
        /// <summary>
        /// Handles <see cref="DateTime"/> conversions between device local and UTC time.
        /// </summary>
        /// <param name="value">The input value to be converted.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The optional converter parameter.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>The converted <c>bool</c> value.</returns>
        /// <remarks>
        /// <para>
        /// Pass <b>ConverterParameter=ToUTC</b> to convert the <see cref="DateTime"/> input from
        /// local time to UTC or <b>ConverterParameter=ToLocal</b> to convert from UTC to local
        /// time.  <b>ToLocal</b> will be assumed if no converter parameter is specified or if the
        /// value passed is not valid.
        /// </para>
        /// <note>
        /// This class only converts <see cref="DateTime"/> values.  All other value types will
        /// be returned without change.
        /// </note>
        /// </remarks>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
            {
                return value;
            }

            DateTime    time            = (DateTime)value;
            string      parameterString = (parameter as string) ?? "ToLocal";
            bool        toLocal         = parameterString.Equals("ToLocal", StringComparison.OrdinalIgnoreCase);

            if (toLocal)
            {
                return FormsHelper.ToLocal(time);
            }
            else
            {
                return FormsHelper.ToUniversal(time);
            }
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
