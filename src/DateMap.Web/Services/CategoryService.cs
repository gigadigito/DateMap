using System.Net.Http.Json;
using DateMap.Web.Models;

namespace DateMap.Web.Services;

public sealed class CategoryService(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("api");

    public async Task<CategoryDto[]> GetAllAsync()
        => (await _http.GetFromJsonAsync<CategoryDto[]>("api/categories")) ?? [];

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/categories", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/categories/{id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/categories/{id}");
        response.EnsureSuccessStatusCode();
    }
}
