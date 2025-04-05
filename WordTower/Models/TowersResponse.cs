namespace WordTower.Models;

public class TowersResponse
{
    public List<Tower> CompletedTowers { get; set; }
    public Tower CurrentTower { get; set; }
    public int TotalScore { get; set; }
}
