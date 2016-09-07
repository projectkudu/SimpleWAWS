using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmTemplateParameter
    {
        public string value { get; set; }

        public CsmTemplateParameter(string value)
        {
            this.value = value;
        }
    }
}