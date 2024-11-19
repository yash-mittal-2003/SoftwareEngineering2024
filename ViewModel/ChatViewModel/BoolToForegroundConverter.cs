using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ViewModel.ChatViewModel
{
    public class BoolToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            try
            {
                if (value is bool isSentByUser)
                {
                    return Brushes.White; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BoolToForegroundConverter: {ex.Message}");
            }
            return Brushes.Black; // Default color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
