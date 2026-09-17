namespace DateMap.Web.Models;

public record ProfileDto(Guid Id, string DisplayName, DateOnly BirthDate, string? Bio, string? Gender, string? InterestedIn, string? City, string[] Photos);
public record LikeResult(bool LikeRegistered, bool MatchCreated, Guid? MatchId);
public record MatchDto(Guid Id, Guid ProfileAId, Guid ProfileBId, int Status, DateTime MatchedAt);
public record GeoPointDto(double Longitude, double Latitude);
public record VenueDto(Guid Id, string Name, string? Description, string Category, string Address, string City, string State, GeoPointDto Location);
public record DateParticipantDto(Guid ProfileId, int Role, int ResponseStatus);
public record DateDto(Guid Id, Guid MatchId, Guid VenueId, DateTime ScheduledAt, int Status, DateParticipantDto[] Participants);
public record CreateDateRequest(Guid MatchId, Guid VenueId, DateTime ScheduledAt);
