using System;
using System.Diagnostics;
using System.Net.Http;
using Tavis.PrivateCache;


namespace ClientSamples.CachingTools
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
        public CacheContent SelectedVariant;


        public static CacheQueryResult CannotUseCache()
        {
            return new CacheQueryResult()
            {
                Status = CacheStatus.CannotUseCache
            };
        }

        public static CacheQueryResult Revalidate(CacheContent cacheContent)
        {
            return new CacheQueryResult()
            {
                Status = CacheStatus.Revalidate,
                SelectedVariant = cacheContent
            };
        }

        public static CacheQueryResult ReturnStored(CacheContent cacheContent)
        {
            return new CacheQueryResult()
            {
                Status = CacheStatus.ReturnStored,
                SelectedVariant = cacheContent
            };
        }

        internal HttpResponseMessage GetHttpResponseMessage(HttpRequestMessage request)
        {
            var response = SelectedVariant.Response;
            response.RequestMessage = request;
            HttpCache.UpdateAgeHeader(response);
            return response;
        }


        internal void ApplyConditionalHeaders(HttpRequestMessage request)
        {
            Debug.Assert(SelectedVariant != null);
            if (SelectedVariant == null || !SelectedVariant.HasValidator) return;

            var httpResponseMessage = SelectedVariant.Response;

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