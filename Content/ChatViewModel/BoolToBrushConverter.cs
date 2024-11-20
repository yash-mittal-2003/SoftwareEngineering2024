using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Content.ChatViewModel
{
    public class BoolToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a SolidColorBrush. 
        /// Returns a light pink brush (#DFB6B2) if the value is true, otherwise returns a dark purple brush (#854F6C).
        /// </summary>
        /// <param name="value">The input value, expected to be a boolean.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter for the converter (not used).</param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>A SolidColorBrush corresponding to the boolean value.</returns>
        /// <exception cref="InvalidCastException">Thrown if the input value cannot be cast to a boolean.</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFB6B2"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#854F6C"));

        }

        /// <summary>
        /// Converts a SolidColorBrush back to a boolean. 
        /// This method is not implemented and always returns null.
        /// </summary>
        /// <param name="value">The value from the target to convert back.</param>
        /// <param name="targetType">The type to convert back to.</param>
        /// <param name="parameter">Optional parameter for the converter (not used).</param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>Always returns null as this method is not implemented.</returns>

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}