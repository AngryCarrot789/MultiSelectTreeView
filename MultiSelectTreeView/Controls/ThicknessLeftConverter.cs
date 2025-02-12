﻿using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Controls {
    class ThicknessLeftConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int)
                return new Thickness {Left = (int) value};
            if (value is double)
                return new Thickness {Left = (double) value};
            return new Thickness();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}