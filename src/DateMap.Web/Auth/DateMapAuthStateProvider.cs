using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DateMap.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace DateMap.Web.Auth;

public sealed class DateMapAuthStateProvider(ITokenStorage tokenStorage, IHttpClientFactory httpClientFactory) : AuthenticationStateProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("api");

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStorage.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        try
        {
            var user = await _http.GetFromJsonAsync<CurrentUserDto>("api/auth/me");
            if (user is null)
            {
                await tokenStorage.RemoveTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            await tokenStorage.RemoveTokenAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyAuthenticationStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
