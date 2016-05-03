using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.AspNet.WebHooks
{
    internal static class Extensions
    {
        public static DateTime ToDateTime(this string self)
        {
            return DateTime.Parse(self, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }
    }
}
