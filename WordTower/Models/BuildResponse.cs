namespace WordTower.Models;

public class BuildResponse
{
    public double Score { get; set; }
    public List<int>? UsedWordIds { get; set; }
}