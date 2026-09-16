using DateMap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

namespace DateMap.Infrastructure;

public sealed class DateMapDbContext(DbContextOptions<DateMapDbContext> options) : DbContext(options)
{
    public DbSet<User> Users=>Set<User>(); public DbSet<Profile> Profiles=>Set<Profile>(); public DbSet<ProfilePhoto> ProfilePhotos=>Set<ProfilePhoto>(); public DbSet<Like> Likes=>Set<Like>(); public DbSet<Match> Matches=>Set<Match>(); public DbSet<Venue> Venues=>Set<Venue>(); public DbSet<VenueOffer> VenueOffers=>Set<VenueOffer>(); public DbSet<DateEvent> DateEvents=>Set<DateEvent>(); public DbSet<DateParticipant> DateParticipants=>Set<DateParticipant>();     public DbSet<Category> Categories=>Set<Category>(); public DbSet<Diary> Diaries=>Set<Diary>(); public DbSet<Event> Events=>Set<Event>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("postgis");
        foreach(var entity in b.Model.GetEntityTypes()) entity.SetTableName(ToSnake(entity.GetTableName()!));
        b.Entity<User>(e=>{e.HasIndex(x=>x.Email).IsUnique();e.Property(x=>x.Name).HasMaxLength(100);e.Property(x=>x.Email).HasMaxLength(320);e.Property(x=>x.PasswordHash).HasMaxLength(1000);e.HasOne(x=>x.Profile).WithOne().HasForeignKey<Profile>(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);});
        b.Entity<Profile>(e=>{e.Property(x=>x.DisplayName).HasMaxLength(100);e.Property(x=>x.Bio).HasMaxLength(1000);e.HasMany(x=>x.Photos).WithOne().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);});
        b.Entity<Like>(e=>{e.HasIndex(x=>new{x.SourceProfileId,x.TargetProfileId}).IsUnique();e.HasOne<Profile>().WithMany().HasForeignKey(x=>x.SourceProfileId).OnDelete(DeleteBehavior.Restrict);e.HasOne<Profile>().WithMany().HasForeignKey(x=>x.TargetProfileId).OnDelete(DeleteBehavior.Restrict);e.ToTable(t=>t.HasCheckConstraint("ck_likes_not_self","source_profile_id <> target_profile_id"));});
        b.Entity<Match>(e=>{e.HasIndex(x=>new{x.ProfileAId,x.ProfileBId}).IsUnique();e.HasOne<Profile>().WithMany().HasForeignKey(x=>x.ProfileAId).OnDelete(DeleteBehavior.Restrict);e.HasOne<Profile>().WithMany().HasForeignKey(x=>x.ProfileBId).OnDelete(DeleteBehavior.Restrict);e.ToTable(t=>t.HasCheckConstraint("ck_matches_canonical","profile_a_id < profile_b_id"));});
        var geoConverter=new ValueConverter<GeoPoint,Point>(v=>new Point(v.Longitude,v.Latitude){SRID=4326},v=>new GeoPoint(v.X,v.Y));
        b.Entity<Venue>(e=>{e.Property(x=>x.Location).HasConversion(geoConverter).HasColumnType("geography (point, 4326)");e.HasIndex(x=>x.Location).HasMethod("gist");e.HasIndex(x=>new{x.Category,x.IsActive});e.HasMany(x=>x.Offers).WithOne().HasForeignKey(x=>x.VenueId).OnDelete(DeleteBehavior.Restrict);
          var created=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
          e.HasData(
            new { Id=Guid.Parse("10000000-0000-0000-0000-000000000001"),Name="Date Map Bistro",Description=(string?)null,Category="Restaurant",Address="Av. Paulista, 1000",City="São Paulo",State="SP",PostalCode="01310-100",Location=new GeoPoint(-46.6534,-23.5614),IsPartner=true,IsActive=true,CreatedAt=created,UpdatedAt=created },
            new { Id=Guid.Parse("10000000-0000-0000-0000-000000000002"),Name="Café Central",Description=(string?)null,Category="Cafe",Address="Rua Augusta, 500",City="São Paulo",State="SP",PostalCode="01304-000",Location=new GeoPoint(-46.6505,-23.5551),IsPartner=false,IsActive=true,CreatedAt=created,UpdatedAt=created },
            new { Id=Guid.Parse("10000000-0000-0000-0000-000000000003"),Name="Bar Encontro",Description=(string?)null,Category="Bar",Address="Rua dos Pinheiros, 700",City="São Paulo",State="SP",PostalCode="05422-001",Location=new GeoPoint(-46.6810,-23.5650),IsPartner=true,IsActive=true,CreatedAt=created,UpdatedAt=created },
            new { Id=Guid.Parse("10000000-0000-0000-0000-000000000004"),Name="Cinema Date",Description=(string?)null,Category="Cinema",Address="Av. Rebouças, 3000",City="São Paulo",State="SP",PostalCode="05402-600",Location=new GeoPoint(-46.6792,-23.5730),IsPartner=false,IsActive=true,CreatedAt=created,UpdatedAt=created });});
        b.Entity<DateEvent>(e=>{e.HasOne<Match>().WithMany().HasForeignKey(x=>x.MatchId).OnDelete(DeleteBehavior.Restrict);e.HasOne<Venue>().WithMany().HasForeignKey(x=>x.VenueId).OnDelete(DeleteBehavior.Restrict);e.HasMany(x=>x.Participants).WithOne().HasForeignKey(x=>x.DateEventId).OnDelete(DeleteBehavior.Restrict);e.HasIndex(x=>x.MatchId);});
        b.Entity<DateParticipant>(e=>{e.HasIndex(x=>new{x.DateEventId,x.ProfileId}).IsUnique();e.HasOne<Profile>().WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Restrict);});
        b.Entity<Category>(e=>{e.Property(x=>x.Name).HasMaxLength(100);e.Property(x=>x.Icon).HasMaxLength(50);e.Property(x=>x.Color).HasMaxLength(7);e.HasIndex(x=>x.UserId);e.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);e.HasIndex(x=>new{x.UserId,x.Name},"ix_categories_user_name").IsUnique();});
        b.Entity<Diary>(e=>{e.Property(x=>x.Title).HasMaxLength(150);e.Property(x=>x.Description).HasMaxLength(2000);e.Property(x=>x.CoverImageUrl).HasMaxLength(2048);e.HasIndex(x=>x.UserId);e.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);});
        b.Entity<Event>(e=>{e.Property(x=>x.Title).HasMaxLength(200);e.Property(x=>x.Description).HasMaxLength(2000);e.Property(x=>x.PlaceName).HasMaxLength(200);e.HasIndex(x=>x.UserId);e.HasIndex(x=>x.DiaryId);e.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);e.HasOne<Diary>().WithMany().HasForeignKey(x=>x.DiaryId).OnDelete(DeleteBehavior.Cascade);e.HasOne<Category>().WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.SetNull);});
        foreach(var entity in b.Model.GetEntityTypes()) foreach(var p in entity.GetProperties()) p.SetColumnName(ToSnake(p.Name));
    }
    public override Task<int> SaveChangesAsync(CancellationToken ct=default)
    { foreach(var x in ChangeTracker.Entries<AuditedEntity>().Where(x=>x.State==EntityState.Modified)) x.Property("UpdatedAt").CurrentValue=DateTime.UtcNow; return base.SaveChangesAsync(ct); }
    private static string ToSnake(string s)=>string.Concat(s.Select((c,i)=>char.IsUpper(c)&&i>0?"_"+char.ToLowerInvariant(c):char.ToLowerInvariant(c).ToString()));
}
