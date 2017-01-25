using System.Collections.Generic;

namespace TryAppService.WebJob.Aggregation
{
    public static class Extensions
    {
        public static void IncrementOrCreate<T>(this Dictionary<T, int> dic, T key)
        {
            if (dic.ContainsKey(key))
            {
                dic[key]++;
            }
            else
            {
                dic.Add(key, 1);
            }
        }
    }
}
