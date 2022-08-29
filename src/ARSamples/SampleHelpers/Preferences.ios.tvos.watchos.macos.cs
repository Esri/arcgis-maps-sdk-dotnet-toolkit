﻿// Copied from https://github.com/xamarin/Essentials/blob/e054b7e19b7fb8f1787af41c95ce4447660422ed/Xamarin.Essentials/Preferences/Preferences.ios.tvos.watchos.macos.cs#L1
#if __IOS__ && NET6_0_OR_GREATER && !MAUI
using System;
using System.Globalization;
using Foundation;

namespace Xamarin.Essentials
{
    public static partial class Preferences
    {
        static readonly object locker = new object();

        static bool PlatformContainsKey(string key, string sharedName)
        {
            lock (locker)
            {
                return GetUserDefaults(sharedName)[key] != null;
            }
        }

        static void PlatformRemove(string key, string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (userDefaults[key] != null)
                        userDefaults.RemoveObject(key);
                }
            }
        }

        static void PlatformClear(string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    var items = userDefaults.ToDictionary();

                    foreach (var item in items.Keys)
                    {
                        if (item is NSString nsString)
                            userDefaults.RemoveObject(nsString);
                    }
                }
            }
        }

        static void PlatformSet<T>(string key, T value, string sharedName)
        {
            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (value == null)
                    {
                        if (userDefaults[key] != null)
                            userDefaults.RemoveObject(key);
                        return;
                    }

                    switch (value)
                    {
                        case string s:
                            userDefaults.SetString(s, key);
                            break;
                        case int i:
                            userDefaults.SetInt(i, key);
                            break;
                        case bool b:
                            userDefaults.SetBool(b, key);
                            break;
                        case long l:
                            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                            userDefaults.SetString(valueString, key);
                            break;
                        case double d:
                            userDefaults.SetDouble(d, key);
                            break;
                        case float f:
                            userDefaults.SetFloat(f, key);
                            break;
                    }
                }
            }
        }

        static T PlatformGet<T>(string key, T defaultValue, string sharedName)
        {
            object value = null;

            lock (locker)
            {
                using (var userDefaults = GetUserDefaults(sharedName))
                {
                    if (userDefaults[key] == null)
                        return defaultValue;

                    switch (defaultValue)
                    {
                        case int i:
                            value = (int)(nint)userDefaults.IntForKey(key);
                            break;
                        case bool b:
                            value = userDefaults.BoolForKey(key);
                            break;
                        case long l:
                            var savedLong = userDefaults.StringForKey(key);
                            value = Convert.ToInt64(savedLong, CultureInfo.InvariantCulture);
                            break;
                        case double d:
                            value = userDefaults.DoubleForKey(key);
                            break;
                        case float f:
                            value = userDefaults.FloatForKey(key);
                            break;
                        case string s:
                            // the case when the string is not null
                            value = userDefaults.StringForKey(key);
                            break;
                        default:
                            // the case when the string is null
                            if (typeof(T) == typeof(string))
                                value = userDefaults.StringForKey(key);
                            break;
                    }
                }
            }

            return (T)value;
        }

        static NSUserDefaults GetUserDefaults(string sharedName)
        {
            if (!string.IsNullOrWhiteSpace(sharedName))
                return new NSUserDefaults(sharedName, NSUserDefaultsType.SuiteName);
            else
                return NSUserDefaults.StandardUserDefaults;
        }
    }
}
#endif