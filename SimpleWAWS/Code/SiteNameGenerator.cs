using System;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public static class SiteNameGenerator
    {
        public static string GenerateName()
        {
            //first 4 parts of a guid
            return string.Format(CultureInfo.InvariantCulture, "{0}-0ee0-4-231-b9ee", Guid.NewGuid().ToString().Substring(0, 8));
        }
        public static string GenerateVmName()
        {
            //first 4 parts of a guid
            return string.Format(CultureInfo.InvariantCulture, "a{0}-0ee0-4-231-b9ee", Guid.NewGuid().ToString().Substring(0, 7));
        }
    }
}
