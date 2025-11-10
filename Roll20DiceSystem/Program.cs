using System;

class Program
{
    static void Main(string[] args)
    {
        // Run tests
        DiceRollerTest.RunAllTests();

        // Demo rolls
        DiceRoller dice = new DiceRoller();
        D20Roller d20 = new D20Roller();

        Console.WriteLine("Demo Rolls:");

        // Roll 2d6 and add to d20 with modifier
        int multiD6 = dice.RollMultipleDice(2, 6); // 2d6
        int d20Result = d20.RollWithModifierAndExtraDice(3, RollType.Advantage, multiD6);
        Console.WriteLine($"d20 (advantage) + 3 modifier + 2d6 ({multiD6}): {d20Result}");

        // Roll 4d4 and add to d20 with disadvantage
        int multiD4 = dice.RollMultipleDice(4, 4); // 4d4
        int d20Result2 = d20.RollWithModifierAndExtraDice(1, RollType.Disadvantage, multiD4);
        Console.WriteLine($"d20 (disadvantage) + 1 modifier + 4d4 ({multiD4}): {d20Result2}");

        // Roll 1d10 and add to d20 normal
        int d10 = dice.RollD10();
        int d20Result3 = d20.RollWithModifierAndExtraDice(0, RollType.Normal, d10);
        Console.WriteLine($"d20 (normal) + 0 modifier + 1d10 ({d10}): {d20Result3}");

        // Roll 5d8 with advantage and add to d20 with modifier
        int multiD8Adv = dice.RollMultipleDice(5, 8, RollType.Advantage);
        int d20Result4 = d20.RollWithModifierAndExtraDice(2, RollType.Advantage, multiD8Adv);
        Console.WriteLine($"d20 (advantage) + 2 modifier + 5d8 (advantage) ({multiD8Adv}): {d20Result4}");
    }
}