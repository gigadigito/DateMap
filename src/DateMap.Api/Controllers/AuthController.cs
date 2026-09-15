using System.Security.Claims;
using DateMap.Application;
using DateMap.Domain;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DateMap.Api.Controllers;
[ApiController,Route("api/auth")] public sealed class AuthController(DateMapDbContext db,PasswordService passwords,ITokenService tokens):ControllerBase
{
 [HttpPost("register")] public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Name)||string.IsNullOrWhiteSpace(req.Email)||string.IsNullOrWhiteSpace(req.Password))
   return BadRequest(new ProblemDetails{Title="Name, email and password are required."});
  if(req.Password.Length<8)
   return BadRequest(new ProblemDetails{Title="Password must have at least 8 characters."});
  var email=req.Email.Trim().ToLowerInvariant();
  if(await db.Users.AnyAsync(x=>x.Email==email))
   return Conflict(new ProblemDetails{Title="Email already registered."});
  var user=new User(req.Name,email,"");
  user.SetPasswordHash(passwords.Hash(user,req.Password));
  var profile=new Profile(user.Id,req.Name.Trim(),DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)));
  db.AddRange(user,profile);
  await db.SaveChangesAsync();
  return Created("/api/auth/me",new RegisterResponse(user.Id,user.Name,user.Email));
 }

 [HttpPost("login")] public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
 {
  if(string.IsNullOrWhiteSpace(req.Email)||string.IsNullOrWhiteSpace(req.Password))
   return BadRequest(new ProblemDetails{Title="Email and password are required."});
  var user=await db.Users.Include(x=>x.Profile).SingleOrDefaultAsync(x=>x.Email==req.Email.Trim().ToLower());
  if(user is null||!user.IsActive||!passwords.Verify(user,user.PasswordHash,req.Password))
   return Unauthorized(new ProblemDetails{Title="Invalid credentials."});
  return Ok(new AuthResponse(tokens.Create(user,user.Profile!.Id),user.Profile.Id));
 }

 [Authorize]
 [HttpGet("me")] public async Task<ActionResult<UserDto>> Me()
 {
  var userId=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  var user=await db.Users.FindAsync(userId);
  if(user is null||!user.IsActive) return NotFound();
  return Ok(new UserDto(user.Id,user.Name,user.Email));
 }
}
