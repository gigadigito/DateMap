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
public sealed class DiaryFactory:WebApplicationFactory<Program>
{
 readonly string _databaseName="datemap-diary-"+Guid.NewGuid();
 protected override void ConfigureWebHost(IWebHostBuilder b)
 { b.UseEnvironment("Testing"); b.ConfigureLogging(l=>{l.ClearProviders();l.AddConsole();}); b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DateMap","Host=unused;Database=unused;Username=unused"},{"Jwt:Key","integration-test-key-with-at-least-thirty-two-bytes"},{"Jwt:Issuer","DateMap.Api"},{"Jwt:Audience","DateMap.Client"}}));
   b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<DateMapDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<DateMapDbContext>>();s.AddDbContext<DateMapDbContext>(o=>o.UseInMemoryDatabase(_databaseName));using var sp=s.BuildServiceProvider();using var scope=sp.CreateScope();scope.ServiceProvider.GetRequiredService<DateMapDbContext>().Database.EnsureCreated();}); }
}
public sealed class DiaryTests:IClassFixture<DiaryFactory>
{
 readonly HttpClient _client;
 public DiaryTests(DiaryFactory f)=>_client=f.CreateClient();

 [Fact] public async Task Get_diaries_without_token_returns_401()
 { var r=await _client.GetAsync("/api/diaries");Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Post_diary_without_token_returns_401()
 { var r=await _client.PostAsJsonAsync("/api/diaries",new{title="Test"});Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Put_diary_without_token_returns_401()
 { var r=await _client.PutAsJsonAsync("/api/diaries/"+Guid.NewGuid(),new{title="Test"});Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Delete_diary_without_token_returns_401()
 { var r=await _client.DeleteAsync("/api/diaries/"+Guid.NewGuid());Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Create_diary_returns_created()
 { var(a,_)=await RegisterAndLogin("diary1@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title="Viagem teste",description="Desc",coverImageUrl=(string?)null});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("Viagem teste",j.GetProperty("title").GetString());
   Assert.Equal("Desc",j.GetProperty("description").GetString());
   Assert.True(j.TryGetProperty("id",out _));
   Assert.False(j.TryGetProperty("userId",out _)); }

 [Fact] public async Task Create_diary_with_empty_title_returns_400()
 { var(a,_)=await RegisterAndLogin("diary2@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title=""});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_diary_with_spaces_only_title_returns_400()
 { var(a,_)=await RegisterAndLogin("diary3@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title="   "});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_diary_title_exceeds_150_returns_400()
 { var(a,_)=await RegisterAndLogin("diary4@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title=new string('A',151)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_diary_description_exceeds_2000_returns_400()
 { var(a,_)=await RegisterAndLogin("diary5@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title="Ok",description=new string('B',2001)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_diary_cover_image_url_exceeds_2048_returns_400()
 { var(a,_)=await RegisterAndLogin("diary6@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title="Ok",coverImageUrl="https://x.com/"+new string('C',2040)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_diary_title_trimmed()
 { var(a,_)=await RegisterAndLogin("diary7@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title="  My Diary  "});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("My Diary",j.GetProperty("title").GetString()); }

 [Fact] public async Task List_diaries_isolation_between_users()
 { var(a,_) =await RegisterAndLogin("listA@test.com");
   var(b,_) =await RegisterAndLogin("listB@test.com");
   Auth(a);await _client.PostAsJsonAsync("/api/diaries",new{title="Diary A1"});
   await _client.PostAsJsonAsync("/api/diaries",new{title="Diary A2"});
   Auth(b);await _client.PostAsJsonAsync("/api/diaries",new{title="Diary B1"});
   Auth(a);var la=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(la);Assert.Equal(2,la.Length);
   Auth(b);var lb=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(lb);Assert.Single(lb);Assert.Equal("Diary B1",lb[0].GetProperty("title").GetString()); }

 [Fact] public async Task Get_by_id_own_returns_ok()
 { var(a,_)=await RegisterAndLogin("getown@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Own"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.GetAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode); }

 [Fact] public async Task Get_by_id_inexistent_returns_404()
 { var(a,_)=await RegisterAndLogin("getinf@test.com");Auth(a);
   var r=await _client.GetAsync($"/api/diaries/{Guid.NewGuid()}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Get_by_id_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("getA@test.com");
   var(b,_) =await RegisterAndLogin("getB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Secret"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.GetAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Update_own_returns_ok()
 { var(a,_)=await RegisterAndLogin("upd@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Old",description="old desc"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.PutAsJsonAsync($"/api/diaries/{id}",new{title="New",description="new desc",coverImageUrl="https://img.test"});
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("New",j.GetProperty("title").GetString());
   Assert.Equal("new desc",j.GetProperty("description").GetString());
   Assert.Equal("https://img.test",j.GetProperty("coverImageUrl").GetString()); }

 [Fact] public async Task Update_preserves_created_at()
 { var(a,_)=await RegisterAndLogin("upddates@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Test"});
   var j=await cr.Content.ReadFromJsonAsync<JsonElement>();
   var id=j.GetProperty("id").GetGuid();
   var createdAt=j.GetProperty("createdAt").GetDateTime();
   await Task.Delay(50);
   var r=await _client.PutAsJsonAsync($"/api/diaries/{id}",new{title="Updated"});
   var uj=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal(createdAt.ToUniversalTime(),uj.GetProperty("createdAt").GetDateTime().ToUniversalTime());
   Assert.True(uj.GetProperty("updatedAt").GetDateTime()>uj.GetProperty("createdAt").GetDateTime()); }

 [Fact] public async Task Update_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("updA@test.com");
   var(b,_) =await RegisterAndLogin("updB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Mine"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.PutAsJsonAsync($"/api/diaries/{id}",new{title="Stolen"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Delete_own_returns_no_content()
 { var(a,_)=await RegisterAndLogin("delown@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="ToDelete"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.DeleteAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NoContent,r.StatusCode);
   var r2=await _client.GetAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r2.StatusCode);
   var list=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(list);Assert.Empty(list); }

 [Fact] public async Task Delete_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("delA@test.com");
   var(b,_) =await RegisterAndLogin("delB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/diaries",new{title="Protected"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.DeleteAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode);
   Auth(a);var r2=await _client.GetAsync($"/api/diaries/{id}");
   Assert.Equal(System.Net.HttpStatusCode.OK,r2.StatusCode); }

 [Fact] public async Task Full_isolation_user_a_user_b()
 { var(a,_) =await RegisterAndLogin("isoA@test.com");
   var(b,_) =await RegisterAndLogin("isoB@test.com");
   Auth(a);var ca=await _client.PostAsJsonAsync("/api/diaries",new{title="Diary secreto de A",description="Somente A"});
   var idA=(await ca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var cb=await _client.PostAsJsonAsync("/api/diaries",new{title="Diary de B"});
   var idB=(await cb.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(a);var la=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(la);Assert.Single(la);Assert.Equal("Diary secreto de A",la[0].GetProperty("title").GetString());
   Auth(b);var lb=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(lb);Assert.Single(lb);Assert.Equal("Diary de B",lb[0].GetProperty("title").GetString());
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.GetAsync($"/api/diaries/{idA}")).StatusCode);
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.PutAsJsonAsync($"/api/diaries/{idA}",new{title="Hacked"})).StatusCode);
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.DeleteAsync($"/api/diaries/{idA}")).StatusCode);
   Auth(a);var after=await _client.GetFromJsonAsync<JsonElement[]>("/api/diaries");
   Assert.NotNull(after);Assert.Single(after);Assert.Equal("Diary secreto de A",after[0].GetProperty("title").GetString()); }

 async Task<(string Token,Guid ProfileId)> RegisterAndLogin(string email)
 { var name=email.Split('@')[0];
   var r=await _client.PostAsJsonAsync("/api/auth/register",new{name,email,password="Secure123!"});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());
   var l=await _client.PostAsJsonAsync("/api/auth/login",new{email,password="Secure123!"});Assert.True(l.IsSuccessStatusCode,await l.Content.ReadAsStringAsync());
   var j=await l.Content.ReadFromJsonAsync<JsonElement>();return(j.GetProperty("token").GetString()!,j.GetProperty("profileId").GetGuid()); }
 void Auth(string token)=>_client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
}
