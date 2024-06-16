using System.Collections;
using System.Collections.Generic;

namespace NamesInYourLanguage
{
    // 굳이 별도로 타입을 만든 이유는
    // 그냥 재밌으니까..
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

        public TValue1 this[TKey key]
        {
            get => dictionary[key].Item1;
            set => dictionary[key] = (value, dictionary[key].Item2);
        }

        public IEnumerator<KeyValuePair<TKey, (TValue1, TValue2)>> GetEnumerator()
        {
            foreach (var pair in dictionary)
            {
                yield return new KeyValuePair<TKey, (TValue1, TValue2)> (pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}