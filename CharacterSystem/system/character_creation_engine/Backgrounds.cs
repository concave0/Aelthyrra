using System.Collections.Generic;

public enum AbilityScore
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

public enum Feat
{
    MagicInitiateCleric,
    MagicInitiateWizard,
    Alert,
    SavageAttacker
}

public enum Skill
{
    Insight,
    Religion,
    SleightOfHand,
    Stealth,
    Athletics,
    Intimidation,
    Arcana,
    History
}

public enum Tool
{
    CalligraphersSupplies,
    ThievesTools,
    GamingSet,
    HealersKit
}

public class EquipmentOption
{
    public List<string> Items { get; set; }
    public int Gold { get; set; }

    public EquipmentOption(List<string> items, int gold)
    {
        Items = items;
        Gold = gold;
    }
}

public abstract class Background
{
    public string Name { get; }
    public List<AbilityScore> AbilityScores { get; }
    public Feat Feat { get; }
    public List<Skill> SkillProficiencies { get; }
    public List<Tool> ToolProficiencies { get; }
    public List<EquipmentOption> EquipmentChoices { get; }

    protected Background(
        string name,
        List<AbilityScore> abilityScores,
        Feat feat,
        List<Skill> skillProficiencies,
        List<Tool> toolProficiencies,
        List<EquipmentOption> equipmentChoices)
    {
        Name = name;
        AbilityScores = abilityScores;
        Feat = feat;
        SkillProficiencies = skillProficiencies;
        ToolProficiencies = toolProficiencies;
        EquipmentChoices = equipmentChoices;
    }
}