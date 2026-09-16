using DateMap.Web;
using DateMap.Web.Auth;
using DateMap.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5154";

builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<JwtBearerHandler>();

builder.Services.AddScoped<JwtBearerHandler>();
builder.Services.AddScoped<ITokenStorage, LocalStorageTokenStorage>();
builder.Services.AddScoped<DateMapAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<DateMapAuthStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DiaryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<EventService>();

await builder.Build().RunAsync();
