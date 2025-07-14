﻿using System;
using System.Windows.Controls;

namespace DArvis.Extensions
{
    public static class ControlExtensions
    {
        public static T FindItem<T>(this ItemsControl control, Func<T, bool> selector) where T : class
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            foreach (T item in control.Items)
            {
                var isMatch = selector(item);

                if (isMatch)
                    return item;
            }

            return null;
        }

        public static T FindItemOrDefault<T>(this ItemsControl control, Func<T, bool> selector, T defaultValue = default(T)) where T : class
        {
            var value = FindItem(control, selector);

            if (value == null)
                return defaultValue;
            else
                return value;
        }
    }
}
