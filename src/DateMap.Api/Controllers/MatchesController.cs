using System.Security.Claims; using DateMap.Infrastructure; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/matches")] public sealed class MatchesController(DateMapDbContext db):ControllerBase
{Guid P=>Guid.Parse(User.FindFirstValue("profile_id")!);[HttpGet]public async Task<IActionResult> List()=>Ok(await db.Matches.AsNoTracking().Where(x=>x.ProfileAId==P||x.ProfileBId==P).ToListAsync());[HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id){var m=await db.Matches.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&(x.ProfileAId==P||x.ProfileBId==P));return m is null?NotFound():Ok(m);} }
