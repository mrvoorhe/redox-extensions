using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxLib.General
{
    #region Public Delegates

    /// <summary>
    /// A function that returns true or false given an item.
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <param name="item">The item</param>
    /// <returns>True or False</returns>
    public delegate bool FilterFunction<T>(T item);

    /// <summary>
    /// A function that transforms an item into some other item of the same type
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <param name="item">The item</param>
    /// <returns>A modified item of the same type</returns>
    public delegate T MapFunction<T>(T item);

    /// <summary>
    /// A function to use with the Iterate method.
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <param name="item">The item</param>
    public delegate void IterateFunction<T>(T item);

    /// <summary>
    /// A function that transforms an item into some other item of the same type
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <typeparam name="TReturnType">Any type</typeparam>
    /// <param name="item">The item</param>
    /// <returns>A modified item of the same type</returns>
    public delegate TReturnType MapFunctionTypeChange<T, TReturnType>(T item);

    /// <summary>
    /// A function that reduces a collection of items into a single item
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <param name="accumulated">The accumulated value from the collection thus far</param>
    /// <param name="item">The current item</param>
    /// <returns>A single item</returns>
    public delegate T ReduceFunction<T>(T accumulated, T item);

    /// <summary>
    /// A function that reduces a collection of items into a single item
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <typeparam name="TReturnType">Any type</typeparam>
    /// <param name="accumulated">The accumulated value from the collection thus far</param>
    /// <param name="item">The current item</param>
    /// <returns>A single item</returns>
    public delegate TReturnType ReduceFunctionWithTypeChange<T, TReturnType>(TReturnType accumulated, T item);

    #endregion

    /// <summary>
    /// This class contains generic functional operations
    /// </summary>
    public static class FunctionalOperations
    {
        #region Public Static Methods

        /// <summary>
        /// Given a filter function and a collection of items, this function will return only the items
        /// in the collection that the filter function returned true for
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="filterFunction">A filter function</param>
        /// <param name="items">A collection of items</param>
        /// <returns>All items that pass the filter function</returns>
        public static IEnumerable<T> Filter<T>(FilterFunction<T> filterFunction, params T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return Filter<T>(filterFunction, new List<T>(items));
        }

        /// <summary>
        /// Given a filter function and a collection of items, this function will return only the items
        /// in the collection that the filter function returned true for
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="filterFunction">A filter function</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>All items that pass the filter function</returns>
        public static IEnumerable<T> Filter<T>(FilterFunction<T> filterFunction, IEnumerable<T> collection)
        {
            if (filterFunction == null)
            {
                throw new ArgumentNullException("filterFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                if (filterFunction(item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Applies a function to each item in the collection
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="mapFunction">The map function to apply across the collection</param>
        /// <param name="items">A collection of items</param>
        /// <returns>The mapped value of each item in the collection</returns>
        public static IEnumerable<T> Map<T>(MapFunction<T> mapFunction, params T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return Map<T>(mapFunction, new List<T>(items));
        }

        /// <summary>
        /// Applies a function to each item in the collection
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="mapFunction">The map function to apply across the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>The mapped value of each item in the collection</returns>
        public static IEnumerable<T> Map<T>(MapFunction<T> mapFunction, IEnumerable<T> collection)
        {
            if (mapFunction == null)
            {
                throw new ArgumentNullException("mapFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                yield return mapFunction(item);
            }
        }

        ///// <summary>
        ///// Applies a function to parallel items in two collections
        ///// </summary>
        ///// <typeparam name="TCollection1">Any type</typeparam>
        ///// <typeparam name="TCollection2">Any type</typeparam>
        ///// <typeparam name="TReturn">The resulting type</typeparam>
        ///// <param name="mapFunction">A function that takes an item from collection1 and an item from collection2</param>
        ///// <param name="collection1">A collection of items</param>
        ///// <param name="collection2">A collection of items</param>
        ///// <returns>The mapped value of each pair of items from the two collections</returns>
        //public static IEnumerable<TReturn> Map2<TCollection1, TCollection2, TReturn>(Func<TCollection1, TCollection2, TReturn> mapFunction, IEnumerable<TCollection1> collection1, IEnumerable<TCollection2> collection2)
        //{
        //    if (mapFunction == null)
        //    {
        //        throw new ArgumentNullException("mapFunction");
        //    }

        //    if (collection1 == null)
        //    {
        //        throw new ArgumentNullException("collection1");
        //    }

        //    if (collection2 == null)
        //    {
        //        throw new ArgumentNullException("collection2");
        //    }

        //    TCollection2[] collection2Array = collection2.ToArray();

        //    int counter = 0;
        //    foreach (TCollection1 item in collection1)
        //    {
        //        yield return mapFunction(item, collection2Array[counter]);
        //        counter++;
        //    }
        //}

        /// <summary>
        /// Reduces a collection of items into a single item
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="reduceFunction">A function to reduce the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>A single value</returns>
        public static T Reduce<T>(ReduceFunction<T> reduceFunction, IEnumerable<T> collection)
        {
            return Reduce(reduceFunction, collection, default(T));
        }

        /// <summary>
        /// Reduces a collection of items into a single item
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="reduceFunction">A function to reduce the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <param name="initializer">An initial value to be passed to the reduce function as the accumulated value</param>
        /// <returns>A single value</returns>
        public static T Reduce<T>(ReduceFunction<T> reduceFunction, IEnumerable<T> collection, T initializer)
        {
            if (reduceFunction == null)
            {
                throw new ArgumentNullException("reduceFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                initializer = reduceFunction(initializer, item);
            }

            return initializer;
        }

        /// <summary>
        /// Reduces a collection of items into a single item
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <typeparam name="TReturnType">Any type</typeparam>
        /// <param name="reduceFunction">A function to reduce the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>A single value</returns>
        public static TReturnType ReduceWithTypeChange<T, TReturnType>(ReduceFunctionWithTypeChange<T, TReturnType> reduceFunction, IEnumerable<T> collection)
        {
            return ReduceWithTypeChange(reduceFunction, collection, default(TReturnType));
        }

        /// <summary>
        /// Reduces a collection of items into a single item
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <typeparam name="TReturnType">Any type</typeparam>
        /// <param name="reduceFunction">A function to reduce the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <param name="initializer">An initial value to be passed to the reduce function as the accumulated value</param>
        /// <returns>A single value</returns>
        public static TReturnType ReduceWithTypeChange<T, TReturnType>(ReduceFunctionWithTypeChange<T, TReturnType> reduceFunction, IEnumerable<T> collection, TReturnType initializer)
        {
            if (reduceFunction == null)
            {
                throw new ArgumentNullException("reduceFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                initializer = reduceFunction(initializer, item);
            }

            return initializer;
        }

        /// <summary>
        /// Applies a function to each item in the collection
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <typeparam name="TReturnType">Any type</typeparam>
        /// <param name="mapFunction">The map function to apply across the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>The mapped value of each item in the collection</returns>
        public static IEnumerable<TReturnType> MapWithTypeChange<T, TReturnType>(MapFunctionTypeChange<T, TReturnType> mapFunction, IEnumerable<T> collection)
        {
            if (mapFunction == null)
            {
                throw new ArgumentNullException("mapFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                yield return mapFunction(item);
            }
        }

        /// <summary>
        /// Applies a collection of map functions to each item in the collection
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="mapFunctionCollection">A collection of map functions to apply to each item</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>The mapped value of each item in the collection</returns>
        public static IEnumerable<T> Multimap<T>(IEnumerable<MapFunction<T>> mapFunctionCollection, IEnumerable<T> collection)
        {
            if (mapFunctionCollection == null)
            {
                throw new ArgumentNullException("mapFunctionCollection");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                yield return ApplyMultiMapToItem<T>(mapFunctionCollection, item);
            }
        }

        /// <summary>
        /// Maps every item in the collection that passes the filter function.  Items that do not pass the filter function are not returned
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="filterFunction">A filter function</param>
        /// <param name="mapFunction">The map function to apply across the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>The mapped value of all items that passed the filter function</returns>
        public static IEnumerable<T> FilterThenMap<T>(FilterFunction<T> filterFunction, MapFunction<T> mapFunction, IEnumerable<T> collection)
        {
            if (filterFunction == null)
            {
                throw new ArgumentNullException("filterFunction");
            }

            if (mapFunction == null)
            {
                throw new ArgumentNullException("mapFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                if (filterFunction(item))
                {
                    yield return mapFunction(item);
                }
            }
        }

        /// <summary>
        /// Maps every item in the collection that passes the filter function.  Items that do not pass the filter function are not returned
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <typeparam name="TReturnType">The return type</typeparam>
        /// <param name="filterFunction">A filter function</param>
        /// <param name="mapFunction">The map function to apply across the collection</param>
        /// <param name="collection">A collection of items</param>
        /// <returns>The mapped value of all items that passed the filter function</returns>
        public static IEnumerable<TReturnType> FilterThenMapWithTypeChange<T, TReturnType>(FilterFunction<T> filterFunction, MapFunctionTypeChange<T, TReturnType> mapFunction, IEnumerable<T> collection)
        {
            if (filterFunction == null)
            {
                throw new ArgumentNullException("filterFunction");
            }

            if (mapFunction == null)
            {
                throw new ArgumentNullException("mapFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                if (filterFunction(item))
                {
                    yield return mapFunction(item);
                }
            }
        }

        /// <summary>
        /// Applies a function across a collection of items
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="iterateFunction">A function to apply to each item</param>
        /// <param name="collection">A collection of items</param>
        public static void Iterate<T>(IterateFunction<T> iterateFunction, IEnumerable<T> collection)
        {
            if (iterateFunction == null)
            {
                throw new ArgumentNullException("iterateFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                iterateFunction(item);
            }
        }

        /// <summary>
        /// Applies a mapping function across a collection and discards the results
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="mapFunction">A function to apply to each item</param>
        /// <param name="collection">A collection of items</param>
        public static void IterateWithMapFunction<T>(MapFunction<T> mapFunction, IEnumerable<T> collection)
        {
            if (mapFunction == null)
            {
                throw new ArgumentNullException("mapFunction");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            foreach (T item in collection)
            {
                mapFunction(item);
            }
        }

        #endregion

        #region Private Static Methods

        private static T ApplyMultiMapToItem<T>(IEnumerable<MapFunction<T>> mapFunctionCollection, T item)
        {
            if (ListOperations.Count(mapFunctionCollection) == 1)
            {
                return ListOperations.First(mapFunctionCollection)(item);
            }

            return ApplyMultiMapToItem<T>(ListOperations.SkipFirst(mapFunctionCollection), ListOperations.First(mapFunctionCollection)(item));
        }

        #endregion
    }
}
