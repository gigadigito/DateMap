using DateMap.Application;
namespace DateMap.Application.Tests;
public sealed class LikeMatchPolicyTests
{
 [Fact] public void Unilateral_like_does_not_create_match()=>Assert.False(LikeMatchPolicy.ShouldCreateMatch(false,false));
 [Fact] public void Reciprocal_like_creates_match()=>Assert.True(LikeMatchPolicy.ShouldCreateMatch(true,false));
 [Fact] public void Existing_match_is_not_duplicated()=>Assert.False(LikeMatchPolicy.ShouldCreateMatch(true,true));
}
