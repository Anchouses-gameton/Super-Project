

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using WordTower;
    using System.Linq;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Threading.Tasks;

namespace WordTower
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // 1. Инициализация данных
                var words = new List<string>
                {
                    "арбуз", "банан", "вишня", "груша", "дыня",
                    "ежевика", "женьшень", "земляника", "инжир", "йогурт",
                    "киви", "лимон", "малина", "нектарин", "орех",
                    "персик", "рябина", "слива", "тыква", "фейхоа",
                    "фундамент", "стена", "окно", "дверь", "пол",
                    "потолок", "крыша", "лестница", "балка", "колонна",
                    "перекрытие", "арка", "купол", "фасад", "карниз",
                    "парапет", "фронтон", "эркер", "веранда", "мансарда",
                    "синхрофазотрон", "бульбозаврикус", "живопрометус"
                };

                var ids = Enumerable.Range(1, 43).ToList();
                var fieldSize = (20, 10);

                // 2. Построение башни
                var builder = new TowerBuilder(words, ids, fieldSize);
                var (placedWords, placedIds, placements, score) = builder.BuildTower();

                // 3. Подготовка данных для визуализатора
                var visualizerData = new
                {
                    words = placedWords,
                    ids = placedIds,
                    placements = placements.Select(p => new
                    {
                        word = p.Word,
                        id = p.Id,
                        direction = p.Direction,
                        start = p.Start
                    }),
                    score
                };

                // 4. Настройка путей
                string projectDir = Directory.GetCurrentDirectory();
                string visualizerDir = "C:\\Users\\_Noble_IGO_\\Source\\Repos\\Super-Project\\WordTower\\dats_city_front-main\\";
                string outputDataPath = Path.Combine(visualizerDir, "public", "data", "tower_data.json");

                // 5. Сохранение данных с правильной кодировкой
                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                Directory.CreateDirectory(Path.GetDirectoryName(outputDataPath));
                await File.WriteAllTextAsync(outputDataPath, JsonSerializer.Serialize(visualizerData, jsonOptions));

                Console.WriteLine($"Данные сохранены: {outputDataPath}");

                // 6. Запуск визуализатора
                await StartVisualizerAsync(visualizerDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static async Task StartVisualizerAsync(string visualizerDir)
        {
            Process serverProcess = null;
            try
            {
                string packageJsonPath = Path.Combine(visualizerDir, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    Console.WriteLine($"Файл package.json не найден по пути: {packageJsonPath}");
                    return;
                }

                Console.WriteLine("Запуск визуализатора...");

                // Установка зависимостей
                await RunNpmCommandAsync(visualizerDir, "install");

                // Запуск сервера и получение Process
                serverProcess = await RunNpmCommandAsync(visualizerDir, "start", false);

                // Даем серверу время на запуск
                await Task.Delay(3000);

                // Открываем браузер
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:3000",
                    UseShellExecute = true
                });

                Console.WriteLine("Сервер визуализации запущен. Нажмите Enter для завершения...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске визуализатора: {ex.Message}");
            }
            finally
            {
                // Завершаем процесс если он запущен
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    serverProcess.Kill();
                    serverProcess.Dispose();
                }
            }
        }

        private static async Task<Process> RunNpmCommandAsync(string workingDir, string command, bool waitForExit = true)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = command,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, args) =>
            {
                tcs.TrySetResult(true);
                process.Dispose();
            };

            process.Start();

            if (waitForExit)
            {
                await tcs.Task;
            }

            return process;
        }

        // Остальные классы (TowerBuilder, WordPlacement и др.) остаются без изменений
    }
}