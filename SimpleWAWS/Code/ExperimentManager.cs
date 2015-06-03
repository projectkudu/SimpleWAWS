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
            new Experiment("blue-banner"),
            new Experiment("no-banner")
        }.OrderBy(e => e.Weight).ToArray();

        private static readonly Experiment _defaultExperiment = new Experiment("Production");

        private static readonly Lazy<int> _totalWeights = new Lazy<int>(() => _experiments.Sum(e => e.Weight));
        private static readonly Random _random = new Random();

        private const string _experimentCookie = "exp1";

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

        public static void AssignExperiment(this HttpContext context)
        {
            // we need this because Application_BeginRequest gets called 3 time for every request for some reason
            // we mark the request with a request headder that it has an anonymous user associated with it then we use it after that.
            var experimentAssigned = context.Request.Headers[_experimentCookie];
            if (context.Request.Cookies[_experimentCookie] == null && context.IsBrowserRequest())
            {
                var experiment = experimentAssigned ?? GetExperiment();
                context.Response.Cookies.Add(new HttpCookie(_experimentCookie, experiment) { Path = "/", Expires = DateTime.UtcNow.AddDays(1) });
                if (experimentAssigned == null)
                {
                    context.Request.Headers.Add(_experimentCookie, experiment);
                }
            }
        }

        public static string GetCurrentExperiment()
        {
            if (HttpContext.Current == null) return "NoRequest";
            return HttpContext.Current.Request.Cookies[_experimentCookie] != null
                ? HttpContext.Current.Request.Cookies[_experimentCookie].Value
                : (!string.IsNullOrEmpty(HttpContext.Current.Request.Headers[_experimentCookie])
                    ? HttpContext.Current.Request.Headers[_experimentCookie]
                    : _defaultExperiment.Name);
        }
    }
}