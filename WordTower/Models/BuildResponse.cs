namespace WordTower.Models;

public class BuildResponse
{
    public int Score { get; set; }  // Пример поля (уточнить!)
    public List<int> UsedWordIds { get; set; }
}