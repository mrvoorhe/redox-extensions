using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib.General
{
    public static class ExtensionMethods
    {
        public static string SeparateWithSpaces(this IEnumerable<string> inputs)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var input in inputs)
            {
                if (!first)
                    sb.Append(" ");
                first = false;
                sb.Append(input);
            }
            return sb.ToString();
        }

        public static IEnumerable<string> InQuotes(this IEnumerable<string> inputs)
        {
            return inputs.Select(input => input.InQuotes());
        }

        public static string InQuotes(this string input)
        {
            return "\"" + input + "\"";
        }
    }
}
