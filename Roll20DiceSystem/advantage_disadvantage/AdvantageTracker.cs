using System.Collections.Generic;

public class AdvantageTracker
{
    private bool hasAdvantage = false;
    private bool hasDisadvantage = false;

    public void AddAdvantage(bool condition)
    {
        if (condition) hasAdvantage = true;
    }

    public void AddDisadvantage(bool condition)
    {
        if (condition) hasDisadvantage = true;
    }

    public RollType GetRollType()
    {
        if (hasAdvantage && hasDisadvantage) return RollType.Normal;
        if (hasAdvantage) return RollType.Advantage;
        if (hasDisadvantage) return RollType.Disadvantage;
        return RollType.Normal;
    }

    public void Clear()
    {
        hasAdvantage = false;
        hasDisadvantage = false;
    }
}