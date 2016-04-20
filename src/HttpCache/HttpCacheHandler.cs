using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tavis.HttpCache
{
    public class HttpCacheHandler : DelegatingHandler
    {
        private readonly HttpCache _httpCache;


        public HttpCacheHandler(HttpMessageHandler innerHandler, Tavis.HttpCache.HttpCache httpCache)
        {
            _httpCache = httpCache;
            InnerHandler = innerHandler;
        }

        // Process Request and Response
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Check if there is an cached representation that matches the request
            var queryResult = await _httpCache.QueryCacheAsync(request).ConfigureAwait(false);

            // Yes, there is and we can return it immediately
            if (queryResult.Status == CacheStatus.ReturnStored)  
            {
                return queryResult.SelectedResponse;
            }

            // If the client requested only cached responses, but we got here, then return Gatewaytimeout 
            if (request.Headers.CacheControl != null && request.Headers.CacheControl.OnlyIfCached)
            {
                return CreateGatewayTimeoutResponse(request);  // https://tools.ietf.org/html/rfc7234#section-5.2.1.7
            }

            // If there is a cached representation, but it is no longer fresh, then make the request conditional 
            if (queryResult.Status == CacheStatus.Revalidate)
            {
                HttpCache.ApplyConditionalHeaders(queryResult,request);
            }

            // Process the request as normal
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);


            // If the current representation is still current, then freshen the headers
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                await _httpCache.UpdateFreshnessAsync(queryResult, response).ConfigureAwait(false); 
                response.Dispose();
                return queryResult.SelectedResponse;
            } 
            
            // If successful and unsafe then invalidate cache
            // TODO
            
            // If this response can be stored, then store it.
            if (_httpCache.CanStore(response))
            {
                if (response.Content != null) await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                await _httpCache.StoreResponseAsync(response).ConfigureAwait(false);
            }

            return response;

        }

        private HttpResponseMessage CreateGatewayTimeoutResponse(HttpRequestMessage request)
        {
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                RequestMessage = request
            };
        }
    }
}