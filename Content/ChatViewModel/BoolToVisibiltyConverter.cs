using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Content.ChatViewModel
{
    public class BoolToVisibilityConverter : IValueConverter
    {

        /// <summary>
        /// Converts a boolean value to a Visibility enum. 
        /// Returns Visibility.Collapsed or Visibility.Visible based on the boolean value and optional inversion logic.
        /// </summary>
        /// <param name="value">The input value, expected to be a boolean.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">
        /// Optional parameter for inversion logic. If a valid boolean parameter is provided, 
        /// it inverts the conversion logic.
        /// </param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>
        /// Visibility.Visible if the boolean is true (or inverted to false), 
        /// otherwise Visibility.Collapsed. Defaults to Visibility.Collapsed for invalid input.
        /// </returns>

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If the parameter is provided, invert the logic
                bool isInverted = parameter != null && bool.TryParse(parameter.ToString(), out bool result) && result;
                return (boolValue ^ isInverted) ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed; // Default to Collapsed if value is not a bool
        }

        /// <summary>
        /// Converts a Visibility enum back to a boolean value. 
        /// Returns true for Visibility.Visible and false for Visibility.Collapsed or invalid inputs.
        /// Handles optional inversion logic.
        /// </summary>
        /// <param name="value">The value from the target to convert back, expected to be a Visibility enum.</param>
        /// <param name="targetType">The type to convert back to.</param>
        /// <param name="parameter">
        /// Optional parameter for inversion logic. If a valid boolean parameter is provided, 
        /// it inverts the conversion logic.
        /// </param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>
        /// A boolean value corresponding to the Visibility state, with optional inversion logic applied.
        /// Defaults to false for invalid input.
        /// </returns>

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isInverted = parameter != null && bool.TryParse(parameter.ToString(), out bool result) && result;
                return (visibility == Visibility.Visible) ^ isInverted;
            }

            return false; // Default to false if value is not Visibility
        }
    }
}