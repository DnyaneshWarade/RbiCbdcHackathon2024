namespace BharatEpaisaApp.Helper.ValueConverter
{
    public class NoteAvailableValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int available)
            {
                return available == 100 ? 0 : available;
            }
            // Default max value if parsing fails
            return 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
