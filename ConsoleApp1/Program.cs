






//ОНО РАБОТАЕТ!!! ПРАВДА СЛОВА В КОНСОЛЬКУ ПОЛЗУТ
//class Program
//{
//    static async Task Main()
//    {
//        const string apiKey = "103c71b6-1d24-40df-8d62-eb8cc1e451d8"; // Ваш реальный ключ
//        const string apiUrl = "https://games-test.datsteam.dev/api/words";

//        var handler = new HttpClientHandler();
//        using var client = new HttpClient(handler);

//        // Устанавливаем специальный заголовок
//        client.DefaultRequestHeaders.Add("X-Auth-Token", apiKey); // ❗ Без "Bearer"!
//        client.DefaultRequestHeaders.Add("accept", "application/json");

//        try
//        {
//            var response = await client.GetAsync(apiUrl);
//            var content = await response.Content.ReadAsStringAsync();

//            if (response.IsSuccessStatusCode)
//            {
//                Console.WriteLine($"✅ Успешный ответ:\n{content}");
//            }
//            else
//            {
//                Console.WriteLine($"❌ Ошибка {(int)response.StatusCode} ({response.StatusCode}):\n{content}");
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"💥 Критическая ошибка: {ex.Message}");
//        }
//    }
//}







using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Первая_попытка_парсинга__консоль;



public static class GameServerClient
{
    private static readonly HttpClient _client = new HttpClient();
    private static GameState _gameState = new GameState();
    const string ApiKey = "103c71b6-1d24-40df-8d62-eb8cc1e451d8";
    public const string BaseUrl = "https://games-test.datsteam.dev/api";

    public static async Task Main()
    {
        Console.WriteLine("Инициализация игры...");

        try
        {
            // Настройка HttpClient
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-Auth-Token", ApiKey);
            _client.DefaultRequestHeaders.Add("accept", "application/json");

            // Загрузка данных
            await LoadGameDataFromServer();
            DisplayGameInfo();

            // Пример построения башни
            //Сюда надот будет добавить логику
            var tower = new Tower(new Vector3(5, 5, 0));
            tower.AddWord(_gameState.GetWordById(1));
            tower.AddWord(_gameState.GetWordById(2));
            tower.AddWord(_gameState.GetWordById(3));

            // Отправка на сервер
            await SendTowerToServer(tower, tower.IsCompleted);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            _client.Dispose();
        }
    }

    public static async Task LoadGameDataFromServer()
    {
        try
        {
            // 1. Отправка запроса
            var response = await _client.GetAsync($"{BaseUrl}/words");

            //2. Проверка ответа
            response.EnsureSuccessStatusCode();

            //3. Чтение контента
            var jsonContent = await response.Content.ReadAsStringAsync();

            //Парсинг данных
            ParseServerResponse(jsonContent);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при загрузке данных: {ex.StatusCode} - {ex.Message}");
            throw;
        }
    }

    private static void ParseServerResponse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        // Парсинг основных параметров
        _gameState.MapSize = ParseVector3(root.GetProperty("mapSize"));
        _gameState.CurrentTurn = root.GetProperty("turn").GetInt32();
        _gameState.NextTurnSec = root.GetProperty("nextTurnSec").GetInt32();
        _gameState.RoundEndsAt = root.GetProperty("roundEndsAt").GetDateTime();
        _gameState.ShuffleLeft = root.GetProperty("shuffleLeft").GetInt32();

        // Очистка предыдущих данных
        _gameState.ClearWords();

        // Парсинг использованных слов
        foreach (var index in root.GetProperty("usedIndexes").EnumerateArray())
        {
            _gameState.MarkWordAsUsed(index.GetInt32());
        }

        // Парсинг доступных слов
        int wordId = 1;
        foreach (var wordElement in root.GetProperty("words").EnumerateArray())
        {
            var word = new Word(wordElement.GetString())
            {
                Id = wordId++,
                Position = Vector3.Zero,
                IsVertical = false
            };
            _gameState.AddWord(word);
        }
    }

    private static Vector3 ParseVector3(JsonElement element)
    {
        return new Vector3(
            element[0].GetInt32(),
            element[1].GetInt32(),
            element[2].GetInt32()
        );
    }

    public static async Task<bool> SendTowerToServer(Tower tower, bool markAsCompleted)
    {
        try
        {
            // Формируем запрос согласно документации Swagger
            var request = new
            {
                doneTowers = markAsCompleted ? new[]
                {
                new
                {
                    id = tower.Id, // Нужно добавить ID башни в класс Tower
                    score = tower.Score
                }
            } : Array.Empty<object>(),
                score = tower.Score,
                tower = new
                {
                    score = tower.Score,
                    words = tower.Words.Select(w => new
                    {
                        dir = w.IsVertical ? 2 : 1,
                        pos = new[] { (int)w.Position.X, (int)w.Position.Y, (int)w.Position.Z },
                        text = w.Text
                    })
                }
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(request, options);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Console.WriteLine($"Отправка запроса на {BaseUrl}/build");
            Console.WriteLine($"JSON: {jsonContent}");

            var response = await _client.PostAsync($"{BaseUrl}/build", httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка сервера: {response.StatusCode}");
                Console.WriteLine($"Ответ сервера: {responseContent}");
                return false;
            }

            Console.WriteLine("Башня успешно отправлена на сервер!");
            Console.WriteLine($"Ответ сервера: {responseContent}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке башни: {ex.Message}");
            return false;
        }
    }

    private static void DisplayGameInfo()
    {
        Console.WriteLine("\n=== ИГРОВЫЕ ДАННЫЕ ===");
        Console.WriteLine($"Текущий ход: {_gameState.CurrentTurn}");
        Console.WriteLine($"Доступно слов: {_gameState.AvailableWords.Count}");
        Console.WriteLine($"Размер карты: {_gameState.MapSize}");
        Console.WriteLine($"Следующий ход через: {_gameState.NextTurnSec} сек");
    }
}

public class BuildTowerRequest
{
    [JsonPropertyName("done")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("words")]
    public List<BuildWord> Words { get; set; } = new();

    public class BuildWord
    {
        [JsonPropertyName("dir")]
        public int Direction { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pos")]
        public int[] Position { get; set; } = new int[3];
    }
}











































//public static class GameServerClient
//{
//    public static class GameServerClient
//    {
//        private static readonly HttpClient _client = new HttpClient();
//        private static GameState _gameState = new GameState();
//        const string ApiKey = "103c71b6-1d24-40df-8d62-eb8cc1e451d8";
//        const string BaseUrl = "https://games-test.datsteam.dev/api";

//        public static async Task Main()
//        {
//            Console.WriteLine("Инициализация игры...");

//            try
//            {
//                // Настройка HttpClient
//                _client.DefaultRequestHeaders.Clear();
//                _client.DefaultRequestHeaders.Add("X-Auth-Token", ApiKey);
//                _client.DefaultRequestHeaders.Add("accept", "application/json");

//                // Проверка подключения
//                if (!await CheckServerConnection())
//                {
//                    Console.WriteLine("Сервер недоступен. Проверьте подключение к интернету.");
//                    return;
//                }

//                await LoadGameDataFromServer();
//                DisplayGameInfo();

//                // Строим тестовую башню
//                var tower = new Tower(new Vector3(5, 5, 0));
//                tower.AddWord(_gameState.GetWordById(1));
//                tower.AddWord(_gameState.GetWordById(2));

//                // Отправляем на сервер
//                bool sentSuccessfully = await SendTowerToServer(tower, false);

//                if (sentSuccessfully && tower.Words.Count >= 3)
//                {
//                    await SendTowerToServer(tower, true);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка: {ex.GetType().Name}: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//            }
//            finally
//            {
//                _client.Dispose();
//            }
//        }

//        private static async Task<bool> CheckServerConnection()
//        {
//            try
//            {
//                var response = await _client.GetAsync($"{BaseUrl}/api/words");
//                return response.IsSuccessStatusCode;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public static async Task<bool> SendTowerToServer(Tower tower, bool markAsCompleted)
//        {
//            try
//            {
//                var request = new
//                {
//                    done = markAsCompleted,
//                    words = tower.Words.Select(w => new
//                    {
//                        dir = w.IsVertical ? 2 : 1,
//                        id = w.Id,
//                        pos = new[] { (int)w.Position.X, (int)w.Position.Y, (int)w.Position.Z }
//                    })
//                };

//                var jsonContent = Newtonsoft.Json.JsonSerializer.Serialize(request);
//                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

//                Console.WriteLine($"Отправка запроса на {BaseUrl}/api/build");
//                Console.WriteLine($"JSON: {jsonContent}");

//                var response = await _client.PostAsync($"{BaseUrl}/api/build", httpContent);
//                var responseContent = await response.Content.ReadAsStringAsync();

//                if (!response.IsSuccessStatusCode)
//                {
//                    Console.WriteLine($"Ошибка сервера: {response.StatusCode}");
//                    Console.WriteLine($"Ответ сервера: {responseContent}");
//                    return false;
//                }

//                Console.WriteLine("Башня успешно отправлена на сервер!");
//                Console.WriteLine($"Ответ сервера: {responseContent}");
//                return true;
//            }
//            catch (HttpRequestException httpEx)
//            {
//                Console.WriteLine($"Ошибка HTTP запроса: {httpEx.Message}");
//                if (httpEx.StatusCode.HasValue)
//                {
//                    Console.WriteLine($"HTTP код: {httpEx.StatusCode}");
//                }
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка при отправке башни: {ex.Message}");
//                return false;
//            }
//        }



//        private static void DisplayGameInfo()
//        {
//            // ... ваша реализация отображения информации ...
//        }
//    }








//    public class BuildTowerRequest
//    {
//        [JsonPropertyName("done")]
//        public bool IsCompleted { get; set; }

//        [JsonPropertyName("words")]
//        public List<BuildWord> Words { get; set; } = new();

//        public class BuildWord
//        {
//            [JsonPropertyName("dir")]
//            public int Direction { get; set; }

//            [JsonPropertyName("id")]
//            public int Id { get; set; }

//            [JsonPropertyName("pos")]
//            public int[] Position { get; set; } = new int[3];
//        }
//    }







//    private static async Task LoadGameDataFromServer(string baseUrl)
//    {
//        try
//        {
//            Console.WriteLine("\nЗагрузка данных с сервера...");
//            var response = await _client.GetAsync($"{baseUrl}/words");

//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception($"Сервер вернул ошибку: {response.StatusCode}");
//            }

//            var jsonContent = await response.Content.ReadAsStringAsync();
//            ParseServerResponse(jsonContent);

//            Console.WriteLine("Данные успешно загружены и обработаны!");
//        }
//        catch (HttpRequestException httpEx)
//        {
//            Console.WriteLine($"Ошибка сетевого запроса: {httpEx.Message}");
//            throw;
//        }
//        catch (Newtonsoft.Json.JsonException jsonEx)
//        {
//            Console.WriteLine($"Ошибка обработки JSON: {jsonEx.Message}");
//            throw;
//        }
//    }



//    //ПАРСЕР ДЛЯ СЕРВАКА
//    private static void ParseServerResponse(string json)
//    {
//        using JsonDocument doc = JsonDocument.Parse(json);
//        JsonElement root = doc.RootElement;

//        // Парсинг основных параметров игры
//        _gameState.MapSize = ParseVector3(root.GetProperty("mapSize"));
//        _gameState.CurrentTurn = root.GetProperty("turn").GetInt32();
//        _gameState.NextTurnSec = root.GetProperty("nextTurnSec").GetInt32();
//        _gameState.RoundEndsAt = root.GetProperty("roundEndsAt").GetDateTime();
//        _gameState.ShuffleLeft = root.GetProperty("shuffleLeft").GetInt32();

//        // Очистка предыдущих данных
//        _gameState.ClearWords();

//        // Парсинг использованных слов
//        foreach (var index in root.GetProperty("usedIndexes").EnumerateArray())
//        {
//            _gameState.MarkWordAsUsed(index.GetInt32());
//        }

//        // Парсинг доступных слов
//        int wordId = 1;
//        foreach (var wordElement in root.GetProperty("words").EnumerateArray())
//        {
//            var word = new Word(wordElement.GetString())
//            {
//                Id = wordId++,
//                Position = Vector3.Zero,
//                IsVertical = false
//            };
//            _gameState.AddWord(word);
//        }
//    }

//    private static Vector3 ParseVector3(JsonElement element)
//    {
//        return new Vector3(
//            element[0].GetInt32(),
//            element[1].GetInt32(),
//            element[2].GetInt32()
//        );
//    }

//    private static void DisplayGameInfo()
//    {
//        Console.WriteLine("\n=== ИНФОРМАЦИЯ О ИГРЕ ===");
//        Console.WriteLine($"Текущий ход: {_gameState.CurrentTurn}");
//        Console.WriteLine($"Доступно слов: {_gameState.AvailableWords.Count}");
//        //Console.WriteLine($"Использовано слов: {_gameState.usedWordIds.Count}"); поправить 
//        Console.WriteLine($"Размер карты: {_gameState.MapSize}");
//        Console.WriteLine($"Следующий ход через: {_gameState.NextTurnSec} сек");
//        Console.WriteLine($"Раунд заканчивается: {_gameState.RoundEndsAt}");
//        Console.WriteLine($"Доступно перемешиваний: {_gameState.ShuffleLeft}");
//    }
//}



























/// <summary>
/// ПАРСЕР ДЛЯ ФАЙЛА
/// </summary>
public static class GameDataLoader
    {
        public static void LoadDataInto(GameState gameState, string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            // Парсинг основных параметров
            gameState.MapSize = ParseVector3(root.GetProperty("mapSize"));
            gameState.CurrentTurn = root.GetProperty("turn").GetInt32();
            gameState.NextTurnSec = root.GetProperty("nextTurnSec").GetInt32();
            gameState.RoundEndsAt = root.GetProperty("roundEndsAt").GetDateTime();
            gameState.ShuffleLeft = root.GetProperty("shuffleLeft").GetInt32();

            // Очистка предыдущих данных
            gameState.ClearWords();

            // Парсинг usedIndexes
            foreach (var index in root.GetProperty("usedIndexes").EnumerateArray())
            {
                gameState.MarkWordAsUsed(index.GetInt32());
            }

            // Парсинг слов
            ParseWords(root.GetProperty("words"), gameState);
        }

        private static Vector3 ParseVector3(JsonElement element)
        {
            return new Vector3(
                element[0].GetInt32(),
                element[1].GetInt32(),
                element[2].GetInt32()
            );
        }

        private static void ParseWords(JsonElement wordsElement, GameState gameState)
        {
            // Вариант 1: слова как массив строк
            if (wordsElement[0].ValueKind == JsonValueKind.String)
            {
                foreach (var wordElement in wordsElement.EnumerateArray())
                {
                    gameState.AddWord(new Word(wordElement.GetString()));
                }
            }
            // Вариант 2: слова как объекты с параметрами
            else
            {
                foreach (var wordObj in wordsElement.EnumerateArray())
                {
                    var word = new Word(wordObj.GetProperty("text").GetString())
                    {
                        Id = wordObj.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0,
                        Position = wordObj.TryGetProperty("pos", out var posProp)
                            ? ParseVector3(posProp)
                            : Vector3.Zero,
                        IsVertical = wordObj.TryGetProperty("isVertical", out var vertProp)
                            && vertProp.GetBoolean()
                    };

                    gameState.AddWord(word);
                }
            }
        }
    }



























