using System.Security.Claims;
using DateMap.Application;
using DateMap.Domain;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/diaries")] public sealed class DiariesController(DateMapDbContext db):ControllerBase
{
 Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 static DiaryDto D(Diary d)=>new(d.Id,d.Title,d.Description,d.CoverImageUrl,d.CreatedAt,d.UpdatedAt);

 [HttpPost] public async Task<ActionResult<DiaryDto>> Create(CreateDiaryRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Title))return BadRequest(new ProblemDetails{Title="Title is required."});
  var title=req.Title.Trim();
  if(title.Length>150)return BadRequest(new ProblemDetails{Title="Title must have at most 150 characters."});
  if(!string.IsNullOrWhiteSpace(req.Description)&&req.Description.Trim().Length>2000)
   return BadRequest(new ProblemDetails{Title="Description must have at most 2000 characters."});
  if(!string.IsNullOrWhiteSpace(req.CoverImageUrl)&&req.CoverImageUrl.Trim().Length>2048)
   return BadRequest(new ProblemDetails{Title="CoverImageUrl must have at most 2048 characters."});
  var diary=new Diary(UserId,title,req.Description,req.CoverImageUrl);
  db.Diaries.Add(diary);
  await db.SaveChangesAsync();
  return CreatedAtAction(nameof(GetById),new{id=diary.Id},D(diary));
 }

 [HttpGet] public async Task<IReadOnlyList<DiaryDto>> List()
  =>await db.Diaries.AsNoTracking().Where(x=>x.UserId==UserId).OrderByDescending(x=>x.CreatedAt).Select(x=>D(x)).ToListAsync();

 [HttpGet("{id:guid}")] public async Task<ActionResult<DiaryDto>> GetById(Guid id)
 {
  var d=await db.Diaries.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  return d is null?NotFound():D(d);
 }

 [HttpPut("{id:guid}")] public async Task<ActionResult<DiaryDto>> Update(Guid id,UpdateDiaryRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Title))return BadRequest(new ProblemDetails{Title="Title is required."});
  var title=req.Title.Trim();
  if(title.Length>150)return BadRequest(new ProblemDetails{Title="Title must have at most 150 characters."});
  if(!string.IsNullOrWhiteSpace(req.Description)&&req.Description.Trim().Length>2000)
   return BadRequest(new ProblemDetails{Title="Description must have at most 2000 characters."});
  if(!string.IsNullOrWhiteSpace(req.CoverImageUrl)&&req.CoverImageUrl.Trim().Length>2048)
   return BadRequest(new ProblemDetails{Title="CoverImageUrl must have at most 2048 characters."});
  var d=await db.Diaries.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(d is null)return NotFound();
  d.Update(title,req.Description,req.CoverImageUrl);
  await db.SaveChangesAsync();
  return Ok(D(d));
 }

 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id)
 {
  var d=await db.Diaries.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(d is null)return NotFound();
  db.Diaries.Remove(d);
  await db.SaveChangesAsync();
  return NoContent();
 }
}
