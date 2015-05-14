using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public class ExperimentEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var experiment = string.Empty;
            try
            {
                experiment = ExperimentManager.GetCurrentExperiment();
            }
            catch (Exception e)
            {
                experiment = "Failed: " + e.Message;
            }

            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("Experiment", experiment));
        }
    }
}