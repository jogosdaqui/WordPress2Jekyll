using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using MySql.Data.MySqlClient;

namespace WordPress2Jekyll.ConsoleApp
{
    public sealed class WordPressReader : IDisposable
    {
        private readonly MySqlConnection _conn;
        public static readonly Regex ImageNamesFromPostContentRegex = new Regex("<a.+<img src=\"\\S+/(\\S+)\".+</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public WordPressReader()
        {
            _conn = new MySqlConnection("Server=127.0.0.1;Database=jogosdaqui;Uid=root;Pwd=adm123!@#");
        }

        public IEnumerable<dynamic> GetPosts()
        {
            return _conn.Query(@"
                SELECT ID AS Id, post_title AS Title, post_name AS Name, post_date As Date, post_content AS Content
                FROM wp_posts 
                WHERE post_status = 'publish' AND post_title <> '' 
                ORDER BY post_date limit 5 offset 0");
        }

        public IEnumerable<dynamic> GetPostImages(dynamic post)
        {
            // Da tabela wp_postmeta.
            var postMetaResults = _conn.Query(@"
                SELECT meta_value AS Path
                FROM wp_postmeta 
                WHERE meta_key = '_wp_attached_file' AND post_id = @Id", new { post.Id }).ToList();

            // Da tabela wp_ewwwio_images.
            var envioImagesResults = _conn.Query(@"
                SELECT REPLACE(path, '/var/www/wp-content/uploads/', '') as Path 
                FROM wp_ewwwio_images 
                WHERE path LIKE @path AND path NOT LIKE '%x%'", new { path = $"%{post.Date:yyyy/MM}/{post.Name}%" });
        
            postMetaResults.AddRange(envioImagesResults);

            // Do parse do conteúdo do post.
            var matches = ImageNamesFromPostContentRegex.Matches(post.Content);

            foreach(Match m in matches)
            {
                postMetaResults.Add(new { Path = $"{post.Date:yyyy/MM}/{m.Groups[1].Value}" });
            }


            return postMetaResults;
        }

        public void Dispose()
        {
            if (_conn != null)
                _conn.Dispose();
        }
    }
}
