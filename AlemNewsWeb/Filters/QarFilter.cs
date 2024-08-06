using System.Text;
using COMMON;
using MODEL;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using AlemNewsWeb.Caches;

namespace AlemNewsWeb.Filters;

public class QarFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller controller)
        {
            var memoryCache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            var language = (context.HttpContext.Request.RouteValues["culture"] ?? string.Empty) as string;
            controller.ViewData["language"] = language;
            var languageList = QarCache.GetLanguageList(memoryCache);
            controller.ViewData["languageList"] = languageList;
            controller.ViewData["additionalContentList"] = QarCache.GetAdditionalContentList(memoryCache, language);
            controller.ViewData["uniqueSeoCode"] = languageList.FirstOrDefault(x => x.LanguageCulture.Equals(language))?.UniqueSeoCode ?? "kk";
            controller.ViewData["menuList"] = QarCache.GetCategoryList(memoryCache, language).Where(x => x.IsHidden == 0).ToList();
            controller.ViewData["allCategoryList"] = QarCache.GetCategoryList(memoryCache, language);
            controller.ViewData["adminList"] = QarCache.GetAllAdminList(memoryCache);
            controller.ViewData["partnerList"] = QarCache.GetPartnerList(memoryCache);
            controller.ViewData["weatherList"] = QarCache.GetWeatherList(memoryCache);
            controller.ViewData["topArticleList"] = QarCache.GetTopArticleList(memoryCache, language, 3, 4);
            var fileInputLanguage = string.Empty;
            var tinyMceLanguage = string.Empty;
            var dateTimePickerLanguage = string.Empty;
            switch (language)
            {
                case "tote":
                    {
                        fileInputLanguage = "kz-tote";
                        tinyMceLanguage = "kz";
                        dateTimePickerLanguage = "kz";
                    }
                    break;
                case "kz":
                    {
                        fileInputLanguage = "kz";
                        tinyMceLanguage = "kk";
                        dateTimePickerLanguage = "kz";
                    }
                    break;
                case "ru":
                    {
                        fileInputLanguage = "ru";
                        tinyMceLanguage = "ru";
                        dateTimePickerLanguage = "ru";
                    }
                    break;
                case "zh-cn":
                    {
                        fileInputLanguage = "zh";
                        tinyMceLanguage = "zh_CN";
                        dateTimePickerLanguage = "zh";
                    }
                    break;
            }

            controller.ViewData["fileInputLanguage"] = fileInputLanguage;
            controller.ViewData["tinyMCELanguage"] = tinyMceLanguage;
            controller.ViewData["dateTimePickerLanguage"] = dateTimePickerLanguage;
            controller.ViewData["controllerName"] = context.HttpContext.Request.RouteValues["controller"].ToString();
            controller.ViewData["actionName"] = context.HttpContext.Request.RouteValues["action"].ToString();
            controller.ViewData["languageIsoCode"] = QarCache.GetLanguageList(memoryCache).FirstOrDefault(x => x.LanguageCulture.Equals(language))?.IsoCode;
            controller.ViewData["siteSetting"] = QarCache.GetSiteSetting(memoryCache);
            controller.ViewData["currencyList"] = QarCache.GetCurrencyList(memoryCache);
            controller.ViewData["host"] = $"https://{context.HttpContext.Request.Host.Host.ToLower()}";

            // string locationIP = context.HttpContext.Request.Headers["X-Real-IP"].Count > 0 ? context.HttpContext.Request.Headers["X-Real-IP"] : context.HttpContext.Connection.RemoteIpAddress.ToString();
            // string currentCity = QarSingleton.GetInstance().GetCityByIP(locationIP);
            // currentCity = string.IsNullOrWhiteSpace(currentCity) ? "Almaty" : currentCity;
            // controller.ViewData["currentCity"] = currentCity;
            // controller.ViewData["currentCityTemp"] = QarSingleton.GetInstance().GetCityTemp(currentCity);

            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                controller.ViewData["realName"] = context.HttpContext.User.Identity.RealName();
                controller.ViewData["adminId"] = context.HttpContext.User.Identity.AdminId();
                controller.ViewData["roleNames"] = context.HttpContext.User.Identity.RoleNames();
                controller.ViewData["skinName"] = context.HttpContext.User.Identity.SkinName();
                var avatarUrl = context.HttpContext.User.Identity.AvatarUrl();
                controller.ViewData["avatarUrl"] = string.IsNullOrEmpty(avatarUrl) ? "/images/default_avatar.png" : avatarUrl;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var controllerName = context.RouteData.Values["controller"].ToString().ToLower();
        if (!controllerName.Equals("admin") && !controllerName.Equals("catalog") && !controllerName.Equals("site") && !controllerName.Equals("qarbase"))
        {
            var actionResult = context.Result;
            var path = context.HttpContext.Request.Path.ToString().ToLower();
            var language = (context.HttpContext.Request.RouteValues["culture"] ?? string.Empty) as string;
            language = string.IsNullOrEmpty(language) ? "kz" : language;
            var qUrl = ""; //context.HttpContext.Request.Scheme + "://" + language + "." + qHost;
            if (actionResult is ViewResult)
            {
                var viewResult = actionResult as ViewResult;
                if (language.Equals("latyn") || language.Equals("tote"))
                {
                    var services = context.HttpContext.RequestServices;
                    var executor = services.GetRequiredService<IActionResultExecutor<ViewResult>>() as ViewResultExecutor;
                    var option = services.GetRequiredService<IOptions<MvcViewOptions>>();
                    var result = executor.FindView(context, viewResult);
                    result.EnsureSuccessful(originalLocations: null);
                    var view = result.View;
                    var builder = new StringBuilder();

                    using (var writer = new StringWriter(builder))
                    {
                        var viewContext = new ViewContext(
                            context,
                            view,
                            viewResult.ViewData,
                            viewResult.TempData,
                            writer,
                            option.Value.HtmlHelperOptions);

                        view.RenderAsync(viewContext).GetAwaiter().GetResult();
                        writer.Flush();
                    }

                    var html = builder.ToString();
                    StringValues sValue = string.Empty;
                    var userAgent = "pc";
                    html = HtmlAgilityPackHelper.ConvertHtmlTextNode(html, language, userAgent, qUrl);
                    var contentresult = new ContentResult();
                    contentresult.Content = html;
                    contentresult.ContentType = "text/html";
                    context.Result = contentresult;
                }
            }
            else if (actionResult is JsonResult)
            {
                var jsonResult = actionResult as JsonResult;
                if (language.Equals("latyn") || language.Equals("tote"))
                {
                    if (jsonResult.Value is AjaxMsgModel model)
                    {
                        switch (language)
                        {
                            case "latyn":
                                {
                                    model.Message = Cyrl2LatynHelper.Cyrl2Latyn(model.Message);
                                }
                                break;
                            case "tote":
                                {
                                    model.Message = Cyrl2ToteHelper.Cyrl2Tote(model.Message);
                                }
                                break;
                        }

                        jsonResult.Value = model;
                    }
                }
            }
        }
    }
}