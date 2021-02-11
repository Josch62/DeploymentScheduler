using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SchedulerCommon.Sql
{
    /// <summary>
    /// Class provided by Marcus Melberg of Melberg Consulting, thanks!
    /// </summary>
    public static class SqlUtility
    {
        public const int DefaultDelayToDistinguishSqlDateInSeconds = 1;

        public static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            if (string.IsNullOrEmpty(sqlScript))
                return Enumerable.Empty<string>();

            // Split by "GO" statements
            var statements = Regex.Split(
                sqlScript, @"^\s*GO\s* ($ | \-\- .*$)",
                RegexOptions.Multiline |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }
    }
}
