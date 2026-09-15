using System.Security.Claims;
using System.Text.RegularExpressions;
using DateMap.Application;
using DateMap.Domain;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Authorize,Route("api/categories")] public sealed class CategoriesController(DateMapDbContext db):ControllerBase
{
 Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 static CategoryDto D(Category c)=>new(c.Id,c.Name,c.Icon,c.Color,c.CreatedAt,c.UpdatedAt);

 [HttpPost] public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Name))return BadRequest(new ProblemDetails{Title="Name is required."});
  if(!string.IsNullOrWhiteSpace(req.Color)&&!Regex.IsMatch(req.Color,"^#[0-9A-Fa-f]{6}$"))
   return BadRequest(new ProblemDetails{Title="Color must be in #RRGGBB format."});
  var name=req.Name.Trim();
  if(await db.Categories.AnyAsync(x=>x.UserId==UserId&&x.Name.ToLower()==name.ToLower()))
   return Conflict(new ProblemDetails{Title="Category already exists."});
  var category=new Category(UserId,name,req.Icon,req.Color);
  db.Categories.Add(category);
  await db.SaveChangesAsync();
  return CreatedAtAction(nameof(GetById),new{id=category.Id},D(category));
 }

 [HttpGet] public async Task<IReadOnlyList<CategoryDto>> List()
  =>await db.Categories.AsNoTracking().Where(x=>x.UserId==UserId).OrderBy(x=>x.Name).Select(x=>D(x)).ToListAsync();

 [HttpGet("{id:guid}")] public async Task<ActionResult<CategoryDto>> GetById(Guid id)
 {
  var c=await db.Categories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  return c is null?NotFound():D(c);
 }

 [HttpPut("{id:guid}")] public async Task<ActionResult<CategoryDto>> Update(Guid id,UpdateCategoryRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Name))return BadRequest(new ProblemDetails{Title="Name is required."});
  if(!string.IsNullOrWhiteSpace(req.Color)&&!Regex.IsMatch(req.Color,"^#[0-9A-Fa-f]{6}$"))
   return BadRequest(new ProblemDetails{Title="Color must be in #RRGGBB format."});
  var c=await db.Categories.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(c is null)return NotFound();
  var name=req.Name.Trim();
  if(await db.Categories.AnyAsync(x=>x.UserId==UserId&&x.Id!=id&&x.Name.ToLower()==name.ToLower()))
   return Conflict(new ProblemDetails{Title="Category already exists."});
  c.Update(name,req.Icon,req.Color);
  await db.SaveChangesAsync();
  return Ok(D(c));
 }

 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id)
 {
  var c=await db.Categories.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
  if(c is null)return NotFound();
  db.Categories.Remove(c);
  await db.SaveChangesAsync();
  return NoContent();
 }
}
