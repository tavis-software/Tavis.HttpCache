using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Tavis.HttpCache
{
    public class CacheEntry
    {

        public CacheKey Key { get; private set; }

        public Guid VariantId { get; private set; }
        
        public DateTimeOffset Expires { get; set; }
        public DateTime Date { get; set; }

        public CacheControlHeaderValue CacheControl { get; set; }

        public bool HasValidator { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string Etag { get; set; }

        private List<string> Vary { get; set; }
        private Dictionary<string, IEnumerable<string>> ResponseVaryHeaders { get; set; }


        public CacheEntry(CacheKey key, HttpResponseMessage response)
        {
            Key = key;
            Vary = response.Headers.Vary.Select(v=> v.ToLowerInvariant()).ToList();
            ResponseVaryHeaders = response.RequestMessage.Headers
                                    .Where(h => Vary.Contains(h.Key.ToLowerInvariant()))
                                    .ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
            VariantId = Guid.NewGuid();
            HasValidator = response.Headers.ETag != null || (response.Content != null && response.Content.Headers.LastModified != null);
            CacheControl = response.Headers.CacheControl ?? new CacheControlHeaderValue();
        }

        public bool IsFresh()
        {
            return  Expires > DateTime.UtcNow;
        }

        public bool Match(HttpRequestMessage request)
        {
            
            foreach (var h in Vary)  // Sort the vary headers so that ordering doesn't generate different stored variants
            {
                if (h != "*")
                {
                    IEnumerable<string> newheader = null;
                    request.Headers.TryGetValues(h, out newheader);
                    var oldheader = ResponseVaryHeaders[h];
                    if (newheader == null || !newheader.SequenceEqual(oldheader))
                    {
                        return false;    
                    }
                }
                else
                {
                    return false;
                }
            }
            return true; 
        }

    }
}