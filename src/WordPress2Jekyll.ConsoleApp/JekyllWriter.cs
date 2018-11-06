using System;
using System.IO;
using System.Text.RegularExpressions;

namespace WordPress2Jekyll.ConsoleApp
{
    public class JekyllWriter
    {
        private readonly string _imagesSourceRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../..", "setup/images/galleries");

        //private readonly string _imagesOutputRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "images", "galleries");
        private readonly string _imagesOutputRootFolder = "/Users/giacomelli/Projects/jogosdaqui.github.io-jekyll/images/galleries";

        //private readonly string _postsOutputRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "posts");
        private readonly string _postsOutputRootFolder = Path.Combine("/Users/giacomelli/Projects/jogosdaqui.github.io-jekyll/_posts");

        private readonly WordPressReader _reader;

        public JekyllWriter(WordPressReader reader)
        {
            _reader = reader;
            Directory.CreateDirectory(_postsOutputRootFolder);
        }

        public void WritePost(dynamic post)
        {
            var fileContent =
$@"---
published: true
layout: post
title: {post.Title}
---
{ConvertPostContent(post.Content)}";

            var fileName = Path.Combine(_postsOutputRootFolder, $"{post.Date:yyyy-MM-dd}-{post.Name}.md");

            File.WriteAllText(fileName, fileContent);

            WritePostImages(post);
        }

        private void WritePostImages(dynamic post)
        {
            var images = _reader.GetPostImages(post);
            var galleryOutputPath = Path.Combine(_imagesOutputRootFolder, post.Date.ToString("yyyy/MM/dd"), post.Name);
            Directory.CreateDirectory(galleryOutputPath);

            foreach (var img in images)
            {
                var filename = Path.GetFileName(img.Path);
                var sourcePath = Path.Combine(_imagesSourceRootFolder, img.Path);
                var outputPath = Path.Combine(galleryOutputPath, filename);

                File.Copy(sourcePath, outputPath, true);
            }
        }

        private string ConvertPostContent(string content)
        {
            content = WordPressReader.ImageNamesFromPostContentRegex.Replace(content, "{% screenshot $1 %}");
        
            return content
                .Replace("<div style=\"text-align: center;\">", String.Empty)
                .Replace("<div style=\"text-align: justify;\">", String.Empty)
                .Replace("</div>", String.Empty)
                .Replace("<p style=\"text-align: justify;\">", String.Empty)
                .Replace("</p>", String.Empty)
                .Replace("http://www.jogosdaqui.com.br/index.php?p=r", "https://jogosdaqui.github.io")
                .Replace("http://www.jogosdaqui.com.br", "https://jogosdaqui.github.io");
        }
    }
}