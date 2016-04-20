using System.Diagnostics;
using System.Net.Http;

namespace Tavis.HttpCache
{

    public enum CacheStatus
    {
        CannotUseCache,
        Revalidate,
        ReturnStored
    }

    
    public class CacheQueryResult
    {
        public CacheStatus Status { get; set; }
        public CacheEntry SelectedEntry;
        public HttpResponseMessage SelectedResponse;


        public static CacheQueryResult CannotUseCache()
        {
            return new CacheQueryResult()
            {
                Status = CacheStatus.CannotUseCache
            };
        }

        public static CacheQueryResult Revalidate(CacheEntry cacheEntry, HttpResponseMessage response)
        {
            HttpCache.UpdateAgeHeader(response);
            return new CacheQueryResult()
            {
                Status = CacheStatus.Revalidate,
                SelectedEntry = cacheEntry,
                SelectedResponse = response
            };
        }

        public static CacheQueryResult ReturnStored(CacheEntry cacheEntry, HttpResponseMessage response)
        {
            HttpCache.UpdateAgeHeader(response);
            return new CacheQueryResult()
            {
                Status = CacheStatus.ReturnStored,
                SelectedEntry = cacheEntry,
                SelectedResponse = response
            };
        }


        internal void ApplyConditionalHeaders(HttpRequestMessage request)
        {
            Debug.Assert(SelectedEntry != null);
            if (SelectedEntry == null || !SelectedEntry.HasValidator) return;

            var httpResponseMessage = SelectedResponse;

            if (httpResponseMessage.Headers.ETag != null)
            {
                request.Headers.IfNoneMatch.Add(httpResponseMessage.Headers.ETag);
            }
            else
            {
                if (httpResponseMessage.Content != null && httpResponseMessage.Content.Headers.LastModified != null)
                {
                    request.Headers.IfModifiedSince = httpResponseMessage.Content.Headers.LastModified;
                }
                
            }
        }

       
    }
}