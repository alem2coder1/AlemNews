namespace MODEL;
public partial class Weather
{
    public int Id { get; set; }
    public string CityName { get; set; }
    public string Temperature { get; set; }
    public int UpdateTime { get; set; }
    public byte QStatus { get; set; }

}