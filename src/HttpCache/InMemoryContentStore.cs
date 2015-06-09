using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tavis.HttpCache
{
    public class InMemoryContentStore : IContentStore
    {
        private readonly object syncRoot = new object();
        private readonly Dictionary<CacheKey, CacheEntryContainer> _CacheContainers = new Dictionary<CacheKey, CacheEntryContainer>();
        private readonly Dictionary<Guid, HttpResponseMessage> _responseCache = new Dictionary<Guid, HttpResponseMessage>();
        

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
            return await CloneResponseAsync(_responseCache[variantId]);
        }

        public async Task AddEntryAsync(CacheEntry entry, HttpResponseMessage response)
        {
            CacheEntryContainer cacheEntryContainer = GetOrCreateContainer(entry.Key);
            lock (syncRoot)
            {
                cacheEntryContainer.Entries.Add(entry);
                _responseCache[entry.VariantId] = response;
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

    public class CacheEntryContainer
    {
        public CacheKey PrimaryCacheKey { get; set; }
        public List<CacheEntry> Entries { get; set; }

        public CacheEntryContainer(CacheKey primaryCacheKey)
        {
            PrimaryCacheKey = primaryCacheKey;
            Entries = new List<CacheEntry>();
        }
    }
}