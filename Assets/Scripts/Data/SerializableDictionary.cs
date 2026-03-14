using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// 可序列化的字典包装类（用于Inspector显示）
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
        
        [SerializeField]
        private List<TValue> values = new List<TValue>();

        private Dictionary<TKey, TValue> _dictionary;

        public Dictionary<TKey, TValue> Dict
        {
            get
            {
                if (_dictionary == null)
                {
                    RebuildDictionary();
                }
                return _dictionary;
            }
        }

        public void RebuildDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
            for (var i = 0; i < keys.Count; i++)
            {
                if (i < values.Count && !_dictionary.ContainsKey(keys[i]))
                {
                    _dictionary.Add(keys[i], values[i]);
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            keys.Add(key);
            values.Add(value);
            RebuildDictionary();
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    values[keys.IndexOf(key)] = value;
                }
                else
                {
                    Add(key, value);
                }

                RebuildDictionary();
            }
        }
        
        public int Count => keys.Count;
        public List<TKey> Keys => keys;
        public List<TValue> Values => values;
    }
}