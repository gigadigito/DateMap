using System.Net;
using System.Net.Http.Json;
using DateMap.Web.Auth;
using DateMap.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace DateMap.Web.Services;

public sealed class AuthService(
    IHttpClientFactory httpClientFactory,
    ITokenStorage tokenStorage,
    DateMapAuthStateProvider authStateProvider)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("api");

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("An account with this email already exists.");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Invalid email or password.");
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        await tokenStorage.SetTokenAsync(login.Token);
        authStateProvider.NotifyAuthenticationStateChanged();
        return login;
    }

    public async Task LogoutAsync()
    {
        await tokenStorage.RemoveTokenAsync();
        authStateProvider.NotifyAuthenticationStateChanged();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync()
    {
        try { return await _http.GetFromJsonAsync<CurrentUserDto>("api/auth/me"); }
        catch { return null; }
    }
}
