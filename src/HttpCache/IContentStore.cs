using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tavis.HttpCache
{
    public interface IContentStore
    {

        // Retreive all CacheEntries for a particular URL/HttpMethod
        // There should be one CacheEntry for each representation
        Task<IEnumerable<CacheEntry>> GetEntriesAsync(CacheKey cacheKey);

        // Retreive stored HttpResponseMessage using VariantId from selected CacheEntry
        Task<HttpResponseMessage> GetResponseAsync(Guid variantId);

        // Add new CacheEntry and HttpResponseMessage
        Task AddEntryAsync(CacheEntry entry, HttpResponseMessage response);

        // Update existing cached Representation
        // It's possible response could be optional in the case of a 304
        // However, the Expires/Max-age headers in the response would need 
        // to be updated at some point
        Task UpdateEntryAsync(CacheEntry entry, HttpResponseMessage response);

    }


}