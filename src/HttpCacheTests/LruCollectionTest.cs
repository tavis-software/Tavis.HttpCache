using System;
using Tavis.HttpCache;
using Xunit;

namespace HttpCacheTests
{
    public class LruCollectionTest
    {

        [Fact]
        public void Should_pop_elements_in_order_of_insertion()
        {
            var lruCollection = new LruCollection<string>();

            var first = "1";
            var second = "2";
            var third = "3";

            lruCollection.AddOrUpdate(first);
            lruCollection.AddOrUpdate(second);
            lruCollection.AddOrUpdate(third);

            var popped1 = lruCollection.Pop();
            var popped2 = lruCollection.Pop();
            var popped3 = lruCollection.Pop();

            Assert.Equal(first, popped1);
            Assert.Equal(second, popped2);
            Assert.Equal(third, popped3);
        }

        [Fact]
        public void Should_pop_elements_in_order_of_insertion_and_update()
        {
            var lruCollection = new LruCollection<string>();

            var first = "1";
            var second = "2";
            var third = "3";

            lruCollection.AddOrUpdate(first);
            lruCollection.AddOrUpdate(second);
            lruCollection.AddOrUpdate(third);

            // Update before popping
            lruCollection.AddOrUpdate(first);

            var popped1 = lruCollection.Pop();
            var popped2 = lruCollection.Pop();
            var popped3 = lruCollection.Pop();

            Assert.Equal(second, popped1);
            Assert.Equal(third, popped2);
            Assert.Equal(first, popped3);
        }

        [Fact]
        public void Should_return_null_if_lru_is_empy()
        {
            var lruCollection = new LruCollection<string>();
            var popped = lruCollection.Pop();
            Assert.Null(popped);
        }

        [Fact]
        public void Should_return_true_if_collection_contains_element()
        {
            var lruCollection = new LruCollection<string>();
            lruCollection.AddOrUpdate("1");
            Assert.True(lruCollection.Contains("1"));
        }

        [Fact]
        public void Should_return_flase_if_collection_doesnt_contain_element()
        {
            var lruCollection = new LruCollection<string>();
            Assert.False(lruCollection.Contains("1"));
        }
    }
}
