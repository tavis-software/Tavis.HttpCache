# Tavis.PrivateCache

This library is an implementation of a HTTP private cache.  It is designed to be plugged into the System.Net.Http.HttpClient class as a message handler.


			
             var httpCache = new HttpCache(new InMemoryContentStore());
             var cachingHandler = new PrivateCacheHandler(httpClientHandler, httpCache);
             var client = new HttpClient(cachingHandler);


The objective of this library is to implement all the functionality described by the [httpbis caching spec](http://tools.ietf.org/html/draft-ietf-httpbis-p6-cache-26), that is applicable to private caches.

The motivation for this library was to provide a PCL compatible replacement for WinInetProxy cache and fix the parts of that library that do not work.


