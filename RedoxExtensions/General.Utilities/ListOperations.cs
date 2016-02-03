using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;

namespace RedoxExtensions.General.Utilities
{
    /// <summary>
    /// Operations on lists -- ie, IEnumerables.
    /// </summary>
    
    public static class ListOperations
    {
        /// <summary>
        /// returns whether the elements of a pair of <c>IEnumerable</c> objects are equal.
        /// If both <c>IEnumerable</c> parameters are <c>null</c> then the method returns <c>true</c>.
        /// If one and only one <c>IEnumerable</c> parameter is <c>null</c> then the method returns <c>false</c>.
        /// </summary>
        /// <typeparam name="T">type of the elements of the <c>IEnumerable</c> parameters</typeparam>
        /// <param name="a"><c>IEnumerable</c> object to be compared</param>
        /// <param name="b"><c>IEnumerable</c> object to be compared</param>
        /// <returns>whether the elements of the given pair of <c>IEnumerable</c> objects are equal</returns>
        public static bool EqualContent<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a == null && b == null)
                return true;

            if ((a != null && b == null) || (a == null && b != null))
                return false;

            IEnumerator<T> e = b.GetEnumerator();
            foreach (T x in a)
            {
                if (!e.MoveNext() || !e.Current.Equals(x))
                    return false;
            }

            return !e.MoveNext();
        }

        /// <summary>
        /// returns whether the elements of a pair of a non-generic <c>IEnumerable</c> objects are equal.
        /// If both <c>IEnumerable</c> parameters are <c>null</c> then the method returns <c>true</c>.
        /// If one and only one non-generic <c>IEnumerable</c> parameter is <c>null</c> then the method returns <c>false</c>.
        /// </summary>
        /// <param name="a">non-generic <c>IEnumerable</c> object to be compared</param>
        /// <param name="b">non-generic <c>IEnumerable</c> object to be compared</param>
        /// <returns>whether the elements of the given pair of non-generic <c>IEnumerable</c> objects are equal</returns>
        public static bool EqualContent(IEnumerable a, IEnumerable b)
        {
            if (a == null && b == null)
                return true;

            if ((a != null && b == null) || (a == null && b != null))
                return false;

            IEnumerator e = b.GetEnumerator();
            foreach (object x in a)
            {
                if (!e.MoveNext() || !e.Current.Equals(x))
                    return false;
            }

            return !e.MoveNext();
        }

        /// <summary>
        /// Returns the first item from the supplied list.  If the list is empty,
        /// throws ArgumentException.
        /// </summary>
        
        public static T First<T>(IEnumerable<T> source)
        {
            // so I'm lazy .. this is the easiest way of getting the first from an enumerable
            foreach (T elt in source)
            {
                return elt;
            }
            throw new ArgumentException("No data");
        }

        /// <summary>
        /// Returns the first item from the supplied list.  If the list is empty,
        /// returns the default value for the element type (usually null).
        /// </summary>
        
        public static T TryFirst<T>(IEnumerable<T> list)
        {
            // so I'm lazy .. this is the easiest way of getting the first from an enumerable
            foreach (T elt in list)
            {
                return elt;
            }
            return default(T);
        }

        /// <summary>
        /// Returns the first item from the supplied list.  If the list is empty,
        /// throws ArgumentException.
        /// </summary>
        
        public static T Last<T>(IEnumerable<T> source)
        {
            IList<T> iList = source as IList<T>;
            if (iList != null)
                return iList[iList.Count - 1];

            T last = default(T); // value is just to shut up the compiler
            bool seen = false;
            foreach (T elt in source)
            {
                last = elt;
                seen = true;
            }
            if (!seen)
            {
                throw new ArgumentException("empty list");
            }
            return last;
        }

        /// <summary>
        /// Returns the first item from the supplied list.  If the list is empty,
        /// returns null.
        /// </summary>
        
        public static T TryLast<T>(IEnumerable<T> source)
        {
            IList<T> iList = source as IList<T>;
            if (iList != null)
                if (iList.Count > 0)
                    return iList[iList.Count - 1];
                else
                    return default(T);

            T last = default(T);
            foreach (T elt in source)
            {
                last = elt;
            }
            return last;
        }

        /// <summary>
        /// Yields a list constructed by skipping the first few from the input list.
        /// </summary>
        /// <param name="toSkip">The number of items to skip.</param>
        /// <param name="src">The original list.</param>
        
        public static IEnumerable<T> SkipFirstN<T>(uint itemsToSkip, IEnumerable<T> source)
        {
            uint skipped = 0;
            foreach (T elt in source)
            {
                if (skipped >= itemsToSkip)
                {
                    yield return elt;
                }
                else
                {
                    skipped++;
                }
            }
        }

        /// <summary>
        /// Yields a list containing all but the first element of the input list.
        /// </summary>
        
        public static IEnumerable<T> SkipFirst<T>(IEnumerable<T> source)
        {
            return SkipFirstN(1, source);
        }


        /// <summary>
        /// Returns the item at the specified position from the supplied list.  
        /// If the list does not contain sufficient items, returns the default 
        /// value for the element type (usually null).
        /// </summary>
        /// <remarks>
        /// Each call to this method does a linear search for the item.  If
        /// you are making repeated calls, consider creating a List{T} from the
        /// enumerable and using direct access to that.
        /// </remarks>
        
        public static T TryItemAt<T>(uint zeroBasedIndex, IEnumerable<T> source)
        {
            T result ;

            TryItemAt(zeroBasedIndex, source, out result);
            return result;
        }

        /// <summary>
        /// Locates the item in a given list.  Sets <code>result</code> to item in <code>list</code> at
        /// <code>zeroBasedIndex</code> or the default value for <code>T</code> if the length of
        /// <code>source</code> is less than <code>zeroBasedIndex - 1</code>.  Returns whether the length
        /// of <code>source</code> is greater than <code>zeroBasedIndex</code>  
        /// </summary>
        /// <typeparam name="T">type of items in <code>list</code></typeparam>
        /// <param name="zeroBasedIndex">index of item in <code>list</code></param>
        /// <param name="source">list</param>
        /// <param name="result">item in <code>list</code> at <code>zeroBasedIndex</code> or the default
        /// value for <code>T</code> if the length of <code>source</code> is less than <code>zeroBasedIndex - 1</code></param>
        /// <returns>whether the length of <code>source</code> is greater than <code>zeroBasedIndex</code></returns>
        public static bool TryItemAt<T>(uint zeroBasedIndex, IEnumerable<T> source, out T result)
        {
            result = default(T);
            
            IList<T> iList = source as IList<T>;
            if (iList != null)
                if (zeroBasedIndex < iList.Count)
                {
                    result = iList[(int)zeroBasedIndex];
                    return true;
                }
                else
                    return false;

            uint i = 0;
            foreach (T elt in source)
            {
                if (zeroBasedIndex == i)
                {
                    result = elt;
                    return true;
                }
                else
                    i++;
            }

            return false;
        }

        /// <summary>
        /// Locates the item in a given list.  Sets <code>result</code> to item in <code>list</code> at
        /// <code>zeroBasedIndex</code> or the default value for <code>T</code> if the length of
        /// <code>source</code> is less than <code>zeroBasedIndex - 1</code>.  Returns whether the length
        /// of <code>source</code> is greater than <code>zeroBasedIndex</code>  
        /// </summary>
        /// <param name="zeroBasedIndex">index of item in <code>list</code></param>
        /// <param name="source">list</param>
        /// <param name="result">item in <code>list</code> at <code>zeroBasedIndex</code> or null if the
        /// length of <code>source</code> is less than <code>zeroBasedIndex - 1</code></param>
        /// <returns>whether the length of <code>source</code> is greater than <code>zeroBasedIndex</code></returns>
        public static bool TryItemAt(uint zeroBasedIndex, System.Collections.IEnumerable source, out object result)
        {
            result = null;

            System.Collections.IList iList = source as System.Collections.IList;
            if (iList != null)
                if (zeroBasedIndex < iList.Count)
                {
                    result = iList[(int)zeroBasedIndex];
                    return true;
                }
                else
                    return false;

            uint i = 0;
            foreach (object elt in source)
            {
                if (zeroBasedIndex == i)
                {
                    result = elt;
                    return true;
                }
                else
                    i++;
            }

            return false;
        }


        /// <summary>
        /// Yields a list of the stringified versions of each item in the source list.
        /// </summary>
        
        public static IEnumerable<string> ToStrings<T>(IEnumerable<T> source)
        {
            foreach (T elt in source)
            {
                yield return elt.ToString();
            }
        }

        /// <summary>
        /// Yields a list of typed pairs from an untyped list
        /// </summary>
        public static IEnumerable<Pair<T1, T2>> ToPairs<T1,T2>(IEnumerable flatList)
        {
            // this approach is a little clumsy, but foreach remains the
            // easiest way to traverse an enumerable.
            bool isFirst = true;
            T1 first = default(T1);
            foreach (object element in flatList)
            {
                if (isFirst)
                    first = (T1)element;
                else
                    yield return new Pair<T1, T2>(first, (T2)element);
                isFirst = !isFirst;
            }

            if (!isFirst)
                throw new ArgumentException("Odd number of elements in input");
        }

        /// <summary>
        /// Converts all elements from one type to another.
        /// </summary>
        /// <remarks>This operation is often called "map".</remarks>
        
        public static IEnumerable<TOut> ConvertAll<TIn, TOut>(Converter<TIn, TOut> converter, IEnumerable<TIn> elements)
        {
            foreach (TIn elt in elements)
                yield return converter(elt);
        }

        /// <summary>
        /// Iterates over two lists in parallel, yielding pairs containing an element from
        /// each list.
        /// </summary>
        /// <remarks>The two lists must contain the same number of elements each, or
        /// an ArgumentOutOfRangeException will be thrown.</remarks>
        
        public static IEnumerable<Pair<T1, T2>> Combine<T1, T2>(IEnumerable<T1> listA, IEnumerable<T2> listB)
        {
            IEnumerator<T1> e1 = listA.GetEnumerator();
            IEnumerator<T2> e2 = listB.GetEnumerator();

            while (true)
            {
                bool ok1 = e1.MoveNext();
                bool ok2 = e2.MoveNext();

                if (!ok1 && !ok2)
                {
                    // done
                    yield break;
                }

                if (!(ok1 && ok2))
                {
                    throw new ArgumentOutOfRangeException("The lists are different lengths");
                }

                yield return new Pair<T1, T2>(e1.Current, e2.Current);
            }
        }

        /// <summary>
        /// Returns the number of items in the list.
        /// </summary>
        
        public static int Count<T>(IEnumerable<T> list)
        {
            IList<T> iList = list as IList<T>;
            if (iList != null)
                return iList.Count;

            IEnumerator<T> e = list.GetEnumerator();
            int seen = 0;
            while (e.MoveNext())
            {
                seen++;
            }
            return seen;
        }

        /// <summary>
        /// Returns the number of items in a given list.
        /// </summary>
        /// <param name="list">list</param>
        /// <returns>the number of items in <c>list</c></returns>
        public static int Count(IEnumerable list)
        {
            IEnumerator e = list.GetEnumerator();
            int seen = 0;
            while (e.MoveNext())
            {
                seen++;
            }
            return seen;
        }

        /// <summary>
        /// Returns true if the number of items in the list is as expected, or
        /// false if it's either smaller or larger.  If the list contains more 
        /// items than expected, they are not enumerated.
        /// </summary>
        
        public static bool CountIs<T>(IEnumerable<T> list, uint expected)
        {
            IList<T> iList = list as IList<T>;
            if (iList != null)
                return (iList.Count == expected);

            IEnumerator<T> e = list.GetEnumerator();
            int seen = 0;
            while (e.MoveNext())
            {
                seen++;
                // the list is too long
                if (seen == expected + 1)
                {
                    return false;
                }
            }
            if (seen == expected)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the list contains no items.  Does not enumerate the whole
        /// list to find out.
        /// </summary>
        
        public static bool IsEmpty<T>(IEnumerable<T> list)
        {
            IList<T> iList = list as IList<T>;
            if (iList != null)
                return (iList.Count == 0);

            foreach (T obj in list)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Enumerates the items in the supplied lists,
        /// one after the other.
        /// </summary>
        
        public static IEnumerable<T> Concatenate<T>(params IEnumerable<T>[] lists)
        {
            foreach (IEnumerable<T> list in lists)
            {
                foreach (T item in list)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Takes a variable number of parameters, and returns an enumerator over them.  This
        /// is useful for initialising lists.
        /// </summary>
        /// <example>List&lt;string&gt; someList = new List&lt;string&gt;(ListOps.Enumerate("one", "two", "three"));</example>
        
        public static IEnumerable<T> Enumerate<T>(params T[] arguments)
        {
            return arguments;
        }

        
        public static string BuildString<T>(IEnumerable<T> items)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T item in items)
            {
                sb.Append(item);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string containing all the items in the supplied list, separated
        /// with the specified joining item.
        /// </summary>
        /// <remarks>This is equivalent to string.Join, but it doesn't require the use
        /// of an actual array.</remarks>
        
        public static string Join<TItem, TJoiner>(TJoiner joiner, IEnumerable<TItem> items)
        {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (TItem item in items)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(joiner);
                }
                sb.Append(item);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates a new list, containing the items returned by the supplied enumerator.
        /// </summary>
        /// <remarks>This is exactly equivalent to using the List{T} constructor taking an enumerator,
        /// except that it allows the compiler to infer the generic type, and avoids repeating it.
        /// </remarks>
        
        public static List<T> Capture<T>(IEnumerable<T> items)
        {
            return new List<T>(items);
        }

        /// <summary>
        /// Creates a non-generic list from a non-generic enumerator
        /// 
        /// <c>ArrayList</c> is used because its suspected that its implementation is the same as the generic <c>List</c>
        /// We need this method because its not possible to construct non-generic collections directly from IEnumerable.
        /// </summary>
        /// <param name="items">sequence of objects</param>
        /// <returns>new <c>ArrayList</c> containing <c>items</c></returns>
        public static ArrayList Capture(IEnumerable items)
        {
            ArrayList result = new ArrayList();

            foreach (object item in items)
                result.Add(item);

            return result;
        }

        /// <summary>
        /// Returns true if the given item appears in the list, or false otherwise.
        /// </summary>
        
        public static bool Contains<T>(T target, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                if (item.Equals(target))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Yields the items in the list, stopping when it reaches the first item that
        /// is equal to the target.  The target item is not yielded.
        /// </summary>
        
        public static IEnumerable<T> ItemsUpTo<T>(T target, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                if (item.Equals(target))
                {
                    yield break;
                }
                yield return item;
            }
        }

        /// <summary>
        /// Converts a list of lists into a single list.
        /// </summary>
        
        public static IEnumerable<T> Flatten<T>(IEnumerable<IEnumerable<T>> listOfLists)
        {
            foreach (IEnumerable<T> list in listOfLists)
            {
                foreach (T item in list)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Converts a list of lists into a single list.
        /// </summary>
        
        public static IEnumerable<T> Flatten<T>(IEnumerable<List<T>> listOfLists)
        {
            foreach (IEnumerable<T> list in listOfLists)
            {
                foreach (T item in list)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Converts a list of lists into a single list.
        /// </summary>
        
        public static IEnumerable<T> Flatten<T>(IEnumerable<T[]> listOfLists)
        {
            foreach (IEnumerable<T> list in listOfLists)
            {
                foreach (T item in list)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Extracts the single item from a list which is
        /// expected to contain exactly one item.  An 
        /// exception is thrown if the list contains zero,
        /// or two or more, items.
        /// </summary>
        
        public static T GetSingleItem<T>(IEnumerable<T> items)
        {
            T singleItem = default(T); // value never used, but needed to appease the compiler
            bool seen = false;
            foreach (T item in items)
            {
                if (seen)
                {
                    // too many items
                    throw new ArgumentException("Too many items in list, expected exactly one");
                }
                seen = true;
                singleItem = item;
            }
            if (!seen)
            {
                throw new ArgumentException("Empty list, expected exactly one item");
            }
            return singleItem;
        }

        /// <summary>
        /// Creates a new list containing the specified items.
        /// </summary>
        
        public static List<T> Create<T>(params T[] items)
        {
            return Capture(items);
        }

        /// <summary>
        /// Counts the occurrences of each unique item in the list, 
        /// and returns a dictionary mapping each item to its count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        
        public static ReadOnlyDictionary<T, int> CountOccurrences<T>(IEnumerable<T> items)
        {
            Dictionary<T, int> occurrences = new Dictionary<T, int>();
            foreach (T item in items)
            {
                // is this the most efficient way to increment,
                // bearing in mind that we don't know if the
                // key exists already?
                int count = 0;
                occurrences.TryGetValue(item, out count);
                occurrences[item] = count + 1;
            }
            return new ReadOnlyDictionary<T, int>(occurrences);
        }

        /// <summary>
        /// Returns a new enumerable that contains all items not matching the target
        /// </summary>
        
        public static IEnumerable<T> Filter<T>(T target, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                if (!Object.Equals(target, item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets a ReadOnlyCollection containing the elements from
        /// the source.  If needed the source will be copied, but if 
        /// the underlying type of the source permits, it will be used
        /// directly.
        /// </summary>
        
        public static ReadOnlyCollection<T> GetReadOnlyCollection<T>(IEnumerable<T> source)
        {
            IList<T> iList = source as IList<T>;
            if (iList != null)
                return new ReadOnlyCollection<T>(iList);
            else
                return new ReadOnlyCollection<T>(Capture(source));
        }

        /// <summary>
        /// Gets an array containing the elements from
        /// the source.  If needed the source will be copied, but if 
        /// the underlying type of the source permits, it will be used
        /// directly.
        /// </summary>
        
        public static T[] GetArray<T>(IEnumerable<T> source)
        {
            T[] array = source as T[];
            if (array != null)
                return array;

            List<T> list = source as List<T>;
            if (list != null)
                return list.ToArray();

            // if we could efficiently count the items we could
            // allocate the array and copy the items ourselves.
            // But if it's a calculated enumerable we don't want
            // to start by counting.  Rather than having two
            // code paths, let's always let List<> sort it out for
            // us.  If this turns out to be inefficient, we can always
            // add the manual version later.
            return Capture(source).ToArray();
        }
    }
}
