using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Dora.Models;

namespace Dora.Converters
{
    public class ListToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Item item = value as Item;
            if (item?.SubData == null)
            {
                if (item.Icon != null)
                {
                    return item.Icon;
                }
                else
                {
                    return "/Dora;component/Assets/file.png";
                }
            }

            return "/Dora;component/Assets/folder.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
