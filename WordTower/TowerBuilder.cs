using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordTower.Models;

namespace WordTower;
public class WordPlacement
{
    public Word Word { get; set; }  // Изменено с string на Word
    public int Direction { get; set; } // 1: Z, 2: X, 3: Y
    public int[] Start { get; set; } = new int[3]; // [x, y, z]

    public override string ToString()
    {
        string dir = Direction switch
        {
            1 => "Z (вглубь)",
            2 => "X (горизонтально)",
            3 => "Y (вертикально)",
            _ => "Неизвестно"
        };
        return $"{Word.Text} (ID: {Word.Id}) at [{Start[0]}, {Start[1]}, {Start[2]}] направление: {dir}";
    }
}

public class TowerFloor
{
    public int ZLevel { get; set; }
    public List<WordPlacement> Placements { get; set; } = new List<WordPlacement>();
    public int Width { get; set; }
    public int Depth { get; set; }
}

public class SpatialAnalyzer
{
    public static bool CheckCollision(WordPlacement a, WordPlacement b)
    {
        var aCoords = GetLetterCoordinates(a);
        var bCoords = GetLetterCoordinates(b);

        foreach (var aKey in aCoords)
        {
            if (bCoords.TryGetValue(aKey.Key, out char bChar) && aKey.Value != bChar)
            {
                return true;
            }
        }
        return false;
    }

    public static Dictionary<string, char> GetLetterCoordinates(WordPlacement placement)
    {
        var coords = new Dictionary<string, char>();
        int x = placement.Start[0];
        int y = placement.Start[1];
        int z = placement.Start[2];

        foreach (char c in placement.Word.Text)
        {
            string key = $"{x},{y},{z}";
            if (!coords.ContainsKey(key))
                coords.Add(key, c);

            switch (placement.Direction)
            {
                case 1: z--; break;
                case 2: x++; break;
                case 3: y++; break;
            }
        }

        return coords;
    }

    public static bool CanPlaceWord(List<WordPlacement> existingPlacements, WordPlacement newPlacement, (int, int) fieldSize)
    {
        var newCoords = GetLetterCoordinates(newPlacement);

        foreach (var existing in existingPlacements)
        {
            var existingCoords = GetLetterCoordinates(existing);
            foreach (var coord in newCoords)
            {
                if (existingCoords.ContainsKey(coord.Key))
                {
                    return false;
                }
            }
        }

        int endX = newPlacement.Start[0];
        int endY = newPlacement.Start[1];

        if (newPlacement.Direction == 2) endX += newPlacement.Word.Text.Length;
        if (newPlacement.Direction == 3) endY += newPlacement.Word.Text.Length;

        if (endX > fieldSize.Item1 || endY > fieldSize.Item2)
        {
            return false;
        }

        return true;
    }
}

public class TowerBuilder
{
    private List<Word> words;
    private (int, int) fieldSize;
    private List<WordPlacement> placements = new List<WordPlacement>();
    private List<TowerFloor> floors = new List<TowerFloor>();

    public TowerBuilder(List<string> wordStrings, List<int> ids, (int, int) fieldSize)
    {
        if (wordStrings.Count != ids.Count)
            throw new ArgumentException("Количество слов и ID должно совпадать");

        this.words = wordStrings.Zip(ids, (text, id) => new Word(id, text)).ToList();
        this.fieldSize = fieldSize;
    }

    public (List<Word>, List<WordPlacement>, double) BuildTower()
    {
        int currentZ = 0;
        var currentFloor = new TowerFloor { ZLevel = currentZ };

        while (TryBuildFloor(currentFloor))
        {
            floors.Add(currentFloor);
            placements.AddRange(currentFloor.Placements);

            currentZ = floors.Min(f => f.ZLevel) - 2;
            currentFloor = new TowerFloor { ZLevel = currentZ };

            words = words.Skip(placements.Count).Concat(words.Take(placements.Count)).ToList();
        }

        double score = TowerScorer.CalculateScore(floors);
        return (words, placements, score);
    }

    // Остальные методы адаптируются для работы с Word вместо string
    private WordPlacement CreatePlacementFromIntersection(
    Word word, int direction, WordPlacement existing, int existingPos, int newPos)
    {
        // Вычисляем позицию пересечения
        int x = existing.Start[0];
        int y = existing.Start[1];
        int z = existing.Start[2];

        switch (existing.Direction)
        {
            case 1: z -= existingPos; break;
            case 2: x += existingPos; break;
            case 3: y += existingPos; break;
        }

        // Вычисляем стартовую позицию нового слова
        switch (direction)
        {
            case 1: z -= newPos; break;
            case 2: x -= newPos; break;
            case 3: y -= newPos; break;
        }

        // Проверка на отрицательные координаты (кроме Z)
        if (x < 0 || y < 0) return null;

        return new WordPlacement
        {
            Word = word,
            Direction = direction,
            Start = new int[] { x, y, z }
        };
    }

    private bool TryBuildFloor(TowerFloor floor)
    {
        bool placedAny = false;
        var remainingWords = new List<Word>(words); // Копируем текущий список слов

        // Фаза 1: Размещение по X
        placedAny |= TryPlaceDirection(floor, remainingWords, 2);

        // Фаза 2: Размещение по Y
        placedAny |= TryPlaceDirection(floor, remainingWords, 3);

        // Фаза 3: Размещение по Z (вглубь)
        placedAny |= TryPlaceDirection(floor, remainingWords, 1);

        // Обновляем оставшиеся слова
        words = remainingWords;
        return placedAny;
    }

    private WordPlacement FindPlacement(TowerFloor floor, Word word, int direction)
    {
        // Первое слово на первом этаже должно быть по X
        if (floor.ZLevel == 0 && floor.Placements.Count == 0)
        {
            if (direction == 2) // X direction
            {
                return new WordPlacement
                {
                    Word = word,
                    Direction = 2,
                    Start = new int[] { 0, 0, 0 }
                };
            }
            return null;
        }

        var existingPlacements = floor.Placements.Concat(GetWordsBelow(floor.ZLevel)).ToList();

        // Для размещения по Z проверяем пересечение с минимум 2 словами
        if (direction == 1 && floor.ZLevel != 0)
        {
            foreach (var existing1 in existingPlacements)
            {
                foreach (var existing2 in existingPlacements.Where(e => e != existing1))
                {
                    var placement = TryCreateZPlacement(word, existing1, existing2);
                    if (placement != null &&
                        SpatialAnalyzer.CanPlaceWord(existingPlacements, placement, fieldSize))
                    {
                        return placement;
                    }
                }
            }
            return null;
        }

        // Для размещения по X и Y
        foreach (var existing in existingPlacements)
        {
            for (int i = 0; i < word.Text.Length; i++)
            {
                for (int j = 0; j < existing.Word.Text.Length; j++)
                {
                    if (word.Text[i] == existing.Word.Text[j])
                    {
                        var newPlacement = CreatePlacementFromIntersection(
                            word, direction, existing, j, i);

                        if (newPlacement != null &&
                            SpatialAnalyzer.CanPlaceWord(existingPlacements, newPlacement, fieldSize))
                        {
                            return newPlacement;
                        }
                    }
                }
            }
        }

        return null;
    }

    private bool TryPlaceDirection(TowerFloor floor, List<Word> remainingWords, int direction)
    {
        bool placedAny = false;
        bool changed;

        do
        {
            changed = false;
            for (int i = 0; i < remainingWords.Count; i++)
            {
                var word = remainingWords[i];
                var placement = FindPlacement(floor, word, direction);

                if (placement != null)
                {
                    floor.Placements.Add(placement);
                    remainingWords.RemoveAt(i);
                    i--;
                    changed = true;
                    placedAny = true;
                    UpdateFloorDimensions(floor, placement);
                }
            }
        } while (changed && remainingWords.Count > 0);

        return placedAny;
    }

    private void UpdateFloorDimensions(TowerFloor floor, WordPlacement placement)
    {
        int endX = placement.Start[0];
        int endY = placement.Start[1];

        if (placement.Direction == 2) endX += placement.Word.Text.Length;
        if (placement.Direction == 3) endY += placement.Word.Text.Length;

        floor.Width = Math.Max(floor.Width, endX);
        floor.Depth = Math.Max(floor.Depth, endY);
    }

    private WordPlacement TryCreateZPlacement(Word word, WordPlacement existing1, WordPlacement existing2)
    {
        var intersections1 = FindPossibleIntersections(word.Text, existing1);
        var intersections2 = FindPossibleIntersections(word.Text, existing2);

        foreach (var (pos1, idx1) in intersections1)
        {
            foreach (var (pos2, idx2) in intersections2)
            {
                if (pos1 == pos2) continue;

                if (word.Text[idx1] == existing1.Word.Text[GetLetterIndex(existing1, pos1)] &&
                    word.Text[idx2] == existing2.Word.Text[GetLetterIndex(existing2, pos2)])
                {
                    int z = Math.Min(pos1.z, pos2.z) - 1;
                    return new WordPlacement
                    {
                        Word = word,
                        Direction = 1,
                        Start = new int[] { pos1.x, pos1.y, z }
                    };
                }
            }
        }
        return null;
    }

    private List<WordPlacement> GetWordsBelow(int zLevel)
    {
        return floors.Where(f => f.ZLevel > zLevel).SelectMany(f => f.Placements).ToList();
    }

    private List<((int x, int y, int z) pos, int wordIndex)> FindPossibleIntersections(
        string word, WordPlacement existing)
    {
        var intersections = new List<((int, int, int), int)>();

        for (int i = 0; i < word.Length; i++)
        {
            for (int j = 0; j < existing.Word.Text.Length; j++)
            {
                if (word[i] == existing.Word.Text[j])
                {
                    var pos = GetLetterPosition(existing, j);
                    intersections.Add((pos, i));
                }
            }
        }

        return intersections;
    }

    private (int x, int y, int z) GetLetterPosition(WordPlacement placement, int letterIndex)
    {
        int x = placement.Start[0];
        int y = placement.Start[1];
        int z = placement.Start[2];

        for (int i = 0; i < letterIndex; i++)
        {
            switch (placement.Direction)
            {
                case 1: z--; break;
                case 2: x++; break;
                case 3: y++; break;
            }
        }

        return (x, y, z);
    }

    private int GetLetterIndex(WordPlacement placement, (int x, int y, int z) pos)
    {
        int x = placement.Start[0];
        int y = placement.Start[1];
        int z = placement.Start[2];

        for (int i = 0; i < placement.Word.Text.Length; i++)
        {
            if (x == pos.x && y == pos.y && z == pos.z)
                return i;

            switch (placement.Direction)
            {
                case 1: z--; break;
                case 2: x++; break;
                case 3: y++; break;
            }
        }

        return -1;
    }
}

public class TowerScorer
{
    public static double CalculateScore(List<TowerFloor> floors)
    {
        double totalScore = 0;

        foreach (var floor in floors.OrderBy(f => f.ZLevel))
        {
            double floorScore = CalculateFloorScore(floor);
            double heightCoeff = Math.Abs(floor.ZLevel);
            totalScore += floorScore * heightCoeff;
        }

        return totalScore;
    }

    private static double CalculateFloorScore(TowerFloor floor)
    {
        double proportion = CalculateProportionCoeff(floor);
        double density = CalculateDensityCoeff(floor);
        return proportion * density;
    }

    private static double CalculateProportionCoeff(TowerFloor floor)
    {
        if (floor.Width == 0 || floor.Depth == 0) return 0;
        double min = Math.Min(floor.Width, floor.Depth);
        double max = Math.Max(floor.Width, floor.Depth);
        return min / max;
    }

    private static double CalculateDensityCoeff(TowerFloor floor)
    {
        int wordsX = floor.Placements.Count(p => p.Direction == 2);
        int wordsY = floor.Placements.Count(p => p.Direction == 3);
        return 1 + (wordsX + wordsY) / 4.0;
    }
}