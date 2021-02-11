using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationEditor.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> Chunk(this string str, int chunkSize)
        {
            for (var i = 0; i < str.Length; i += chunkSize)
            {
                yield return str.Substring(i, Math.Min(chunkSize, str.Length - i));
            }
        }
    }
}
