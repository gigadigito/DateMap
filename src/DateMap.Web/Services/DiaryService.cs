using System.Net.Http.Json;
using DateMap.Web.Models;

namespace DateMap.Web.Services;

public sealed class DiaryService(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("api");

    public async Task<DiaryDto[]> GetAllAsync()
        => (await _http.GetFromJsonAsync<DiaryDto[]>("api/diaries")) ?? [];

    public async Task<DiaryDto?> GetByIdAsync(Guid id)
        => await _http.GetFromJsonAsync<DiaryDto>($"api/diaries/{id}");

    public async Task<DiaryDto> CreateAsync(CreateDiaryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/diaries", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DiaryDto>())!;
    }

    public async Task<DiaryDto> UpdateAsync(Guid id, UpdateDiaryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/diaries/{id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DiaryDto>())!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/diaries/{id}");
        response.EnsureSuccessStatusCode();
    }
}
