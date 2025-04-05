using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Первая_попытка_парсинга__консоль
{
    public class GameState
    {
        // Основные параметры игры
        public Vector3 MapSize { get; set; }
        public int CurrentTurn { get; set; }
        public int NextTurnSec { get; set; }
        public DateTime RoundEndsAt { get; set; }
        public int ShuffleLeft { get; set; }
        public int TotalScore { get; private set; }

        // Коллекции данных
        private readonly Dictionary<int, Word> _wordsDictionary;
        private readonly HashSet<int> _usedWordIds = new();
        private readonly List<Tower> _towers = new();

        // Публичные интерфейсы
        public IReadOnlyList<Tower> Towers => _towers.AsReadOnly();
        public IReadOnlyCollection<Word> AvailableWords => _wordsDictionary.Values;
        public int RemainingWords => _wordsDictionary.Count - _usedWordIds.Count;


        public GameState()
        {
            _wordsDictionary = new Dictionary<int, Word>();
        }

        // Основные методы
        public void ClearWords()
        {
            _wordsDictionary.Clear();
            _usedWordIds.Clear();
            Word.ResetIdCounter(); // Сбрасываем автоинкрементный ID
        }

        public void AddWord(Word word)
        {
            // Если слово без ID или с нулевым ID - назначаем автоинкрементный
            if (word.Id == 0)
            {
                word.Id = _wordsDictionary.Count > 0
                    ? _wordsDictionary.Keys.Max() + 1
                    : 1;
            }

            _wordsDictionary[word.Id] = word;
        }

        public void MarkWordAsUsed(int wordId)
        {
            if (_wordsDictionary.ContainsKey(wordId))
            {
                _usedWordIds.Add(wordId);
            }
        }


        private readonly List<Tower> _tower = new List<Tower>();
        public IReadOnlyList<Tower> Tower => _towers.AsReadOnly();

        public bool TryBuildTower(int wordId, Vector3 position)
        {
            // Проверяем доступность слова
            if (!_wordsDictionary.TryGetValue(wordId, out var word) || word.IsUsedInTower)
            {
                Console.WriteLine($"Слово с ID {wordId} недоступно для использования");
                return false;
            }

            // Ищем существующую незавершенную башню в этой позиции
            var existingTower = _towers
                .Where(t => !t.IsCompleted && t.BasePosition == position)
                .FirstOrDefault();

            if (existingTower != null)
            {
                existingTower.AddWord(word);
            }
            else
            {
                // Создаем новую башню
                var newTower = new Tower(position);
                newTower.AddWord(word);
                _towers.Add(newTower);
            }

            _usedWordIds.Add(wordId);
            return true;
        }



        // Метод для получения слова по ID
        public Word GetWordById(int wordId)
        {
            if (_wordsDictionary.TryGetValue(wordId, out var word))
            {
                return word;
            }
            throw new KeyNotFoundException($"Слово с ID {wordId} не найдено");
        }

        // Безопасная версия (возвращает null если не найдено)
        public Word? TryGetWordById(int wordId)
        {
            _wordsDictionary.TryGetValue(wordId, out var word);
            return word;
        }

        // Метод для создания новой башни
        public Tower CreateTower(Vector3 position, int firstWordId)
        {
            var word = GetWordById(firstWordId);
            if (word.IsUsedInTower)
            {
                throw new InvalidOperationException($"Слово {word.Text} уже используется");
            }

            var tower = new Tower(position);
            tower.AddWord(word);
            _towers.Add(tower);
            _usedWordIds.Add(word.Id);

            return tower;
        }

        // Метод для добавления слова в существующую башню
        public bool AddWordToTower(int towerIndex, int wordId)
        {
            if (towerIndex < 0 || towerIndex >= _towers.Count)
                return false;

            var word = TryGetWordById(wordId);
            if (word == null || word.IsUsedInTower)
                return false;

            _towers[towerIndex].AddWord(word);
            _usedWordIds.Add(word.Id);
            return true;
        }




        private int DetermineDirection(Vector3 position)
        {
            // Логика определения направления по позиции
            return position.X > MapSize.X / 2 ? 1 : 2;
        }

        private void UpdateScore()
        {
            TotalScore = _towers.Sum(t => t.Score);
        }






        public void DisplayState()
        {
            Console.WriteLine("\n=== ИГРОВОЕ СОСТОЯНИЕ ===");
            Console.WriteLine($"Ход: {CurrentTurn} | Очки: {TotalScore}");
            Console.WriteLine($"Башен: {_towers.Count} | Слов осталось: {RemainingWords}");

            foreach (var tower in _towers.OrderBy(t => t.BasePosition))
            {
                Console.WriteLine($"\nБашня ({tower.BasePosition}):");
                Console.WriteLine($"  Слов: {tower.Words.Count} | Очки: {tower.Score}");
                Console.WriteLine($"  Слова: {string.Join(", ", tower.Words.Select(w => w.Text))}");
            }
        }















        public List<Word> GetAvailableWords()
        {
            return _wordsDictionary.Values
                .Where(w => !w.IsUsedInTower && !_usedWordIds.Contains(w.Id))
                .ToList();
        }

        public bool CanRequestNewWords =>
            ShuffleLeft > 0 &&
            _towers.All(t => t.Height == 0);

        public void DecreaseWordCount(int count)
        {
            ShuffleLeft--;
            // Уменьшаем количество слов
        }

        public Tower GetCurrentTower()
        {
            return _towers.LastOrDefault(t => !t.IsCompleted);
        }

        public void CompleteTower(Tower tower)
        {
            tower.IsCompleted = true;
            TotalScore += tower.Score;
        }
    }
}
