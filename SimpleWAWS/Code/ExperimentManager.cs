using SimpleWAWS.Models;
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
            //new Experiment("blue-banner"),
            //new Experiment("no-banner")
        }.OrderBy(e => e.Weight).ToArray();

        private static readonly Experiment _defaultExperiment = new Experiment("Production");

        private static readonly Lazy<int> _totalWeights = new Lazy<int>(() => _experiments.Sum(e => e.Weight));

        private const string _experimentCookie = "exp0";

        private static string GetExperiment()
        {
            if (_experiments.Length == 0) return _defaultExperiment.Name;
            if (_experiments.Length == 1) return _experiments.First().Name;

            var random = new Random();
            var randomSlot = random.Next(0, _totalWeights.Value);

            for (var i = 0; i < _experiments.Length - 1; i++)
            {
                if (randomSlot > _experiments[i].Weight &&
                    randomSlot < _experiments[i + 1].Weight)
                    return _experiments[i].Name;
            }

            return _experiments.Last().Name;
        }

        public static void AssignExperiment(this HttpContext context)
        {
            if (context.Request.Cookies[_experimentCookie] == null)
            {
                context.Response.Cookies.Add(new HttpCookie(_experimentCookie, GetExperiment()) { Path = "/", Expires = DateTime.UtcNow.AddDays(1) });
            }
        }

        public static string GetCurrentExperiment()
        {
            if (HttpContext.Current == null) return "NoRequest";

            return HttpContext.Current.Request.Cookies[_experimentCookie] != null
                ? HttpContext.Current.Request.Cookies[_experimentCookie].Value
                : _defaultExperiment.Name;
        }

    }
}