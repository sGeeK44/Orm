using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace SmartWay.Orm
{
    public class DistinctCollection<T> : IEnumerable<T> where T : IDistinctable
    {
        private readonly Dictionary<string, T> _items =
            new Dictionary<string, T>(StringComparer.InvariantCultureIgnoreCase);

        private readonly object _syncRoot = new object();

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _items.Count;
                }
            }
        }

        public T this[string itemKey]
        {
            get
            {
                lock (_syncRoot)
                {
                    return _items[itemKey.ToLower()];
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_syncRoot)
                {
                    return _items.ElementAt(index).Value;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_syncRoot)
            {
                return _items.Values.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        public bool Contains(string itemKey)
        {
            lock (_syncRoot)
            {
                if (itemKey == null)
                    return false;

                return _items.ContainsKey(itemKey);
            }
        }

        public void Add(T item)
        {
            // Manage overriding case
            if (_items.ContainsKey(item.Key))
                _items[item.Key] = item;
            else
                _items.Add(item.Key, item);
        }
    }
}