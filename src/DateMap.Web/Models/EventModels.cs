namespace DateMap.Web.Models;

public record EventDto(Guid Id, Guid DiaryId, Guid? CategoryId, string Title, string? Description, DateTime EventDate, string? PlaceName, double? Latitude, double? Longitude, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateEventRequest(Guid DiaryId, Guid? CategoryId, string Title, string? Description, DateTime EventDate, string? PlaceName, double? Latitude, double? Longitude);
public record UpdateEventRequest(Guid DiaryId, Guid? CategoryId, string Title, string? Description, DateTime EventDate, string? PlaceName, double? Latitude, double? Longitude);
