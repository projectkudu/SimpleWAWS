using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmArrayWrapper<T>
    {
        public CsmWrapper<T>[] value { get; set; }
    }
}