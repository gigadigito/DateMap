using DateMap.Domain;
namespace DateMap.Domain.Tests;
public sealed class DomainTests
{
 [Fact] public void Self_like_rejected(){var id=Guid.NewGuid();Assert.Throws<DomainException>(()=>new Like(id,id));}
 [Fact] public void Match_pair_is_canonical_and_stable(){var a=Guid.NewGuid();var b=Guid.NewGuid();var x=new Match(a,b);var y=new Match(b,a);Assert.Equal(x.ProfileAId,y.ProfileAId);Assert.Equal(x.ProfileBId,y.ProfileBId);}
 [Fact] public void Invitee_can_accept_and_date_becomes_scheduled(){var a=Guid.NewGuid();var b=Guid.NewGuid();var d=new DateEvent(Guid.NewGuid(),Guid.NewGuid(),a,b,DateTime.UtcNow.AddDays(1));d.Accept(b);Assert.Equal(DateStatus.Scheduled,d.Status);}
 [Fact] public void Stranger_cannot_accept(){var d=new DateEvent(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),DateTime.UtcNow.AddDays(1));Assert.Throws<DomainException>(()=>d.Accept(Guid.NewGuid()));}
 [Fact] public void Cancelled_date_cannot_be_accepted(){var a=Guid.NewGuid();var b=Guid.NewGuid();var d=new DateEvent(Guid.NewGuid(),Guid.NewGuid(),a,b,DateTime.UtcNow.AddDays(1));d.Cancel(a);Assert.Throws<DomainException>(()=>d.Accept(b));}
 [Fact] public void Invalid_transition_fails(){var a=Guid.NewGuid();var b=Guid.NewGuid();var d=new DateEvent(Guid.NewGuid(),Guid.NewGuid(),a,b,DateTime.UtcNow.AddDays(1));d.Decline(b);Assert.Throws<DomainException>(()=>d.Accept(b));}
}
