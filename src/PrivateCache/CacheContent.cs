using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Tavis.PrivateCache
{
    public class CacheContent
    {
        public CacheEntry CacheEntry { get; set; }

        public string Key { get; set; }
        
        public DateTimeOffset Expires { get; set; }
        public CacheControlHeaderValue CacheControl { get; set; }
        public bool HasValidator { get; set; }
        public HttpResponseMessage Response { get; set; }

        public bool IsFresh()
        {
            return  Expires > DateTime.UtcNow;
        }

       
    }
}