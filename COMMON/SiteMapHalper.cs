using System.Xml.Linq;

namespace COMMON;

public enum SiteMapType
{
    SiteMap = 0,
    ArticleSiteMap = 1,
    TagSiteMap = 2
}

public class SiteMapHalper
{
    private static readonly XNamespace XNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
    private static readonly XNamespace XImage = XNamespace.Get("http://www.google.com/schemas/sitemap-image/1.1");

    #region SiteMap XML құру + CreateXML(string filePath , SiteMapType type)
    private static void CreateXml(string filePath, SiteMapType type)
    {
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        if (type == SiteMapType.SiteMap)
        {
            XDocument xDoc = new XDocument();
            XProcessingInstruction pi = new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"/sitemap/templates/xml-sitemapindex.xsl\"");
            xDoc.Add(pi);
            XElement xUrlset = new XElement(XNamespace + "sitemapindex");
            xDoc.Add(xUrlset);
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                xDoc.Save(fileStream);
            }
        }
        else if (type == SiteMapType.ArticleSiteMap)
        {
            XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            XNamespace schemaLocation = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");
            XDocument xDoc = new XDocument();
            XProcessingInstruction pi = new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"/sitemap/templates/xml-sitemap.xsl\"");
            xDoc.Add(pi);
            XElement xUrlset = new XElement(XNamespace + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", "http://www.w3.org/1999/xhtml"),
                new XAttribute(XNamespace.Xmlns + "image", XImage),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation", schemaLocation));
            xDoc.Add(xUrlset);
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                xDoc.Save(fileStream);
            }
        }
        else if (type == SiteMapType.TagSiteMap)
        {
            XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            XNamespace schemaLocation = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");
            XDocument xDoc = new XDocument();
            XProcessingInstruction pi = new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"/sitemap/templates/xml-sitemap.xsl\"");
            xDoc.Add(pi);
            XElement xUrlset = new XElement(XNamespace + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", "http://www.w3.org/1999/xhtml"),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation", schemaLocation));
            xDoc.Add(xUrlset);
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                xDoc.Save(fileStream);
            }
        }
    }
    #endregion

    #region XML XDocument алу + LoadXML(string filePath, SiteMapType type)
    public static XDocument LoadXml(string filePath, SiteMapType type)
    {
        XDocument xDoc = null;
        try
        {
            xDoc = XDocument.Load(filePath);
        }
        catch
        {
            CreateXml(filePath, type);
            xDoc = XDocument.Load(filePath);
        }
        return xDoc;
    }
    #endregion

    #region XDocument сақтау +SaveXML(XDocument xDoc,string filePath)
    public static void SaveXml(XDocument xDoc, string filePath)
    {
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }
        using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
        {
            xDoc.Save(fileStream);
        }
    }
    #endregion

    #region Қосылған ең соңғы мақаланың idName ын алу + GetLastAddArticleIdName(XDocument xDoc)
    public static string GetLastAddArticleIdName(XDocument xDoc)
    {
        var urlset = xDoc.Root;
        if (urlset == null || urlset.Name.LocalName != "urlset")
        {
            return string.Empty;
        }

        XElement urlObj = urlset.Elements(XNamespace + "url").LastOrDefault();
        if (urlObj != null)
        {
            XElement locObj = urlObj.Element(XNamespace + "loc");
            string locValue = locObj.Value;
            Uri uri = new Uri(locValue);
            int index = uri.LocalPath.LastIndexOf("/article/");
            return uri.LocalPath.Substring(index + 9);
        }
        return string.Empty;
    }
    #endregion

    #region Ең соңғы қосылған tagты алу + GetLastAddTagName(XDocument xDoc)
    public static string GetLastAddTagName(XDocument xDoc)
    {
        var urlset = xDoc.Root;
        if (urlset == null || urlset.Name.LocalName != "urlset")
        {
            return string.Empty;
        }

        XElement urlObj = urlset.Elements(XNamespace + "url").LastOrDefault();
        if (urlObj != null)
        {
            XElement locObj = urlObj.Element(XNamespace + "loc");
            string locValue = locObj.Value;
            Uri uri = new Uri(locValue);
            int index = uri.LocalPath.LastIndexOf("/tag/");
            return uri.LocalPath.Substring(index + 5);
        }
        return string.Empty;
    }
    #endregion

    #region Мақала SiteMap ға қосыу немесе озгерту +AddOrUpdateArticleLinkToSiteMapXML(XDocument xDoc, string loc, string lastMod, string changeFreq, string priority, string imageLog, string imageTitle)
    public static XDocument AddOrUpdateArticleLinkToSiteMapXml(XDocument xDoc, string loc, string lastMod, string changeFreq, string priority, string imageLog, string imageTitle)
    {
        var urlset = xDoc.Root;
        loc = loc.Trim().ToLower();
        XElement urlObj = urlset.Elements(XNamespace + "url").FirstOrDefault(x => x.Element(XNamespace + "loc").Value.Equals(loc));
        if (urlObj != null)
        {
            urlObj.Element(XNamespace + "loc").SetValue(loc);
            urlObj.Element(XNamespace + "lastmod").SetValue(lastMod);
            urlObj.Element(XNamespace + "changefreq").SetValue(changeFreq);
            urlObj.Element(XNamespace + "priority").SetValue(priority);
            XElement imageXElement = urlObj.Element(XImage + "image");
            if (imageXElement != null)
            {
                imageXElement.Element(XImage + "loc").SetValue(imageLog);
                imageXElement.Element(XImage + "title").SetValue(imageTitle);
            }
        }
        else
        {
            urlset.Add(new XElement(XNamespace + "url",
              new XElement(XNamespace + "loc", loc),
              new XElement(XNamespace + "lastmod", lastMod),
              new XElement(XNamespace + "changefreq", changeFreq),
              new XElement(XNamespace + "priority", priority),
              new XElement(XImage + "image",
                            new XElement(XImage + "loc", imageLog),
                            new XElement(XImage + "title", imageTitle))
              ));
        }
        return xDoc;
    }
    #endregion

    #region Мақала tag ын қосыу немесе озгерту +AddOrUpdateTagLinkToSiteMapXML(XDocument xDoc, string loc, string lastMod, string changeFreq, string priority)
    public static XDocument AddOrUpdateTagLinkToSiteMapXml(XDocument xDoc, string loc, string lastMod, string changeFreq, string priority)
    {
        var urlset = xDoc.Root;
        loc = loc.Trim().ToLower();
        XElement urlObj = urlset.Elements(XNamespace + "url").FirstOrDefault(x => x.Element(XNamespace + "loc").Value.Equals(loc));
        if (urlObj != null)
        {
            urlObj.Element(XNamespace + "loc").SetValue(loc);
            urlObj.Element(XNamespace + "lastmod").SetValue(lastMod);
            urlObj.Element(XNamespace + "changefreq").SetValue(changeFreq);
            urlObj.Element(XNamespace + "priority").SetValue(priority);
        }
        else
        {
            urlset.Add(new XElement(XNamespace + "url",
              new XElement(XNamespace + "loc", loc),
              new XElement(XNamespace + "lastmod", lastMod),
              new XElement(XNamespace + "changefreq", changeFreq),
              new XElement(XNamespace + "priority", priority)));
        }
        return xDoc;
    }
    #endregion

    #region Мақала SiteMap ынаношыру + DeleteArticleLinkToSiteMapXML(XDocument xDoc, string loc)
    public static XDocument DeleteArticleLinkToSiteMapXml(XDocument xDoc, string loc)
    {
        var urlset = xDoc.Root;
        if (urlset == null || urlset.Name.LocalName != "urlset")
        {
            return xDoc;
        }

        List<XElement> urlObjList = urlset.Elements(XNamespace + "url").Where(x => x.Element(XNamespace + "loc").Value.Contains(loc)).ToList();
        if (urlObjList != null && urlObjList.Count > 0)
        {
            urlObjList.Remove();
        }
        return xDoc;
    }
    #endregion

    #region Sitemap Index тегі ең соңғы Sitemap құжатының сілтемесін алу + GetLastSiteMapPath(XDocument xDoc, SiteMapType type)
    public static string GetLastSiteMapPath(XDocument xDoc, SiteMapType type)
    {
        var siteMapIndex = xDoc.Root;
        XElement sitemapObj = null;
        if (type == SiteMapType.ArticleSiteMap)
        {
            sitemapObj = siteMapIndex.Elements(XNamespace + "sitemap").LastOrDefault(x => x.Element(XNamespace + "loc").Value.Contains("/article/"));
        }
        else if (type == SiteMapType.TagSiteMap)
        {
            sitemapObj = siteMapIndex.Elements(XNamespace + "sitemap").LastOrDefault(x => x.Element(XNamespace + "loc").Value.Contains("/tag/"));
        }
        if (sitemapObj != null)
        {
            return sitemapObj.Element(XNamespace + "loc").Value;
        }
        return string.Empty;
    }
    #endregion

    #region Sitemap Index құжатына қосыу немесе озгерту + AddOrUpdateSiteMapXML(XDocument xDoc, string loc,string lastMod)
    public static XDocument AddOrUpdateSiteMapXml(XDocument xDoc, string loc, string lastMod)
    {
        var siteMapIndex = xDoc.Root;
        XElement sitemapObj = siteMapIndex.Elements(XNamespace + "sitemap").FirstOrDefault(x => x.Element(XNamespace + "loc").Value.Equals(loc));
        if (sitemapObj != null)
        {
            sitemapObj.Element(XNamespace + "loc").SetValue(loc);
            sitemapObj.Element(XNamespace + "lastmod").SetValue(lastMod);
        }
        else
        {
            siteMapIndex.Add(new XElement(XNamespace + "sitemap",
            new XElement(XNamespace + "loc", loc),
            new XElement(XNamespace + "lastmod", lastMod)));
        }
        return xDoc;
    }
    #endregion
}