using System.Globalization;

namespace BharatEpaisaApp.Helper.ValueConverter
{
    public class EpochToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && long.TryParse(value.ToString(), out var epoch))
            {
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
                return dateTime.ToString("dd-MM-yyyy"); // Customize the format as needed
            }
            return string.Empty; // Return an empty string if the value is not valid
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
