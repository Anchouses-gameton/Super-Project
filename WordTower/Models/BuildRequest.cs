namespace WordTower.Models;

public class BuildRequest
{
    public bool Done { get; set; }
    public List<BuildWord> Words { get; set; } = new List<BuildWord>();
}
