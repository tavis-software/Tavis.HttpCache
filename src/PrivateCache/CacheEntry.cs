using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Tavis.PrivateCache;

namespace ClientSamples.CachingTools
{
    public class CacheEntry
    {
        public PrimaryCacheKey Key { get; private set; }
        public HttpHeaderValueCollection<string> VaryHeaders { get; private set; }

        internal CacheEntry(PrimaryCacheKey key, HttpHeaderValueCollection<string> varyHeaders)
        {
            Key = key;
            VaryHeaders = varyHeaders;
        }

        public string CreateSecondaryKey(HttpRequestMessage request)
        {
            
            var key = "";
            foreach (var h in VaryHeaders)
            {
                key += h + ":" + String.Join(",", request.Headers.GetValues(h));
            }
            return key.ToLower();
        }

        public CacheContent CreateContent(HttpResponseMessage response)
        {
            return new CacheContent()
            {
                CacheEntry = this,
                Key = CreateSecondaryKey(response.RequestMessage),
                HasValidator = response.Headers.ETag != null || (response.Content != null && response.Content.Headers.LastModified != null),
                Expires = HttpCache.GetExpireDate(response),
                CacheControl = response.Headers.CacheControl ?? new CacheControlHeaderValue(),
                Response = response,
            };
        }
       
    }
}