using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher
{
    internal static class Extensions
    {
        public static T[] Add<T>(this T[] array, T item)
        {
            return Add(array, item, false);
        }

        public static T[] Add<T>(this T[] array, T item, bool isUnique)
        {
            List<T> collection = new List<T>(array);
            if ((isUnique && collection.IndexOf(item) == -1) || (!isUnique)) collection.Add(item);
            return collection.ToArray();
        }

        public static T[] Remove<T>(this T[] array, T item)
        {
            List<T> collection = new List<T>(array);
            collection.Remove(item);
            return collection.ToArray();
        }
    }
}
