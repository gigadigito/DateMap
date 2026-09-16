namespace DateMap.Domain;

public sealed class DomainException(string message) : Exception(message);
public abstract class Entity { public Guid Id { get; protected set; } = Guid.NewGuid(); }
public abstract class AuditedEntity : Entity { public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow; protected void Touch() => UpdatedAt = DateTime.UtcNow; }

public sealed class User : AuditedEntity
{
    private User() { }
    public User(string name, string email, string passwordHash) { Name = name.Trim(); Email = email.Trim().ToLowerInvariant(); PasswordHash = passwordHash; }
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public Profile? Profile { get; private set; }
    public void SetPasswordHash(string value) { PasswordHash = value; Touch(); }
}

public sealed class Profile : AuditedEntity
{
    private Profile() { }
    public Profile(Guid userId, string displayName, DateOnly birthDate) { UserId = userId; Update(displayName, birthDate, null, null, null, null); }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = "";
    public DateOnly BirthDate { get; private set; }
    public string? Bio { get; private set; }
    public string? Gender { get; private set; }
    public string? InterestedIn { get; private set; }
    public string? City { get; private set; }
    public bool IsActive { get; private set; } = true;
    public List<ProfilePhoto> Photos { get; private set; } = [];
    public void Update(string displayName, DateOnly birthDate, string? bio, string? gender, string? interestedIn, string? city)
    { if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("DisplayName is required."); DisplayName=displayName.Trim(); BirthDate=birthDate; Bio=bio; Gender=gender; InterestedIn=interestedIn; City=city; Touch(); }
}

public sealed class ProfilePhoto : Entity
{
    private ProfilePhoto() { }
    public ProfilePhoto(Guid profileId, string url, int displayOrder, bool isPrimary) { ProfileId=profileId; Url=url; DisplayOrder=displayOrder; IsPrimary=isPrimary; }
    public Guid ProfileId { get; private set; } public string Url { get; private set; } = ""; public int DisplayOrder { get; private set; } public bool IsPrimary { get; private set; } public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}

public sealed class Like : Entity
{
    private Like() { }
    public Like(Guid sourceProfileId, Guid targetProfileId) { if(sourceProfileId==targetProfileId) throw new DomainException("A profile cannot like itself."); SourceProfileId=sourceProfileId; TargetProfileId=targetProfileId; }
    public Guid SourceProfileId { get; private set; } public Guid TargetProfileId { get; private set; } public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}

public enum MatchStatus { Active, Unmatched, Blocked }
public sealed class Match : AuditedEntity
{
    private Match() { }
    public Match(Guid first, Guid second) { if(first==second) throw new DomainException("A match requires two profiles."); (ProfileAId,ProfileBId)=Canonical(first,second); MatchedAt=DateTime.UtcNow; }
    public Guid ProfileAId { get; private set; } public Guid ProfileBId { get; private set; } public MatchStatus Status { get; private set; }=MatchStatus.Active; public DateTime MatchedAt { get; private set; }
    public bool Contains(Guid id)=>ProfileAId==id||ProfileBId==id;
    public static (Guid A,Guid B) Canonical(Guid a,Guid b)=>a.CompareTo(b)<0?(a,b):(b,a);
}

public readonly record struct GeoPoint(double Longitude,double Latitude);
public sealed class Venue : AuditedEntity
{
    private Venue() { }
    public Venue(string name,string category,string address,string city,string state,string postalCode,double longitude,double latitude,bool partner=false) { Name=name;Category=category;Address=address;City=city;State=state;PostalCode=postalCode;Location=new(longitude,latitude);IsPartner=partner; }
    public string Name {get;private set;}=""; public string? Description {get;private set;} public string Category {get;private set;}=""; public string Address {get;private set;}=""; public string City {get;private set;}=""; public string State {get;private set;}=""; public string PostalCode {get;private set;}=""; public GeoPoint Location {get;private set;} public bool IsPartner {get;private set;} public bool IsActive {get;private set;}=true; public List<VenueOffer> Offers {get;private set;}=[];
}
public sealed class VenueOffer : AuditedEntity { private VenueOffer(){} public Guid VenueId{get;private set;} public string Title{get;private set;}=""; public string? Description{get;private set;} public DateTime ValidFrom{get;private set;} public DateTime ValidUntil{get;private set;} public bool IsActive{get;private set;}=true; }

public enum DateStatus { Proposed, Accepted, VenueSelected, Scheduled, CheckinAvailable, Started, PresenceVerified, Completed, Declined, Cancelled, NoShow, Disputed }
public enum ParticipantRole { Proposer, Invitee }
public enum ResponseStatus { Pending, Accepted, Declined }
public sealed class DateParticipant : Entity { private DateParticipant(){} public DateParticipant(Guid dateEventId,Guid profileId,ParticipantRole role){DateEventId=dateEventId;ProfileId=profileId;Role=role;ResponseStatus=role==ParticipantRole.Proposer?ResponseStatus.Accepted:ResponseStatus.Pending;} public Guid DateEventId{get;private set;} public Guid ProfileId{get;private set;} public ParticipantRole Role{get;private set;} public ResponseStatus ResponseStatus{get;private set;} public DateTime CreatedAt{get;private set;}=DateTime.UtcNow; internal void Accept()=>ResponseStatus=ResponseStatus.Accepted; internal void Decline()=>ResponseStatus=ResponseStatus.Declined; }

public sealed class DateEvent : AuditedEntity
{
    private DateEvent() { }
    public DateEvent(Guid matchId,Guid venueId,Guid proposedBy,Guid invitee,DateTime scheduledAt)
    { if(scheduledAt.Kind!=DateTimeKind.Utc) throw new DomainException("ScheduledAt must be UTC."); MatchId=matchId;VenueId=venueId;ProposedByProfileId=proposedBy;ScheduledAt=scheduledAt;Status=DateStatus.Proposed; Participants=[new(Id,proposedBy,ParticipantRole.Proposer),new(Id,invitee,ParticipantRole.Invitee)]; }
    public Guid MatchId{get;private set;} public Guid VenueId{get;private set;} public Guid ProposedByProfileId{get;private set;} public DateTime ScheduledAt{get;private set;} public DateStatus Status{get;private set;} public DateTime? AcceptedAt{get;private set;} public DateTime? CancelledAt{get;private set;} public DateTime? CompletedAt{get;private set;} public List<DateParticipant> Participants{get;private set;}=[];
    public void Accept(Guid profileId){Ensure(DateStatus.Proposed);var p=Participants.SingleOrDefault(x=>x.ProfileId==profileId&&x.Role==ParticipantRole.Invitee)??throw new DomainException("Only the invited participant can accept.");p.Accept();AcceptedAt=DateTime.UtcNow;Status=DateStatus.Scheduled;Touch();}
    public void Decline(Guid profileId){Ensure(DateStatus.Proposed);var p=Participants.SingleOrDefault(x=>x.ProfileId==profileId&&x.Role==ParticipantRole.Invitee)??throw new DomainException("Only the invited participant can decline.");p.Decline();Status=DateStatus.Declined;Touch();}
    public void Cancel(Guid profileId){if(!Participants.Any(x=>x.ProfileId==profileId))throw new DomainException("Only a participant can cancel.");if(Status is DateStatus.Cancelled or DateStatus.Completed)throw new DomainException("This date cannot be cancelled.");Status=DateStatus.Cancelled;CancelledAt=DateTime.UtcNow;Touch();}
    private void Ensure(DateStatus required){if(Status!=required)throw new DomainException($"Invalid transition from {Status}.");}
}

public sealed class Category : AuditedEntity
{
    private static readonly System.Text.RegularExpressions.Regex ColorRegex=new(@"^#[0-9A-Fa-f]{6}$",System.Text.RegularExpressions.RegexOptions.Compiled);
    private Category() { }
    public Category(Guid userId,string name,string? icon,string? color)
    { if(string.IsNullOrWhiteSpace(name))throw new DomainException("Name is required."); UserId=userId;Name=name.Trim();Icon=icon?.Trim();SetColor(color); }
    public Guid UserId { get; private set; } public string Name { get; private set; } = ""; public string? Icon { get; private set; } public string? Color { get; private set; }
    public void Update(string name,string? icon,string? color)
    { if(string.IsNullOrWhiteSpace(name))throw new DomainException("Name is required."); Name=name.Trim();Icon=icon?.Trim();SetColor(color);Touch(); }
    private void SetColor(string? color){if(!string.IsNullOrWhiteSpace(color)&&!ColorRegex.IsMatch(color))throw new DomainException("Color must be in #RRGGBB format.");Color=color?.Trim();}
}

public sealed class Diary : AuditedEntity
{
    private Diary() { }
    public Diary(Guid userId,string title,string? description,string? coverImageUrl)
    { if(string.IsNullOrWhiteSpace(title))throw new DomainException("Title is required."); UserId=userId;Title=title.Trim();Description=string.IsNullOrWhiteSpace(description)?null:description.Trim();CoverImageUrl=string.IsNullOrWhiteSpace(coverImageUrl)?null:coverImageUrl.Trim(); }
    public Guid UserId { get; private set; } public string Title { get; private set; } = ""; public string? Description { get; private set; } public string? CoverImageUrl { get; private set; }
    public void Update(string title,string? description,string? coverImageUrl)
    { if(string.IsNullOrWhiteSpace(title))throw new DomainException("Title is required."); Title=title.Trim();Description=string.IsNullOrWhiteSpace(description)?null:description.Trim();CoverImageUrl=string.IsNullOrWhiteSpace(coverImageUrl)?null:coverImageUrl.Trim();Touch(); }
}

public sealed class Event : AuditedEntity
{
    private Event() { }
    public Event(Guid userId,Guid diaryId,Guid? categoryId,string title,string? description,DateTime eventDate,string? placeName,double? latitude,double? longitude)
    { if(string.IsNullOrWhiteSpace(title))throw new DomainException("Title is required."); if(latitude.HasValue!=longitude.HasValue)throw new DomainException("Latitude and longitude must be provided together."); if(latitude is < -90 or > 90)throw new DomainException("Latitude must be between -90 and 90."); if(longitude is < -180 or > 180)throw new DomainException("Longitude must be between -180 and 180."); UserId=userId;DiaryId=diaryId;CategoryId=categoryId;Title=title.Trim();Description=string.IsNullOrWhiteSpace(description)?null:description.Trim();EventDate=eventDate;PlaceName=string.IsNullOrWhiteSpace(placeName)?null:placeName.Trim();Latitude=latitude;Longitude=longitude; }
    public Guid UserId { get; private set; } public Guid DiaryId { get; private set; } public Guid? CategoryId { get; private set; } public string Title { get; private set; } = ""; public string? Description { get; private set; } public DateTime EventDate { get; private set; } public string? PlaceName { get; private set; } public double? Latitude { get; private set; } public double? Longitude { get; private set; }
    public void Update(Guid? categoryId,string title,string? description,DateTime eventDate,string? placeName,double? latitude,double? longitude)
    { if(string.IsNullOrWhiteSpace(title))throw new DomainException("Title is required."); if(latitude.HasValue!=longitude.HasValue)throw new DomainException("Latitude and longitude must be provided together."); if(latitude is < -90 or > 90)throw new DomainException("Latitude must be between -90 and 90."); if(longitude is < -180 or > 180)throw new DomainException("Longitude must be between -180 and 180."); CategoryId=categoryId;Title=title.Trim();Description=string.IsNullOrWhiteSpace(description)?null:description.Trim();EventDate=eventDate;PlaceName=string.IsNullOrWhiteSpace(placeName)?null:placeName.Trim();Latitude=latitude;Longitude=longitude;Touch(); }
}
