/// 
/// Reference https://github.com/projectkudu/ARMClient/tree/master/ARMClient.Authentication
///

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWAWS.Authentication
{
    public class AADCacheManager<T>
    {
        private readonly int _min;
        private readonly int _max;
        private readonly TimeSpan _ttl;
        private readonly Dictionary<string, CacheItem<T>> _caches;

        public AADCacheManager(int min, int max, TimeSpan ttl)
        {
            _min = min;
            _max = max;
            _ttl = ttl;
            _caches = new Dictionary<string, CacheItem<T>>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetValue(string key, out T value)
        {
            lock (_caches)
            {
                CacheItem<T> item;
                if (_caches.TryGetValue(key, out item))
                {
                    if (item.IsValid())
                    {
                        value = item.Value;
                        return true;
                    }

                    _caches.Remove(key);
                }

                value = default(T);
                return false;
            }
        }

        public void Add(string key, T value)
        {
            lock (_caches)
            {
                if (_caches.Count >= _max)
                {
                    foreach (var toDelete in _caches.OrderBy(p => p.Value.Expire).Select(p => p.Key).ToArray())
                    {
                        _caches.Remove(toDelete);
                        if (_caches.Count <= _min)
                        {
                            break;
                        }
                    }
                }

                _caches[key] = new CacheItem<T>(value, DateTime.UtcNow.Add(_ttl));
            }
        }

        public int Count
        {
            get
            {
                lock (_caches)
                {
                    return _caches.Count;
                }
            }
        }

        class CacheItem<TValue>
        {
            public CacheItem(TValue value, DateTime expire)
            {
                Value = value;
                Expire = expire;
            }

            public TValue Value { get; private set; }

            public DateTime Expire { get; private set; }

            public bool IsValid()
            {
                return Expire > DateTime.UtcNow;
            }
        }
    }
}