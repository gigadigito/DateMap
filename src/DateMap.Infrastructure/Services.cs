using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DateMap.Application;
using DateMap.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DateMap.Infrastructure;
public sealed class PasswordService { private readonly PasswordHasher<User> _hasher=new(); public string Hash(User user,string password)=>_hasher.HashPassword(user,password); public bool Verify(User user,string hash,string password)=>_hasher.VerifyHashedPassword(user,hash,password)!=PasswordVerificationResult.Failed; }
public sealed class JwtTokenService(IConfiguration config):ITokenService
{
 public string Create(User user,Guid profileId){var key=config["Jwt:Key"]??throw new InvalidOperationException("Jwt:Key is not configured.");var claims=new[]{new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),new Claim("profile_id",profileId.ToString())};var creds=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256);return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(config["Jwt:Issuer"],config["Jwt:Audience"],claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:creds));}
}
