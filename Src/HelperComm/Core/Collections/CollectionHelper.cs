using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace System.Collections.Generic
{
    /// <summary>集合扩展</summary>
    public static class CollectionHelper
    {
        /// <summary>集合转为数组</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ICollection<T> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            var count = collection.Count;
            if (count == 0) return new T[0];
            lock (collection)
            {
                count = collection.Count;
                if (count == 0) return new T[0];

                var arr = new T[count - index];
                collection.CopyTo(arr, index);

                return arr;
            }
        }
    }
}