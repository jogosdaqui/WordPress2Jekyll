using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WordPress2Jekyll.ConsoleApp
{
    public static class Replacer
    {
        private static readonly Dictionary<string, string> _textReplaces = new Dictionary<string, string>();
        private static readonly Dictionary<Regex, string>_regexReplaces = new Dictionary<Regex, string>();

        static Replacer()
        {
            _regexReplaces.LoadFile("regex-replaces.txt", (k, v) => (new Regex(k, RegexOptions.Compiled | RegexOptions.IgnoreCase), v));
            _textReplaces.LoadFile("text-replaces.txt", (k, v) => (k, v));
        }

        public static string Replace(string content)
        {
            foreach (var r in _regexReplaces)
            {
                content = r.Key.Replace(content, r.Value);
            }

            foreach (var r in _textReplaces)
            {
                content = content.Replace(r.Key, r.Value);
            }

            return content;
        }

        private static void LoadFile<TKey>(this Dictionary<TKey, string> replaces, string fileName, Func<string, string, (TKey, string)> handleLine)
        {
            var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName));

            for (int i = 0; i < lines.Length; i++)
            {
                var key = lines[i];
                var value = String.Empty;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    i++;

                    if (lines[j] == String.Empty)
                        break;

                    if (!String.IsNullOrEmpty(value))
                        value += "\n";

                    value += lines[j];
                }

                var handledLine = handleLine(key, value);
                replaces.Add(handledLine.Item1, handledLine.Item2);
            }
        }
    }
}
