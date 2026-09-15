namespace DateMap.Application;
public static class LikeMatchPolicy { public static bool ShouldCreateMatch(bool reciprocalLikeExists,bool matchExists)=>reciprocalLikeExists&&!matchExists; }
