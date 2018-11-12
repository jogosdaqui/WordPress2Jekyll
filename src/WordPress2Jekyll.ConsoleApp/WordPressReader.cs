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
        public static readonly Regex SlideShowIdFromPostContentRegex = new Regex(@"#SLIDESHOW#(?<id>\d+)#/SLIDESHOW#", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly string PhpSiteImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../..", "setup/images/php-site/image");
        public static readonly string PhpSiteSlidesShowFolder = Path.Combine(PhpSiteImagesFolder, "slidesshow");
        public static readonly string PhpSiteLogosFolder = Path.Combine(PhpSiteImagesFolder, "logos");

        private readonly MySqlConnection _conn;
        private readonly string _postName;
        private readonly int _maxPosts;

        public WordPressReader(string postName = null, int maxPosts = int.MaxValue)
        {
            _postName = postName;
            _maxPosts = maxPosts;
            _conn = new MySqlConnection("Server=127.0.0.1;Database=jogosdaqui;Uid=root;Pwd=adm123!@#");
        }

        public static string PreparePostContentForImagesSearch(string postContent)
        {
            return postContent
                .Replace("</p>", $"</p>{Environment.NewLine}")
                .Replace("</a>", $"</a>{Environment.NewLine}")
                .Replace("</div>", $"</div>{Environment.NewLine}");
        }

        public IEnumerable<dynamic> GetPosts()
        {
            var posts = new List<dynamic>();

            posts.AddRange(_conn.Query(@"
            SELECT 
                ID AS Id, 
                post_title AS Title, 
                REPLACE(REPLACE(post_name, 'a%c2%a7a', 'ca'), 'i%c2%ad', 'i') AS Name, 
                post_date As Date, 
                '' AS Developer,
                post_content AS Content,
                cast(cntIdTypeLabel as char(2)) AS Type,
                'WordPress' as SourceSite
            FROM wp_posts p
                LEFT JOIN jdcontent c ON BINARY lower(p.post_title) = BINARY lower(c.cntNmContent)
            WHERE 
                post_status = 'publish' 
                AND post_title <> '' 
                AND post_type = 'post'
                AND (@postName IS NULL OR post_name = @postName)
            ORDER BY post_date DESC
            limit @maxPosts offset 0", new { postName = _postName, maxPosts = _maxPosts }));

            var remainingMaxPosts = _maxPosts - posts.Count;

            if (remainingMaxPosts > 0)
            {
                posts.AddRange(_conn.Query(@"
                SELECT 
                    cntIdContent AS Id,
                    cntNmContent AS Title,
                    '' as Name,
                    cntDtCreate as Date,
                    cmpNmCompany Developer,
                    cpgTxPage as Content,
                    cast(cntIdTypeLabel as char(2)) AS Type,
                    'PHP' as SourceSite
                FROM jdcontent 
                    INNER JOIN jdcontentpage ON cntIdContent = cpgIdContent
                    LEFT JOIN jdtechniquefiche ON cntIdContent = tcfIdGame
                    LEFT JOIN jdtechniquefichedeveloper ON tdvIdTechniqueFiche = tcfIdTechniqueFiche
                    LEFT JOIN jdcompany ON cmpIdCompany = tdvIdDeveloper
                WHERE 
                    cntidcontent > 242 
                ORDER BY
                    cntDtCreate DESC
                limit @maxPosts offset 0", new { postName = _postName, maxPosts = remainingMaxPosts }));
            }

            posts = posts.Take(_maxPosts).ToList();

            foreach (var p in posts)
            {
                if (String.IsNullOrEmpty(p.Name))
                    p.Name = StringHelper.Slugify(p.Title);

                 p.Type = PostHelper.GetPostType(p);
            }

            return posts
                .Where(p => _postName == null || p.Name.Equals(_postName))
                .ToList();
        }

        public IEnumerable<dynamic> GetPostImages(dynamic post)
        {
            var results = new List<dynamic>();

            if (post.SourceSite.Equals("WordPress"))
            {
                // Da tabela wp_postmeta.
                results.AddRange(_conn.Query(@"
                SELECT meta_value AS Path
                FROM wp_postmeta 
                WHERE meta_key = '_wp_attached_file' AND post_id = @Id", new { post.Id }));


                // Da tabela wp_ewwwio_images.
                var postNameNormalized = StringHelper.NormalizePostName(post.Name);
                results.AddRange(_conn.Query(@"
                SELECT REPLACE(path, '/var/www/wp-content/uploads/', '') as Path 
                FROM wp_ewwwio_images 
                WHERE (path LIKE @path OR path LIKE @normalizedPath) AND path NOT LIKE '%x%'", new
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
            }
            else if (post.Id > 242)
            {
                // Da tabela jdSlideShow.
                var slideShowResult = _conn.QueryFirstOrDefault(@"
                SELECT
                    slsIdSlideShow AS 'SlideShowId'
                FROM 
                    jdslideshow
                WHERE slsIdcontent = @idContent
                ", new { idContent = post.Id });
               
                if (slideShowResult != null)
                {
                    var folder = Path.Combine(PhpSiteSlidesShowFolder, slideShowResult.SlideShowId.ToString());

                    if (Directory.Exists(folder))
                    {
                        var folderImages = Directory.GetFiles(folder);

                        foreach (var fi in folderImages)
                        {
                            results.Add(new { Path = fi.Replace($"{PhpSiteSlidesShowFolder}/", String.Empty) });
                        }
                    }
                }

                // Pasta de logos.
                var logoFolder = Path.Combine(PhpSiteImagesFolder, "logos");
                var logoFiles = Directory.GetFiles(logoFolder, $"{StringHelper.NormalizePostName(post.Name)}.*");

                if (logoFiles.Length > 0)
                {
                    results.Add(new { Path = logoFiles[0].Replace($"{PhpSiteLogosFolder}/", String.Empty) });
                }
             }

            // Garante que a regex vão obter todas as imagens nos posts que estão salvos inteiramente numa única linha.
            var normalizedContent = PreparePostContentForImagesSearch(post.Content);

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
