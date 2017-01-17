using System.Collections.Generic;

namespace SimpleWAWS.Models.CsmModels
{
    public class GraphArrayWrapper<T>
    {
        public IEnumerable<T> value { get; set; }
    }
}