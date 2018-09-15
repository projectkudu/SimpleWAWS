using System;
using System.Collections.Generic;

namespace SimpleWAWS.Models
{
    public class MakeSubscriptionFreeTrialResult
    {
        public IEnumerable<ResourceGroup> Ready { get; set; }
        public IEnumerable<string> ToCreateInRegions { get; set; }
        public IEnumerable<TemplateStats> ToCreateTemplates { get; set; }
        public IEnumerable<ResourceGroup> ToDelete { get; set; }
    }
    public class TemplateStats
    {
        public string TemplateName { get; set; }
        public int RemainingCount { get; set; }
    }
}