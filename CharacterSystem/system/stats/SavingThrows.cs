using System;
using System.Collections.Generic;

public class SavingThrows
{
    private readonly Dictionary<Ability, bool> _proficiencies = new Dictionary<Ability, bool>();
    private readonly Player _player;
    private readonly int _proficiencyBonus;

    public SavingThrows(
        int str, int dex, int con, int intl, int wis, int chr,
        int proficiencyBonus,
        bool isStrProficient = false, bool isDexProficient = false, bool isConProficient = false,
        bool isIntProficient = false, bool isWisProficient = false, bool isChrProficient = false)
    {
        _player = null; // This can optionally be set via a constructor accepting Player if you want more integration.
        _proficiencyBonus = proficiencyBonus;
        _proficiencies[Ability.STR] = isStrProficient;
        _proficiencies[Ability.DEX] = isDexProficient;
        _proficiencies[Ability.CON] = isConProficient;
        _proficiencies[Ability.INT] = isIntProficient;
        _proficiencies[Ability.WIS] = isWisProficient;
        _proficiencies[Ability.CHR] = isChrProficient;
    }

    // If you want to use Player for modifier calculation, add a Player property or constructor param
    public SavingThrows(Player player, int proficiencyBonus,
        bool isStrProficient = false, bool isDexProficient = false, bool isConProficient = false,
        bool isIntProficient = false, bool isWisProficient = false, bool isChrProficient = false)
    {
        _player = player;
        _proficiencyBonus = proficiencyBonus;
        _proficiencies[Ability.STR] = isStrProficient;
        _proficiencies[Ability.DEX] = isDexProficient;
        _proficiencies[Ability.CON] = isConProficient;
        _proficiencies[Ability.INT] = isIntProficient;
        _proficiencies[Ability.WIS] = isWisProficient;
        _proficiencies[Ability.CHR] = isChrProficient;
    }

    public bool IsProficient(Ability ability) => _proficiencies.TryGetValue(ability, out bool prof) && prof;

    public int GetSavingThrowBonus(Ability ability)
    {
        int mod = 0;
        if (_player != null)
        {
            switch (ability)
            {
                case Ability.STR: mod = _player.GetModifier(_player.STR); break;
                case Ability.DEX: mod = _player.GetModifier(_player.DEX); break;
                case Ability.CON: mod = _player.GetModifier(_player.CON); break;
                case Ability.INT: mod = _player.GetModifier(_player.INT); break;
                case Ability.WIS: mod = _player.GetModifier(_player.WIS); break;
                case Ability.CHR: mod = _player.GetModifier(_player.CHR); break;
            }
        }
        // If no player, fallback to 0 (or accept raw scores/logic)
        return mod + (IsProficient(ability) ? _proficiencyBonus : 0);
    }

    // Integration: Saving throw roll using Player's GatewayRollObserver for advantage/disadvantage
    public int RollSavingThrow(Ability ability)
    {
        if (_player != null)
        {
            _player.PrepareSavingThrow(ability); // You'd add this method to Player to set context
            int roll = _player.RollGateway.RollD20();
            int bonus = GetSavingThrowBonus(ability);
            Console.WriteLine($"{_player.Name} saving throw for {ability}: d20={roll} + bonus={bonus} = {roll + bonus} ({_player.RollGateway.LastDeterminedRollType})");
            return roll + bonus;
        }
        else
        {
            // Fallback roll (no player context)
            int roll = new Random().Next(1, 21);
            int bonus = GetSavingThrowBonus(ability);
            Console.WriteLine($"Saving throw for {ability}: d20={roll} + bonus={bonus} = {roll + bonus}");
            return roll + bonus;
        }
    }

    public override string ToString()
    {
        var lines = new List<string>();
        foreach (Ability ability in Enum.GetValues(typeof(Ability)))
        {
            string prof = IsProficient(ability) ? "(P)" : "";
            lines.Add($"{ability}: {GetSavingThrowBonus(ability)} {prof}");
        }
        return "Saving Throws:\n" + string.Join("\n", lines);
    }
}