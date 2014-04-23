﻿using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ClientSamples.CachingTools;

namespace Tavis.PrivateCache.InMemoryStore
{
    public class InMemoryContentStore : IContentStore
    {
        private readonly object syncRoot = new object();
        private readonly Dictionary<PrimaryCacheKey, InMemoryCacheEntry> _responseCache = new Dictionary<PrimaryCacheKey, InMemoryCacheEntry>();

        public Task<CacheEntry> GetEntryAsync(PrimaryCacheKey cacheKey)
        {
            // NB: Task.FromResult doesn't exist in MS.BCL.Async
            TaskCompletionSource<CacheEntry> ret = new TaskCompletionSource<CacheEntry>();

            if (_responseCache.ContainsKey(cacheKey)) 
            {
                ret.SetResult(_responseCache[cacheKey].CacheEntry);
            } 
            else 
            {
                ret.SetResult(null);
            }

            return ret.Task;
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

            InMemoryCacheEntry inMemoryCacheEntry = null;

            if (!_responseCache.ContainsKey(entry.Key))
            {
                inMemoryCacheEntry = new InMemoryCacheEntry(entry);
                lock (syncRoot)
                {
                    _responseCache[entry.Key] = inMemoryCacheEntry;
                }
            }
            else
            {
                inMemoryCacheEntry = _responseCache[entry.Key];
            }
            
            var newContent = await CloneAsync(content);
            lock (syncRoot)
            {
                inMemoryCacheEntry.Responses[content.Key] = newContent;
            }
        }

        private async Task<CacheContent> CloneAsync(CacheContent cacheContent)
        {
            var newResponse = new HttpResponseMessage(cacheContent.Response.StatusCode);
            var ms = new MemoryStream();

            foreach (var v in cacheContent.Response.Headers) newResponse.Headers.TryAddWithoutValidation(v.Key, v.Value);


            if (cacheContent.Response.Content != null) 
            {
                var bytes = await cacheContent.Response.Content.ReadAsByteArrayAsync();
                newResponse.Content = new ByteArrayContent(bytes);
            
                foreach (var v in cacheContent.Response.Content.Headers) newResponse.Content.Headers.TryAddWithoutValidation(v.Key, v.Value);
            } 
            else 
            {
                newResponse.Content = null;
            }

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

    public class InMemoryCacheEntry
    {
        public CacheEntry CacheEntry { get; set; }
        public Dictionary<string,CacheContent> Responses { get; set; }

        public InMemoryCacheEntry(CacheEntry cacheEntry)
        {
            CacheEntry = cacheEntry;
            Responses = new Dictionary<string, CacheContent>();
        }
    }
}