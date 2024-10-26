using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BharatEpaisaApp.Helper.ValueConverter
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string colorString)
            {
                if (Color.TryParse(colorString, out Color color))
                {
                    return color;
                }
            }
            // Default color if parsing fails
            return Color.FromRgb(0,0,0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
