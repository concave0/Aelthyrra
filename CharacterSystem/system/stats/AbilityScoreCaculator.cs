using System;
using System.Collections.Generic;

// Calculator for skills and saving throws
public class AbilityCalculator
{
    private readonly AbilityScores scores;
    private readonly int proficiencyBonus;

    // Provide proficiencies for skills and saves
    private readonly HashSet<Skill> skillProficiencies;
    private readonly HashSet<Ability> savingThrowProficiencies;

    public AbilityCalculator(AbilityScores abilityScores, int proficiencyBonus,
        IEnumerable<Skill> skillProficiencies = null,
        IEnumerable<Ability> savingThrowProficiencies = null)
    {
        scores = abilityScores;
        this.proficiencyBonus = proficiencyBonus;
        this.skillProficiencies = skillProficiencies != null ? new HashSet<Skill>(skillProficiencies) : new HashSet<Skill>();
        this.savingThrowProficiencies = savingThrowProficiencies != null ? new HashSet<Ability>(savingThrowProficiencies) : new HashSet<Ability>();
    }

    // Skill modifier (includes proficiency if present)
    public int GetSkillModifier(Skill skill)
    {
        int baseMod = skill switch
        {
            Skill.Athletics => scores.StrMod,
            Skill.Acrobatics => scores.DexMod,
            Skill.SleightOfHand => scores.DexMod,
            Skill.Stealth => scores.DexMod,
            Skill.AnimalHandling => scores.WisMod,
            Skill.Insight => scores.WisMod,
            Skill.Medicine => scores.WisMod,
            Skill.Perception => scores.WisMod,
            Skill.Survival => scores.WisMod,
            Skill.Arcana => scores.IntMod,
            Skill.History => scores.IntMod,
            Skill.Investigation => scores.IntMod,
            Skill.Nature => scores.IntMod,
            Skill.Religion => scores.IntMod,
            Skill.Deception => scores.ChrMod,
            Skill.Intimidation => scores.ChrMod,
            Skill.Performance => scores.ChrMod,
            Skill.Persuasion => scores.ChrMod,
            _ => 0
        };
        return baseMod + (skillProficiencies.Contains(skill) ? proficiencyBonus : 0);
    }

    // Saving throw modifier (includes proficiency if present)
    public int GetSavingThrowModifier(Ability ability)
    {
        int baseMod = ability switch
        {
            Ability.STR => scores.StrMod,
            Ability.DEX => scores.DexMod,
            Ability.CON => scores.ConMod,
            Ability.INT => scores.IntMod,
            Ability.WIS => scores.WisMod,
            Ability.CHR => scores.ChrMod,
            _ => 0
        };
        return baseMod + (savingThrowProficiencies.Contains(ability) ? proficiencyBonus : 0);
    }

    // Skill summary
    public Dictionary<Skill, int> GetAllSkillModifiers()
    {
        var dict = new Dictionary<Skill, int>();
        foreach (Skill skill in Enum.GetValues(typeof(Skill)))
            dict[skill] = GetSkillModifier(skill);
        return dict;
    }

    // Saving throw summary
    public Dictionary<Ability, int> GetAllSavingThrowModifiers()
    {
        var dict = new Dictionary<Ability, int>();
        foreach (Ability ability in Enum.GetValues(typeof(Ability)))
            dict[ability] = GetSavingThrowModifier(ability);
        return dict;
    }
}