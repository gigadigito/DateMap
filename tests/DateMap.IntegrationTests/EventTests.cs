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
public sealed class EventFactory:WebApplicationFactory<Program>
{
 readonly string _databaseName="datemap-event-"+Guid.NewGuid();
 protected override void ConfigureWebHost(IWebHostBuilder b)
 { b.UseEnvironment("Testing"); b.ConfigureLogging(l=>{l.ClearProviders();l.AddConsole();}); b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DateMap","Host=unused;Database=unused;Username=unused"},{"Jwt:Key","integration-test-key-with-at-least-thirty-two-bytes"},{"Jwt:Issuer","DateMap.Api"},{"Jwt:Audience","DateMap.Client"}}));
   b.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<DateMapDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<DateMapDbContext>>();s.AddDbContext<DateMapDbContext>(o=>o.UseInMemoryDatabase(_databaseName));using var sp=s.BuildServiceProvider();using var scope=sp.CreateScope();scope.ServiceProvider.GetRequiredService<DateMapDbContext>().Database.EnsureCreated();}); }
}
public sealed class EventTests:IClassFixture<EventFactory>
{
 readonly HttpClient _client;
 public EventTests(EventFactory f)=>_client=f.CreateClient();

 async Task<Guid> CreateDiary(string token,string title)
 { _client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
   var r=await _client.PostAsJsonAsync("/api/diaries",new{title});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());
   return(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); }

 async Task<Guid> CreateCategory(string token,string name)
 { _client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
   var r=await _client.PostAsJsonAsync("/api/categories",new{name,color="#FF0000"});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());
   return(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); }

 [Fact] public async Task Post_event_without_token_returns_401()
 { var r=await _client.PostAsJsonAsync("/api/events",new{title="Test"});Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Get_event_without_token_returns_401()
 { var r=await _client.GetAsync($"/api/events/{Guid.NewGuid()}");Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Put_event_without_token_returns_401()
 { var r=await _client.PutAsJsonAsync($"/api/events/{Guid.NewGuid()}",new{title="Test"});Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Delete_event_without_token_returns_401()
 { var r=await _client.DeleteAsync($"/api/events/{Guid.NewGuid()}");Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task List_events_by_diary_without_token_returns_401()
 { var r=await _client.GetAsync($"/api/diaries/{Guid.NewGuid()}/events");Assert.Equal(System.Net.HttpStatusCode.Unauthorized,r.StatusCode); }

 [Fact] public async Task Create_event_returns_created()
 { var(a,_)=await RegisterAndLogin("ev1@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"Diary1");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Churrasco",description="Sexta",eventDate=DateTime.UtcNow,placeName="Casa",latitude=-23.55,longitude=-46.63});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("Churrasco",j.GetProperty("title").GetString());
   Assert.Equal("Sexta",j.GetProperty("description").GetString());
   Assert.Equal("Casa",j.GetProperty("placeName").GetString());
   Assert.Equal(-23.55,j.GetProperty("latitude").GetDouble());
   Assert.Equal(-46.63,j.GetProperty("longitude").GetDouble());
   Assert.Equal(diaryId,j.GetProperty("diaryId").GetGuid());
   Assert.False(j.TryGetProperty("userId",out _)); }

 [Fact] public async Task Create_event_empty_title_returns_400()
 { var(a,_)=await RegisterAndLogin("ev2@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title=""});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_title_exceeds_200_returns_400()
 { var(a,_)=await RegisterAndLogin("ev3@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title=new string('A',201)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_description_exceeds_2000_returns_400()
 { var(a,_)=await RegisterAndLogin("ev4@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",description=new string('B',2001)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_place_name_exceeds_200_returns_400()
 { var(a,_)=await RegisterAndLogin("ev5@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",placeName=new string('C',201)});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_latitude_without_longitude_returns_400()
 { var(a,_)=await RegisterAndLogin("ev6@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",latitude=-23.55});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_longitude_without_latitude_returns_400()
 { var(a,_)=await RegisterAndLogin("ev7@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",longitude=-46.63});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r.StatusCode); }

 [Fact] public async Task Create_event_latitude_out_of_range_returns_400()
 { var(a,_)=await RegisterAndLogin("ev8@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r1=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",latitude=91.0,longitude=0.0});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r1.StatusCode);
   var r2=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",latitude=-91.0,longitude=0.0});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r2.StatusCode); }

 [Fact] public async Task Create_event_longitude_out_of_range_returns_400()
 { var(a,_)=await RegisterAndLogin("ev9@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r1=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",latitude=0.0,longitude=181.0});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r1.StatusCode);
   var r2=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Ok",latitude=0.0,longitude=-181.0});
   Assert.Equal(System.Net.HttpStatusCode.BadRequest,r2.StatusCode); }

 [Fact] public async Task Create_event_nonexistent_diary_returns_404()
 { var(a,_)=await RegisterAndLogin("ev10@test.com");Auth(a);
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId=Guid.NewGuid(),title="Ok"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Create_event_other_users_diary_returns_404()
 { var(a,_) =await RegisterAndLogin("evA@test.com");
   var(b,_) =await RegisterAndLogin("evB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"Secret");
   Auth(b);var r=await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="Hacked"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Create_event_nonexistent_category_returns_404()
 { var(a,_)=await RegisterAndLogin("ev11@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,categoryId=Guid.NewGuid(),title="Ok"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Create_event_other_users_category_returns_404()
 { var(a,_) =await RegisterAndLogin("evCA@test.com");
   var(b,_) =await RegisterAndLogin("evCB@test.com");
   Auth(a);var catA=await CreateCategory(a,"CatA");
   Auth(b);var diaryB=await CreateDiary(b,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryB,categoryId=catA,title="Ok"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Create_event_with_valid_category_returns_created()
 { var(a,_)=await RegisterAndLogin("ev12@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");var catId=await CreateCategory(a,"Food");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,categoryId=catId,title="Sushi"});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal(catId,j.GetProperty("categoryId").GetGuid()); }

 [Fact] public async Task Create_event_without_coordinates_returns_created()
 { var(a,_)=await RegisterAndLogin("ev13@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var r=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="NoLoc"});
   Assert.Equal(System.Net.HttpStatusCode.Created,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.True(j.TryGetProperty("latitude",out _)); }

 [Fact] public async Task Get_event_by_id_own_returns_ok()
 { var(a,_)=await RegisterAndLogin("ev14@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="GetMe"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.GetAsync($"/api/events/{id}");
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode); }

 [Fact] public async Task Get_event_by_id_inexistent_returns_404()
 { var(a,_)=await RegisterAndLogin("ev15@test.com");Auth(a);
   var r=await _client.GetAsync($"/api/events/{Guid.NewGuid()}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Get_event_by_id_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("evGA@test.com");
   var(b,_) =await RegisterAndLogin("evGB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="Secret"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.GetAsync($"/api/events/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Update_event_returns_ok()
 { var(a,_)=await RegisterAndLogin("ev16@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Old",eventDate=DateTime.UtcNow});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.PutAsJsonAsync($"/api/events/{id}",new{diaryId,title="New",eventDate=DateTime.UtcNow.AddDays(1),latitude=-10.0,longitude=-20.0});
   Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var j=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal("New",j.GetProperty("title").GetString());
   Assert.Equal(-10.0,j.GetProperty("latitude").GetDouble()); }

 [Fact] public async Task Update_event_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("evUA@test.com");
   var(b,_) =await RegisterAndLogin("evUB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="Mine"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.PutAsJsonAsync($"/api/events/{id}",new{diaryId=await CreateDiary(b,"B"),title="Stolen"});
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task Delete_event_own_returns_no_content()
 { var(a,_)=await RegisterAndLogin("ev17@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="ToDelete"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   var r=await _client.DeleteAsync($"/api/events/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NoContent,r.StatusCode);
   var r2=await _client.GetAsync($"/api/events/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r2.StatusCode); }

 [Fact] public async Task Delete_event_other_user_returns_404()
 { var(a,_) =await RegisterAndLogin("evDA@test.com");
   var(b,_) =await RegisterAndLogin("evDB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="Protected"});
   var id=(await cr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
   Auth(b);var r=await _client.DeleteAsync($"/api/events/{id}");
   Assert.Equal(System.Net.HttpStatusCode.NotFound,r.StatusCode); }

 [Fact] public async Task List_events_by_diary_ordered_by_event_date()
 { var(a,_)=await RegisterAndLogin("ev18@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var baseDate=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
   await _client.PostAsJsonAsync("/api/events",new{diaryId,title="C",eventDate=baseDate.AddDays(3)});
   await _client.PostAsJsonAsync("/api/events",new{diaryId,title="A",eventDate=baseDate.AddDays(1)});
   await _client.PostAsJsonAsync("/api/events",new{diaryId,title="B",eventDate=baseDate.AddDays(2)});
   var r=await _client.GetAsync($"/api/diaries/{diaryId}/events");Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var list=await r.Content.ReadFromJsonAsync<JsonElement[]>();
   Assert.NotNull(list);Assert.Equal(3,list!.Length);
   Assert.Equal("A",list[0].GetProperty("title").GetString());
   Assert.Equal("B",list[1].GetProperty("title").GetString());
   Assert.Equal("C",list[2].GetProperty("title").GetString()); }

 [Fact] public async Task List_events_by_other_users_diary_returns_empty()
 { var(a,_) =await RegisterAndLogin("evLA@test.com");
   var(b,_) =await RegisterAndLogin("evLB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"D");
   await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="A's event"});
   Auth(b);var r=await _client.GetAsync($"/api/diaries/{diaryA}/events");Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var list=await r.Content.ReadFromJsonAsync<JsonElement[]>();Assert.NotNull(list);Assert.Empty(list!); }

 [Fact] public async Task List_events_by_nonexistent_diary_returns_empty()
 { var(a,_)=await RegisterAndLogin("ev19@test.com");Auth(a);
   var r=await _client.GetAsync($"/api/diaries/{Guid.NewGuid()}/events");Assert.Equal(System.Net.HttpStatusCode.OK,r.StatusCode);
   var list=await r.Content.ReadFromJsonAsync<JsonElement[]>();Assert.NotNull(list);Assert.Empty(list!); }

 [Fact] public async Task Full_isolation_user_a_user_b()
 { var(a,_) =await RegisterAndLogin("evIsoA@test.com");
   var(b,_) =await RegisterAndLogin("evIsoB@test.com");
   Auth(a);var diaryA=await CreateDiary(a,"Diary A");
   var catA=await CreateCategory(a,"CatA");
   await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,categoryId=catA,title="Event A1",eventDate=DateTime.UtcNow});
   await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryA,title="Event A2",eventDate=DateTime.UtcNow.AddDays(1)});
   Auth(b);var diaryB=await CreateDiary(b,"Diary B");
   await _client.PostAsJsonAsync("/api/events",new{diaryId=diaryB,title="Event B1",eventDate=DateTime.UtcNow});
   Auth(a);var la=await _client.GetFromJsonAsync<JsonElement[]>($"/api/diaries/{diaryA}/events");
   Assert.NotNull(la);Assert.Equal(2,la!.Length);
   Auth(b);var lb=await _client.GetFromJsonAsync<JsonElement[]>($"/api/diaries/{diaryB}/events");
   Assert.NotNull(lb);Assert.Single(lb!);
   Auth(b);var le=await (await _client.GetAsync($"/api/diaries/{diaryA}/events")).Content.ReadFromJsonAsync<JsonElement[]>();Assert.NotNull(le);Assert.Empty(le!); }

 [Fact] public async Task Create_preserves_created_at_and_updated_at()
 { var(a,_)=await RegisterAndLogin("ev20@test.com");Auth(a);
   var diaryId=await CreateDiary(a,"D");
   var cr=await _client.PostAsJsonAsync("/api/events",new{diaryId,title="Dates"});
   var j=await cr.Content.ReadFromJsonAsync<JsonElement>();
   var id=j.GetProperty("id").GetGuid();
   var createdAt=j.GetProperty("createdAt").GetDateTime();
   await Task.Delay(50);
   var r=await _client.PutAsJsonAsync($"/api/events/{id}",new{diaryId,title="Updated",eventDate=DateTime.UtcNow});
   var uj=await r.Content.ReadFromJsonAsync<JsonElement>();
   Assert.Equal(createdAt.ToUniversalTime(),uj.GetProperty("createdAt").GetDateTime().ToUniversalTime());
   Assert.True(uj.GetProperty("updatedAt").GetDateTime()>uj.GetProperty("createdAt").GetDateTime()); }

 async Task<(string Token,Guid ProfileId)> RegisterAndLogin(string email)
 { var name=email.Split('@')[0];
   var r=await _client.PostAsJsonAsync("/api/auth/register",new{name,email,password="Secure123!"});Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());
   var l=await _client.PostAsJsonAsync("/api/auth/login",new{email,password="Secure123!"});Assert.True(l.IsSuccessStatusCode,await l.Content.ReadAsStringAsync());
   var j=await l.Content.ReadFromJsonAsync<JsonElement>();return(j.GetProperty("token").GetString()!,j.GetProperty("profileId").GetGuid()); }
 void Auth(string token)=>_client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
}
