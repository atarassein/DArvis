﻿using System;
using System.Text.RegularExpressions;

namespace DArvis.Extensions
{
    public static class TimeSpanExtensions
    {
        static readonly Regex TimeSpanSecondsRegex = new(@"\s*(?<seconds>-?[0-9]*\.?[0-9]{1,})\s*s\s*", RegexOptions.Compiled);
        static readonly Regex TimeSpanMinutesRegex = new(@"\s*(?<minutes>-?[0-9]*\.?[0-9]{1,})\s*m\s*", RegexOptions.Compiled);
        static readonly Regex TimeSpanHoursRegex = new(@"\s*(?<hours>-?[0-9]*\.?[0-9]{1,})\s*h\s*", RegexOptions.Compiled);
        static readonly Regex TimeSpanDaysRegex = new(@"\s*(?<days>-?[0-9]*\.?[0-9]{1,})\s*d\s*", RegexOptions.Compiled);

        public static string ToFractionalEnglish(this TimeSpan timeSpan, bool useShortNotation = false, string format = null)
            => ToEnglish(timeSpan, useShortNotation, false, true, format);

        public static string ToShortEnglish(this TimeSpan timeSpan, bool includeMinorComponent = true, string format = null)
            => ToEnglish(timeSpan, true, includeMinorComponent, false, format);

        public static string ToLongEnglish(this TimeSpan timeSpan, bool includeMinorComponent = true, string format = null)
            => ToEnglish(timeSpan, false, includeMinorComponent, false, format);

        static string ToEnglish(this TimeSpan timeSpan, bool useShortNotation, bool includeMinorComponent = true, bool isFracitonal = false, string format = null)
        {
            var pluralDays = " days";
            var singularDay = " day";
            var pluralHours = " hours";
            var singularHour = " hour";
            var pluralMinutes = " minutes";
            var singularMinute = " minute";
            var pluralSeconds = " seconds";
            var singularSecond = " second";

            if (useShortNotation)
            {
                pluralDays = singularDay = "d";
                pluralHours = singularHour = "h";
                pluralMinutes = singularMinute = "m";
                pluralSeconds = singularSecond = "s";
            }

            if (timeSpan.Days > 0)
            {
                if (isFracitonal)
                    return timeSpan.TotalDays.ToPluralString(pluralDays, singularDay, format ?? "0.#");

                if (timeSpan.Hours > 0 && includeMinorComponent)
                    return string.Format("{0} {1}", timeSpan.Days.ToPluralString(pluralDays, singularDay, format), timeSpan.Hours.ToPluralString(pluralHours, singularHour, format));
                else
                    return string.Format("{0}", timeSpan.Days.ToPluralString(pluralDays, singularDay, format));
            }

            if (timeSpan.Hours > 0)
            {
                if (isFracitonal)
                    return timeSpan.TotalHours.ToPluralString(pluralHours, singularHour, format ?? "0.#");

                if (timeSpan.Minutes > 0 && includeMinorComponent)
                    return string.Format("{0} {1}", timeSpan.Hours.ToPluralString(pluralHours, singularHour, format), timeSpan.Minutes.ToPluralString(pluralMinutes, singularMinute, format));
                else
                    return string.Format("{0}", timeSpan.Hours.ToPluralString(pluralHours, singularHour, format));
            }

            if (timeSpan.Minutes > 0)
            {
                if (isFracitonal)
                    return timeSpan.TotalMinutes.ToPluralString(pluralMinutes, singularMinute, format ?? "0.#");

                if (timeSpan.Seconds > 0 && includeMinorComponent)
                    return string.Format("{0} {1}", timeSpan.Minutes.ToPluralString(pluralMinutes, singularMinute, format), timeSpan.Seconds.ToPluralString(pluralSeconds, singularSecond, format));
                else
                    return string.Format("{0}", timeSpan.Minutes.ToPluralString(pluralMinutes, singularMinute, format));
            }

            if (timeSpan.Seconds > 0)
            {
                if (isFracitonal)
                    return timeSpan.TotalSeconds.ToPluralString(pluralSeconds, singularSecond, format ?? "0.#");

                return string.Format("{0}", timeSpan.Seconds.ToPluralString(pluralSeconds, singularSecond, format));
            }
            else
            {
                return string.Format("{0}", timeSpan.TotalSeconds.ToPluralString(pluralSeconds, singularSecond, format));
            }
        }

        public static string ToPluralString(this int value, string pluralString, string singularString, string format = null)
        {
            if (string.IsNullOrWhiteSpace(format))
                format = "";

            if (value == 1)
                return string.Format("{0}{1}", value.ToString(format), singularString);
            else
                return string.Format("{0}{1}", value.ToString(format), pluralString);
        }

        public static string ToPluralString(this double value, string pluralString, string singularString, string format = null)
        {
            if (string.IsNullOrWhiteSpace(format))
                format = "";

            if (value == 1.0)
                return string.Format("{0}{1}", value.ToString(format), singularString);
            else
                return string.Format("{0}{1}", value.ToString(format), pluralString);
        }

        public static bool TryParse(string text, out TimeSpan value)
        {
            value = TimeSpan.Zero;

            var daysMatch = TimeSpanDaysRegex.Match(text);
            var hoursMatch = TimeSpanHoursRegex.Match(text);
            var minutesMatch = TimeSpanMinutesRegex.Match(text);
            var secondsMatch = TimeSpanSecondsRegex.Match(text);

            if (daysMatch.Success)
            {
                if (double.TryParse(daysMatch.Groups["days"].Value, out var days))
                    value = value.Add(TimeSpan.FromDays(days));
                else
                    return false;
            }

            if (hoursMatch.Success)
            {
                if (double.TryParse(hoursMatch.Groups["hours"].Value, out var hours))
                    value = value.Add(TimeSpan.FromHours(hours));
                else
                    return false;
            }

            if (minutesMatch.Success)
            {
                if (double.TryParse(minutesMatch.Groups["minutes"].Value, out var minutes))
                    value = value.Add(TimeSpan.FromMinutes(minutes));
                else
                    return false;
            }

            if (secondsMatch.Success)
            {
                if (double.TryParse(secondsMatch.Groups["seconds"].Value, out var seconds))
                    value = value.Add(TimeSpan.FromSeconds(seconds));
                else
                    return false;
            }

            return daysMatch.Success || hoursMatch.Success || minutesMatch.Success || secondsMatch.Success;
        }
    }
}
