using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tavis.HttpCache
{
    public class InMemoryContentStoreWithEviction : IContentStore
    {
        private readonly object syncRoot = new object();
        private readonly Dictionary<CacheKey, CacheEntryContainer> _CacheContainers = new Dictionary<CacheKey, CacheEntryContainer>();
        private readonly Dictionary<Guid, HttpResponseMessage> _responseCache = new Dictionary<Guid, HttpResponseMessage>();

        private int CacheCapacity = 1000;
        private LruCollection<CacheKey> _lruCollection = new LruCollection<CacheKey>();

        public InMemoryContentStoreWithEviction(int cacheCapacity)
        {
            CacheCapacity = cacheCapacity;
        }

        public async Task<IEnumerable<CacheEntry>> GetEntriesAsync(CacheKey cacheKey)
        {
            if (_CacheContainers.ContainsKey(cacheKey))
            {
                return _CacheContainers[cacheKey].Entries;
            }
            return null;
        }

        public async Task<HttpResponseMessage> GetResponseAsync(Guid variantId)
        {
            return await CloneResponseAsync(_responseCache[variantId]).ConfigureAwait(false);
        }

        public async Task AddEntryAsync(CacheEntry entry, HttpResponseMessage response)
        {
            CacheEntryContainer cacheEntryContainer = GetOrCreateContainer(entry.Key);
            lock (syncRoot)
            {
                _lruCollection.AddOrUpdate(entry.Key);
                cacheEntryContainer.Entries.Add(entry);
                _responseCache[entry.VariantId] = response;

                if (IsCacheFull())
                {
                    RemoveOldestItemFromCache();
                }
            }
        }

        private bool IsCacheFull()
        {
            if (_responseCache.Count >= CacheCapacity)
            {
                return true;
            }

            return false;
        }

        private void RemoveOldestItemFromCache()
        {
            var cacheKey = _lruCollection.Pop();
            CacheEntryContainer entry;
            var exists = _CacheContainers.TryGetValue(cacheKey, out entry);
            if (exists)
            {
                foreach (var item in entry.Entries)
                {
                    _responseCache.Remove(item.VariantId);
                }
                _CacheContainers.Remove(cacheKey);
            }
        }

        public async Task UpdateEntryAsync(CacheEntry entry, HttpResponseMessage response)
        {

            CacheEntryContainer cacheEntryContainer = GetOrCreateContainer(entry.Key);

            lock (syncRoot)
            {
                var oldentry = cacheEntryContainer.Entries.First(e => e.VariantId == entry.VariantId);
                cacheEntryContainer.Entries.Remove(oldentry);
                cacheEntryContainer.Entries.Add(entry);
                _responseCache[entry.VariantId] = response;

                _lruCollection.AddOrUpdate(entry.Key);
            }
        }

        private CacheEntryContainer GetOrCreateContainer(CacheKey key)
        {
            CacheEntryContainer cacheEntryContainer;

            if (!_CacheContainers.ContainsKey(key))
            {
                cacheEntryContainer = new CacheEntryContainer(key);
                lock (syncRoot)
                {
                    _CacheContainers[key] = cacheEntryContainer;
                }
            }
            else
            {
                cacheEntryContainer = _CacheContainers[key];
            }
            return cacheEntryContainer;
        }

        private async Task<HttpResponseMessage> CloneResponseAsync(HttpResponseMessage response)
        {
            var newResponse = new HttpResponseMessage(response.StatusCode);
            var ms = new MemoryStream();

            foreach (var v in response.Headers) newResponse.Headers.TryAddWithoutValidation(v.Key, v.Value);


            if (response.Content != null)
            {
                await response.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                newResponse.Content = new StreamContent(ms);
                foreach (var v in response.Content.Headers) newResponse.Content.Headers.TryAddWithoutValidation(v.Key, v.Value);
            }

            return newResponse;
        }
    }
}