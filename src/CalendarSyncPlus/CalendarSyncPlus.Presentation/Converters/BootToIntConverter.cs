﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CalendarSyncPlus.Presentation.Converters
{
    public class BootToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                    return 0;

            }
            catch (Exception)
            {
                throw;
            }
            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (((int)value) == 0)
                    return true;

            }
            catch (Exception)
            {
                throw;
            }
            return false;
        }
    }
}
