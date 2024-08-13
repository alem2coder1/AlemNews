using System.Globalization;
using AlemNewsWeb.Caches;
using COMMON;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Authorization;
using MODEL;
using MODEL.FormatModels;
using MODEL.ViewModels;
using Serilog;

namespace AlemNewsWeb.Controllers;

[Authorize(Roles = "Admin")]
public class CatalogController : QarBaseController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public CatalogController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }
    #region release +release()
    public IActionResult Release()
    {
        string additionalType = ActionName;
        using (var _connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(_connection, additionalType);
        }
        ViewData["title"] = T("ls_Welcome");
        ViewData["showFieldList"] = new List<string>(){
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View($"~/Views/Console/QarBase/AdditionalContent.cshtml");
    }
    #endregion

    #region Category +Category(string query)

    public IActionResult Category(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Articlecategory");
        switch (query)
        {
            case "create":
                {
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "edit":
                {
                    var articleCategoryId = GetIntQueryParam("id", 0);
                    if (articleCategoryId <= 0)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        var category = connection
                            .GetList<Articlecategory>("where qStatus = 0 and id = @articleCategoryId ",
                                new { articleCategoryId }).FirstOrDefault();
                        if (category == null)
                            return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                        ViewData["category"] = category;
                        ViewData["multiLanguageList"] = GetMultilanguageList(connection, nameof(Articlecategory),
                            new List<int> { category.Id });
                    }

                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "list":
                {
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
                }
            default:
                {
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                }
        }
    }

    #endregion

    #region Category +Category(Articlecategory item)

    [HttpPost]
    public IActionResult Category(Articlecategory item)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");

        if (string.IsNullOrEmpty(item.Language))
            return MessageHelper.RedirectAjax(T("ls_Selectlanguage"), "error", "", "language");

        //if (!QarCache.GetLanguageList(_memoryCache).Exists(x=>x.LanguageCulture.Equals(item.Language)))
        //    return MessageHelper.RedirectAjax(T("ls_Selectlanguage"), "error", "", "language");
        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "title");
        item.ShortDescription ??= string.Empty;
        item.LatynUrl = (item.LatynUrl ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(item.LatynUrl) && !RegexHelper.IsLatinString(item.LatynUrl))
            return MessageHelper.RedirectAjax("Use latyn, number, - ", "error", "", "latynUrl");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();

        using (var connection = Utilities.GetOpenConnection())
        {
            item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Articlecategory), item.LatynUrl, item.Title,
                item.Id, item.Language);
            int? res = 0;
            if (item.Id == 0)
            {
                if (item.DisplayOrder == 0)
                {
                    item.DisplayOrder =
                        connection.Query<int?>("select max(displayOrder) from articlecategory where qStatus = 0")
                            .FirstOrDefault() ?? 0;
                    item.DisplayOrder += 1;
                }

                string blockType = null;

                if (item.ParentId == 0)
                {
                    blockType = "block" + item.DisplayOrder;
                }
                else
                {
                    blockType = connection.Query<string>(
                        "select blockType from articlecategory where parentId = @ParentId",
                        new { ParentId = item.ParentId }
                    ).FirstOrDefault() ?? " ";
                }
                
               
                res = connection.Insert(new Articlecategory
                {
                    Title = item.Title,
                    OldLatynUrl = item.LatynUrl,
                    LatynUrl = item.LatynUrl,
                    Language = item.Language,
                    BlockType = blockType,
                    ParentId = item.ParentId,
                    ShortDescription = item.ShortDescription,
                    DisplayOrder = item.DisplayOrder,
                    IsHidden = item.IsHidden,
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 0
                });
                if (res > 0)
                {
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                    return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                }
            }
            else
            {
                var category = connection
                    .GetList<Articlecategory>("where qStatus = 0 and id = @id", new { id = item.Id }).FirstOrDefault();
                if (category == null)
                    return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong"), "error", "", "");
                string blockType = null;

                if (item.ParentId == 0)
                {
                    blockType = "block" + item.DisplayOrder;
                }
                else
                {
                    blockType = connection.Query<string>(
                        "select blockType from articlecategory where parentId = @ParentId",
                        new { ParentId = item.ParentId }
                    ).FirstOrDefault() ?? " ";
                }
                category.Title = item.Title;
                category.ParentId = item.ParentId;
                category.Language = item.Language;
                category.LatynUrl = item.LatynUrl;
                category.ShortDescription = item.ShortDescription;
                category.IsHidden = item.IsHidden;
                category.DisplayOrder = item.DisplayOrder;
                category.UpdateTime = currentTime;
                category.BlockType = blockType;
                res = connection.Update(category);
                if (res > 0)
                {
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                    return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={category.Id}",
                        "");
                }
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get article category list +GetCategoryList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetCategoryList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 9;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using (var connection = Utilities.GetOpenConnection())
        {
            var querySql = " from articlecategory where qStatus = 0 ";
            object queryObj = new { keyword = "%" + keyword + "%" };
            var orderSql = "";
            if (!string.IsNullOrEmpty(keyword)) querySql += " and (title like @keyword)";

            if (model.OrderList != null && model.OrderList.Count > 0)
                foreach (var item in model.OrderList)
                    switch (item.Column)
                    {
                        case 3:
                            {
                                orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " displayOrder " + item.Dir;
                            }
                            break;
                        case 4:
                            {
                                orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                            }
                            break;
                    }

            if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

            var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
            var totalPage = total % length == 0 ? total / length : total / length + 1;
            var articleCategoryList = connection
                .Query<Articlecategory>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                    queryObj).ToList();
            var languageList = QarCache.GetLanguageList(_memoryCache);
            var dataList = articleCategoryList.Select(x => new
            {
                x.Id,
                x.Title,
                Language = languageList
                    .FirstOrDefault(l => l.LanguageCulture.Equals(x.Language, StringComparison.OrdinalIgnoreCase))
                    ?.FullName,
                x.DisplayOrder,
                x.LatynUrl,
                AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
            }).ToList();
            return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
                new { start, length, keyword, total, totalPage, dataList });
        }
    }

    #endregion

    #region Set article category status +SetCategoryStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetCategoryStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Chooseatleastone"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
                {
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        using (var tran = connection.BeginTransaction())
                        {
                            try
                            {
                                var articleCategoryList = connection
                                    .GetList<Articlecategory>($"where qStatus = 0 and id in ({string.Join(",", idList)})")
                                    .ToList();
                                foreach (var articleCategory in articleCategoryList)
                                {
                                    articleCategory.QStatus = 1;
                                    articleCategory.UpdateTime = currentTime;
                                    connection.Update(articleCategory);
                                }

                                tran.Commit();
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetArticleList));
                                return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, ActionName);
                                tran.Rollback();
                                return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                            }
                        }
                    }
                }
            default:
                {
                    return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
                }
        }
    }

    #endregion

    #region Article +Article(string query)

    public IActionResult Article(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Articles");
        var adminList = QarCache.GetAllAdminList(_memoryCache);
        ViewData["adminList"] = adminList;
        switch (query)
        {
            case "create":
                {
                    ViewData["categoryList"] = QarCache.GetCategoryList(_memoryCache);
                    var roleIdList = HttpContext.User.Identity.RoleIds();
                    ViewData["canSchedule"] =
                        IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
                    ViewData["canFocus"] =
                        IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "edit":
                {
                    var articleId = GetIntQueryParam("id", 0);
                    if (articleId <= 0)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        var article = connection
                            .GetList<Article>("where qStatus <> 1 and id = @articleId ", new { articleId })
                            .FirstOrDefault();
                        if (article == null)
                            return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                        ViewData["article"] = article;
                        ViewData["category"] = connection
                            .GetList<Articlecategory>("where qStatus = 0 and id = @id", new { id = article.CategoryId })
                            .FirstOrDefault();
                        ViewData["multiLanguageList"] =
                            GetMultilanguageList(connection, nameof(Article), new List<int> { article.Id });
                        ViewData["tagList"] = connection.Query<string>(
                            "select title from tag where id in (select tagId from tagmap where qStatus = 0 and tableName = @tableName and itemId = @itemId)",
                            new { tableName = nameof(Article), itemId = article.Id }).ToList();
                    }

                    ViewData["categoryList"] = QarCache.GetCategoryList(_memoryCache);
                    var roleIdList = HttpContext.User.Identity.RoleIds();
                    ViewData["canSchedule"] =
                        IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
                    ViewData["canFocus"] =
                        IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "list":
                {
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
                }
            default:
                {
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                }
        }
    }

    #endregion

    #region Article +Article(Article item, string tags, byte isAutoPublish, string autoPublishTimeStr, byte publishNow)

    [HttpPost]
    public IActionResult Article(Article item, string tags, byte isAutoPublish, string autoPublishTimeStr,
        byte publishNow)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");

        if (item.CategoryId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Pleaseselectthecategory"), "error", "", "");

        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "title");

        if (string.IsNullOrWhiteSpace(item.ThumbnailCopyright))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "thumbnailCopyright");

        if (string.IsNullOrEmpty(tags) || string.IsNullOrEmpty(tags = tags.Trim()))
            return MessageHelper.RedirectAjax(T("ls_Wrttlsttg"), "error", "", null);

        var tagList = new List<string>();
        try
        {
            var tagModelList = JsonHelper.DeserializeObject<List<TagModel>>(tags);
            tagList = tagModelList.Select(x => x.Value).Distinct().ToList();
        }
        catch (Exception ex)
        {
            return MessageHelper.RedirectAjax(ex.Message, "error", "", null);
        }

        if (tagList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Wrttlsttg"), "error", "", null);

        if (isAutoPublish > 0)
        {
            if (!DateTime.TryParseExact(autoPublishTimeStr, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dateTimeAutoPublish))
                return MessageHelper.RedirectAjax("Мақала автоматты жолданатын уақытын дұрыс жазыңыз!", "error", "",
                    null);
            item.AutoPublishTime = UnixTimeHelper.ConvertToUnixTime(dateTimeAutoPublish);
        }
        else
        {
            item.AutoPublishTime = 0;
        }

        var roleIdList = HttpContext.User.Identity.RoleIds();
        var canSchedule = IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
        var canFocus = IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");

        item.ThumbnailUrl ??= string.Empty;
        item.ThumbnailCopyright ??= string.Empty;
        item.ShortDescription = string.IsNullOrWhiteSpace(item.ShortDescription)
            ? HtmlAgilityPackHelper.GetShortDescription(item.FullDescription)
            : item.ShortDescription;
        item.FullDescription = HtmlAgilityPackHelper.GetHtmlBoyInnerHtml(item.FullDescription);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        int? res = 0;
        var qStatus = (byte)(publishNow == 1 ? isAutoPublish > 0 ? 2 : 0 : 3);
        int categoryParentId = 0;
        if (item.CategoryId != 0)
        {
             categoryParentId = QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
                .Where(x => x.Id == item.CategoryId)
                .Select(x => x.ParentId) 
                .FirstOrDefault(); 
        }
        using (var connection = Utilities.GetOpenConnection())
        using (var tran = connection.BeginTransaction())
        {
            try
            {
                item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Article), "", item.Title, item.Id, "");
                
                if (item.Id == 0)
                {
                    
                    res = connection.Insert(new Article
                    {
                        CategoryId = item.CategoryId,
                        CategoryParentId = categoryParentId,
                        Title = item.Title,
                        LatynUrl = item.LatynUrl,
                        OldId = 0,
                        OldLatynUrl = string.Empty,
                        RecArticleIds = string.Empty,
                        ThumbnailUrl = item.ThumbnailUrl,
                        ShortDescription = item.ShortDescription,
                        FullDescription = item.FullDescription,
                        Author = item.Author,
                        AddAdminId = GetAdminId(),
                        UpdateAdminId = GetAdminId(),
                        AutoPublishTime = item.AutoPublishTime,
                        FocusAdditionalFile = string.Empty,
                        SearchName = string.Empty,
                        HasAudio = 0,
                        HasVideo = 0,
                        CommentCount = 0,
                        DislikeCount = 0,
                        LikeCount = 0,
                        HasImage = 0,
                        IsFocusNews = (byte)(canFocus ? item.IsFocusNews : 0),
                        IsList = 0,
                        IsTop = 0,
                        IsPinned = item.IsPinned,
                        IsFeatured = item.IsFeatured,
                        ViewCount = 0,
                        ThumbnailCopyright = item.ThumbnailCopyright,
                        AddTime = currentTime,
                        UpdateTime = currentTime,
                        QStatus = qStatus
                    });
                    if (res > 0)
                    {
                        UpdateMediaInfo(connection, item.ThumbnailUrl, "");
                        foreach (var mediaPath in HtmlAgilityPackHelper.GetMediaPathList(item.FullDescription))
                            UpdateMediaInfo(connection, mediaPath, "");

                        InsertOrEditTagList(connection, tagList, nameof(Article), res ?? 0);
                        if (item.IsPinned == 1)
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                        if (item.IsFocusNews == 1)
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));

                        tran.Commit();
                        return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                    }
                }

                else
                {
                    var article = connection.GetList<Article>("where qStatus <> 1 and id = @id", new { id = item.Id })
                        .FirstOrDefault();
                    if (article == null)
                        return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong"), "error", "", "");

                    var pinnedIsChanged = article.IsPinned == item.IsPinned;
                    var focusIsChanged = article.IsFocusNews == item.IsFocusNews;

                    article.CategoryId = item.CategoryId;
                    article.Title = item.Title;
                    article.LatynUrl = item.LatynUrl;
                    article.IsPinned = item.IsPinned;
                    article.IsFeatured = item.IsFeatured;
                    article.ThumbnailCopyright = item.ThumbnailCopyright;


                    if (!article.ThumbnailUrl.Equals(item.ThumbnailUrl))
                    {
                        UpdateMediaInfo(connection, item.ThumbnailUrl, article.ThumbnailUrl);
                        article.ThumbnailUrl = item.ThumbnailUrl;
                    }

                    if (canFocus) article.IsFocusNews = item.IsFocusNews;

                    if (canSchedule) article.AutoPublishTime = item.AutoPublishTime;

                    var oldMediaPathList = HtmlAgilityPackHelper.GetMediaPathList(article.FullDescription);
                    article.ShortDescription = item.ShortDescription;
                    article.CategoryParentId = categoryParentId;
                    article.FullDescription = item.FullDescription;
                    article.Author = item.Author;
                    article.UpdateAdminId = GetAdminId();
                    article.UpdateTime = currentTime;
                    article.QStatus = qStatus;
                    res = connection.Update(article);
                    if (res > 0)
                    {
                        InsertOrEditTagList(connection, tagList, nameof(Article), article.Id);
                        var newMediaList = HtmlAgilityPackHelper.GetMediaPathList(item.FullDescription);
                        foreach (var oldMediaPath in oldMediaPathList)
                            if (!newMediaList.Contains(oldMediaPath))
                                UpdateMediaInfo(connection, "", oldMediaPath); //Remove Old Media

                        foreach (var newMediaPath in newMediaList)
                            if (!oldMediaPathList.Contains(newMediaPath))
                                UpdateMediaInfo(connection, newMediaPath, ""); //Add New Media

                        if (pinnedIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                        if (focusIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));

                        tran.Commit();
                        return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={article.Id}",
                            "");
                    }
                }
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Log.Error(ex, ActionName);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Biography +Biography(Article item, IFormFile additionalFile)

    [HttpPost]
    public IActionResult Biography(Article item, IFormFile additionalFile)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");

        if (item.CategoryId != 53) // Biography
            return MessageHelper.RedirectAjax(T("ls_Pleaseselectthecategory"), "error", "", "");

        if (item.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(item.ThumbnailUrl))
                return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", "thumbnailUrl");

            if (additionalFile == null)
                return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", "additionalFile");

            if (!additionalFile.ContentType.StartsWith("image/"))
                return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", "additionalFile");
        }

        if (string.IsNullOrWhiteSpace(item.ThumbnailCopyright))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "thumbnailCopyright");

        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "title");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var relativePath = string.Empty;
        var absolutePath = string.Empty;

        if (additionalFile != null)
        {
            var fileFormat = Path.GetExtension(additionalFile.FileName).ToLower();

            if (!ImageFileExtensions.Contains(fileFormat))
            {
                var message = T("ls_Ieffnofas").Replace("{name}", fileFormat)
                    .Replace("{extensions}", string.Join(", ", ImageFileExtensions));
                return MessageHelper.RedirectAjax(message, "error", "", "additionalFile");
            }

            if (fileFormat.Equals(".jpeg")) fileFormat = ".jpg";

            var fileName = UnixTimeHelper.UnixTimeToDateTime(currentTime).ToString("yyyyMMddHHmmssfff");
            relativePath = $"/uploads/images/{fileName}{fileFormat}";
            absolutePath = PathHelper.Combine(_environment.WebRootPath, relativePath);

            FileHelper.EnsureDir(absolutePath);

            using (var fs = System.IO.File.OpenWrite(absolutePath))
            {
                additionalFile.CopyTo(fs);
            }
        }

        item.ThumbnailUrl ??= string.Empty;
        int? res = 0;

        using (var connection = Utilities.GetOpenConnection())
        {
            item.LatynUrl = GetDistinctLatynUrl(connection, nameof(MODEL.Article), "", item.Title, item.Id, "");
            if (item.Id == 0)
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        res = connection.Insert(new Article
                        {
                            CategoryId = item.CategoryId,
                            Title = item.Title,
                            LatynUrl = item.LatynUrl,
                            OldId = 0,
                            OldLatynUrl = string.Empty,
                            RecArticleIds = string.Empty,
                            ThumbnailUrl = item.ThumbnailUrl,
                            ShortDescription = string.Empty,
                            FullDescription = string.Empty,
                            Author = item.Author,
                            AddAdminId = GetAdminId(),
                            UpdateAdminId = GetAdminId(),
                            AutoPublishTime = 0,
                            FocusAdditionalFile = relativePath,
                            SearchName = string.Empty,
                            HasAudio = 0,
                            HasVideo = 0,
                            CommentCount = 0,
                            DislikeCount = 0,
                            LikeCount = 0,
                            HasImage = 0,
                            IsFocusNews = 0,
                            IsList = 0,
                            IsTop = 0,
                            IsPinned = 0,
                            IsFeatured = 0,
                            ViewCount = 0,
                            ThumbnailCopyright = item.ThumbnailCopyright,
                            AddTime = currentTime,
                            UpdateTime = currentTime,
                            QStatus = 0
                        });

                        if (res > 0)
                        {
                            UpdateMediaInfo(connection, item.ThumbnailUrl, "");

                            tran.Commit();
                            return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                                $"/{CurrentLanguage}/{ControllerName.ToLower()}/article/edit?id={res}", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        Log.Error(ex, ActionName);
                    }
                }
            else
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        var article = connection
                            .GetList<Article>("where qStatus <> 1 and id = @id", new { id = item.Id }).FirstOrDefault();
                        if (article == null)
                            return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong"), "error", "", "");

                        article.Title = item.Title;
                        article.LatynUrl = item.LatynUrl;
                        article.ThumbnailCopyright = item.ThumbnailCopyright;

                        if (!string.IsNullOrWhiteSpace(item.ThumbnailUrl) &&
                            !article.ThumbnailUrl.Equals(item.ThumbnailUrl))
                        {
                            UpdateMediaInfo(connection, item.ThumbnailUrl, article.ThumbnailUrl);
                            article.ThumbnailUrl = item.ThumbnailUrl;
                        }

                        if (!string.IsNullOrWhiteSpace(relativePath) &&
                            !article.FocusAdditionalFile.Equals(relativePath))
                        {
                            FileHelper.Delete(PathHelper.Combine(_environment.WebRootPath,
                                article.FocusAdditionalFile));
                            article.FocusAdditionalFile = relativePath;
                        }

                        article.UpdateAdminId = GetAdminId();
                        article.UpdateTime = currentTime;
                        res = connection.Update(article);
                        if (res > 0)
                        {
                            tran.Commit();
                            return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                                $"/{CurrentLanguage}/{ControllerName.ToLower()}/article/edit?id={article.Id}", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        Log.Error(ex, ActionName);
                    }
                }
        }

        FileHelper.Delete(absolutePath);
        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get Article List +GetArticleList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetArticleList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        string timeSelectSql = string.Empty;
        if (!string.IsNullOrWhiteSpace(model.DateTimeStart) && !string.IsNullOrWhiteSpace(model.DateTimeEnd))
        {
            var startTime = UnixTimeHelper.ConvertToUnixTime(model.DateTimeStart);
            var endTime = UnixTimeHelper.ConvertToUnixTime(model.DateTimeEnd);
            if (startTime > 0 && endTime > 0 && startTime < endTime)
            {
                timeSelectSql = $" and addTime >= {startTime} and addTime <= {endTime} ";
            }
        }
        using (var connection = Utilities.GetOpenConnection())
        {
            var querySql = " from article where qStatus <> 1 " + timeSelectSql;
            object queryObj = new { keyword };
            var orderSql = "";
            if (!string.IsNullOrWhiteSpace(keyword))
                querySql += " and match(title, fullDescription) against(@keyword in natural language mode) ";

            if (model.OrderList != null && model.OrderList.Count > 0)
                foreach (var item in model.OrderList)
                    switch (item.Column)
                    {
                        case 3:
                        case 4:
                            {
                                orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                            }
                            break;
                    }

            if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

            var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
            var totalPage = total % length == 0 ? total / length : total / length + 1;
            var adminList = QarCache.GetAllAdminList(_memoryCache);
            var categoryList = QarCache.GetCategoryList(_memoryCache);
            var dataList = connection
                .Query<Article>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                    queryObj).Select(x => new
                    {
                        x.Id,
                        x.Title,
                        AddAdmin = adminList.FirstOrDefault(a => a.Id == x.AddAdminId)?.Name,
                        UpdateAdmin = adminList.FirstOrDefault(a => a.Id == x.UpdateAdminId)?.Name,
                        ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl)
                        ? NoImage
                        : x.ThumbnailUrl.Replace("_big.", "_small."),
                        AutoPublishTime = x.QStatus == 2
                        ? UnixTimeHelper.UnixTimeToDateTime(x.AutoPublishTime).ToString("dd/MM/yyyy HH:mm")
                        : "",
                        LatynUrl =
                        $"/{categoryList.FirstOrDefault(c => c.Id == x.CategoryId)?.Language}/article/{x.LatynUrl}.html",
                        x.ViewCount,
                        x.ThumbnailCopyright,
                        x.QStatus,
                        AddTime = x.AddTime > 0 ? UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm") : ""
                    }).ToList();
            return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
                new { start, length, keyword, total, totalPage, dataList });
        }
    }

    #endregion

    #region Set Article Status +SetArticleStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetArticleStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Chooseatleastone"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
                {
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        using (var tran = connection.BeginTransaction())
                        {
                            try
                            {
                                var articleList = connection
                                    .GetList<Article>($"where qStatus = 0 and id in ({string.Join(",", idList)})").ToList();
                                foreach (var article in articleList)
                                {
                                    article.QStatus = 1;
                                    article.UpdateTime = currentTime;
                                    UpdateMediaInfo(connection, "", article.ThumbnailUrl);
                                    connection.Update(article);
                                }

                                tran.Commit();
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetArticleList));
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetTopArticleList));
                                return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, ActionName);
                                tran.Rollback();
                                return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                            }
                        }
                    }
                }
            default:
                {
                    return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
                }
        }
    }

    #endregion

    #region Aboutus +AboutUs()

    public IActionResult AboutUs()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_AboutUs");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion

    #region Contact us +ContactUs()

    public IActionResult ContactUs()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_ContactUs");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion

    #region Advertise +Advertise()

    public IActionResult Advertise()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_Advertise");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion


    #region Partner +Partner(string query)
    public IActionResult Partner(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Ourpartners");
        switch (query)
        {
            case "create":
                {
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "edit":
                {
                    int partnerId = GetIntQueryParam("id", 0);
                    if (partnerId <= 0)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    using (var _connection = Utilities.GetOpenConnection())
                    {
                        Partner partner = _connection.GetList<Partner>("where qStatus = 0 and id = @partnerId ", new { partnerId }).FirstOrDefault();
                        if (partner == null)
                            return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                        ViewData["partner"] = partner;
                        ViewData["multiLanguageList"] = GetMultilanguageList(_connection, nameof(Partner), new List<int> { partner.Id });
                    }
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "list":
                {
                    return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
                }
            default:
                {
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                }
        }
    }
    #endregion

    #region Partner +Partner(Partner item,IFormFile thumbnailImage, string multiLanguageJson)
    [HttpPost]
    public IActionResult Partner(Partner item, IFormFile thumbnailImage, string multiLanguageJson)
    {

        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "title");

        if (string.IsNullOrEmpty(item.LinkUrl))
            return MessageHelper.RedirectAjax(T("ls_Thisfieldisrequired"), "error", "", "linkUrl");

        List<Multilanguage> multiLanguageList = null;
        try
        {
            multiLanguageList = JsonHelper.DeserializeObject<List<Multilanguage>>(multiLanguageJson);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, ActionName);
            return MessageHelper.RedirectAjax(T("ls_ErrordecodingJSONdata"), "error", "", "multiLanguageJson");
        }

        item.ShortDescription ??= string.Empty;
        item.ThumbnailUrl ??= string.Empty;
        if (thumbnailImage != null)
        {
            if (!thumbnailImage.ContentType.Contains("image") || !ImageFileExtensions.Any(item => thumbnailImage.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
            {
                return MessageHelper.RedirectAjax(T("ls_Theimageisinvalidornotsupported"), "error", "", null);
            }
            string tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fileFormat = Path.GetExtension(thumbnailImage.FileName).ToLower();
            item.ThumbnailUrl = "/uploads/images/" + tempKey + fileFormat;
            string absolutePath = _environment.WebRootPath + item.ThumbnailUrl;
            if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            using (MemoryStream stream = new MemoryStream())
            {
                thumbnailImage.CopyTo(stream);
                ImgHandler.ConvertImageColorsToUlyWhite(stream, absolutePath);
            }

        }
        int currentTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now);
        int? res = 0;
        using (var _connection = Utilities.GetOpenConnection())
        {
            if (item.Id == 0)
            {
                if (item.DisplayOrder == 0)
                {
                    item.DisplayOrder = _connection.Query<int?>("select max(displayOrder) from partner where qStatus = 0").FirstOrDefault() ?? 0;
                    item.DisplayOrder += 1;
                }
                res = _connection.Insert(new Partner()
                {
                    Title = item.Title,
                    LinkUrl = item.LinkUrl,
                    ThumbnailUrl = item.ThumbnailUrl,
                    ShortDescription = item.ShortDescription,
                    DisplayOrder = item.DisplayOrder,
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 0
                });
                if (res > 0)
                {
                    UpdateMediaInfo(_connection, item.ThumbnailUrl, "");
                    SaveMultilanguageList(_connection, multiLanguageList, nameof(Partner), res ?? 0);
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPartnerList));
                    return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success", $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                }
            }
            else
            {
                Partner partner = _connection.GetList<Partner>("where qStatus = 0 and id = @id", new { id = item.Id }).FirstOrDefault();
                if (partner == null)
                    return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong"), "error", "", "");
                partner.Title = item.Title;
                partner.ShortDescription = item.ShortDescription;
                partner.LinkUrl = item.LinkUrl;
                if (!partner.ThumbnailUrl.Equals(item.ThumbnailUrl))
                {
                    UpdateMediaInfo(_connection, item.ThumbnailUrl, partner.ThumbnailUrl);
                    partner.ThumbnailUrl = item.ThumbnailUrl;
                }
                partner.DisplayOrder = item.DisplayOrder;
                partner.UpdateTime = currentTime;
                res = _connection.Update(partner);
                if (res > 0)
                {
                    SaveMultilanguageList(_connection, multiLanguageList, nameof(Partner), partner.Id);
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPartnerList));
                    return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success", $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={partner.Id}", "");
                }
            }

        }
        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");

    }
    #endregion

    #region Get Partner List +GetPartnerList(APIUnifiedModel model)
    [HttpPost]
    public IActionResult GetPartnerList(ApiUnifiedModel model)
    {
        int start = model.Start > 0 ? model.Start : 0;
        int length = model.Length > 0 ? model.Length : 10;
        string keyWord = (model.Keyword ?? string.Empty).Trim();
        using (var _connection = Utilities.GetOpenConnection())
        {
            string querySql = " from partner where qStatus = 0 ";
            object queryObj = new { keyWord = "%" + keyWord + "%" };
            string orderSql = "";
            if (!string.IsNullOrEmpty(keyWord))
            {
                querySql += " and (title like @keyWord)";
            }
            if (model.OrderList != null && model.OrderList.Count > 0)
            {
                foreach (DataTableOrderModel item in model.OrderList)
                {
                    switch (item.Column)
                    {
                        case 3: { orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " displayOrder " + item.Dir; } break;
                        case 4: { orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir; } break;
                    }
                }
            }

            if (string.IsNullOrEmpty(orderSql))
            {
                orderSql = " addTime desc ";
            }

            int total = _connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
            int totalPage = total % length == 0 ? total / length : total / length + 1;
            var partnerList = _connection.Query<Partner>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}", queryObj).ToList();

            var dataList = partnerList.Select(x => new
            {
                x.Id,
                x.Title,
                x.DisplayOrder,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? NoImage : x.ThumbnailUrl,
                x.LinkUrl,
                AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
            }).ToList();
            return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "", new { start, length, keyWord, total, totalPage, dataList });
        }
    }
    #endregion

    #region Set Partner Status +SetPartnerStatus(string manageType,List<int> idList)
    [HttpPost]
    public IActionResult SetPartnerStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count() == 0)
            return MessageHelper.RedirectAjax(T("ls_Chooseatleastone"), "error", "", null);
        int currentTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now);
        switch (manageType)
        {
            case "delete":
                {
                    using (var _connection = Utilities.GetOpenConnection())
                    {
                        using (var _tran = _connection.BeginTransaction())
                        {
                            try
                            {
                                List<Partner> partnerList = _connection.GetList<Partner>($"where qStatus = 0 and id in ({string.Join(",", idList)})").ToList();
                                foreach (Partner partner in partnerList)
                                {
                                    partner.QStatus = 1;
                                    partner.UpdateTime = currentTime;
                                    _connection.Update(partner);
                                }
                                _tran.Commit();
                                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPartnerList));
                                return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                            }
                            catch (Exception ex)
                            {
                                Serilog.Log.Error(ex, ActionName);
                                _tran.Rollback();
                                return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                            }
                        }
                    }
                }
            default:
                {
                    return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
                }
        }

    }
    #endregion


}