using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ClientSamples.CachingTools;

namespace Tavis.PrivateCache
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
            var key = new StringBuilder(); 
            foreach (var h in VaryHeaders.OrderBy(v => v))  // Sort the vary headers so that ordering doesn't generate different stored variants
            {
                if (h != "*")
                {
                    key.Append(h).Append(':');
                    bool addedOne = false;
                    
                    foreach (var val in request.Headers.GetValues(h))
                    {
                        key.Append(val).Append(',');
                        addedOne = true;
                    }
                    
                    if (addedOne)
                    {
                        key.Length--;  // truncate trailing comma.
                    }
                }
                else
                {
                    key.Append('*');
                }
            }
            return key.ToString().ToLowerInvariant();
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
