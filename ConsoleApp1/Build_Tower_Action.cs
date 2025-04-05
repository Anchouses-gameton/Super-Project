using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Первая_попытка_парсинга__консоль
{
    public class BuildTowerAction : GameAction
    {
        public required int WordId { get; init; }
        public required Vector3 Position { get; init; }

        public override bool Execute(GameState state)
        {
            //try
            //{
            //    //// 1. Проверка через оба способа
            //    //if (state.IsWordUsed(WordId))
            //    //{
            //    //    Log($"Слово с ID {WordId} уже использовано!");
            //    //    return false;
            //    //}

            //    //// 2. Получаем слово через словарь для производительности
            //    //if (!state.TryGetWord(WordId, out var word))
            //    //{
            //    //    Log($"Слово с ID {WordId} не найдено!");
            //    //    return false;
            //    //}

            //    // 3. Логика построения
            //    word.Position = Position;
            //    state.MarkWordAsUsed(WordId); // Используем новый метод

            //    // 4. Обновление состояния
            //    state.CurrentTurn++;

            //    Log($"Построена башня из слова '{word.Text}' на позиции {Position}");
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    Log($"Ошибка при построении: {ex.Message}");
            //    return false;
            //}
            return true;
        }
    }
}
