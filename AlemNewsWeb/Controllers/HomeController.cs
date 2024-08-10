using COMMON;
using DBHelper;
using AlemNewsWeb.Caches;
using Dapper;
using MODEL;
using MODEL.FormatModels;
using System.Text;
using AlemNewsWeb.Attributes;

namespace AlemNewsWeb.Controllers;

[NoRole]
public class HomeController : QarBaseController
{
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Index +Index()

    public IActionResult Index()
    {
        
        ViewData["pinnedArticle"] = QarCache.GetPinnedArticleList(_memoryCache, CurrentLanguage, 3).ToList();
        ViewData["latestArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 25);
        ViewData["focusArticleList"] = QarCache.GetFocusArticleList(_memoryCache, CurrentLanguage, 12);
        ViewData["proverbList"] = QarCache.GetProverbList(_memoryCache);
        var categoryList = QarCache.GetCategoryList(_memoryCache, CurrentLanguage);
        foreach (var category in categoryList.Where(x => x.ParentId == 0))
        {
            ViewData[$"{category.BlockType}ParentTitle"] = category.Title;
            ViewData[$"{category.BlockType}AllUrl"] = $"/{CurrentLanguage}/category/{category.LatynUrl}.html";
            ViewData[$"{category.BlockType}ArticleAllList"] = QarCache.GetArticleAllList(_memoryCache, CurrentLanguage, category.Id);
        }
        foreach (var category in categoryList.Where(x => !string.IsNullOrEmpty(x.BlockType)).ToList())
        {
            var takeCount = 4;
            switch (category.BlockType)
            {
                case "block1":
                    {
                        takeCount = 5;
                    }
                    break;
                case "block2":
                    {
                        takeCount = 4;
                    }
                    break;
            }
            ViewData[$"{category.BlockType}Title"] = category.Title;
            ViewData[$"{category.BlockType}Url"] = $"/{CurrentLanguage}/category/{category.LatynUrl}.html";
            ViewData[$"{category.BlockType}ArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, takeCount, category.Id);
            ViewData["block6title"] = QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
                .Where(x => x.BlockType == "block6" && x.ParentId != 0).ToList();
        }
        if (Request.Cookies.TryGetValue("question", out string questionIdStr))
        {
            ViewData["questionId"] = questionIdStr;
        }

        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Категория +Category(string query)

    public IActionResult Category(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Redirect($"/404.html");
        latynUrl = latynUrl[..^5];
        var category = QarCache.GetCategoryList(_memoryCache, CurrentLanguage).FirstOrDefault(x => x.LatynUrl.Equals(latynUrl, StringComparison.OrdinalIgnoreCase));
        ViewData["latynUrl"] = latynUrl;
        return category == null ? Redirect($"/404.html") : Article($"category-{category.Id}");
    }

    #endregion

    #region Tag +Tag(string query)

    public IActionResult Tag(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Redirect($"/404.html");
        latynUrl = latynUrl[..^5];
        return Article($"tag-{latynUrl}");
    }

    #endregion

    #region Author +Author(string query)

    public IActionResult Author(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Redirect($"/404.html");
        latynUrl = latynUrl[..^5];
        return string.IsNullOrWhiteSpace(latynUrl) ? Redirect($"/404.html") : Article($"author-{latynUrl}");
    }

    #endregion

    #region Мақала +Article(string query)

    public IActionResult Article(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        // var categoryId = GetIntQueryParam("cId", 0);
        // var tagId = GetIntQueryParam("tagId", 0);
        int categoryId = 0;
        string author = string.Empty, tagUrl = string.Empty;

        if (query.StartsWith("category-") && int.TryParse(query.Split('-')[1], out categoryId))
        {
            query = "list";
        }

        if (query.Length > 7 && query.StartsWith("author-") && !string.IsNullOrWhiteSpace(query[7..]))
        {
            author = query[7..];
            query = "list";
        }

        if (query.Length > 4 && query.StartsWith("tag-") && !string.IsNullOrWhiteSpace(query[4..]))
        {
            tagUrl = query[4..];
            query = "list";
        }

        ViewData["query"] = query;

        using var connection = Utilities.GetOpenConnection();
        switch (query)
        {
            case "list":
                {
                    var page = GetIntQueryParam("page", 1);
                    var pageSize = GetIntQueryParam("pageSize", 12);
                    var keyword = GetStringQueryParam("keyword");
                    page = page <= 1 ? 1 : page;
                    pageSize = pageSize <= 0 ? 12 : pageSize;
                    var querySql = " where qStatus = 0 ";
                    object queryObj = new { categoryId, keyword, author }; // tagId
                    var subSelectSql = string.Empty;
                    var subOrderBySql = string.Empty;
                    bool isEmpty = false;

                    var ogTitle = $"{T("ls_Search")}: {keyword}";

                    if (categoryId > 0)
                    {
                        var subCategoryList = connection.GetList<Articlecategory>("where qStatus = 0 and parentId = @categoryId", new { categoryId }).ToList();
                        if (subCategoryList.Count > 0)
                        {
                            ViewData["subCategoryList"] = subCategoryList;
                            var categoryIdArr = subCategoryList.Select(x => x.Id).Append(categoryId).ToArray();
                            querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";
                        }
                        else
                        {
                            querySql += " and categoryId = @categoryId ";
                        }
                    }

                    // if (tagId > 0)
                    // {
                    //     querySql += $" and id in (select itemId from {nameof(Tagmap).ToLower()} where tableName = '{nameof(MODEL.Article)}' and tagId = @tagId) ";
                    // }

                    if (!string.IsNullOrWhiteSpace(author))
                    {
                        querySql += " and author = @author ";
                    }

                    if (!string.IsNullOrWhiteSpace(tagUrl))
                    {
                        var tag = connection.GetList<Tag>("where latynUrl = @tagUrl", new { tagUrl }).FirstOrDefault();

                        if (tag == null)
                        {
                            isEmpty = true;
                        }
                        else
                        {
                            ogTitle = $"{T("ls_Tags")}:" + tag.Title;
                            ViewData["tagId"] = tag.Id;

                            var articleIds = connection.Query<int>($"select itemId from {nameof(Tagmap).ToLower()} where qStatus = 0 and tagId = @tagId", new { tagId = tag.Id }).ToArray();

                            if (articleIds.Length > 0)
                            {
                                querySql += $" and id in ({string.Join(',', articleIds)}) ";
                            }
                        }

                    }

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        querySql += " and match(title, fullDescription) against(@keyword in natural language mode) ";
                        // querySql += " and MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE) ";
                        // querySql += " and match(title, fullDescription) against(@keyword IN NATURAL LANGUAGE MODE) ";
                        // subSelectSql = ", (MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) * 2 AS title_relevance, (MATCH(fullDescription) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) AS description_relevance ";
                        // subOrderBySql = " (MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) DESC, ";
                    }

                    var total = isEmpty ? 0 : connection.RecordCount<Article>(querySql, queryObj);
                    var articleList = isEmpty ? new List<Article>() : connection.Query<Article>(
                        $"select id, title,categoryId, author, shortDescription, thumbnailUrl, latynUrl, addTime, viewCount {subSelectSql} from article {querySql} order by {subOrderBySql} addTime desc limit {(page - 1) * pageSize}, {pageSize} ",
                        queryObj).Select(x => new Article()
                        {
                            Id = x.Id,
                            Title = x.Title,
                            Author = x.Author,
                            ShortDescription = x.ShortDescription,
                            CategoryId = x.CategoryId,
                            ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? NoImage : x.ThumbnailUrl,
                            LatynUrl = x.LatynUrl,
                            AddTime = x.AddTime,
                            ViewCount = x.ViewCount,
                        }).ToList();

                    ViewData["page"] = page;
                    ViewData["pageSize"] = pageSize;
                    ViewData["total"] = total;
                    ViewData["totalPage"] = total % pageSize == 0 ? total / pageSize : total / pageSize + 1;
                    ViewData["articleList"] = articleList;
                    if (categoryId > 0)
                    {
                        ogTitle = QarCache.GetCategoryList(_memoryCache, CurrentLanguage).FirstOrDefault(x => x.Id == categoryId)?.Title;
                        ViewData["categoryId"] = categoryId;
                    }

                    // if (tagId > 0)
                    // {
                    //     og_title = $"{T("ls_Tags")}:" + _connection
                    //         .Query<string>($"select title from {nameof(Tag).ToLower()} where id = @tagId", new { tagId })
                    //         .FirstOrDefault();
                    //     ViewData["tagId"] = tagId;
                    // }

                    if (!string.IsNullOrWhiteSpace(author))
                    {
                        ogTitle = $"{T("ls_Author")}: {author}";
                    }

                    ViewData["og_title"] = ogTitle;
                    ViewData["menuTitle"] = ogTitle;
                    ViewData["keyword"] = keyword;
                    // ViewData["topArticleList"] = QarCache.GetTopArticleList(_memoryCache, CurrentLanguage, 30, 25);
                    return View($"~/Views/Themes/{CurrentTheme}/Home/ArticleList.cshtml");
                }

            default:
                {
                    var latynUrl = (query ?? string.Empty).Trim().ToLower();
                    if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Redirect($"/404.html");
                    latynUrl = latynUrl[..^5];
                    var article = connection.GetList<Article>("where qStatus in (0,2,3) and latynUrl = @latynUrl", new { latynUrl }).FirstOrDefault();
                    if (article == null)
                    {
                        var newLatynUrl = connection
                            .Query<string>(
                                $"select latynUrl from article where qStatus in (0,2,3) and id in (select itemId from {nameof(Latynurl).ToLower()} where qStatus = 0 and tableName = 'article' and latynUrl = @latynUrl)", new { latynUrl }).FirstOrDefault() ?? string.Empty;

                        if (!string.IsNullOrEmpty(newLatynUrl))
                        {
                            return RedirectPermanent($"/{CurrentLanguage}/article/{newLatynUrl}.html");
                        }

                        return Redirect($"/404.html");
                    }

                    // var rnd = new Random();
                    // rnd.Next(1, 6);
                    var randomNumber = 1;
                    article.ViewCount += randomNumber;
                    connection.Update(article);

                    var lastArticle = connection.Query<Article>("select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 and id < @articleId order by id desc limit 1", new { articleId = article.Id }).FirstOrDefault();
                    var nextArticle = connection.Query<Article>("select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 and id > @articleId order by id asc limit 1", new { articleId = article.Id }).FirstOrDefault();

                    // var excludeNextLastSql = "and id <> @lastArticleId and id <> @nextArticleId ";
                    // object queryObj = new
                    // { id = article.Id, lastArticleId = lastArticle?.Id ?? 0, nextArticleId = nextArticle?.Id ?? 0 };

                    // var recArticleList = new List<Article>();
                    // if (!string.IsNullOrEmpty(article.RecArticleIds))
                    // {
                    //     recArticleList = _connection.Query<Article>(
                    //         $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} and id in ({article.RecArticleIds})",
                    //         queryObj).ToList();
                    // }

                    // if (recArticleList.Count < 1)
                    // {
                    //     var rLastArticle = _connection
                    //         .Query<Article>(
                    //             $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} and id < @id order by id desc limit 1",
                    //             queryObj).FirstOrDefault();
                    //     if (rLastArticle != null)
                    //     {
                    //         recArticleList.Add(rLastArticle);
                    //     }
                    // }

                    // if (recArticleList.Count < 2)
                    // {
                    //     var rNextArticle = _connection.Query<Article>(
                    //         $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} {(recArticleList.Count > 0 ? $" and id not in ({string.Join(",", recArticleList.Select(x => x.Id).ToArray())}) " : "")} and id > @id order by id asc limit 1",
                    //         queryObj).FirstOrDefault();
                    //     if (rNextArticle == null)
                    //     {
                    //         rNextArticle = _connection.Query<Article>(
                    //             $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} {(recArticleList.Count > 0 ? $"and id not in ({string.Join(",", recArticleList.Select(x => x.Id).ToArray())})" : "")}  and id < @id order by id desc limit 1",
                    //             queryObj).FirstOrDefault();
                    //     }

                    //     if (rNextArticle != null)
                    //     {
                    //         recArticleList.Add(rNextArticle);
                    //     }
                    // }

                    // foreach (var recArticle in recArticleList)
                    // {
                    //     recArticle.ThumbnailUrl = string.IsNullOrEmpty(recArticle.ThumbnailUrl) ? NoImage : recArticle.ThumbnailUrl;
                    // }

                    if (lastArticle != null)
                    {
                        lastArticle.ThumbnailUrl = string.IsNullOrWhiteSpace(lastArticle.ThumbnailUrl) ? NoImage : lastArticle.ThumbnailUrl;
                        ViewData["lastArticle"] = lastArticle;
                    }

                    if (nextArticle != null)
                    {
                        nextArticle.ThumbnailUrl = string.IsNullOrWhiteSpace(nextArticle.ThumbnailUrl) ? NoImage : nextArticle.ThumbnailUrl;
                        ViewData["nextArticle"] = nextArticle;
                    }

                    ViewData["article"] = article;
                    ViewData["rating"] = connection
                        .GetList<Rating>("where qStatus = 0 and articleId = @articleId", new { articleId = article.Id })
                        .FirstOrDefault();
                    // ViewData["recArticleList"] = recArticleList;
                    ViewData["og_title"] = article.Title;
                    ViewData["og_type"] = "article";
                    ViewData["og_description"] = System.Net.WebUtility.HtmlDecode(article.ShortDescription);
                    ViewData["og_image"] = article.ThumbnailUrl.Replace("_small.", "_big.");
                    ViewData["og_url"] = $"/{CurrentLanguage}/article/{article.LatynUrl}.html";
                    var tagList = connection
                        .GetList<Tag>($"where id in (select tagId from {nameof(Tagmap).ToLower()} where itemId = @itemId)",
                            new { itemId = article.Id }).ToList();
                    ViewData["tagList"] = tagList;
                    ViewData["og_keywords"] = string.Join(",", tagList.Select(x => x.Title).ToArray());
                    ViewData["latestArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 25);
                    ViewData["menuTitle"] = QarCache.GetCategoryList(_memoryCache, CurrentLanguage).FirstOrDefault(x => x.Id == article.CategoryId)?.Title;
                    ViewData["menuLink"] = $"/{CurrentLanguage}/category/" + QarCache.GetCategoryList(_memoryCache, CurrentLanguage).FirstOrDefault(x => x.Id == article.CategoryId)?.LatynUrl + ".html";

                    if (HttpContext.User.Identity.IsAuthenticated)
                    {
                        ViewData["canEditArticle"] = true;
                    }

                    return View($"~/Views/Themes/{CurrentTheme}/Home/ArticleView.cshtml");
                }
        }
    }

    #endregion

    #region Save Aritcle Rating +SaveArticleRating(int articleId, string ratingType)

    [HttpPost]
    public IActionResult SaveArticleRating(int articleId, string ratingType)
    {
        ratingType = (ratingType ?? string.Empty).Trim().ToLower();
        if (articleId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong"), "error", "", "articleId");

        string[] ratingTypeArr = { "satisfied", "dissatisfied", "funny", "outrageous" };

        if (string.IsNullOrEmpty(ratingType) || !ratingTypeArr.Contains(ratingType))
            return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", ratingType);

        const string cookieKey = "ratingAritcleIds";

        try
        {
            var articleIdList = new List<int>();
            if (Request.Cookies.TryGetValue(cookieKey, out string articleIds))
            {
                articleIdList = articleIds.Split(',').Select(x => Convert.ToInt32(x)).ToList();
                if (articleIdList.Contains(articleId))
                    return MessageHelper.RedirectAjax("Сіз баға бергенсіз!", "error", "", "");
            }

            int res = 0;

            using (var connection = Utilities.GetOpenConnection())
            {
                if (connection.RecordCount<Rating>("where qStatus = 0 and articleId = @articleId ",
                        new { articleId }) == 0)
                {
                    connection.Insert(new Rating()
                    {
                        ArticleId = articleId,
                        Satisfied = 0,
                        Dissatisfied = 0,
                        Funny = 0,
                        Outrageous = 0,
                        QStatus = 0
                    });

                    res = 1;
                }
                else
                {
                    res = connection.Query<int>(
                        $"select {ratingType} from {nameof(Rating).ToLower()} where qStatus = 0 and articleId = @articleId ",
                        new { articleId }).FirstOrDefault() + 1;
                }

                connection.Execute(
                    $"update rating set {ratingType} = {ratingType} + 1 where qStatus = 0 and articleId = @articleId",
                    new { articleId });
            }

            articleIdList.Add(articleId);
            Response.Cookies.Append(cookieKey, string.Join(',', articleIdList));
            return MessageHelper.RedirectAjax(T("ls_Thankforopinion"), "success", "", res);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "SaveAritcleRating");
            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
        }
    }

    #endregion

    #region Additional Content +AC(string query)
    public IActionResult Ac(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrWhiteSpace(query) || !query.EndsWith(".html")) return Redirect("/404.html");

        query = query[..^5];
        var ac = QarCache.GetAdditionalContentList(_memoryCache, CurrentLanguage)
            .FirstOrDefault(x => x.AdditionalType.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (ac == null) return Redirect("/404.html");

        ViewData["ac"] = ac;
        ViewData["query"] = query;

        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/{ActionName}.cshtml");
    }
    #endregion

    #region Rss +Xml(string query)
    public IActionResult Xml(string query)
    {
        if (!query.Equals("rss.xml", StringComparison.OrdinalIgnoreCase)) return Redirect("/404.html");

        string siteUrl = QarSingleton.GetInstance().GetSiteUrl();

        var rssItemList = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 32).Select(x => new RssItem
        {
            Title = x.Title,
            Description = x.ShortDescription,
            ThumbnailUrl = x.ThumbnailUrl.Replace("_big.", "_small.").Replace(".middle", "_small."),
            Link = $"{siteUrl}/{CurrentLanguage}/article/{x.LatynUrl}.html",
            PubDate = UnixTimeHelper.UnixTimeToDateTime(x.AddTime)
        }).ToList();

        var rss = RssHelper.GenerateRss(rssItemList, QarCache.GetSiteSetting(_memoryCache), CurrentLanguage);

        return Content(rss, "text/xml", Encoding.UTF8);
    }
    #endregion

}