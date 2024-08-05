using System.Reflection;
using AlemNewsWeb.Caches;
using AlemNewsWeb.Controllers;
using COMMON;
using Dapper;
using DBHelper;
using Hangfire;
using MODEL;
using MODEL.ViewModels;
using Serilog;

namespace AlemNewsWeb.Hangfire;

public class QarJob
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public QarJob(IMemoryCache memoryCache, IWebHostEnvironment environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Delete Old Log Files +JobDeleteOldLogFiles()

    public void JobDeleteOldLogFiles()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var logDirectoryPath = _environment.ContentRootPath +
                                   (_environment.ContentRootPath.EndsWith("/") ? "" : "/") + "logs";
            var directory = new DirectoryInfo(logDirectoryPath);
            if (!directory.Exists) return;
            var txtFiles = directory.GetFiles("*.txt");
            foreach (var file in txtFiles)
            {
                var timeDifference = DateTime.Now - file.CreationTime;
                if (timeDifference.Days > 7) file.Delete();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "jobDeleteOldLogFiles");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Job Save Relogin AdminIds +JobSaveReloginAdminIds()

    public void JobSaveReloginAdminIds()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            using (var connection = Utilities.GetOpenConnection())
            {
                var reloginAdminList = connection.GetList<Admin>("where reLogin = 1").ToList();
                foreach (var reloginAdmin in reloginAdminList)
                    QarSingleton.GetInstance().AddReLoginAdmin(reloginAdmin.Id, reloginAdmin.UpdateTime);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Автоматты жолданатын мақалаларды жолдау +JobPublishAutoPublishArticle()

    public void JobPublishAutoPublishArticle()
    {
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var pinnedIsChanged = false;
        var focusIsChanged = false;

        using (var connection = Utilities.GetOpenConnection())
        {
            var articleList = connection
                .Query<Article>(
                    "select id, isPinned, isFocusNews, autoPublishTime from article where QStatus = 2 and autoPublishTime <= @currentTime",
                    new { currentTime }).ToList();
            if (articleList != null && articleList.Count > 0)
            {
                var querySql = string.Empty;
                foreach (var item in articleList)
                {
                    if (!pinnedIsChanged && item.IsPinned == 1) pinnedIsChanged = true;
                    if (!focusIsChanged && item.IsFocusNews == 1) focusIsChanged = true;
                    // item.QStatus = 0;
                    // item.AddTime = item.AutoPublishTime;
                    // item.UpdateTime = item.AutoPublishTime;
                    // _connection.Update(item);
                    querySql +=
                        $"update article set addTime = {item.AutoPublishTime}, updateTime = {item.AutoPublishTime} , qStatus = 0 where id = {item.Id};";
                    if (querySql.Length > 900)
                        Task.Run(async () =>
                        {
                            await connection.ExecuteAsync(querySql);
                            querySql = string.Empty;
                        }).Wait();
                }

                if (!string.IsNullOrWhiteSpace(querySql))
                    Task.Run(async () =>
                    {
                        await connection.ExecuteAsync(querySql);
                        querySql = string.Empty;
                    }).Wait();
            }
        }

        if (pinnedIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
        if (focusIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
    }

    #endregion

    #region Sync Collect Article Media +JobSyncOldDbArticle()

    public void JobSyncOldDbArticle()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var takeCount = 100;
                var lastOldArticle = 0;
                using (var connection = Utilities.GetOpenConnection())
                {
                    lastOldArticle = connection.Query<int?>("select max(oldId) from article where 1 = 1")
                        .FirstOrDefault() ?? 0;
                }

                var oldArticleList = new List<OldDbArticleModel>();

                #region Get Old Article List

                using (var oldDbConnection = Utilities.GetOldDbConnection())
                {
                    var querySql = @"SELECT 
    p.id as id,
    p.title,
    p.title_slug as oldLatynUrl,
    p.content as fullDescription,
		COALESCE(NULLIF(p.image_default, ''), p.image_url) as thumbnailUrl,
		p.category_id as categoryId,
		p.image_description as thumbnailCopyright,
		p.author,
		p.pageviews as viewCount,
    UNIX_TIMESTAMP(p.created_at) as addTime,
		UNIX_TIMESTAMP(COALESCE(p.updated_at,p.created_at)) as updateTime
FROM 
    posts AS p
WHERE  p.id >  @lastOldArticle 
		order by p.id asc limit @takeCount;";
                    oldArticleList = oldDbConnection
                        .Query<OldDbArticleModel>(querySql, new { takeCount, lastOldArticle }).ToList();
                    foreach (var oldArticle in oldArticleList)
                    {
                        var queryTagSql =
                            @"select tag as title, tag_slug as latynUrl from tags where post_id =  @oldArticleId;";

                        oldArticle.TagList = oldDbConnection
                            .Query<Tag>(queryTagSql, new { oldArticleId = oldArticle.Id }).ToList();
                    }
                }

                #endregion

                if (oldArticleList == null || oldArticleList.Count == 0) return;

                #region Insert New Database

                using (var connection = Utilities.GetOpenConnection())
                {
                    foreach (var oldArticle in oldArticleList)
                    {
                        int? res = 0;
                        using (var tran = connection.BeginTransaction())
                        {
                            try
                            {
                                #region Get Article Tag Id

                                var tagIdList = new List<uint>();
                                if (oldArticle.TagList != null && oldArticle.TagList.Count > 0)
                                    foreach (var tag in oldArticle.TagList)
                                    {
                                        var tagId = connection
                                            .Query<uint?>("select id from tag where title = @tagTitle ",
                                                new { tagTitle = tag.Title }).FirstOrDefault() ?? 0;
                                        if (tagId > 0)
                                        {
                                            tagIdList.Add(tagId);
                                        }
                                        else
                                        {
                                            res = connection.Insert(new Tag
                                            {
                                                Title = tag.Title,
                                                TaggedCount = 1,
                                                LatynUrl = tag.LatynUrl
                                            });
                                            if (res > 0) tagIdList.Add(Convert.ToUInt32(res ?? 0));
                                        }
                                    }

                                #endregion

                                #region Save Article

                                if (string.IsNullOrEmpty(oldArticle.LatynUrl))
                                    oldArticle.LatynUrl = QarBaseController.GetDistinctLatynUrl(connection,
                                        nameof(Article), oldArticle.LatynUrl, oldArticle.Title, 0, "");

                                oldArticle.Title = oldArticle.Title.Length > 255
                                    ? oldArticle.Title[..250] + "..."
                                    : oldArticle.Title;
                                oldArticle.FullDescription =
                                    HtmlAgilityPackHelper.ConvertShortcodeToHtml(oldArticle.FullDescription);
                                oldArticle.FullDescription =
                                    (oldArticle.FullDescription ?? string.Empty).Replace(Environment.NewLine, "<br/>");
                                oldArticle.ShortDescription =
                                    HtmlAgilityPackHelper.GetShortDescription(oldArticle.FullDescription);
                                res = connection.Insert(new Article
                                {
                                    LatynUrl = oldArticle.LatynUrl,
                                    OldLatynUrl = oldArticle.OldLatynUrl ?? string.Empty,
                                    CategoryId = oldArticle.CategoryId,
                                    OldId = oldArticle.Id,
                                    Title = oldArticle.Title ?? string.Empty,
                                    Author = 0,
                                    SearchName = oldArticle.Title ?? string.Empty,
                                    ThumbnailUrl = oldArticle.ThumbnailUrl ?? string.Empty,
                                    ShortDescription = oldArticle.ShortDescription ?? string.Empty,
                                    FullDescription = oldArticle.FullDescription ?? string.Empty,
                                    ViewCount = oldArticle.ViewCount,
                                    ThumbnailCopyright = oldArticle.ThumbnailCopyright ?? string.Empty,
                                    IsFocusNews = 0,
                                    AddAdminId = 0,
                                    UpdateAdminId = 0,
                                    AutoPublishTime = 0,
                                    RecArticleIds = "",
                                    AddTime = oldArticle.AddTime,
                                    UpdateTime = oldArticle.UpdateTime,
                                    FocusAdditionalFile = "",
                                    HasAudio = 0,
                                    HasVideo = 0,
                                    HasImage = 0,
                                    CommentCount = 0,
                                    DislikeCount = 0,
                                    IsFeatured = 0,
                                    IsList = 0,
                                    IsPinned = 0,
                                    IsTop = 0,
                                    LikeCount = 0,
                                    QStatus = 5
                                });

                                #endregion

                                if (res > 0)
                                {
                                    #region Save Article Tag Map

                                    foreach (var tagId in tagIdList.Select(v => (int)v))
                                    {
                                        connection.Insert(new Tagmap
                                        {
                                            TableName = nameof(Article),
                                            ItemId = Convert.ToInt32(res ?? 0),
                                            TagId = tagId,
                                            QStatus = 0
                                        });
                                        connection.Execute(
                                            "update tag set taggedCount = (select count(1) from tagmap where tagId = @tagId) where id = @tagId",
                                            new { tagId });
                                    }

                                    #endregion
                                }

                                tran.Commit();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, key);
                                tran.Rollback();
                            }
                        }
                    }
                }

                #endregion
            }).Wait();
        }

        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Enqueue<QarJob>(x => x.JobSyncOldDbArticle());
        }
    }

    #endregion

    #region Sync Collect OldDb Category +JobSyncOldDbCategory()

    public void JobSyncOldDbCategory()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var oldCategoryList = new List<Articlecategory>();

                #region Get Old Category List

                using (var oldDbConnection = Utilities.GetOldDbConnection())
                {
                    var querySql =
                        @"select id, `name` as title, name_slug as oldLatynUrl, parent_id as parentId, description as shortDescription, 
block_type as blockType, category_order as displayOrder, show_on_menu as isHidden,
UNIX_TIMESTAMP(created_at) as addTime from categories order by id;";
                    oldCategoryList = oldDbConnection.Query<Articlecategory>(querySql).ToList();
                }

                #endregion

                if (oldCategoryList == null || oldCategoryList.Count == 0) return;

                #region Insert New Database

                using (var connection = Utilities.GetOpenConnection())
                {
                    foreach (var oldCategory in oldCategoryList)
                    {
                        int? res = 0;
                        using (var tran = connection.BeginTransaction())
                        {
                            try
                            {
                                #region Save Article

                                if (string.IsNullOrEmpty(oldCategory.LatynUrl))
                                    oldCategory.LatynUrl = QarBaseController.GetDistinctLatynUrl(connection,
                                        nameof(Articlecategory), oldCategory.LatynUrl, oldCategory.Title, 0, "");

                                oldCategory.Title = oldCategory.Title.Length > 255
                                    ? oldCategory.Title.Substring(0, 250) + "..."
                                    : oldCategory.Title;

                                res = connection.Insert(new Articlecategory
                                {
                                    Language = "kz",
                                    Title = oldCategory.Title ?? string.Empty,
                                    LatynUrl = oldCategory.LatynUrl ?? string.Empty,
                                    OldLatynUrl = oldCategory.OldLatynUrl ?? string.Empty,
                                    ParentId = oldCategory.ParentId,
                                    DisplayOrder = oldCategory.DisplayOrder,
                                    BlockType = oldCategory.BlockType ?? string.Empty,
                                    IsHidden = (byte)(oldCategory.IsHidden == 1 ? 0 : 1),
                                    ShortDescription = oldCategory.ShortDescription ?? string.Empty,
                                    AddTime = oldCategory.AddTime,
                                    UpdateTime = oldCategory.AddTime,
                                    QStatus = 5
                                });

                                #endregion

                                tran.Commit();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, key);
                                tran.Rollback();
                            }
                        }
                    }
                }

                #endregion
            }).Wait();
        }

        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Курс мәнін сақтау +JobSaveCurrencyRate()

    public void JobSaveCurrencyRate()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            using (var connection = Utilities.GetOpenConnection())
            {
                var currencyList = connection.GetList<Currency>("where qStatus = 0").ToList();
                var currentTime = UnixTimeHelper.GetCurrentUnixTime();
                currencyList = HtmlAgilityPackHelper.GetMig_kzCurrencyRateList(currencyList);
                foreach (var currency in currencyList)
                {
                    currency.UpdateTime = currentTime;
                    connection.Update(currency);
                }
            }

            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCurrencyList));
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Job Generate Tag LatynUrl +JobGenerateTagLatynUrl()

    public void JobGenerateTagLatynUrl()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var takeCount = 1000;

                using (var connection = Utilities.GetOpenConnection())
                {
                    var querySql = "where latynUrl = '' order by id asc limit @takeCount";
                    object queryObj = new { takeCount };

                    var tagList = connection.GetList<Tag>(querySql, queryObj).ToList();

                    if (tagList.Count == 0) return;

                    var sql = string.Empty;

                    foreach (var tag in tagList)
                    {
                        var url = QarBaseController.GetDistinctLatynUrl(connection, nameof(Tag), "",
                            tag.Title, tag.Id, "");

                        sql += $"update tag set latynUrl = '{url}' where id = {tag.Id};";

                        if (sql.Length > 600)
                        {
                            connection.Execute(sql);
                            sql = string.Empty;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        connection.Execute(sql);
                        sql = string.Empty;
                    }
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobGenerateTagLatynUrl(), TimeSpan.FromSeconds(5));
        }
    }

    #endregion

    #region Sitemap ты сақтау +JobSaveSiteMap()

    public void JobSaveSiteMap()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var pageIndex = 1;
            var pageSize = 1000;
            var webRoot = _environment.WebRootPath;
            var siteMapFullPath = webRoot + "/sitemap.xml";
            if (File.Exists(siteMapFullPath)) File.Delete(siteMapFullPath);
            using (var connection = Utilities.GetOpenConnection())
            {
                while (true)
                {
                    var articleList = connection
                        .GetList<Article>("where qStatus = 0 and id >= @minId and id < @maxId order by Id",
                            new { minId = (pageIndex - 1) * pageSize, maxId = pageIndex * pageSize }).Select(x =>
                            new Article
                            {
                                Id = x.Id,
                                QStatus = x.QStatus,
                                UpdateTime = x.UpdateTime,
                                LatynUrl = x.LatynUrl,
                                ThumbnailUrl = x.ThumbnailUrl,
                                Title = x.Title
                            }).ToList();

                    if (articleList == null || articleList.Count == 0) break;
                    var articleSiteMapPath = $"article-sitemap{pageIndex++}.xml";
                    var articleSiteMapFullPath = webRoot + "/sitemap/xml/" + articleSiteMapPath;
                    if (File.Exists(articleSiteMapFullPath)) File.Delete(articleSiteMapFullPath);
                    var articleSiteMapDoc = SiteMapHalper.LoadXml(articleSiteMapFullPath, SiteMapType.ArticleSiteMap);
                    var lastTime = string.Empty;
                    foreach (var item in articleList)
                    {
                        if (item.QStatus == 0)
                        {
                            SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                                $"{QarSingleton.GetInstance().GetSiteUrl()}/kz/article/" + item.LatynUrl + ".html",
                                UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                                $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                                item.ThumbnailUrl.Replace("_small", "_big"), item.Title);
                            SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                                $"{QarSingleton.GetInstance().GetSiteUrl()}/latyn/article/" + item.LatynUrl + ".html",
                                UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                                $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                                item.ThumbnailUrl.Replace("_small", "_big"), Cyrl2ToteHelper.Cyrl2Tote(item.Title));
                            SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                                $"{QarSingleton.GetInstance().GetSiteUrl()}/tote/article/" + item.LatynUrl + ".html",
                                UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                                $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                                item.ThumbnailUrl.Replace("_small", "_big"), Cyrl2LatynHelper.Cyrl2Latyn(item.Title));
                        }

                        lastTime = UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime);
                    }

                    SiteMapHalper.SaveXml(articleSiteMapDoc, articleSiteMapFullPath);
                    var siteMapDoc = SiteMapHalper.LoadXml(siteMapFullPath, SiteMapType.SiteMap);
                    SiteMapHalper.AddOrUpdateSiteMapXml(siteMapDoc,
                        $"{QarSingleton.GetInstance().GetSiteUrl()}/sitemap/xml/" + articleSiteMapPath, lastTime);
                    SiteMapHalper.SaveXml(siteMapDoc, siteMapFullPath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectArticleMedia()

    public void JobSyncCollectArticleMedia()
    {
        var key = "JobSyncCollectArticleMedia";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                var takeCount = 100;
                using (var connection = Utilities.GetOpenConnection())
                {
                    var total = connection.RecordCount<Article>("where qStatus <> 1");
                    var leftCount = connection.RecordCount<Article>("where qStatus = 5");
                    var articleList = connection
                        .GetList<Article>("where qStatus = 5 order by id desc limit @takeCount ", new { takeCount })
                        .ToList();
                    foreach (var article in articleList)
                    {
                        var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                        var saveDirectoryPath = _environment.WebRootPath;
                        var newArticle =
                            await HtmlAgilityPackHelper.DownloadArticleMedia(article, baseUrl, saveDirectoryPath);
                        if (newArticle == null) continue;
                        // newArticle.UpdateTime = UnixTimeHelper.GetCurrentUnixTime()
                        newArticle.QStatus = 0;
                        connection.Update(newArticle);
                    }
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectArticleMedia");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectArticleMedia(), TimeSpan.FromSeconds(1));
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectOldServerArticleMedia()

    public void JobSyncCollectOldServerArticleMedia()
    {
        var key = "JobSyncCollectOldServerArticleMedia";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                var takeCount = 100;
                var maxId = QarSingleton.GetInstance().GetIntValue("maxArticleId");
                var articleList = new List<Article>();

                using (var connection = Utilities.GetOldServerDbConnection())
                {
                    articleList = connection
                        .GetList<Article>("where id <= @maxId and qStatus <> 1 order by id desc limit @takeCount ",
                            new { takeCount, maxId }).ToList();
                }

                QarSingleton.GetInstance().SetIntValue("maxArticleId", maxId - articleList.Count);

                var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                var saveDirectoryPath = _environment.WebRootPath;

                foreach (var article in articleList)
                    await HtmlAgilityPackHelper.DownloadArticleMedia(article, baseUrl, saveDirectoryPath);
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectOldServerArticleMedia");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectOldServerArticleMedia(), TimeSpan.FromSeconds(60));
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectAdminAvatar()

    public void JobSyncCollectAdminAvatar()
    {
        var key = "JobSyncCollectAdminAvatar";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                using (var connection = Utilities.GetOpenConnection())
                {
                    var adminList = connection.GetList<Admin>("where qStatus <> 1 ").ToList();
                    foreach (var admin in adminList)
                    {
                        var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                        var saveDirectoryPath = _environment.WebRootPath;

                        var newAdmin =
                            await HtmlAgilityPackHelper.DownloadAdminAvatar(admin, baseUrl, saveDirectoryPath);
                        if (newAdmin == null) continue;
                        newAdmin.QStatus = 0;
                        connection.Update(newAdmin);
                    }
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectAdminAvatar");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectAdminAvatar(), TimeSpan.FromMinutes(1));
        }
    }

    #endregion
}