using System.Collections.Generic;

namespace UdonTrigger
{
    public static class DictionaryExtension
    {
        public static void Marge<TKey, TValuePair>(this IDictionary<TKey, TValuePair> a,
                                                        IDictionary<TKey, TValuePair> b)
        {
            foreach (var item in b)
            {
                if (a.ContainsKey(item.Key))
                {
                    continue;
                }
                a[item.Key] = item.Value;
            }
        }
    }
}