//-----------------------------------------------------------------------------
// FILE:        BooleanValueConverter.cs
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
    /// Handles conversion between an object and a <c>bool</c> value within XAML
    /// bindings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class can be used convert any value into a boolean.  For scalar inputs,
    /// a value of <b>zero</b> or <c>double.NaN</c> or <c>float.NaN</c> will be converted to <c>false</c>
    /// and any other value will be converted to <c>true</c>.
    /// </para>
    /// <para>
    /// For strings or arrays, <c>null</c> or an empty array/string will be converted 
    /// to <c>false</c> and all other values to <c>true</c>.
    /// </para>
    /// <para>
    /// For all other reference values, <c>null</c> will be converted to <c>true</c> and any other 
    /// value will be converted to <c>true</c>.
    /// </para>
    /// <para>
    /// Pass <b>ConverterParameter=Invert</b> to invert the result of the logic described above.
    /// </para>
    /// </remarks>
    public class BooleanValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts the value passed into a <see cref="CornerRadius"/>.
        /// </summary>
        /// <param name="value">The input value to be converted.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The optional converter parameter.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>The converted <c>bool</c> value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string  parameterString = parameter as string;
            bool    invert          = parameterString != null && parameterString.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool    result;

            if (value == null)
            {
                result = false;
            }
            else
            {
                var type = value.GetType();

                if (type.IsArray)
                {
                    var arrayValue = (Array)value;

                    result = arrayValue.Length > 0;
                }

                if (value is string)
                {
                    result = ((string)value).Length > 0;
                }
                else
                {
                    if (value is bool)
                    {
                        result = (bool)value;
                    }
                    else if (value is int)
                    {
                        result = (int)value != 0;
                    }
                    else if (value is double)
                    {
                        var dv = (double)value;

                        result = !double.IsNaN(dv) && dv != 0.0;
                    }
                    else if (value is float)
                    {
                        var fv = (float)value;

                        result = !float.IsNaN(fv) && fv != 0.0F;
                    }
                    else if (value is long)
                    {
                        result = (long)value != 0;
                    }
                    else if (value is byte)
                    {
                        result = (byte)value != 0;
                    }
                    else if (value is uint)
                    {
                        result = (uint)value != 0;
                    }
                    else if (value is ulong)
                    {
                        result = (ulong)value != 0;
                    }
                    else if (value is sbyte)
                    {
                        result = (sbyte)value != 0;
                    }
                    else if (value is decimal)
                    {
                        result = (decimal)value != 0;
                    }
                    else
                    {
                        result = true;
                    }
                }
            }

            return invert ? !result : result;
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
