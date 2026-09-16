namespace DateMap.Web.Models;

public record CategoryDto(Guid Id, string Name, string? Icon, string? Color, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateCategoryRequest(string Name, string? Icon, string? Color);
public record UpdateCategoryRequest(string Name, string? Icon, string? Color);
