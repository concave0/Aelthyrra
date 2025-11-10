using System;

public class DiceRoller
{
    private readonly Random _random;

    public DiceRoller()
    {
        _random = new Random();
    }

    private int RollDie(int sides, RollType rollType = RollType.Normal)
    {
        int roll1 = _random.Next(1, sides + 1);
        if (rollType == RollType.Normal)
            return roll1;

        int roll2 = _random.Next(1, sides + 1);
        return rollType == RollType.Advantage ? Math.Max(roll1, roll2) : Math.Min(roll1, roll2);
    }

    public int RollD4(RollType rollType = RollType.Normal) => RollDie(4, rollType);
    public int RollD6(RollType rollType = RollType.Normal) => RollDie(6, rollType);
    public int RollD8(RollType rollType = RollType.Normal) => RollDie(8, rollType);
    public int RollD10(RollType rollType = RollType.Normal) => RollDie(10, rollType);
    public int RollD100(RollType rollType = RollType.Normal) => RollDie(100, rollType);

    // Add this method for rolling multiple dice
    public int RollMultipleDice(int diceCount, int sides, RollType rollType = RollType.Normal)
    {
        int sum = 0;
        for (int i = 0; i < diceCount; i++)
        {
            sum += RollDie(sides, rollType);
        }
        return sum;
    }
}