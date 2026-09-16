namespace DateMap.Web.Models;

public record DiaryDto(Guid Id, string Title, string? Description, string? CoverImageUrl, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateDiaryRequest(string Title, string? Description, string? CoverImageUrl);
public record UpdateDiaryRequest(string Title, string? Description, string? CoverImageUrl);
