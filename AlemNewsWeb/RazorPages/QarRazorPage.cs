using COMMON;
using Microsoft.AspNetCore.Mvc.Razor;
using MODEL;
using AlemNewsWeb.Caches;

namespace AlemNewsWeb;
public abstract class QarRazorPage<TModel> : RazorPage<TModel>
{
    IWebHostEnvironment _environment = null;

    public string T(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey)) return localKey;
        var memoryCache = ViewContext.HttpContext.RequestServices.GetService<IMemoryCache>();
        return QarCache.GetLanguageValue(memoryCache, localKey, CurrentLanguage);
    }

    public List<T> QarList<T>(string vdName) where T : new()
    {
        if (ViewData[vdName] is List<T> value)
        {
            return value;
        }

        return new List<T>();
    }

    public T QarModel<T>(string vdName)
    {
        if (ViewData[vdName] is T value)
        {
            return value;
        }

        return default;
    }

    public string GetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        _environment ??= ViewContext.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

        if (_environment == null || _environment.IsDevelopment())
        {
            return url.StartsWith("http") ? url : SiteUrl + url;
        }

        return url;
    }

    public Additionalcontent GetAc(string additionalType)
    {
        var additionalContentList = QarList<Additionalcontent>("additionalContentList");
        return additionalContentList.FirstOrDefault(x => x.AdditionalType.Equals(additionalType, StringComparison.OrdinalIgnoreCase));
    }

    public string CurrentLanguage => (ViewData["language"] ?? string.Empty) as string;

    public string CurrentTheme
    {
        get => QarSingleton.GetInstance().GetSiteTheme();
    }

    public string SiteUrl
    {
        get => QarSingleton.GetInstance().GetSiteUrl();
    }

    // public string SiteUrl => (ViewData["siteUrl"] ?? string.Empty) as string;
    public string Query => (ViewData["query"] ?? string.Empty) as string;
    public string ControllerName => (ViewData["controllerName"] ?? string.Empty) as string;
    public string ActionName => (ViewData["actionName"] ?? string.Empty) as string;
    public string SkinName => (ViewData["skinName"] ?? string.Empty) as string;
    public string Title => (ViewData["title"] ?? string.Empty) as string;
    public string ac => (ViewData["additionalContentList"] ?? string.Empty) as string;
    public List<Articlecategory> CategoryList => QarList<Articlecategory>("allCategoryList");
    public List<Language> LanguageList => (ViewData["languageList"] ?? new List<Language>()) as List<Language>;

    public List<Multilanguage> MultiLanguageList =>
        (ViewData["multiLanguageList"] ?? new List<Multilanguage>()) as List<Multilanguage>;
    public Additionalcontent GetAC(string additionalType)
    {
        var additionalContentList = QarList<Additionalcontent>("additionalContentList");
        return additionalContentList.FirstOrDefault(x => x.AdditionalType.Equals(additionalType, StringComparison.OrdinalIgnoreCase));
    }
    public Sitesetting SiteSetting => ViewData["siteSetting"] != null ? (ViewData["siteSetting"] as Sitesetting) : null;
    public bool CanView => Convert.ToBoolean(ViewData["canView"] ?? false);
    public bool CanCreate => Convert.ToBoolean(ViewData["canCreate"] ?? false);
    public bool CanEdit => Convert.ToBoolean(ViewData["canEdit"] ?? false);
    public bool CanDelete => Convert.ToBoolean(ViewData["canDelete"] ?? false);
}