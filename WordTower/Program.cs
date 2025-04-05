using WordTower.Models;
using WordTower;
using Newtonsoft.Json;

<<<<<<< HEAD
var token = "токен";
=======
var token = "апи";
>>>>>>> 7c30727 (пофиксил запросы к апи, начал тестить алг для построения башни)
var client = new GameClient("https://games-test.datsteam.dev", token);

try
{
    var wordsResponse = await client.GetWordsAsync();
    var wordsWithIds = wordsResponse.Words
        .Select((text, index) => new Word(index, text))
        .ToList();

    Console.WriteLine($"Получено слов: {wordsResponse.Words.Count}");
    foreach (var word in wordsWithIds)
    {
        Console.WriteLine($"ID: {word.Id}, Слово: {word.Text}");
    }

    // 2. Подготавливаем данные для TowerBuilder
    var wordStrings = wordsResponse.Words; // List<string>
    var ids = Enumerable.Range(0, wordStrings.Count).ToList(); // Создаем ID от 0 до N-1
    var fieldSize = (wordsResponse.MapSize[0], wordsResponse.MapSize[1]); // (ширина, глубина)

<<<<<<< HEAD
// Получаем информацию о башнях
var towersResponse = await client.GetTowersAsync();
Console.WriteLine($"Всего башен: {towersResponse.CompletedTowers.Count}");
=======
    // 3. Создаем и используем TowerBuilder
    var builder = new TowerBuilder(wordStrings, ids, fieldSize);

    // 4. Строим башню (возвращаем 3 значения вместо 4)
    var (remainingWords, placements, score) = builder.BuildTower();

    // 5. Выводим результаты
    Console.WriteLine($"Построено башен: {placements.Count}");
    Console.WriteLine($"Осталось слов: {remainingWords.Count}");
    Console.WriteLine($"Общий счет: {score}");

    // 6. Пример отправки запроса на строительство
    if (placements.Count > 0)
    {
        var buildRequest = new BuildRequest
        {
            Done = false,
            Words = placements.Select(p => new BuildWord
            {
                Id = p.Word.Id,
                Dir = p.Direction,
                Pos = p.Start
            }).ToList()
        };

        var buildResponse = await client.BuildTowerAsync(buildRequest);
        Console.WriteLine($"Сервер ответил: {buildResponse?.Score ?? 0} баллов");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
>>>>>>> 7c30727 (пофиксил запросы к апи, начал тестить алг для построения башни)
