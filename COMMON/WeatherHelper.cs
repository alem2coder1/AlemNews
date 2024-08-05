using Newtonsoft.Json.Linq;

namespace COMMON;

public class WeatherHelper
{
    private readonly static string ApiKey = "7f714ddf252a38653c839e980a020779";

    public static async Task<string> GetCurrentWeatherAsync(string city)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                string url =
                    $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={ApiKey}&units=metric";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);
                double temp = (double)json["main"]["temp"];
                return Math.Round(temp, 0) + "\u2103";
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "COMMON:GetCurrentWeatherAsync");
            return "";
        }
    }
}