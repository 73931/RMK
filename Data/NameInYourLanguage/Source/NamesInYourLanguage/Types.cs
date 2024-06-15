using System.Collections;
using System.Collections.Generic;

namespace NamesInYourLanguage
{
    public class DictionaryWithMetaValue<TKey, TValue1, TValue2> : IEnumerable<KeyValuePair<TKey, (TValue1, TValue2)>>
    {
        private Dictionary<TKey, (TValue1, TValue2)> dictionary;
        public DictionaryWithMetaValue()
        {
            dictionary = new Dictionary<TKey, (TValue1, TValue2)>();
        }

        public void Add(TKey key, TValue1 value1,  TValue2 value2)
        {
            dictionary.Add(key, (value1, value2));
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

        public (TValue1, TValue2) this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
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






























/*
namespace NamesInYourLanguage
{
    public class NameTag : IDictionary<string, Dictionary<string, NameTriple>>
    {
        private string key;

        private readonly string valueKey = "value";
        private readonly string tripleKey = "originalTriple";

        private Dictionary<string, object> innerDict;

        public NameTag(string key, string value, NameTriple originalTriple)
        {
            this.key = key;

            this.innerDict = new Dictionary<string, object>
            {
                {valueKey , value},
                {tripleKey, originalTriple}
            };
        }

        public string Key => key;
        public object this[string key] => this[key];

        public ICollection<string> Keys => this.innerDict.Keys;
        public ICollection<object> Values => this.innerDict.Values;

        public int Count => this.innerDict.Count;
        public bool IsReadOnly => false;

        public void Add(string key, string value, NameTriple originalTriple)
        {
            Dictionary<string, object> innerContent = new Dictionary<string, object>
            this.innderDict.Add(key, new Dictionary<string, object>{ { valueKey, value } } );
        }


            /*
        public string Value
        { get { return (string)innerDict[valueKey]; } }
        public NameTriple OriginalTriple
        { get { return (NameTriple)innerDict[tripleKey]; } }

        /*
        public bool TryGetValue(string key, out string value)
        {
            if (innerDict.TryGetValue(valueKey, out object innerValue))
            {
                if (value is innerValue)
                {
                    value = innerValue;
                    return true;
                }
            }

            value = default;
            return false;
        }
        */
    }
}
