using System;

public static class RollHelper
{
    private static Random _random = new Random();

    /// <summary>
    /// Rolls a d20 with Advantage/Disadvantage/Normal.
    /// </summary>
    public static int RollD20(RollType rollType = RollType.Normal)
    {
        int roll1 = _random.Next(1, 21);
        if (rollType == RollType.Normal)
            return roll1;

        int roll2 = _random.Next(1, 21);
        return rollType == RollType.Advantage ? Math.Max(roll1, roll2) : Math.Min(roll1, roll2);
    }

    /// <summary>
    /// Rolls any die with sides, with Advantage/Disadvantage/Normal.
    /// </summary>
    public static int RollDie(int sides, RollType rollType = RollType.Normal)
    {
        int roll1 = _random.Next(1, sides + 1);
        if (rollType == RollType.Normal)
            return roll1;

        int roll2 = _random.Next(1, sides + 1);
        return rollType == RollType.Advantage ? Math.Max(roll1, roll2) : Math.Min(roll1, roll2);
    }
}