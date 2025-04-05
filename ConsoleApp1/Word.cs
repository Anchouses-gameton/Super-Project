using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Первая_попытка_парсинга__консоль
{
    public class Word
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public bool IsVertical { get; set; }
        public bool IsUsedInTower { get; private set; } // Новое свойство
        // Счетчик для генерации ID, если они не приходят с сервера
        private static int _idCounter = 1;

        public Word(string text)
        {
            this.Id = _idCounter++;
            this.Text = text;
            this.Position = Vector3.Zero;
            this.IsVertical = false;
        }
        // Метод для пометки слова как использованного
        public void MarkAsUsed()
        {
            this.IsUsedInTower = true;
            this.Position = Vector3.Zero; // Сбрасываем позицию при использовании
        }

        // Метод для сброса статуса (если потребуется)
        public void ResetUsage()
        {
            this.IsUsedInTower = false;
        }

        public static void ResetIdCounter()
        {
            _idCounter = 1;
        }
    }
}
