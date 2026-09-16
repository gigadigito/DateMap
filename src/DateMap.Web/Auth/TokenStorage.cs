using Microsoft.JSInterop;

namespace DateMap.Web.Auth;

public interface ITokenStorage
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task RemoveTokenAsync();
}

public sealed class LocalStorageTokenStorage(IJSRuntime js) : ITokenStorage
{
    private const string Key = "datemap_token";

    public async Task<string?> GetTokenAsync()
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", Key); }
        catch { return null; }
    }

    public async Task SetTokenAsync(string token)
        => await js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public async Task RemoveTokenAsync()
        => await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
