// ============================================================
//  DrinkFlagResolver.cs — GDD §17.3, deviation -> DrinkFlag.
//  One rule, one file: change §17.3 in the GDD, change this file.
// ============================================================

public static class DrinkFlagResolver
{
    /// <summary>
    /// GDD §17.3:
    ///   deviation == 0 and methodMatch and iceMatch  -> Perfect
    ///   deviation == 0                               -> Seem_Like  (method or ice wrong)
    ///   0 &lt; deviation &lt;= MaxTolerance and matched -> Seem_Like
    ///   otherwise                                    -> Fail
    /// </summary>
    public static DrinkFlag Resolve(int deviation, bool methodMatch, bool iceMatch, bool hasRecipe)
    {
        if (!hasRecipe || deviation > DrinkDeviation.MaxTolerance) return DrinkFlag.Fail;
        if (deviation == 0 && methodMatch && iceMatch) return DrinkFlag.Perfect;
        return DrinkFlag.Seem_Like;
    }
}
