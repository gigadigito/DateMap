using DateMap.Infrastructure; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/venues")] public sealed class VenuesController(DateMapDbContext db):ControllerBase
{[HttpGet]public async Task<IActionResult> List([FromQuery]string? category)=>Ok(await db.Venues.AsNoTracking().Where(x=>x.IsActive&&(category==null||x.Category==category)).ToListAsync());[HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id){var v=await db.Venues.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.IsActive);return v is null?NotFound():Ok(v);} }
