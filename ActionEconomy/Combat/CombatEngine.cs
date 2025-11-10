using System;
using System.Collections.Generic;

public class CombatEngine
{
    private readonly List<Player> _combatants;

    public CombatEngine(List<Player> combatants)
    {
        _combatants = combatants;
    }

    // Example: Resolve an attack from attacker to target
    public int ResolveAttack(Player attacker, Player target, WeaponType weapon)
    {
        // Determine advantage/disadvantage context for the attack.
        bool hasAdvantage = false;
        bool hasDisadvantage = false;

        // Example logic (expand for your rules!):
        // Advantage if attacker is invisible or target is prone (ranged)
        // Disadvantage if attacker is blinded or target is in cover
        if (attacker.IsInvisible) hasAdvantage = true;
        if (target.IsProne && weapon.IsRanged) hasAdvantage = true;
        if (attacker.IsBlinded) hasDisadvantage = true;
        if (target.HasCover) hasDisadvantage = true;

        // Set context in GatewayRollObserver
        attacker.RollGateway.ObserveContext(hasAdvantage, hasDisadvantage);

        // Roll attack
        int attackRoll = attacker.RollGateway.RollD20();
        int attackBonus = attacker.GetAttackBonus(weapon);
        int totalAttack = attackRoll + attackBonus;

        Console.WriteLine($"{attacker.Name} attacks {target.Name} with {weapon}: d20={attackRoll} + bonus={attackBonus} = {totalAttack} ({attacker.RollGateway.LastDeterminedRollType})");

        // Compare to target AC and return result
        bool hit = totalAttack >= target.AC;
        Console.WriteLine(hit
            ? $"Hit! ({totalAttack} >= {target.AC})"
            : $"Miss! ({totalAttack} < {target.AC})");

        return totalAttack;
    }

    // Example: Roll initiative for all combatants
    public Dictionary<Player, int> RollInitiative()
    {
        var result = new Dictionary<Player, int>();
        foreach (var player in _combatants)
        {
            // Initiative context: Apply advantage/disadvantage if conditions warrant
            bool hasAdvantage = false;
            bool hasDisadvantage = false;
            // Expand your game logic here!

            player.RollGateway.ObserveContext(hasAdvantage, hasDisadvantage);
            int roll = player.RollGateway.RollD20();
            int initiativeBonus = player.GetModifier(player.DEX); // usually initiative uses DEX modifier

            result[player] = roll + initiativeBonus;
            Console.WriteLine($"{player.Name}: Initiative roll {roll} + bonus {initiativeBonus} = {result[player]} ({player.RollGateway.LastDeterminedRollType})");
        }
        return result;
    }

    // Extend with more combat actions (Hide, Dodge, Help, etc.) using the same pattern
}