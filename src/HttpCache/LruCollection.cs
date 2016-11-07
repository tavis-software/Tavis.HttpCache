using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tavis.HttpCache;

namespace Tavis.HttpCache
{
    public class LruCollection<T>
    {
        private readonly SortedDictionary<long, T> _sortedElementDictionary = new SortedDictionary<long, T>();
        private readonly Dictionary<T, long> _elementToIdx = new Dictionary<T, long>();

        private long _entryIdx = long.MinValue;
        public LruCollection() { }

        // Add or update reference
        public void AddOrUpdate(T element)
        {
            if (_elementToIdx.ContainsKey(element))
            {
                Update(element);
            }
            else
            {
                Add(element);
            }
        }

        public bool Contains(T element)
        {
            return _elementToIdx.ContainsKey(element);
        }

        private void Add(T element)
        {
            _entryIdx = _entryIdx + 1;
            _sortedElementDictionary.Add(_entryIdx, element);
            _elementToIdx.Add(element, _entryIdx);
        }

        private void Update(T element)
        {
            var oldIdx = _elementToIdx[element];
            _sortedElementDictionary.Remove(oldIdx);

            _entryIdx = _entryIdx + 1;
            _sortedElementDictionary[_entryIdx] = element;
            _elementToIdx[element] = _entryIdx;
        }

        // Remove least recently used reference
        public T Pop()
        {
            if (_sortedElementDictionary.Count == 0)
            {
                return default(T);
            }

            var oldestEntry = _sortedElementDictionary.First();
            var element = oldestEntry.Value;
            var oldIdx = oldestEntry.Key;

            _sortedElementDictionary.Remove(oldIdx);
            _elementToIdx.Remove(element);

            return element;
        }

    }
}
