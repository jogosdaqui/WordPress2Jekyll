﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                if (Regex.IsMatch(content, $"[^a-z0-9]{Regex.Escape(kt.Key)}[^a-z0-9]", RegexOptions.IgnoreCase))
                    tags.AddRange(kt.Value.Split(' '));
            }

            if (!String.IsNullOrEmpty(post.Developer))
                tags.Add(StringHelper.Slugify(post.Developer));

            return String.Join(" ", tags.Distinct());
        }
    }
}
