using System.Security.Claims;
using DateMap.Application;
using DateMap.Domain;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/events")] public sealed class EventsController(DateMapDbContext db):ControllerBase
{
 Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 static EventDto D(Event e)=>new(e.Id,e.DiaryId,e.CategoryId,e.Title,e.Description,e.EventDate,e.PlaceName,e.Latitude,e.Longitude,e.CreatedAt,e.UpdatedAt);

 [HttpPost] public async Task<ActionResult<EventDto>> Create(CreateEventRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Title))return BadRequest(new ProblemDetails{Title="Title is required."});
  var title=req.Title.Trim();
  if(title.Length>200)return BadRequest(new ProblemDetails{Title="Title must have at most 200 characters."});
  if(!string.IsNullOrWhiteSpace(req.Description)&&req.Description.Trim().Length>2000)
   return BadRequest(new ProblemDetails{Title="Description must have at most 2000 characters."});
  if(!string.IsNullOrWhiteSpace(req.PlaceName)&&req.PlaceName.Trim().Length>200)
   return BadRequest(new ProblemDetails{Title="PlaceName must have at most 200 characters."});
  if(req.Latitude.HasValue!=req.Longitude.HasValue)
   return BadRequest(new ProblemDetails{Title="Latitude and longitude must be provided together."});
  if(req.Latitude is < -90 or > 90)
   return BadRequest(new ProblemDetails{Title="Latitude must be between -90 and 90."});
  if(req.Longitude is < -180 or > 180)
   return BadRequest(new ProblemDetails{Title="Longitude must be between -180 and 180."});
  if(!await db.Diaries.AnyAsync(x=>x.Id==req.DiaryId&&x.UserId==UserId))
   return NotFound(new ProblemDetails{Title="Diary not found."});
  if(req.CategoryId.HasValue&&!await db.Categories.AnyAsync(x=>x.Id==req.CategoryId.Value&&x.UserId==UserId))
   return NotFound(new ProblemDetails{Title="Category not found."});
  var ev=new Event(UserId,req.DiaryId,req.CategoryId,title,req.Description,req.EventDate,req.PlaceName,req.Latitude,req.Longitude);
  db.Events.Add(ev);await db.SaveChangesAsync();
  return CreatedAtAction(nameof(GetById),new{id=ev.Id},D(ev));
 }

 [HttpGet("{id:guid}")] public async Task<ActionResult<EventDto>> GetById(Guid id)
 {
  var e=await db.Events.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  return e is null?NotFound():D(e);
 }

 [HttpPut("{id:guid}")] public async Task<ActionResult<EventDto>> Update(Guid id,UpdateEventRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Title))return BadRequest(new ProblemDetails{Title="Title is required."});
  var title=req.Title.Trim();
  if(title.Length>200)return BadRequest(new ProblemDetails{Title="Title must have at most 200 characters."});
  if(!string.IsNullOrWhiteSpace(req.Description)&&req.Description.Trim().Length>2000)
   return BadRequest(new ProblemDetails{Title="Description must have at most 2000 characters."});
  if(!string.IsNullOrWhiteSpace(req.PlaceName)&&req.PlaceName.Trim().Length>200)
   return BadRequest(new ProblemDetails{Title="PlaceName must have at most 200 characters."});
  if(req.Latitude.HasValue!=req.Longitude.HasValue)
   return BadRequest(new ProblemDetails{Title="Latitude and longitude must be provided together."});
  if(req.Latitude is < -90 or > 90)
   return BadRequest(new ProblemDetails{Title="Latitude must be between -90 and 90."});
  if(req.Longitude is < -180 or > 180)
   return BadRequest(new ProblemDetails{Title="Longitude must be between -180 and 180."});
  var e=await db.Events.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(e is null)return NotFound();
  if(!await db.Diaries.AnyAsync(x=>x.Id==req.DiaryId&&x.UserId==UserId))
   return NotFound(new ProblemDetails{Title="Diary not found."});
  if(req.CategoryId.HasValue&&!await db.Categories.AnyAsync(x=>x.Id==req.CategoryId.Value&&x.UserId==UserId))
   return NotFound(new ProblemDetails{Title="Category not found."});
  e.Update(req.CategoryId,title,req.Description,req.EventDate,req.PlaceName,req.Latitude,req.Longitude);
  await db.SaveChangesAsync();
  return Ok(D(e));
 }

 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id)
 {
  var e=await db.Events.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(e is null)return NotFound();
  db.Events.Remove(e);await db.SaveChangesAsync();
  return NoContent();
 }

 [HttpGet("~/api/diaries/{diaryId:guid}/events")] public async Task<IReadOnlyList<EventDto>> ListByDiary(Guid diaryId)
 {
  if(!await db.Diaries.AnyAsync(x=>x.Id==diaryId&&x.UserId==UserId))
   return [];
  return await db.Events.AsNoTracking().Where(x=>x.DiaryId==diaryId&&x.UserId==UserId).OrderBy(x=>x.EventDate).Select(x=>D(x)).ToListAsync();
 }
}
