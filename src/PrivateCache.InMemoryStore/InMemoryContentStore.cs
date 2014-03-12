using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClientSamples.CachingTools;

namespace PrivateCache.InMemoryStore
{
    public class InMemoryContentStore : IContentStore
    {
        private readonly Dictionary<PrimaryCacheKey, InMemmoryCacheEntry> _responseCache = new Dictionary<PrimaryCacheKey, InMemmoryCacheEntry>();

        public async Task<CacheEntry> GetEntryAsync(PrimaryCacheKey cacheKey)
        {
            if (_responseCache.ContainsKey(cacheKey))
            {
                return _responseCache[cacheKey].CacheEntry;
            }
            return null;
        }

        public async Task<CacheContent> GetContentAsync(CacheEntry cacheEntry, string secondaryKey)
        {
            var inMemoryCacheEntry = _responseCache[cacheEntry.Key];
            if (inMemoryCacheEntry.Responses.ContainsKey(secondaryKey))
            {
                return await CloneAsync(inMemoryCacheEntry.Responses[secondaryKey]);
            }
            return null;
        }

        public async Task UpdateEntryAsync(CacheContent content)
        {
            CacheEntry entry = content.CacheEntry;

            InMemmoryCacheEntry inMemoryCacheEntry = null;

            if (!_responseCache.ContainsKey(entry.Key))
            {
                inMemoryCacheEntry = new InMemmoryCacheEntry(entry);
                _responseCache[entry.Key] = inMemoryCacheEntry;
            }
            else
            {
                inMemoryCacheEntry = _responseCache[entry.Key];
            }
            
            inMemoryCacheEntry.Responses[content.Key] = await CloneAsync(content);

        }


        private async Task<CacheContent> CloneAsync(CacheContent cacheContent)
        {
            var newResponse = await new HttpMessageContent(cacheContent.Response).ReadAsHttpResponseMessageAsync();

            var newContent = new CacheContent()
            {
                CacheEntry = cacheContent.CacheEntry,
                Key = cacheContent.Key,
                Expires = cacheContent.Expires,
                HasValidator = cacheContent.HasValidator,
                CacheControl = cacheContent.CacheControl,
                Response = newResponse
            };
            return newContent;
        }

    }

    public class InMemmoryCacheEntry
    {
        public CacheEntry CacheEntry { get; set; }
        public Dictionary<string,CacheContent> Responses { get; set; }

        public InMemmoryCacheEntry(CacheEntry cacheEntry)
        {
            CacheEntry = cacheEntry;
            Responses = new Dictionary<string, CacheContent>();
        }
    }
}