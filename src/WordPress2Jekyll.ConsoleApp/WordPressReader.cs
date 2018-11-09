using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using MySql.Data.MySqlClient;

namespace WordPress2Jekyll.ConsoleApp
{
    public sealed class WordPressReader : IDisposable
    {
        public static readonly Regex ImageNamesFromPostContentRegex = new Regex("((<a.+)*<img.+src=\"\\S+/(?<image>\\S+)\".+(</a>)*|image=\"\\S+/(?<image>\\S+)\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex CaptionsFromPostContentRegex = new Regex(@"\[caption id=.attachment_(?<id>\d+).+\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex GalleryIdFromPostContentRegex = new Regex("\\[tribulant_slideshow gallery_id=\"(\\d+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly MySqlConnection _conn;
        private readonly string _postName;

        public WordPressReader(string postName = null)
        {
            _postName = postName;
            _conn = new MySqlConnection("Server=127.0.0.1;Database=jogosdaqui;Uid=root;Pwd=adm123!@#");
        }

        public static string NormalizePostName(string postName)
        {
            return postName.ToLowerInvariant().Replace("-", "");
        }

        public IEnumerable<dynamic> GetPosts()
        {
            return _conn.Query(@"
                SELECT 
                    ID AS Id, 
                    post_title AS Title, 
                    REPLACE(REPLACE(post_name, 'a%c2%a7a', 'ca'), 'i%c2%ad', 'i') AS Name, 
                    post_date As Date, 
                    post_content AS Content
                FROM wp_posts 
                WHERE 
                    post_status = 'publish' 
                    AND post_title <> '' 
                    AND post_type = 'post'
                    AND (@postName IS NULL OR post_name = @postName)
                ORDER BY post_date 
                -- limit 10 offset 0", new { postName = _postName });
        }

        public IEnumerable<dynamic> GetPostImages(dynamic post)
        {
            // Da tabela wp_postmeta.
            var results = _conn.Query(@"
                SELECT meta_value AS Path
                FROM wp_postmeta 
                WHERE meta_key = '_wp_attached_file' AND post_id = @Id", new { post.Id }).ToList();


            // Da tabela wp_ewwwio_images.
            var postNameNormalized = NormalizePostName(post.Name);
            results.AddRange(_conn.Query(@"
                SELECT REPLACE(path, '/var/www/wp-content/uploads/', '') as Path 
                FROM wp_ewwwio_images 
                WHERE (path LIKE @path OR path LIKE @normalizedPath) AND path NOT LIKE '%x%'",new 
            { 
                path = $"%{post.Date:yyyy/MM}/{post.Name}%",
                normalizedPath = $"%{post.Date:yyyy/MM}/{postNameNormalized}%"
            }));

            // Da tabela wp_gallery_galleries.
            var galleryMatch = GalleryIdFromPostContentRegex.Match(post.Content);

            if (galleryMatch.Success)
            {
                results.AddRange(_conn.Query(@"
                SELECT REPLACE(image_url, 'http://jogosdaqui.com.br/wp-content/uploads/', '') as Path
                FROM wp_gallery_galleries g
                    INNER JOIN wp_gallery_galleriesslides gs ON g.Id = gs.gallery_id
                    INNER JOIN wp_gallery_slides s ON gs.slide_id = s.id
                WHERE g.Id = @galleryId
                ", new
                {
                    galleryId = galleryMatch.Groups[1].Value
                }));
            }

            // Garante que a regex vão obter todas as imagens nos posts que estão salvos inteiramente numa única linha.
            var normalizedContent = post.Content.Replace("</p>", $"</p>{Environment.NewLine}");

            // Do parse do conteúdo do post.
            var matches = ImageNamesFromPostContentRegex.Matches(normalizedContent);

            foreach(Match m in matches)
            {
                results.Add(new { Path = $"{post.Date:yyyy/MM}/{m.Groups["image"].Value}" });
            }

            // Os captions que existem no conteúdo.
            matches = CaptionsFromPostContentRegex.Matches(normalizedContent);

            foreach (Match m in matches)
            {
                var attachment = _conn.QueryFirst(@"
                SELECT guid
                FROM wp_posts 
                WHERE ID = @id", new { id = m.Groups["id"].Value });

                results.Add(new { Path = $"{post.Date:yyyy/MM}/{Path.GetFileName(attachment.guid)}" });
            }


            return results
                .Distinct()
                .OrderBy(i => !i.Path.Equals(post.Name, StringComparison.OrdinalIgnoreCase))
                .ThenBy(i => !i.Path.Contains("logo"))
                .ThenBy(r => r.Path);
        }

        public void Dispose()
        {
            if (_conn != null)
                _conn.Dispose();
        }
    }
}
