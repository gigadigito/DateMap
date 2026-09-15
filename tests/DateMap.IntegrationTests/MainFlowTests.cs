using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DateMap.Domain;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DateMap.IntegrationTests;
public sealed class DateMapFactory:WebApplicationFactory<Program>
{
 readonly string _databaseName="datemap-"+Guid.NewGuid();
 protected override void ConfigureWebHost(IWebHostBuilder b)
 { b.UseEnvironment("Testing"); b.ConfigureLogging(l=>{l.ClearProviders();l.AddConsole();}); b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DateMap","Host=unused;Database=unused;Username=unused"},{"Jwt:Key","integration-test-key-with-at-least-thirty-two-bytes"},{"Jwt:Issuer","DateMap.Api"},{"Jwt:Audience","DateMap.Client"}}));
   b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<DateMapDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<DateMapDbContext>>();s.AddDbContext<DateMapDbContext>(o=>o.UseInMemoryDatabase(_databaseName));using var sp=s.BuildServiceProvider();using var scope=sp.CreateScope();scope.ServiceProvider.GetRequiredService<DateMapDbContext>().Database.EnsureCreated();}); }
}
public sealed class MainFlowTests: IClassFixture<DateMapFactory>
{
 readonly HttpClient _client; public MainFlowTests(DateMapFactory f)=>_client=f.CreateClient();
 [Fact] public async Task Register_Likes_Match_Date_Accept_Scheduled()
 {
  var a=await Register("a@datemap.test");var b=await Register("b@datemap.test");
  Auth(a.Token);var uResponse=await _client.PostAsync($"/api/likes/{b.ProfileId}",null);Assert.True(uResponse.IsSuccessStatusCode,await uResponse.Content.ReadAsStringAsync());var unilateral=await uResponse.Content.ReadFromJsonAsync<JsonElement>();Assert.False(unilateral.GetProperty("matchCreated").GetBoolean());
  Auth(b.Token);var rResponse=await _client.PostAsync($"/api/likes/{a.ProfileId}",null);Assert.True(rResponse.IsSuccessStatusCode,await rResponse.Content.ReadAsStringAsync());var reciprocal=await rResponse.Content.ReadFromJsonAsync<JsonElement>();Assert.True(reciprocal.GetProperty("matchCreated").GetBoolean());var matchId=reciprocal.GetProperty("matchId").GetGuid();
  Auth(a.Token);var venues=await _client.GetFromJsonAsync<JsonElement>("/api/venues");var venueId=venues[0].GetProperty("id").GetGuid();var create=await _client.PostAsJsonAsync("/api/dates",new{matchId,venueId,scheduledAt=DateTime.UtcNow.AddDays(1)});create.EnsureSuccessStatusCode();var dateId=(await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
  Auth(b.Token);var accepted=await _client.PostAsync($"/api/dates/{dateId}/accept",null);accepted.EnsureSuccessStatusCode();Assert.Equal((int)DateStatus.Scheduled,(await accepted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
 }
 async Task<(string Token,Guid ProfileId)> Register(string email){var name=email.Split('@')[0];var r=await _client.PostAsJsonAsync("/api/auth/register",new{name,email,password="Secure123!"});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());var l=await _client.PostAsJsonAsync("/api/auth/login",new{email,password="Secure123!"});Assert.True(l.IsSuccessStatusCode,await l.Content.ReadAsStringAsync());var j=await l.Content.ReadFromJsonAsync<JsonElement>();return(j.GetProperty("token").GetString()!,j.GetProperty("profileId").GetGuid());}
 void Auth(string token)=>_client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
}
