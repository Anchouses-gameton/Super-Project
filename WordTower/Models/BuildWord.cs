namespace WordTower.Models;

public class BuildWord
{
    public int Id { get; set; }
    public int Dir { get; set; } // 1=Z, 2=X, 3=Y
    public int[] Pos { get; set; } // [x, y, z]
}
