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
public sealed class CategoryFactory:WebApplicationFactory<Program>
{
 readonly string _databaseName="datemap-cat-"+Guid.NewGuid();
 protected override void ConfigureWebHost(IWebHostBuilder b)
 { b.UseEnvironment("Testing"); b.ConfigureLogging(l=>{l.ClearProviders();l.AddConsole();}); b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DateMap","Host=unused;Database=unused;Username=unused"},{"Jwt:Key","integration-test-key-with-at-least-thirty-two-bytes"},{"Jwt:Issuer","DateMap.Api"},{"Jwt:Audience","DateMap.Client"}}));
   b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<DateMapDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<DateMapDbContext>>();s.AddDbContext<DateMapDbContext>(o=>o.UseInMemoryDatabase(_databaseName));using var sp=s.BuildServiceProvider();using var scope=sp.CreateScope();scope.ServiceProvider.GetRequiredService<DateMapDbContext>().Database.EnsureCreated();}); }
}
public sealed class CategoryTests:IClassFixture<CategoryFactory>
{
 readonly HttpClient _client;
 public CategoryTests(CategoryFactory f)=>_client=f.CreateClient();

 [Fact] public async Task Get_categories_without_token_returns_401()
 { var r=await _client.GetAsync("/api/categories");Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Post_category_without_token_returns_401()
 { var r=await _client.PostAsJsonAsync("/api/categories",new{name="Test"});Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Create_category_returns_created()
 { var(a,_)=await RegisterAndLogin("cat1@test.com");
   Auth(a);var r=await _client.PostAsJsonAsync("/api/categories",new{name="Restaurantes",icon="restaurant",color="#E63946"});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("Restaurantes",j.GetProperty("name").GetString());
   Assert.Equal("restaurant",j.GetProperty("icon").GetString());
   Assert.Equal("#E63946",j.GetProperty("color").GetString());
   Assert.False(j.TryGetProperty("userId",out _)); }

 [Fact] public async Task Create_category_with_empty_name_returns_400()
 { var(a,_)=await RegisterAndLogin("cat2@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/categories",new{name="",icon="test",color="#000000"});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_category_with_invalid_color_returns_400()
 { var(a,_)=await RegisterAndLogin("cat3@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/categories",new{name="Test",icon="test",color="red"});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_category_with_short_color_returns_400()
 { var(a,_)=await RegisterAndLogin("cat4@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/categories",new{name="Test",icon="test",color="#FFF"});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Duplicate_category_for_same_user_returns_409()
 { var(a,_)=await RegisterAndLogin("cat5@test.com");Auth(a);
   await _client.PostAsJsonAsync("/api/categories",new{name="Dups"});
   var r=await _client.PostAsJsonAsync("/api/categories",new{name="Dups"});
   Assert.Equal(System.Net.HttpStatusCode.Conflict,r.StatusCode); }

 [Fact] public async Task Duplicate_category_case_insensitive_returns_409()
 { var(a,_)=await RegisterAndLogin("cat6@test.com");Auth(a);
   await _client.PostAsJsonAsync("/api/categories",new{name="MyCat"});
   var r=await _client.PostAsJsonAsync("/api/categories",new{name="mycat"});
   Assert.Equal(System.Net.HttpStatusCode.Conflict,r.StatusCode); }

 [Fact] public async Task List_categories_returns_only_own()
 { var(a,_) =await RegisterAndLogin("listA@test.com");
   var(b,_) =await RegisterAndLogin("listB@test.com");
   Auth(a);await _client.PostAsJsonAsync("/api/categories",new{name="Restaurante"});
   Auth(b);await _client.PostAsJsonAsync("/api/categories",new{name="Viagem"});
   Auth(a);var listA=await _client.GetFromJsonAsync<JsonElement[]>("/api/categories");
   Assert.NotNull(listA);Assert.Single(listA);Assert.Equal("Restaurante",listA[0].GetProperty("name").GetString());
   Auth(b);var listB=await _client.GetFromJsonAsync<JsonElement[]>("/api/categories");
   Assert.NotNull(listB);Assert.Single(listB);Assert.Equal("Viagem",listB[0].GetProperty("name").GetString()); }

 [Fact] public async Task Get_category_by_id_own_returns_ok()
 { var(a,_)=await RegisterAndLogin("getown@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/categories",new{name="Own"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.GetAsync($"/api/categories/{id}");
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode); }

 [Fact] public async Task Get_category_by_id_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("getA@test.com");
   var(b,_) =await RegisterAndLogin("getB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/categories",new{name="Secret"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.GetAsync($"/api/categories/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Update_category_own_returns_ok()
 { var(a,_)=await RegisterAndLogin("updown@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/categories",new{name="Old"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.PutAsJsonAsync($"/api/categories/{id}",new{name="New",icon="heart",color="#FF0000"});
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("New",j.GetProperty("name").GetString());
   Assert.Equal("heart",j.GetProperty("icon").GetString());
   Assert.Equal("#FF0000",j.GetProperty("color").GetString()); }

 [Fact] public async Task Update_category_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("updA@test.com");
   var(b,_) =await RegisterAndLogin("updB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/categories",new{name="Mine"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.PutAsJsonAsync($"/api/categories/{id}",new{name="Stolen"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Delete_category_own_returns_no_content()
 { var(a,_)=await RegisterAndLogin("delown@test.com");Auth(a);
   var cr=await _client.PostAsJsonAsync("/api/categories",new{name="ToDelete"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.DeleteAsync($"/api/categories/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NoContent,r.StatusCode);
   var list=await _client.GetFromJsonAsync<JsonElement[]>("/api/categories");
   Assert.NotNull(list);Assert.Empty(list); }

 [Fact] public async Task Delete_category_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("delA@test.com");
   var(b,_) =await RegisterAndLogin("delB@test.com");
   Auth(a);var cr=await _client.PostAsJsonAsync("/api/categories",new{name="Protected"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.DeleteAsync($"/api/categories/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Full_isolation_between_users()
 { var(a,_)=await RegisterAndLogin("isoA@test.com");
   var(b,_)=await RegisterAndLogin("isoB@test.com");
   Auth(a);var ca=await _client.PostAsJsonAsync("/api/categories",new{name="Restaurantes"});
   var idA=(await ca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var cb=await _client.PostAsJsonAsync("/api/categories",new{name="Viagens"});
   var idB=(await cb.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(a);var la=await _client.GetFromJsonAsync<JsonElement[]>("/api/categories");
   Assert.NotNull(la);Assert.Single(la);Assert.Equal("Restaurantes",la[0].GetProperty("name").GetString());
   Auth(b);var lb=await _client.GetFromJsonAsync<JsonElement[]>("/api/categories");
   Assert.NotNull(lb);Assert.Single(lb);Assert.Equal("Viagens",lb[0].GetProperty("name").GetString());
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.GetAsync($"/api/categories/{idA}")).StatusCode);
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.PutAsJsonAsync($"/api/categories/{idA}",new{name="Hacked"})).StatusCode);
   Auth(b);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.DeleteAsync($"/api/categories/{idA}")).StatusCode);
   Auth(a);Assert.Equal(System.Net.HttpStatusCode.NotFound,(await _client.GetAsync($"/api/categories/{idB}")).StatusCode); }

 async Task<(string Token,Guid ProfileId)> RegisterAndLogin(string email)
 { var name=email.Split('@')[0];
   var r=await _client.PostAsJsonAsync("/api/auth/register",new{name,email,password="Secure123!"});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());
   var l=await _client.PostAsJsonAsync("/api/auth/login",new{email,password="Secure123!"});Assert.True(l.IsSuccessStatusCode,await l.Content.ReadAsStringAsync());
   var j=await l.Content.ReadFromJsonAsync<JsonElement>();return(j.GetProperty("token").GetString()!,j.GetProperty("profileId").GetGuid()); }
 void Auth(string token)=>_client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
}
