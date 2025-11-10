using System;

public class DiceRollerTest
{
    public static void RunAllTests()
    {
        TestDiceRoller();
        TestD20Roller();
        Console.WriteLine("All tests passed!");
    }

    private static void TestDiceRoller()
    {
        DiceRoller dice = new DiceRoller();
        // Test single die ranges
        if (dice.RollD4() < 1 || dice.RollD4() > 4) throw new Exception("d4 out of range");
        if (dice.RollD6() < 1 || dice.RollD6() > 6) throw new Exception("d6 out of range");
        if (dice.RollD8() < 1 || dice.RollD8() > 8) throw new Exception("d8 out of range");
        if (dice.RollD10() < 1 || dice.RollD10() > 10) throw new Exception("d10 out of range");
        if (dice.RollD100() < 1 || dice.RollD100() > 100) throw new Exception("d100 out of range");

        // Test multiple dice
        int multiD6 = dice.RollMultipleDice(3, 6); // 3d6, should be 3-18
        if (multiD6 < 3 || multiD6 > 18) throw new Exception("3d6 out of range");

        int multiD4Adv = dice.RollMultipleDice(2, 4, RollType.Advantage); // 2d4 with advantage
        if (multiD4Adv < 2 || multiD4Adv > 8) throw new Exception("2d4 (adv) out of range");

        // Test advantage/disadvantage single die
        int adv = dice.RollD6(RollType.Advantage);
        int disadv = dice.RollD6(RollType.Disadvantage);
        if (adv < 1 || adv > 6) throw new Exception("d6 advantage out of range");
        if (disadv < 1 || disadv > 6) throw new Exception("d6 disadvantage out of range");
    }

    private static void TestD20Roller()
    {
        D20Roller d20 = new D20Roller();
        DiceRoller dice = new DiceRoller();

        // Test normal roll range
        int roll = d20.Roll();
        if (roll < 1 || roll > 20) throw new Exception("d20 out of range");

        // Test modifier only
        int modRoll = d20.RollWithModifierAndExtraDice(5);
        if (modRoll < 6 || modRoll > 25) throw new Exception("d20 + modifier out of range");

        // Test advantage/disadvantage
        int adv = d20.RollWithModifierAndExtraDice(0, RollType.Advantage);
        int disadv = d20.RollWithModifierAndExtraDice(0, RollType.Disadvantage);
        if (adv < 1 || adv > 20) throw new Exception("d20 advantage out of range");
        if (disadv < 1 || disadv > 20) throw new Exception("d20 disadvantage out of range");

        // Test extra dice (single)
        int d6 = dice.RollD6();
        int combined = d20.RollWithModifierAndExtraDice(2, RollType.Normal, d6);
        if (combined < 4 || combined > 28) throw new Exception("d20 + d6 + modifier out of range");

        // Test extra dice (multiple)
        int multiD6 = dice.RollMultipleDice(3, 6); // 3d6: 3-18
        int combinedMulti = d20.RollWithModifierAndExtraDice(2, RollType.Normal, multiD6);
        if (combinedMulti < 6 || combinedMulti > 40) throw new Exception("d20 + 3d6 + modifier out of range");
    }
}