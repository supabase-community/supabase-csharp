using System.Collections.Generic;
using System.Linq;

namespace Supabase.Core.Extensions
{
    /// <summary>
    /// Extensions for the `Dictionary` Classes
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Merges two dictionaries, allowing overwrite priorities leftward.
        ///
        /// Works in C#3/VS2008:
        /// Returns a new dictionary of this ... others merged leftward.
        /// Keeps the type of 'this', which must be default-instantiable.
        /// Example:
        ///   result = map.MergeLeft(other1, other2, ...)
        /// From: https://stackoverflow.com/a/2679857/3629438
        /// </summary>
        /// <param name="me"></param>
        /// <param name="others"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public static T MergeLeft<T, TKey, TValue>(this T me, params IDictionary<TKey, TValue>[] others)
            where T : IDictionary<TKey, TValue>, new() =>
            others.Prepend(me).SelectMany(pairs => pairs).Aggregate(new T(), (newMap, pair) =>
            {
                newMap[pair.Key] = pair.Value;
                return newMap;
            });
    }
}
