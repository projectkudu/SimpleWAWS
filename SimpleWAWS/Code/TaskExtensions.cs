using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class TaskExtensions
    {
        public static void Ignore(this Task task)
        {
            //Empty ignore functions for tasks.
        }

        public static Task IgnoreFailure(this Task task)
        {
            return Util.SafeGuard(() => task);
        }

        public static Task<T> IgnoreFailure<T>(this Task<T> task)
        {
            return Util.SafeGuard<T>(() => task);
        }

        public static IEnumerable<Task> IgnoreFailures(this IEnumerable<Task> collection)
        {
            return collection.Select(t => Util.SafeGuard(() => t));
        }

        public static async Task<IEnumerable<T>> IgnoreAndFilterFailures<T>(this IEnumerable<Task<T>> collection)
        {
            return (await collection.Select(t => Util.SafeGuard<T>(() => t)).WhenAll()).NotDefaults();
        }

        public static Task WhenAll(this IEnumerable<Task> collection)
        {
            return Task.WhenAll(collection);
        }

        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> collection)
        {
            return Task.WhenAll(collection);
        }
    }
}