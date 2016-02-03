using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace RedoxExtensions.General.Utilities
{
    /// <summary>
    /// Class that defines a dictionary for read-only access.
    /// </summary>
    /// <typeparam name="TKey">The key of the dictionary</typeparam>
    /// <typeparam name="TValue">The value of the dictionary</typeparam>
    
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private IDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Constructor with IDictionary.
        /// </summary>
        /// <param name="source">The given IDictionary</param>
        
        public ReadOnlyDictionary(IDictionary<TKey, TValue> source)
        {
            _dictionary = source;
        }

        /// <summary>
        /// Gets an enumerator for the key value pair.
        /// </summary>
        /// <returns>The created enumerator</returns>
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> item in _dictionary)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Checks whether the ReadOnlyDictionary contains a given key.
        /// </summary>
        /// <param name="key">The given key</param>
        /// <returns>True if the key exists</returns>
        
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Tries to get the value for a given key. The value is returned as out parameter for the call.
        /// </summary>
        /// <param name="key">The given key</param>
        /// <param name="value">The returned value</param>
        /// <returns>True if a value for the given key was found</returns>
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Checks whether the ReadOnlyDictionary contains a given item.
        /// </summary>
        /// <param name="item">The key-value pair to check</param>
        /// <returns>True if the item exists</returns>
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the ReadOnlyDictionary to a given array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The given array.</param>
        /// <param name="arrayIndex">The specified array index.</param>
        
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns the value for a given key.
        /// </summary>
        /// <param name="key">The given key</param>
        /// <returns>The returned value</returns>
        
        public TValue this[TKey key]
        {
            get
            {
                return _dictionary[key];
            }
        }

        /// <summary>
        /// A collection with the dictionary keys.
        /// </summary>
        
        public ICollection<TKey> Keys
        {
            get
            {
                ReadOnlyCollection<TKey> keys = new ReadOnlyCollection<TKey>(new List<TKey>(_dictionary.Keys));
                return (ICollection<TKey>)keys;
            }
        }


        /// <summary>
        /// A collection with the dictionary values.
        /// </summary>
        
        public ICollection<TValue> Values
        {
            get
            {
                ReadOnlyCollection<TValue> values = new ReadOnlyCollection<TValue>(new List<TValue>(_dictionary.Values));
                return (ICollection<TValue>)values;
            }
        }

        /// <summary>
        /// The number of items in the dictionary.
        /// </summary>
        
        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        /// <summary>
        /// Indicates whether the dictionary is read-only. Always true.
        /// </summary>
        
        public bool IsReadOnly
        {
            get { return true; }
        }


        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotImplementedException();
        }


        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotImplementedException();
        }


        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return this[key];
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }


        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotImplementedException();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region IDictionary Members

        void IDictionary.Add(object key, object value)
        {
            throw new NotImplementedException();
        }

        void IDictionary.Clear()
        {
            throw new NotImplementedException();
        }

        bool IDictionary.Contains(object key)
        {
            return ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }

        bool IDictionary.IsFixedSize
        {
            get { return ((IDictionary)_dictionary).IsFixedSize; }
        }

        ICollection IDictionary.Keys
        {
            get 
            {
                ReadOnlyCollection<TKey> keys = new ReadOnlyCollection<TKey>(new List<TKey>(_dictionary.Keys));
                return (ICollection)keys;
            }
        }

        void IDictionary.Remove(object key)
        {
            throw new NotImplementedException();
        }

        ICollection IDictionary.Values
        {
            get
            {
                ReadOnlyCollection<TValue> values = new ReadOnlyCollection<TValue>(new List<TValue>(_dictionary.Values));
                return (ICollection)values;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return this[(TKey)key];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return ((IDictionary)_dictionary).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((IDictionary)_dictionary).SyncRoot; }
        }

        #endregion
    }
}
