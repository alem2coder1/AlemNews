namespace MODEL;

public partial class Astanaweather
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Temperature { get; set; }
    public int UpdateTime { get; set; }
    public byte QStatus { get; set; }
}