using WordTower.Models;
using WordTower;

var token = "токен";
var client = new GameClient("https://games-test.datsteam.dev", token);

// Получаем слова
var wordsResponse = await client.GetWordsAsync();
Console.WriteLine($"Получено слов: {wordsResponse.Words.Count}");

// Запрашиваем новый набор (если нужно)
if (wordsResponse.Words.Count < 500)
{
    var shuffleResponse = await client.ShuffleWordsAsync();
    Console.WriteLine($"Новый набор: {shuffleResponse.Words.Count} слов");
}

// Строим башню
var buildRequest = new BuildRequest
{
    Done = false,
    Words = new List<BuildWord>
    {
        new BuildWord { Id = 1, Dir = 2, Pos = new[] { 0, 0, 0 } },
        new BuildWord { Id = 2, Dir = 3, Pos = new[] { 1, 0, 0 } }
    }
};

var buildResponse = await client.BuildTowerAsync(buildRequest);
Console.WriteLine($"Башня построена, текущий счёт: {buildResponse.Score}");

// Получаем информацию о башнях
var towersResponse = await client.GetTowersAsync();
Console.WriteLine($"Всего башен: {towersResponse.CompletedTowers.Count}");
