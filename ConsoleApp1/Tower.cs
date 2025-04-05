using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Первая_попытка_парсинга__консоль
{
    public class Tower
    {
        public List<Word> Words { get; } = new List<Word>();
        public Vector3 BasePosition { get; }
        public int Height => Words.Count;
        public int Score { get; private set; }
        public bool IsCompleted { get; set; }
        public int Id { get; set; } = new Random().Next(1, 10000); // Генерация уникального ID
        public Tower(Vector3 basePosition)
        {
            BasePosition = basePosition;
        }

        public void AddWord(Word word)
        {
            if (word.IsUsedInTower)
                throw new InvalidOperationException("Слово уже используется в другой башне");

            word.Position = CalculateWordPosition(Height);
            word.MarkAsUsed();
            Words.Add(word);

            UpdateScore();

            if (Height >= 3) // Условие завершения башни
                CompleteTower();
        }

        private Vector3 CalculateWordPosition(int wordIndex)
        {
            return new Vector3(
                BasePosition.X,
                BasePosition.Y,
                BasePosition.Z + wordIndex * 2 // Каждое слово на 2 единицы выше предыдущего
            );
        }

        private void UpdateScore()
        {
            Score = Words.Sum(w => w.Text.Length * 10); // 10 очков за букву
        }

        private void CompleteTower()
        {
            IsCompleted = true;
            Score += 100; // Бонус за завершение
            Console.WriteLine($"Башня завершена! Очки: {Score}");
        }
    }
}
