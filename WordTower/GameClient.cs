using System.Text;
using System.Text.Json;
using WordTower.Models;

namespace WordTower;

public class GameClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _token;

    public GameClient(string baseUrl, string token)
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
        _token = token;

        _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _token);
        _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
    }

    // Методы для работы с API
    public async Task<WordsResponse> GetWordsAsync()
    {
        return await GetAsync<WordsResponse>("/api/words");
    }

    public async Task<ShuffleResponse> ShuffleWordsAsync()
    {
        return await PostAsync<ShuffleResponse>("/api/shuffle", null);
    }

    public async Task<TowersResponse> GetTowersAsync()
    {
        return await GetAsync<TowersResponse>("/api/towers");
    }

    public async Task<BuildResponse> BuildTowerAsync(BuildRequest request)
    {
        return await PostAsync<BuildResponse>("/api/build", request);
    }

    // Общие методы для GET/POST
    private async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
        return await HandleResponse<T>(response);
    }

    private async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
        return await HandleResponse<T>(response);
    }

    private async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);
        return JsonSerializer.Deserialize<T>(responseContent);
    }
}
