using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClientSamples.CachingTools;

namespace Tavis.PrivateCache
{
    public class HttpCache
    {
        private readonly IContentStore _contentStore;

        public Dictionary<HttpMethod, object> CacheableMethods = new Dictionary<HttpMethod, object>
        {
            {HttpMethod.Get, null},
            {HttpMethod.Head, null},
            {HttpMethod.Post,null}
        };

        public HttpCache(IContentStore contentStore)
        {
            _contentStore = contentStore;
        }

        public async Task<CacheQueryResult> QueryCacheAsync(HttpRequestMessage request)
        {
            // Do we have anything stored for this method and URI?
            var cacheEntry = await _contentStore.GetEntryAsync(new PrimaryCacheKey(request.RequestUri, request.Method));
            if (cacheEntry == null)
            {
                return CacheQueryResult.CannotUseCache();
            }

            

            // Do we have a matching variant representation?

            var secondaryKey = cacheEntry.CreateSecondaryKey(request);
            if (secondaryKey == "*")   // Vary: * never matches
            {
                return CacheQueryResult.CannotUseCache();
            }
            var selectedResponse =  await _contentStore.GetContentAsync(cacheEntry, secondaryKey);
            if (selectedResponse == null)
            {
                return CacheQueryResult.CannotUseCache();
            }
            
            // Do caching directives require that we revalidate it regardless of freshness?
            var requestCacheControl = request.Headers.CacheControl ?? new CacheControlHeaderValue();
            if ((requestCacheControl.NoCache || selectedResponse.CacheControl.NoCache))
            {
                return CacheQueryResult.Revalidate(selectedResponse);
            }

            // Is it fresh?
            if (selectedResponse.IsFresh())
            {
                if (requestCacheControl.MinFresh != null)
                {
                    if (HttpCache.CalculateAge(selectedResponse.Response) <= requestCacheControl.MinFresh)
                    {
                        return CacheQueryResult.ReturnStored(selectedResponse);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(selectedResponse);    
                }
                
            }

            // Did the client say we can serve it stale?
            if (requestCacheControl.MaxStale)
            {
                if (requestCacheControl.MaxStaleLimit != null)
                {
                    if ((DateTime.UtcNow -  selectedResponse.Expires) <= requestCacheControl.MaxStaleLimit)
                    {
                        return CacheQueryResult.ReturnStored(selectedResponse);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(selectedResponse);    
                }
            }

            // Do we have a selector to allow us to do a conditional request to revalidate it?
            if (selectedResponse.HasValidator)  
            {
                return CacheQueryResult.Revalidate(selectedResponse);
            }

            // Can't do anything to help
            return CacheQueryResult.CannotUseCache();

        }

        public bool CanStore(HttpResponseMessage response)
        {
            // Only cache responses from methods that allow their responses to be cached
            if (!CacheableMethods.ContainsKey(response.RequestMessage.Method)) return false;
            
            // Only allow responses with status classes (5xx, 4xx,etc) that we understand to be cached 
            if ((int)response.StatusCode > 599 ) return false;

            var cacheControlHeaderValue = response.Headers.CacheControl;

            // Ensure that storing is not explicitly prohibited
            if (cacheControlHeaderValue != null && cacheControlHeaderValue.NoStore) return false;

            if (response.RequestMessage.Headers.CacheControl != null && response.RequestMessage.Headers.CacheControl.NoStore) return false;

            // Ensure we have some freshness directives as this cache doesn't do heuristic based caching

            if (response.Content != null && response.Content.Headers.Expires != null) return true;
            if (cacheControlHeaderValue == null) return false;
            if (cacheControlHeaderValue.MaxAge != null) return true;
            if (cacheControlHeaderValue.SharedMaxAge != null) return true;
            

            return false;
        }

        public async Task UpdateContentAsync(HttpResponseMessage notModifiedResponse, CacheContent cacheContent)
        {
            var newExpires = HttpCache.GetExpireDate(notModifiedResponse);
            if (newExpires > cacheContent.Expires)
            {
                cacheContent.Expires = newExpires;
            }
           //TODO Copy headers from notModifiedResponse to cacheContent
           
            await _contentStore.UpdateEntryAsync(cacheContent);
        }

        public async Task StoreResponseAsync(HttpResponseMessage response)
        {
            var primaryCacheKey = new PrimaryCacheKey(response.RequestMessage.RequestUri, response.RequestMessage.Method);

            CacheEntry cacheEntry = await _contentStore.GetEntryAsync(primaryCacheKey) ?? new CacheEntry(primaryCacheKey, response.Headers.Vary);

            var content = cacheEntry.CreateContent(response);
            await _contentStore.UpdateEntryAsync(content);

        }

        public static DateTimeOffset GetExpireDate(HttpResponseMessage response)
        {
            if (response.Headers.CacheControl != null && response.Headers.CacheControl.MaxAge != null)
            {
                return DateTime.UtcNow + response.Headers.CacheControl.MaxAge.Value;
            }
            else
            {
                if (response.Content != null && response.Content.Headers.Expires != null)
                {
                    return response.Content.Headers.Expires.Value;
                }
            }
            return DateTime.UtcNow;  // Store but assume stale
        }

        public static void UpdateAgeHeader(HttpResponseMessage response)
        {
            if (response.Headers.Date.HasValue)
            {
                response.Headers.Age = CalculateAge(response);
            }
        }

        public static TimeSpan CalculateAge(HttpResponseMessage response)
        {
            var age = DateTime.UtcNow - response.Headers.Date.Value;
            if (age.TotalMilliseconds < 0) age = new TimeSpan(0);
            
            return new TimeSpan(0, 0, (int) Math.Round(age.TotalSeconds));;
        }
    }
}