using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib.General
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> inputs, T extra)
        {
            foreach (var i in inputs)
                yield return i;
            yield return extra;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> inputs, T extra)
        {
            yield return extra;
            foreach (var i in inputs)
                yield return i;
        }

        public static string AggregateWithComma(this IEnumerable<string> elements)
        {
            return elements.AggregateWith(", ");
        }

        public static string AggregateWithSpace(this IEnumerable<string> elements)
        {
            return elements.AggregateWith(" ");
        }

        public static string AggregateWithUnderscore(this IEnumerable<string> elements)
        {
            return elements.AggregateWith("_");
        }

        public static string AggregateWithNewLine(this IEnumerable<string> elements)
        {
            return elements.AggregateWith(System.Environment.NewLine);
        }

        public static string AggregateWith(this IEnumerable<string> elements, string separator)
        {
            if (elements.Any())
                return elements.Aggregate((buff, s) => buff + separator + s);

            return string.Empty;
        }
    }
}
