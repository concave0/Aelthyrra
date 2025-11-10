using System;
using System.Collections.Generic;

public enum Skill
{
    Acrobatics,         // DEX
    AnimalHandling,     // WIS
    Arcana,             // INT
    Athletics,          // STR
    Deception,          // CHR
    History,            // INT
    Insight,            // WIS
    Intimidation,       // CHR
    Investigation,      // INT
    Medicine,           // WIS
    Nature,             // INT
    Perception,         // WIS
    Performance,        // CHR
    Persuasion,         // CHR
    Religion,           // INT
    SleightOfHand,      // DEX
    Stealth,            // DEX
    Survival            // WIS
}

public class Skills
{
    private readonly Dictionary<Skill, bool> _proficiencies = new Dictionary<Skill, bool>();
    private readonly Dictionary<Skill, bool> _expertise = new Dictionary<Skill, bool>();
    private readonly Player _player;

    public Skills(Player player)
    {
        _player = player;
        // Initialize all skills as not proficient and not expertise by default
        foreach (Skill skill in Enum.GetValues(typeof(Skill)))
        {
            _proficiencies[skill] = false;
            _expertise[skill] = false;
        }
    }

    public void SetProficiency(Skill skill, bool isProficient = true)
    {
        _proficiencies[skill] = isProficient;
    }

    public bool IsProficient(Skill skill) => _proficiencies[skill];

    public void SetExpertise(Skill skill, bool isExpert = true)
    {
        _expertise[skill] = isExpert;
        if (isExpert) _proficiencies[skill] = true; // Expertise always means proficiency
    }

    public bool HasExpertise(Skill skill) => _expertise[skill];

    public int GetSkillBonus(Skill skill)
    {
        int abilityMod = GetAbilityModifierForSkill(skill);
        int profBonus = 0;
        if (HasExpertise(skill))
            profBonus = 2 * _player.ProficiencyBonus;
        else if (IsProficient(skill))
            profBonus = _player.ProficiencyBonus;
        return abilityMod + profBonus;
    }

    // Returns the ability modifier for the relevant skill
    public int GetAbilityModifierForSkill(Skill skill)
    {
        switch (skill)
        {
            case Skill.Athletics:
                return _player.GetModifier(_player.STR);
            case Skill.Acrobatics:
            case Skill.SleightOfHand:
            case Skill.Stealth:
                return _player.GetModifier(_player.DEX);
            case Skill.AnimalHandling:
            case Skill.Insight:
            case Skill.Medicine:
            case Skill.Perception:
            case Skill.Survival:
                return _player.GetModifier(_player.WIS);
            case Skill.Arcana:
            case Skill.History:
            case Skill.Investigation:
            case Skill.Nature:
            case Skill.Religion:
                return _player.GetModifier(_player.INT);
            case Skill.Deception:
            case Skill.Intimidation:
            case Skill.Performance:
            case Skill.Persuasion:
                return _player.GetModifier(_player.CHR);
        }
        return 0;
    }

    // Passive Perception: 10 + higher of Wisdom (Perception) or Intelligence (Investigation) check modifier
    public int PassivePerception()
    {
        int perceptionMod = GetSkillBonus(Skill.Perception);
        int investigationMod = GetSkillBonus(Skill.Investigation);
        int higher = perceptionMod >= investigationMod ? perceptionMod : investigationMod;
        return 10 + higher;
    }

    // Integration: Skill check roll using Player's GatewayRollObserver for advantage/disadvantage
    public int RollSkillCheck(Skill skill)
    {
        _player.PrepareSkillCheck(skill); // Sets context for advantage/disadvantage
        int roll = _player.RollGateway.RollD20();
        int bonus = GetSkillBonus(skill);
        Console.WriteLine($"{_player.Name} skill check for {skill}: d20={roll} + bonus={bonus} = {roll + bonus} ({_player.RollGateway.LastDeterminedRollType})");
        return roll + bonus;
    }

    public override string ToString()
    {
        var lines = new List<string>();
        foreach (Skill skill in Enum.GetValues(typeof(Skill)))
        {
            string prof = HasExpertise(skill) ? "(E)" : IsProficient(skill) ? "(P)" : "";
            lines.Add($"{skill}: {GetSkillBonus(skill)} {prof}");
        }
        lines.Add($"Passive Perception: {PassivePerception()}");
        return string.Join("\n", lines);
    }
}