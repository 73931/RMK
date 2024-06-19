using System;
using System.Collections;
using System.Collections.Generic;

namespace NamesInYourLanguage
{
    // 기본적으로 키와 첫 번째 값을 일반적인 Dictionary처럼 사용할 수 있지만
    // 필요할 때만 같이 저장된 두 번째 값을 활용할 수 있는걸 만들어보고 싶었는데
    // 막상 만들다 보니깐 엄청 필요성 있는 것 같진 않기도 하고
    public class DictionaryWithMetaValue<TKey, TValue1, TValue2> : IEnumerable<KeyValuePair<TKey, (TValue1, TValue2)>>
    {
        private Dictionary<TKey, (TValue1, TValue2)> dictionary;

        public DictionaryWithMetaValue()
        {
            dictionary = new Dictionary<TKey, (TValue1, TValue2)>();
        }

        public void Add(TKey key, TValue1 value1, TValue2 value2)
        {
            dictionary.Add(key, (value1, value2));
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue1 value1)
        {
            if (dictionary.TryGetValue(key, out var values))
            {
                value1 = values.Item1;
                return true;
            }
            value1 = default;
            return false;
        }

        public bool TryGetMetaValue(TKey key, out TValue2 value2)
        {
            if (dictionary.TryGetValue(key, out var values))
            {
                value2 = values.Item2;
                return true;
            }
            value2 = default;
            return false;
        }

        public bool TrySetMetaValue(TKey key, TValue2 value2)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = (dictionary[key].Item1, value2);
                return true;
            }
            return false;
        }

        public TValue1 this[TKey key]
        {
            get => dictionary[key].Item1;
            set => dictionary[key] = (value, dictionary[key].Item2);
        }

        public IEnumerator<KeyValuePair<TKey, (TValue1, TValue2)>> GetEnumerator()
        {
            foreach (var pair in dictionary)
            {
                yield return new KeyValuePair<TKey, (TValue1, TValue2)>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    /*
    public class HashKeyDictionary<TKey, TValue>
    {
        private readonly HashSet<TKey> keys;
        private readonly Dictionary<TKey, TValue> dictionary;

        public HashKeyDictionary()
        {
            keys = new HashSet<TKey>();
            dictionary = new Dictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                dictionary.Add(key, value);
            }
            else
            {
                throw new ArgumentException("Add operation failed. The input parameter is already set.");
            }
        }

        public bool ContainsKey(TKey key)
        {
            return keys.Contains(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (keys.Contains(key))
            {
                if (dictionary.TryGetValue(key, out value))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }
    }
    */
}