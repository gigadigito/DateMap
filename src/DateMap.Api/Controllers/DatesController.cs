using System.Security.Claims; using DateMap.Application; using DateMap.Domain; using DateMap.Infrastructure; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/dates")] public sealed class DatesController(DateMapDbContext db):ControllerBase
{Guid P=>Guid.Parse(User.FindFirstValue("profile_id")!);
 [HttpPost]public async Task<IActionResult>Create(CreateDateRequest r){var m=await db.Matches.SingleOrDefaultAsync(x=>x.Id==r.MatchId);if(m is null)return NotFound();if(!m.Contains(P))return Forbid();if(!await db.Venues.AnyAsync(x=>x.Id==r.VenueId&&x.IsActive))return NotFound();var invited=m.ProfileAId==P?m.ProfileBId:m.ProfileAId;var d=new DateEvent(m.Id,r.VenueId,P,invited,r.ScheduledAt.ToUniversalTime());db.DateEvents.Add(d);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=d.Id},d);}
 [HttpGet]public async Task<IActionResult>List()=>Ok(await db.DateEvents.AsNoTracking().Include(x=>x.Participants).Where(x=>x.Participants.Any(p=>p.ProfileId==P)).ToListAsync());
 [HttpGet("{id:guid}")]public async Task<IActionResult>Get(Guid id){var d=await db.DateEvents.AsNoTracking().Include(x=>x.Participants).SingleOrDefaultAsync(x=>x.Id==id&&x.Participants.Any(p=>p.ProfileId==P));return d is null?NotFound():Ok(d);}
 [HttpPost("{id:guid}/accept")]public async Task<IActionResult>Accept(Guid id){var d=await Find(id);d.Accept(P);await db.SaveChangesAsync();return Ok(d);}
 [HttpPost("{id:guid}/decline")]public async Task<IActionResult>Decline(Guid id){var d=await Find(id);d.Decline(P);await db.SaveChangesAsync();return Ok(d);}
 [HttpPost("{id:guid}/cancel")]public async Task<IActionResult>Cancel(Guid id){var d=await Find(id);d.Cancel(P);await db.SaveChangesAsync();return Ok(d);}
 async Task<DateEvent>Find(Guid id)=>await db.DateEvents.Include(x=>x.Participants).SingleOrDefaultAsync(x=>x.Id==id&&x.Participants.Any(p=>p.ProfileId==P))??throw new KeyNotFoundException("Date not found.");}
