using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SimpleWAWS.Code;
using SimpleWAWS.Trace;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SimpleWAWS.Models
{
    class Util
    {
        public static async Task SafeGuard(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard Exception");
            }
        }
        public static async Task<T> SafeGuard<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard<T> Exception");
                return default(T);
            }
        }

        //http://stackoverflow.com/a/1054087
        static Random random = new Random();
        public static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result.ToLowerInvariant();
            return result + random.Next(16).ToString("X").ToLowerInvariant();
        }
        public static void FireAndForget(string hostName)
        {
            try
            {
                var httpHeaders = "GET / HTTP/1.0\r\n" +
                "Host: " + hostName + "\r\n" +
                "\r\n";
                using (var tcpClient = new TcpClient(hostName, 80))
                {
                    tcpClient.Client.Send(Encoding.ASCII.GetBytes(httpHeaders));
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                //log and ignore any tcp exceptions
                SimpleTrace.Diagnostics.Error(ex, "TCP Error");
            }
        }
    }
}
