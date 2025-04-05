using Newtonsoft.Json;
using System.Text;
using WordTower.Models;

namespace WordTower;

public class GameClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GameClient(string baseUrl, string token)
    {
        _httpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "X-Auth-Token", token },
                { "accept", "application/json" }
            }
        };
        _baseUrl = baseUrl;
    }

    public async Task<WordsResponse> GetWordsAsync() => await GetAsync<WordsResponse>("/api/words");

    public async Task<ShuffleResponse> ShuffleWordsAsync() => await PostAsync<ShuffleResponse>("/api/shuffle", null);

    public async Task<TowersResponse> GetTowersAsync() => await GetAsync<TowersResponse>("/api/towers");

    public async Task<BuildResponse> BuildTowerAsync(BuildRequest request) => await PostAsync<BuildResponse>("/api/build", request);

    private async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
        return await HandleResponse<T>(response);
    }

    private async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
        return await HandleResponse<T>(response);
    }

    private async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Ответ сервера: {responseContent}"); // Добавьте эту строку!

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Ошибка API: {response.StatusCode} - {responseContent}");

        return JsonConvert.DeserializeObject<T>(responseContent);
    }
}