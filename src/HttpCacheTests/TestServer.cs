using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace HttpCacheTests
{
    public static class TestServer
    {

        public static HttpServer CreateServer()
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            config.MessageHandlers.Add(new AddDateHeader());
            return new HttpServer(config);
        }


    }

    public class AddDateHeader : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Headers.Date = DateTime.UtcNow;
            return response;
        }
    }
}
