using System.Net.Http.Headers;

namespace COMMON;

public class LanguagePackHelper
{
    private static readonly string BaseAddress = "https://www.sozdikqor.org";
    public static string GetLanguagePackJsonString()
    {
        var result = string.Empty;
        try
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseAddress);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/query/all");
                var response = client.SendAsync(request).Result;
                result = response.Content.ReadAsStringAsync().Result;
            }
            if (!string.IsNullOrEmpty(AppContext.BaseDirectory) && System.IO.Directory.Exists(AppContext.BaseDirectory))
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "language_pack.txt");
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllText(path, result);
            }
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(AppContext.BaseDirectory) && System.IO.Directory.Exists(AppContext.BaseDirectory))
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "language_pack.txt");
                if (System.IO.File.Exists(path))
                {
                    result = System.IO.File.ReadAllText(path);
                }
                else
                {
                    Serilog.Log.Error(ex, "COMMON:GetLanguagePackJsonString");
                }
            }
            else
            {
                Serilog.Log.Error(ex, "COMMON:GetLanguagePackJsonString");
            }
        }
        return result;
    }
}