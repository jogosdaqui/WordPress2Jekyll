using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordPress2Jekyll.ConsoleApp
{
    public static class TagMapper
    {
        private static readonly Dictionary<string, string> _knowTags = new Dictionary<string, string>();

        static TagMapper()
        {
            var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tags-map.txt"));

            foreach(var line in lines)
            {
                var parts = line.Split(" = ");
                _knowTags.Add(parts[0], parts.Length > 1 ? parts[1] : parts[0].Replace(' ', '-'));
            }
        }

        public static string GetTags(dynamic post)
        {
            var tags = new List<string>();
            var content = $"{post.Content.ToLowerInvariant()}\n{post.Title.ToLowerInvariant()}";

            foreach (var kt in _knowTags)
            {
                if (content.Contains(kt.Key))
                    tags.AddRange(kt.Value.Split(' '));
            }

            return String.Join(" ", tags.Distinct());
        }
    }
}
