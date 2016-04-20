# Tavis.HttpCache

This library is an implementation of a HTTP cache.  It is designed to be plugged into the System.Net.Http.HttpClient class as a message handler.
	
             var httpCache = new HttpCache(new InMemoryContentStore());
             var cachingHandler = new HttpCacheHandler(httpClientHandler, httpCache);
             var client = new HttpClient(cachingHandler);

With just a few lines of code you can enable caching of any HTTP request made by a client application. The rest of the application remains unchanged.  The existing HTTP requests will be intercepted and short-circuited if a fresh representation is cached.

It is not necessary to implement logic on the client to decide how long content should be cached for as this can be communicated by the server through the use of a `cache-control` header.  This allows the server to determine how long it believes content should be considered fresh for.

Where responses have validators such as Etags and Last-Modified headers, the caching library will automatically make a conditional request to avoid retreiving a representation that is already present in the cache.

If a client wishes to force a request to return the most up-to-date content, it can use the `no-cache` request directive. Or if a client disagrees with the freshness policies determined by the server it can use `min-fresh` and `max-stale` directives to override the server opinion.  A client can even make a request directly to the cache by using `OnlyIfCached`.

The objective of this library is to implement all the functionality described by the [HTTP caching specification RFC 7234](https://tools.ietf.org/html/rfc7234).

The motivation for this library was to provide a PCL compatible replacement for WinInetProxy cache and fix the parts of that library that do not work.

The caching logic and the storage mechanism are separated to allow different storage options.  Currently there is only an in-memory store.  A file based store is high on the list of priorities.
