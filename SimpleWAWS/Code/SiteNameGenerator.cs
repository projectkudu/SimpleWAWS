using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace SimpleWAWS.Models
{

    public static class SiteNameGenerator
    {
        static Random rnd = new Random(10001);

        public static string GenerateName()
        {
            //first 4 parts of a guid
            return string.Format(CultureInfo.InvariantCulture, "{0}-0ee0-4-231-b9ee", Guid.NewGuid().ToString().Substring(0, 8));
        }
        public static string GenerateLinuxSiteName()
        {
            bool siteAlreadyCreated = false;
            var nextName = String.Empty;
            var tries = 10;
            while (!siteAlreadyCreated && tries-- > 0)
            {
                var nextAdjective = rnd.Next(SimpleWawsService.Adjectives.Capacity - 1);
                var nextAnimal = rnd.Next(SimpleWawsService.Animals.Capacity - 1);
                nextName = string.Format(CultureInfo.InvariantCulture, "{0}{1}-{2}", SimpleWawsService.Adjectives[nextAdjective], SimpleWawsService.Animals[nextAnimal],Guid.NewGuid().ToString().Split('-')[1]);
                try
                {
                    IPHostEntry host;
                    host = Dns.GetHostEntry($"{nextName}.azurewebsites.net");
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.HostNotFound)
                        siteAlreadyCreated = true;
                }
                catch (Exception ex)
                {
                    AppInsights.TelemetryClient.TrackException(ex);
                }
                }
                return nextName;
        }
    }
}
