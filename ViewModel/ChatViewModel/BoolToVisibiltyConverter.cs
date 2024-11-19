using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ViewModel.ChatViewModel
{
    public class BoolToVisibilityConverter : IValueConverter
    {
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