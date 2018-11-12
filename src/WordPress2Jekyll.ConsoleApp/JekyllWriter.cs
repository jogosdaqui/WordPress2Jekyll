using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace WordPress2Jekyll.ConsoleApp
{
    public class JekyllWriter
    {
        private readonly string _imagesSourceRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../..", "setup/images/galleries");
        private readonly string _imagesOutputRootFolder = "/Users/giacomelli/Projects/jogosdaqui.github.io-jekyll/images/galleries";
        private readonly string _postsOutputRootFolder = Path.Combine("/Users/giacomelli/Projects/jogosdaqui.github.io-jekyll/_posts");
        private static readonly Regex _wordPressTagsRegex = new Regex(@"\[tribulant_slideshow.+\]", RegexOptions.Compiled);
        private static readonly Regex _youtubeTagRegex = new Regex(@"(\[sc:.*Youtube videoid=.(?<id>[a-z0-9]+).+\]|httpv://youtu.be/(?<id>[a-z0-9]+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _quoteTagRegex = new Regex(@"\[sc.*Quote.+Text=.(?<text>.+). author=.(?<author>.+).\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _eventTagRegex = new Regex(@"\[sc:.*EventInfo.+name=.(?<name>.+). when=.(?<when>.+). where=.(?<where>.+). who=.(?<who>.+). howmuch=.(?<howmuch>.+). moreinfo=.(?<moreinfo>.+)"".*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly WordPressReader _reader;
        private readonly bool _writeSourceContent;

        public JekyllWriter(WordPressReader reader, bool writeSourceContent = false)
        {
            _reader = reader;
            _writeSourceContent = writeSourceContent;
            Directory.CreateDirectory(_postsOutputRootFolder);
        }

        public void WritePost(dynamic post)
        {
            var fileContent =
$@"---
published: true
layout: post
title: '{ConvertPostTitle(post.Title)}'
categories: {post.Type}
tags: {TagMapper.GetTags(post)}
---
{ConvertPostContent(post.Content)}";

            var fileName = Path.Combine(_postsOutputRootFolder, $"{post.Date:yyyy-MM-dd}-{post.Name}.md");

            File.WriteAllText(fileName, fileContent);

            if (_writeSourceContent)
                File.WriteAllText($"{fileName}.source.txt", post.Content);

            WritePostImages(post);
        }

        private void WritePostImages(dynamic post)
        {
            IEnumerable<dynamic> images = _reader.GetPostImages(post);
            var galleryOutputPath = Path.Combine(_imagesOutputRootFolder, post.Date.ToString("yyyy/MM/dd"), post.Name);
            Directory.CreateDirectory(galleryOutputPath);

            var logoFound = false;

            foreach (var img in images)
            {
                var path = img.Path;
                var filename = Path.GetFileName(path);
                var filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                // Se o arquivo de imagem tem o mesmo nome do post ou tem logo no nome, 
                // então faz o arquivo ter o nome 'logo' no destino.
                if (!logoFound &&
                   (filenameWithoutExtension.Equals(post.Name, StringComparison.OrdinalIgnoreCase)
                   || filenameWithoutExtension.Equals(WordPressReader.NormalizePostName(post.Name), StringComparison.OrdinalIgnoreCase)
                   || filenameWithoutExtension.Contains("logo")
                   || images.Count() == 1))
                {
                    logoFound = true;
                    filename = $"logo{Path.GetExtension(path)}";
                }

                var sourcePath = Path.Combine(_imagesSourceRootFolder, path);
                var outputPath = Path.Combine(galleryOutputPath, filename);

                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, outputPath, true);
            }


        }

        private string ConvertPostTitle(string title)
        {
            return title
                .Replace("'", "&#39;")
                .Replace("Apoie o jogo brasileiro Cowboy vs Aliens vs Ninjas", "Apoie o jogo brasileiro Comboy vs Aliens vs Ninjas"); // Tem um char especial no nome original q não consegui identificar.
        }


        private string ConvertPostContent(string content)
        {
            // Garante que posts que estão em apenas uma linha sejam corretamente interpretados
            // pelos replaces abaixo.
            content = WordPressReader.PreparePostContentForImagesSearch(content);
            content = WordPressReader.ImageNamesFromPostContentRegex.Replace(content, String.Empty);

            return Replacer.Replace(content);
        }
    }
}