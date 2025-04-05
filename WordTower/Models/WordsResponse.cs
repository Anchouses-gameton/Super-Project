namespace WordTower.Models;

public class WordsResponse
{
    public int[] MapSize { get; set; }
    public int NextTurnSec { get; set; }
    public DateTime RoundEndsAt { get; set; }
    public int ShuffleLeft { get; set; }
    public int Turn { get; set; }
    public List<int> UsedIndexes { get; set; }
    public List<Word> Words { get; set; }
}
