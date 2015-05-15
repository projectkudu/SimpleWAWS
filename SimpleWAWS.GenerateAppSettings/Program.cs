using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWAWS.GenerateAppSettings
{
    class Program
    {
        static void Main(string[] args)
        {
            File.ReadAllLines(args[0])
                .Where(l => l.StartsWith("set", StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Substring("set".Length).Trim())
                .Select(l => new { Key = l.Substring(0, l.IndexOf("=")), Value = l.Substring(l.IndexOf("=") + 1) })
                .ToList()
                .ForEach(e => Console.Write(string.Format("{{\n    \"name\": \"{0}\",\n    \"value\": \"{1}\"\n}},\n", e.Key, e.Value)));
        }
    }
}
