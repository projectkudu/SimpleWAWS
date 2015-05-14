using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class GraphArrayWrapper<T>
    {
        public IEnumerable<T> value { get; set; }
    }
}