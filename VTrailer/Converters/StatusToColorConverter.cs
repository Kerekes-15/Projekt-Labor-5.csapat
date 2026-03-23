using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;


namespace VTrailer.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Elérhető":
                        return new SolidColorBrush(Colors.MediumSeaGreen); 
                    case "Kölcsönözve":
                        return new SolidColorBrush(Colors.Crimson); 
                    case "Szervizben":
                        return new SolidColorBrush(Colors.DarkOrange); 
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
