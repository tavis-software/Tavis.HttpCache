using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tavis;
using Tavis.PrivateCache;
using Tavis.PrivateCache.InMemoryStore;
using Xunit;

namespace PrivateCacheTests
{
    public class ResponseDirectiveTests
    {
        private readonly Uri _BaseAddress;

        public ResponseDirectiveTests()
        {
            _BaseAddress = new Uri(string.Format("http://{0}:1001", Environment.MachineName));
        }

        [Fact]
        public async Task Simple_private_caching()
        {
            // Cache-Control: max-age=5

            var client = CreateCachingEnabledClient();

            var response = await client.GetAsync("/CacheableResource");  // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            Thread.Sleep(1000); // Pause to see non-zero age

            var response2 = await client.GetAsync("/CacheableResource");  // No round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            Thread.Sleep(7000); // Pause for resource to expire

            var response3 = await client.GetAsync("/CacheableResource");   // Server round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }




        [Fact]
        public async Task Simple_private_caching_using_query_parameter()
        {
            var client = CreateCachingEnabledClient();

            var response = await client.GetAsync("/CacheableResource?value=10");  // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);


            var response2 = await client.GetAsync("/CacheableResource?value=20");  // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);

            var response3 = await client.GetAsync("/CacheableResource?value=10");  // From cache
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);

            Thread.Sleep(7000);  // Pause for resource to expire

            var response4 = await client.GetAsync("/CacheableResource?value=10");  // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromServer(response4);
        }


        [Fact]
        public async Task Private_caching_a_POST_response()
        {
            // Cache-Control: max-age=5

            var client = CreateCachingEnabledClient();

            var response = await client.PostAsync("/CacheablePostResponse", new StringContent("Here is a message"));  // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await client.GetAsync("/CacheablePostResponse");  // No round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            Thread.Sleep(7000); // Pause for resource to expire

            var response3 = await client.GetAsync("/CacheablePostResponse");   // Server round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }

        [Fact]
        public async Task Simple_private_caching_by_method()
        {

            var client = CreateCachingEnabledClient();

            var headRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative),
                Method = HttpMethod.Head
            };
            var response = await client.SendAsync(headRequest);  // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);


            var response2 = await client.GetAsync("/CacheableResource");  // Server round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);


            var headRequest2 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative),
                Method = HttpMethod.Head
            };
            var response3 = await client.SendAsync(headRequest2);  // Local round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);


            var response4 = await client.GetAsync("/CacheableResource");   // Local round trip
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromCache(response4);

            // Cached HEAD response != cached GET response
            var content3 = await response3.Content.ReadAsStringAsync();
            var content4 = await response4.Content.ReadAsStringAsync();
            Assert.NotEqual(content3, content4);
        }

        [Fact]
        public async Task Private_caching_a_redirect()
        {
            // Cache-Control: max-age=5

            var client = CreateCachingEnabledClient();


            var response = await client.GetAsync("/PointA");  // Server round trip
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            HttpAssert.FromServer(response);


            var response2 = await client.GetAsync("/PointA");  // No round trip
            Assert.Equal(HttpStatusCode.Found, response2.StatusCode);
            HttpAssert.FromCache(response2);


            Thread.Sleep(7000); // Pause for resource to expire


            var response3 = await client.GetAsync("/PointA");   // Server round trip
            Assert.Equal(HttpStatusCode.Found, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }



        [Fact]
        public async Task Simple_private_caching_with_etag()
        {
            var client = CreateCachingEnabledClient();

            var response = await client.GetAsync("/ResourceWithEtag"); // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await client.GetAsync("/ResourceWithEtag"); // From cache
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            Thread.Sleep(7000); // Pause for resource to expire

            var response3 = await client.GetAsync("/ResourceWithEtag"); // Server roundtrip and 304
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);

            var response4 = await client.GetAsync("/ResourceWithEtag"); // Server roundtrip and 304 / but should be cached
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromCache(response4);
        }




        [Fact]
        public async Task Private_caching_with_accept_language_vary_header()
        {
            var client = CreateCachingEnabledClient();

            var linkEnglish = new Link()
            {
                Target = new Uri("/VaryingCacheableResource", UriKind.Relative)
            };
            linkEnglish.RequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

            var linkFrench = new Link()
            {
                Target = new Uri("/VaryingCacheableResource", UriKind.Relative)
            };
            linkFrench.RequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr"));

            var response = await client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await response.Content.ReadAsStringAsync());
            HttpAssert.FromServer(response);

            var responseExplicitEn = await client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await responseExplicitEn.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitEn);

            var responseExplicitEn2 = await client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await responseExplicitEn2.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitEn2);

            var responseExplicitFr = await client.SendAsync(linkFrench.CreateRequest());
            Assert.Equal("Ce donnée est caché", await responseExplicitFr.Content.ReadAsStringAsync());
            HttpAssert.FromServer(responseExplicitFr);

            var responseExplicitFr2 = await client.SendAsync(linkFrench.CreateRequest());
            Assert.Equal("Ce donnée est caché", await responseExplicitFr2.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitFr2);

            var responseExplicitEn3 = await client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await responseExplicitEn3.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitEn3);

        }

        [Fact]
        public async Task Private_caching_with_encoding_vary_header()
        {

            var client = CreateCachingEnabledClient();

            var linkCompressed = new Link()
            {
                Target = new Uri("/VaryingCompressedContent", UriKind.Relative)
            };
            linkCompressed.RequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            var linkUnCompressed = new Link()
            {
                Target = new Uri("/VaryingCompressedContent", UriKind.Relative)
            };
            linkUnCompressed.RequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));

            var response = await client.SendAsync(linkCompressed.CreateRequest());
            var content = await response.Content.ReadAsStringAsync();
            HttpAssert.FromServer(response);

            var response2 = await client.SendAsync(linkCompressed.CreateRequest());
            var content2 = await response2.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response2);

            var response3 = await client.SendAsync(linkUnCompressed.CreateRequest());
            var content3 = await response3.Content.ReadAsStringAsync();
            HttpAssert.FromServer(response3);

            var response4 = await client.SendAsync(linkUnCompressed.CreateRequest());
            var content4 = await response4.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response4);

            var response5 = await client.SendAsync(linkCompressed.CreateRequest());
            var content5 = await response5.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response5);
        }


        private HttpClient CreateCachingEnabledClient()
        {
            var httpClientHandler = TestServer.CreateServer();
            
            var clientHandler = new PrivateCacheHandler(httpClientHandler, new HttpCache(new InMemoryContentStore()));
            var client = new HttpClient(clientHandler) { BaseAddress = _BaseAddress };
            return client;
        }


    }



    public static class HttpAssert
    {
        public static void FromCache(HttpResponseMessage response)
        {
            Assert.NotNull(response.Headers.Age);
        }

        public static void FromServer(HttpResponseMessage response)
        {
            Assert.Null(response.Headers.Age);
        }
    }
}
