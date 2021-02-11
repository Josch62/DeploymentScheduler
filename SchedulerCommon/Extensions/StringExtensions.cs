using System;
using System.Text;

namespace SchedulerCommon.Extensions
{
    public static class StringExtensions
    {
        private const int MaxLogSize = 30000; //Actually, string max size is 32000 but usually we log small amount of metadata which we want to make room for.

        /// <summary>
        /// Taken from: http://stackoverflow.com/questions/244531/is-there-an-alternative-to-string-replace-that-is-case-insensitive
        /// </summary>
        /// <param name="original">The original value</param>
        /// <param name="oldValue">The old value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="comparison">The type of comparison</param>
        /// <returns>The resulting rext</returns>
        public static string ReplaceFirst(this string original, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = original.IndexOf(oldValue, comparison);
            if (index != -1)
            {
                sb.Append(original.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
            }

            sb.Append(original.Substring(previousIndex));

            return sb.ToString();
        }

        /// <summary>
        /// Taken from: http://stackoverflow.com/questions/444798/case-insensitive-containsstring
        /// </summary>
        /// <param name="source">The source text</param>
        /// <param name="value">The value to check for containment in source</param>
        /// <param name="comparison">The comparison method</param>
        /// <returns>True if the provided text is contained in the source text</returns>
        public static bool Contains(this string source, string value, StringComparison comparison)
        {
            return source.IndexOf(value, comparison) >= 0;
        }

        public static string ToNullStringIfNull(this string value)
        {
            return value ?? "<null>";
        }

        public static string TruncateStringForLogging(this string original, int numberOfArgumentsToSplit = 1)
        {
            var maxSize = MaxLogSize / numberOfArgumentsToSplit;
            if (string.IsNullOrEmpty(original) || original.Length <= maxSize)
                return original;
            return original.Substring(0, maxSize) + "...";
        }
    }
}
