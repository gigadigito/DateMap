using System.Net.Http.Json;
using System.Text.Json;
using DateMap.Web.Models;

namespace DateMap.Web.Services;

public sealed class DatingService(IHttpClientFactory clients)
{
    private readonly HttpClient http = clients.CreateClient("api");

    public Task<ProfileDto?> MyProfile() => http.GetFromJsonAsync<ProfileDto>("api/profile/me");
    public Task<ProfileDto?> Profile(Guid id) => http.GetFromJsonAsync<ProfileDto>($"api/profiles/{id}");
    public Task<ProfileDto[]?> Discover() => http.GetFromJsonAsync<ProfileDto[]>("api/discovery");
    public Task<MatchDto[]?> Matches() => http.GetFromJsonAsync<MatchDto[]>("api/matches");
    public Task<DateDto[]?> Dates() => http.GetFromJsonAsync<DateDto[]>("api/dates");
    public Task<VenueDto[]?> Venues() => http.GetFromJsonAsync<VenueDto[]>("api/venues");

    public async Task<LikeResult?> Like(Guid id)
    {
        using var response = await http.PostAsync($"api/likes/{id}", null);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<LikeResult>();
    }

    public async Task<DateDto?> CreateDate(CreateDateRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/dates", request);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<DateDto>();
    }

    public async Task<ProfileDto?> UpdateProfile(ProfileDto profile)
    {
        using var response = await http.PutAsJsonAsync("api/profile/me", new
        {
            profile.DisplayName, profile.BirthDate, profile.Bio,
            profile.Gender, profile.InterestedIn, profile.City
        });
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<ProfileDto>();
    }

    public async Task<DateDto?> DateAction(Guid id, string action)
    {
        using var response = await http.PostAsync($"api/dates/{id}/{action}", null);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<DateDto>();
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var message = "Não foi possível concluir a ação. Tente novamente.";
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("title", out var title))
                message = title.GetString() switch
                {
                    "Like already exists." => "Você já curtiu esta pessoa.",
                    "A profile cannot like itself." => "Você não pode curtir seu próprio perfil.",
                    _ => message
                };
        }
        catch (JsonException) { }
        throw new InvalidOperationException(message);
    }
}
