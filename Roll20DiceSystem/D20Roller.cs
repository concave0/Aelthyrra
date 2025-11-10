// using System;


public class D20Roller
{
    private readonly Random _random;

    public D20Roller()
    {
        _random = new Random();
    }

    /// <summary>
    /// Rolls a 20-sided die and returns the result (1-20).
    /// </summary>
    public int Roll()
    {
        return _random.Next(1, 21);
    }

    /// <summary>
    /// Rolls a 20-sided die with modifier, roll type, and an additional dice amount.
    /// </summary>
    /// <param name="modifier">The modifier to add to the roll.</param>
    /// <param name="rollType">Normal, Advantage, or Disadvantage.</param>
    /// <param name="extraDiceAmount">The amount from another dice roll (e.g., d4/d6/d8/d10/d100).</param>
    /// <returns>The final result after applying modifier, roll type, and extra dice amount.</returns>
    public int RollWithModifierAndExtraDice(int modifier, RollType rollType = RollType.Normal, int extraDiceAmount = 0)
    {
        int roll1 = Roll();
        int baseRoll;
        if (rollType == RollType.Normal)
        {
            baseRoll = roll1;
        }
        else
        {
            int roll2 = Roll();
            baseRoll = rollType == RollType.Advantage ? Math.Max(roll1, roll2) : Math.Min(roll1, roll2);
        }
        return baseRoll + modifier + extraDiceAmount;
    }
}