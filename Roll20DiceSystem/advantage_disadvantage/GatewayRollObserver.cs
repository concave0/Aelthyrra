using System;

public class GatewayRollObserver
{
    private AdvantageTracker _advantageTracker = new AdvantageTracker();

    public RollType LastDeterminedRollType { get; private set; }

    public void ObserveContext(bool hasAdvantage, bool hasDisadvantage)
    {
        _advantageTracker.Clear();
        _advantageTracker.AddAdvantage(hasAdvantage);
        _advantageTracker.AddDisadvantage(hasDisadvantage);
    }

    public RollType GetCurrentRollType()
    {
        LastDeterminedRollType = _advantageTracker.GetRollType();
        return LastDeterminedRollType;
    }

    public int RollD20()
    {
        var type = GetCurrentRollType();
        return RollHelper.RollD20(type);
    }
}