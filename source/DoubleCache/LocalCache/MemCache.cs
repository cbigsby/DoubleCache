﻿using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace DoubleCache.LocalCache
{
    public class MemCache : ICacheAside
    {
        public void Add<T>(string key, T item)
        {
           MemoryCache.Default.Set(key, item, DateTimeOffset.UtcNow.AddMinutes(5));
        }

        public async Task<object> GetAsync(string key, Type type, Func<Task<object>> dataRetriever)
        {
            var item = MemoryCache.Default.Get(key);
            if (item != null && item.GetType() == type)
                return item;

            item = await dataRetriever.Invoke();
            Add(key, item);
            return item.GetType() == type ? item : null;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> dataRetriever) where T : class
        {
            var item = MemoryCache.Default.Get(key) as T;
            if (item != null)
                return item;
            {
                item = await dataRetriever.Invoke();
                Add(key, item);
            }
            return item;
        }
    }
}
