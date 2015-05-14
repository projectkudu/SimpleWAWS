﻿using System;

namespace SimpleWAWS.Models
{
    public static class SiteNameGenerator
    {
        public static string GenerateName()
        {
            //first 4 parts of a guid
            return string.Format("{0}-0ee0-4-231-b9ee", Guid.NewGuid().ToString().Substring(0, 8));
        }
    }
}
