using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace PrivateCacheTests
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
