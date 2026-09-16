namespace DateMap.Web.Models;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, Guid ProfileId);
public record RegisterRequest(string Name, string Email, string Password);
public record RegisterResponse(Guid Id, string Name, string Email);
public record CurrentUserDto(Guid Id, string Name, string Email);
