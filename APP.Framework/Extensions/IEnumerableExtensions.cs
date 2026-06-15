using System.Linq;
using APP.Framework;

using System.Security.Cryptography;
namespace System.Collections.Generic
{
    /// <summary>
    ///   Extensions methods for the class IEnumrable&lt;T&gt;
    /// </summary>
    public static class IEnumerableExtensions
    {

        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

     
    public static void Shuffles<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    /// <summary>
    ///   Execute the action against every element of the collection
    /// </summary>
    /// <typeparam name = "TSource">The type of the source.</typeparam>
    /// <param name = "collection">The collection.</param>
    /// <param name = "action">The action.</param>
    public static void ForAll<TSource>(this IEnumerable<TSource> collection, Action<TSource> action)
        {
            ArgumentValidator.IsNotNull("action", action);

            if (collection != null)
            {
                foreach (TSource source in collection)
                {
                    action(source);
                }
            }
        }

        /// <summary>
        ///   Determines whether the specified collection is empty.
        /// </summary>
        /// <typeparam name = "TSource">The type of the source.</typeparam>
        /// <param name = "collection">The collection.</param>
        /// <returns>
        ///   <c>true</c> if the specified collection is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty<TSource>(this IEnumerable<TSource> collection)
        {
            return collection == null || collection.Count() <= 0;
        }

        // row n * row m
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> listOfList)
        {
            // base case: 
            IEnumerable<IEnumerable<T>> toReturn =  new[] { Enumerable.Empty<T>() };
            foreach (var oneList in listOfList)
            {
                // Union returns Distinct values, Concat will only merge result (
                toReturn =
                  from row in toReturn
                  from item in oneList
                  select row.Concat(new[] { item });
                
            }
            return toReturn;
        }
    }
}