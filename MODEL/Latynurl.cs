namespace MODEL;

public partial class Latynurl
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Language { get; set; }
    public string LatynUrl { get; set; }
    public byte QStatus { get; set; }
    public string TableName { get; set; }
}
