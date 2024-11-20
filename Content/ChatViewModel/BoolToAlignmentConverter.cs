using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Content.ChatViewModel
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        /// <summary>
        /// Converts boolean value that indicate if message is sent by the user into a HorizontalAlignment.
        /// Returns Right alignment if is sent by user, otherwise Left.
        /// </summary>
        /// <param name="value">The input value, expected to be a boolean.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter for the converter (not used).</param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>HorizontalAlignment.Right if the message is sent by the user, otherwise HorizontalAlignment.Left.</returns>
        /// <exception cref="Exception">Logs error messages in case of exceptions.</exception>

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool isSentByUser && isSentByUser)
                {
                    return HorizontalAlignment.Right;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BoolToalign: {ex.Message}");
            }
            return HorizontalAlignment.Left;
        }

        /// <summary>
        /// Converts a HorizontalAlignment value back to a boolean. 
        /// This method is not implemented and will throw a NotImplementedException when called.
        /// </summary>
        /// <param name="value">The value from the target to convert back.</param>
        /// <param name="targetType">The type to convert back to.</param>
        /// <param name="parameter">Optional parameter for the converter (not used).</param>
        /// <param name="culture">Culture information for conversion.</param>
        /// <returns>Not applicable as the method is not implemented.</returns>
        /// <exception cref="NotImplementedException">Always thrown as this method is not implemented.</exception>

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}