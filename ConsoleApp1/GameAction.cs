using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Первая_попытка_парсинга__консоль
{
    public abstract class GameAction
    {
        public abstract bool Execute(GameState state);

        protected void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
