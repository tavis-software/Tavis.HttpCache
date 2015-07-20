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
            var queryResult = await _httpCache.QueryCacheAsync(request).ConfigureAwait(false);

            if (queryResult.Status == CacheStatus.ReturnStored)
            {
                return queryResult.SelectedResponse;
            }

            if (request.Headers.CacheControl != null && request.Headers.CacheControl.OnlyIfCached)
            {
                return CreateGatewayTimeoutResponse(request);  // https://tools.ietf.org/html/rfc7234#section-5.2.1.7
            }

            if (queryResult.Status == CacheStatus.Revalidate)
            {
                HttpCache.ApplyConditionalHeaders(queryResult,request);
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                await _httpCache.UpdateFreshnessAsync(queryResult, response).ConfigureAwait(false); 
                response.Dispose();
                return queryResult.SelectedResponse;
            } 
            
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