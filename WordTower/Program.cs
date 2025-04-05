using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WordTower
{
    public class Program
    {
        public static void Main(string[] args)
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
                        id = p.Id,
                        pos = p.Start,
                        word = p.Word,
                        dir = p.Direction
                    }),
                    score
                };

                // 4. Настройка путей
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
                File.WriteAllText(outputDataPath, JsonSerializer.Serialize(visualizerData, jsonOptions));

                Console.WriteLine($"Данные сохранены: {outputDataPath}");

                // 6. Запуск визуализатора
                StartVisualizer(visualizerDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void StartVisualizer(string visualizerDir)
        {
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
                RunNpmCommand(visualizerDir, "install");

                // Запуск сервера
                var serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C cd /D \"{visualizerDir}\" && npm start",
                        WorkingDirectory = visualizerDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };
                serverProcess.Start();

                // Даем серверу время на запуск
                System.Threading.Thread.Sleep(3000);

                // Открываем браузер
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:3000",
                    UseShellExecute = true
                });

                Console.WriteLine("Сервер визуализации запущен. Нажмите Enter для завершения...");
                Console.ReadLine();

                if (!serverProcess.HasExited)
                {
                    serverProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске визуализатора: {ex.Message}");
            }
        }

        private static void RunNpmCommand(string workingDir, string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /D \"{workingDir}\" && npm {command}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Команда 'npm {command}' завершилась с ошибкой (код {process.ExitCode})");
            }
        }
    }
}