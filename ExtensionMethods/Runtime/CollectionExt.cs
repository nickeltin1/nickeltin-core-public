using System;
using System.Collections.Generic;
using Random = System.Random;

namespace nickeltin.Core.Runtime
{
    public static class CollectionExt
    {
        private static readonly Random random = new Random();
        
        public static T GetRandom<T>(this IList<T> list)
        {
            return list.Count == 0 ? default : list[random.Next(0, list.Count)];
        }
  
        public static T GetRandom<T>(this T[] list)
        {
            return list.Length == 0 ? default : list[random.Next(0, list.Length)];
        }

        public static T GetRandom<T>(this IReadOnlyList<T> list, Random randomSource)
        {
            return list.Count == 0 ? default : list[randomSource.Next(0, list.Count)];
        }

        public static IList<T> ShiftLeft<T>(this IList<T> list, int by)
        {
            for (int i = by; i < list.Count; i++) list[i - by] = list[i];
            for (int i = list.Count - by; i < list.Count; i++) list[i] = default;
            return list;
        }
        
        public static IList<T> ShiftRight<T>(this IList<T> list, int by)
        {
            for (int i = list.Count - by - 1; i >= 0; i--) list[i + by] = list[i];
            for (int i = 0; i < by; i++) list[i] = default;
            return list;
        }
        
        public static void ShiftLeft<T>(this T[] array, int by)
        {
            Array.Copy(array, by, array, 0, array.Length - by);
            Array.Clear(array, array.Length - by, by);
        }

        public static void ShiftRight<T>(this T[] array, int by)
        {
            Array.Copy(array, 0, array, by, array.Length - by);
            Array.Clear(array, 0, by);
        }

        public static void Shuffle<T>(this T[] array)
        {
            array.Shuffle(random);
        }
        
        public static void Shuffle<T>(this T[] array, Random seed)
        {
            var n = array.Length;
            while (n > 1)
            {
                var k = seed.Next(n);
                n--;
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
    }
}