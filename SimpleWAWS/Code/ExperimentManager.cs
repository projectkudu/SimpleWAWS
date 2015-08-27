using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class ExperimentManager
    {
        private static readonly Experiment[] _experiments = new Experiment[]
        {
          // Assign Experiments
        }.OrderBy(e => e.Weight).ToArray();

        private static readonly Experiment _defaultExperiment = new Experiment("Production");

        private static readonly Lazy<int> _totalWeights = new Lazy<int>(() => _experiments.Sum(e => e.Weight));
        private static readonly Random _random = new Random();

        private const string _experimentCookie = "exp2";

        private static string GetExperiment()
        {
            if (_experiments.Length == 0) return _defaultExperiment.Name;
            if (_experiments.Length == 1) return _experiments.First().Name;

            var randomSlot = _random.Next(0, _totalWeights.Value);
            var totalSum = 0;

            for (var i = 0; i < _experiments.Length; i++)
            {
                if (randomSlot < _experiments[i].Weight + totalSum)
                    return _experiments[i].Name;

                totalSum += _experiments[i].Weight;
            }

            return _experiments.Last().Name;
        }

        public static void AssignExperiment(HttpContextBase context)
        {
            if (context.Request.Cookies[_experimentCookie] == null && context.IsBrowserRequest())
            {
                var experiment = GetExperiment();
                context.Response.Cookies.Add(new HttpCookie(_experimentCookie, experiment) { Path = "/", Expires = DateTime.UtcNow.AddDays(1) });
            }
        }

        private static string GetFromRequest(string cookieName, string defaultValue)
        {
            if (HttpContext.Current == null) return "NoRequest";
            return HttpContext.Current.Request.Cookies[cookieName] != null
                ? HttpContext.Current.Request.Cookies[cookieName].Value
                : (!string.IsNullOrEmpty(HttpContext.Current.Request.Headers[cookieName])
                    ? HttpContext.Current.Request.Headers[cookieName]
                    : defaultValue);
        }

        public static string GetCurrentExperiment()
        {
            return GetFromRequest(_experimentCookie, _defaultExperiment.Name);
        }

        public static string GetCurrentSourceVariation()
        {
            return GetFromRequest("sv", "-");
        }
    }
}
