using DateMap.Domain;
namespace DateMap.Application;
public record RegisterRequest(string Name,string Email,string Password);
public record LoginRequest(string Email,string Password);
public record AuthResponse(string Token,Guid ProfileId);
public record RegisterResponse(Guid Id,string Name,string Email);
public record UserDto(Guid Id,string Name,string Email);
public record UpdateProfileRequest(string DisplayName,DateOnly BirthDate,string? Bio,string? Gender,string? InterestedIn,string? City);
public record ProfileDto(Guid Id,string DisplayName,DateOnly BirthDate,string? Bio,string? Gender,string? InterestedIn,string? City,IReadOnlyList<string> Photos);
public record LikeResult(bool LikeRegistered,bool MatchCreated,Guid? MatchId);
public record CreateDateRequest(Guid MatchId,Guid VenueId,DateTime ScheduledAt);
public record CreateCategoryRequest(string Name,string? Icon,string? Color);
public record UpdateCategoryRequest(string Name,string? Icon,string? Color);
public record CategoryDto(Guid Id,string Name,string? Icon,string? Color,DateTime CreatedAt,DateTime UpdatedAt);
public record CreateDiaryRequest(string Title,string? Description,string? CoverImageUrl);
public record UpdateDiaryRequest(string Title,string? Description,string? CoverImageUrl);
public record DiaryDto(Guid Id,string Title,string? Description,string? CoverImageUrl,DateTime CreatedAt,DateTime UpdatedAt);
public interface ICurrentUser { Guid UserId { get; } }
public interface ITokenService { string Create(User user,Guid profileId); }
