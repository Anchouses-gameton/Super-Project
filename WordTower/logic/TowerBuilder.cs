using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WordTower
{
    public class WordPlacement
    {
        public string Word { get; set; }
        public int Id { get; set; }
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
            return $"{Word} (ID: {Id}) at [{Start[0]}, {Start[1]}, {Start[2]}] направление: {dir}";
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

            // Проверяем пересечения с разными буквами
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

            foreach (char c in placement.Word)
            {
                string key = $"{x},{y},{z}";
                if (!coords.ContainsKey(key))
                    coords.Add(key, c);

                switch (placement.Direction)
                {
                    case 1: z--; break; // Z direction
                    case 2: x++; break; // X direction
                    case 3: y++; break; // Y direction
                }
            }

            return coords;
        }

        public static bool CanPlaceWord(List<WordPlacement> existing, WordPlacement newPlacement, (int, int) fieldSize)
        {
            // Проверка границ поля
            if (newPlacement.Direction == 2 && newPlacement.Start[0] + newPlacement.Word.Length > fieldSize.Item1)
                return false;
            if (newPlacement.Direction == 3 && newPlacement.Start[1] + newPlacement.Word.Length > fieldSize.Item2)
                return false;
            if (newPlacement.Start[2] < 0 && newPlacement.Direction == 1 &&
                newPlacement.Start[2] - newPlacement.Word.Length < -100) // Ограничение по глубине
                return false;

            // Для первого слова разрешаем размещение без пересечений
            if (existing.Count == 0)
                return true;

            // Проверка что слово пересекается хотя бы с одним существующим
            bool intersects = false;
            foreach (var existingPlacement in existing)
            {
                var intersection = GetIntersectionPoints(existingPlacement, newPlacement);
                if (intersection.Count > 0)
                {
                    intersects = true;
                    // Проверяем что пересекающиеся буквы совпадают
                    foreach (var point in intersection)
                    {
                        char c1 = GetLetterAtPosition(existingPlacement, point);
                        char c2 = GetLetterAtPosition(newPlacement, point);
                        if (c1 != c2)
                            return false;
                    }
                }
            }

            if (!intersects)
                return false;

            // Проверка минимального расстояния между параллельными словами
            foreach (var existingPlacement in existing)
            {
                if (existingPlacement.Direction == newPlacement.Direction)
                {
                    if (AreParallelTooClose(existingPlacement, newPlacement))
                        return false;
                }
            }

            return true;
        }

        private static bool AreParallelTooClose(WordPlacement a, WordPlacement b)
        {
            if (a.Direction != b.Direction) return false;

            int minDistance = 1;

            if (a.Direction == 2) // X direction
            {
                if (a.Start[1] == b.Start[1] && a.Start[2] == b.Start[2] &&
                    Math.Abs(a.Start[0] - b.Start[0]) < minDistance)
                    return true;
            }
            else if (a.Direction == 3) // Y direction
            {
                if (a.Start[0] == b.Start[0] && a.Start[2] == b.Start[2] &&
                    Math.Abs(a.Start[1] - b.Start[1]) < minDistance)
                    return true;
            }
            else if (a.Direction == 1) // Z direction
            {
                if (a.Start[0] == b.Start[0] && a.Start[1] == b.Start[1] &&
                    Math.Abs(a.Start[2] - b.Start[2]) < minDistance)
                    return true;
            }

            return false;
        }

        private static List<(int, int, int)> GetIntersectionPoints(WordPlacement a, WordPlacement b)
        {
            var aCoords = GetLetterCoordinates(a);
            var bCoords = GetLetterCoordinates(b);
            var intersections = new List<(int, int, int)>();

            foreach (var aKey in aCoords.Keys)
            {
                if (bCoords.ContainsKey(aKey))
                {
                    var parts = aKey.Split(',');
                    intersections.Add((
                        int.Parse(parts[0]),
                        int.Parse(parts[1]),
                        int.Parse(parts[2])
                    ));
                }
            }

            return intersections;
        }

        private static char GetLetterAtPosition(WordPlacement placement, (int x, int y, int z) point)
        {
            int x = placement.Start[0];
            int y = placement.Start[1];
            int z = placement.Start[2];

            for (int i = 0; i < placement.Word.Length; i++)
            {
                if (x == point.x && y == point.y && z == point.z)
                    return placement.Word[i];

                switch (placement.Direction)
                {
                    case 1: z--; break;
                    case 2: x++; break;
                    case 3: y++; break;
                }
            }

            return '\0';
        }
    }

    public class TowerBuilder
    {
        private List<string> words;
        private List<int> ids;
        private (int, int) fieldSize;
        private List<WordPlacement> placements = new List<WordPlacement>();
        private List<TowerFloor> floors = new List<TowerFloor>();

        public TowerBuilder(List<string> words, List<int> ids, (int, int) fieldSize)
        {
            if (words.Count != ids.Count)
                throw new ArgumentException("Количество слов и ID должно совпадать");

            this.words = new List<string>(words);
            this.ids = new List<int>(ids);
            this.fieldSize = fieldSize;
        }

        public (List<string>, List<int>, List<WordPlacement>, double) BuildTower()
        {
            int currentZ = 0;
            var currentFloor = new TowerFloor { ZLevel = currentZ };

            while (TryBuildFloor(currentFloor))
            {
                floors.Add(currentFloor);
                placements.AddRange(currentFloor.Placements);

                // Переход на следующий этаж с учетом зазора
                currentZ = floors.Min(f => f.ZLevel) - 2;
                currentFloor = new TowerFloor { ZLevel = currentZ };

                // Перемещаем неразмещенные слова в конец очереди
                words = words.Skip(placements.Count).Concat(words.Take(placements.Count)).ToList();
                ids = ids.Skip(placements.Count).Concat(ids.Take(placements.Count)).ToList();
            }

            var placedWords = placements.Select(p => p.Word).ToList();
            var placedIds = placements.Select(p => p.Id).ToList();
            double score = TowerScorer.CalculateScore(floors);

            return (placedWords, placedIds, placements, score);
        }

        private bool TryBuildFloor(TowerFloor floor)
        {
            bool placedAny = false;
            var remainingWords = words.Zip(ids, (w, id) => (w, id)).ToList();

            // Фаза 1: Размещение по X
            placedAny |= TryPlaceDirection(floor, remainingWords, 2);

            // Фаза 2: Размещение по Y
            placedAny |= TryPlaceDirection(floor, remainingWords, 3);

            // Фаза 3: Размещение по Z (вглубь)
            placedAny |= TryPlaceDirection(floor, remainingWords, 1);

            // Обновляем оставшиеся слова
            words = remainingWords.Select(x => x.Item1).ToList();
            ids = remainingWords.Select(x => x.Item2).ToList();

            return placedAny;
        }

        private bool TryPlaceDirection(TowerFloor floor, List<(string, int)> remainingWords, int direction)
        {
            bool placedAny = false;
            bool changed;

            do
            {
                changed = false;
                for (int i = 0; i < remainingWords.Count; i++)
                {
                    var (word, id) = remainingWords[i];
                    var placement = FindPlacement(floor, word, id, direction);

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

        private WordPlacement FindPlacement(TowerFloor floor, string word, int id, int direction)
        {
            // Первое слово на первом этаже должно быть по X
            if (floor.ZLevel == 0 && floor.Placements.Count == 0)
            {
                if (direction == 2) // X direction
                {
                    return new WordPlacement
                    {
                        Word = word,
                        Id = id,
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
                        var placement = TryCreateZPlacement(word, id, existing1, existing2);
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
                for (int i = 0; i < word.Length; i++)
                {
                    for (int j = 0; j < existing.Word.Length; j++)
                    {
                        if (word[i] == existing.Word[j])
                        {
                            var newPlacement = CreatePlacementFromIntersection(
                                word, id, direction, existing, j, i);

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

        private WordPlacement TryCreateZPlacement(string word, int id, WordPlacement existing1, WordPlacement existing2)
        {
            // Находим возможные точки пересечения с двумя словами
            var intersections1 = FindPossibleIntersections(word, existing1);
            var intersections2 = FindPossibleIntersections(word, existing2);

            foreach (var (pos1, idx1) in intersections1)
            {
                foreach (var (pos2, idx2) in intersections2)
                {
                    if (pos1 == pos2) continue; // Одна и та же точка

                    // Проверяем что буквы совпадают
                    if (word[idx1] == existing1.Word[GetLetterIndex(existing1, pos1)] &&
                        word[idx2] == existing2.Word[GetLetterIndex(existing2, pos2)])
                    {
                        // Создаем размещение по Z
                        int z = Math.Min(pos1.z, pos2.z) - 1;
                        return new WordPlacement
                        {
                            Word = word,
                            Id = id,
                            Direction = 1,
                            Start = new int[] { pos1.x, pos1.y, z }
                        };
                    }
                }
            }

            return null;
        }

        private List<((int x, int y, int z) pos, int wordIndex)> FindPossibleIntersections(
            string word, WordPlacement existing)
        {
            var intersections = new List<((int, int, int), int)>();

            for (int i = 0; i < word.Length; i++)
            {
                for (int j = 0; j < existing.Word.Length; j++)
                {
                    if (word[i] == existing.Word[j])
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

            for (int i = 0; i < placement.Word.Length; i++)
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

        private List<WordPlacement> GetWordsBelow(int zLevel)
        {
            return floors.Where(f => f.ZLevel > zLevel).SelectMany(f => f.Placements).ToList();
        }

        private WordPlacement CreatePlacementFromIntersection(
            string word, int id, int direction, WordPlacement existing, int existingPos, int newPos)
        {
            int x = existing.Start[0];
            int y = existing.Start[1];
            int z = existing.Start[2];

            // Вычисляем позицию пересечения
            switch (existing.Direction)
            {
                case 1: z -= existingPos; break;
                case 2: x += existingPos; break;
                case 3: y += existingPos; break;
            }

            // Вычисляем стартовую позицию
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
                Id = id,
                Direction = direction,
                Start = new int[] { x, y, z }
            };
        }

        private void UpdateFloorDimensions(TowerFloor floor, WordPlacement placement)
        {
            int endX = placement.Start[0];
            int endY = placement.Start[1];

            if (placement.Direction == 2) endX += placement.Word.Length;
            if (placement.Direction == 3) endY += placement.Word.Length;

            floor.Width = Math.Max(floor.Width, endX);
            floor.Depth = Math.Max(floor.Depth, endY);
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
}