using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class FunctionTemplate : BaseTemplate
    {
        public static BaseTemplate DefaultFunctionTemplate
        {
            get { return new FunctionTemplate() { Name = "FunctionsContainer", AppService = AppService.Function }; }
        }
    }
}