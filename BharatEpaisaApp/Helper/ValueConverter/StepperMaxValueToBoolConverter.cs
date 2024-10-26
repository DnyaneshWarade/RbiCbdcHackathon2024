namespace BharatEpaisaApp.Helper.ValueConverter
{
    public class StepperMaxValueToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int max)
            {
                if (max == 0 || max == 100)
                {
                    return false;
                }
            }
            // Default max value if parsing fails
            return true;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
