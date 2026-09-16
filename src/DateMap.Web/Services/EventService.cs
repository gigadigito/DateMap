using System.Net.Http.Json;
using DateMap.Web.Models;

namespace DateMap.Web.Services;

public sealed class EventService(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("api");

    public async Task<EventDto[]> GetByDiaryAsync(Guid diaryId)
        => (await _http.GetFromJsonAsync<EventDto[]>($"api/diaries/{diaryId}/events")) ?? [];

    public async Task<EventDto?> GetByIdAsync(Guid id)
        => await _http.GetFromJsonAsync<EventDto>($"api/events/{id}");

    public async Task<EventDto> CreateAsync(CreateEventRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/events", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task<EventDto> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/events/{id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/events/{id}");
        response.EnsureSuccessStatusCode();
    }
}
