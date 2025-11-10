using System.Collections.Generic;

public class Acolyte : Background
{
    public Acolyte() : base(
        "Acolyte",
        new List<AbilityScore> { AbilityScore.Intelligence, AbilityScore.Wisdom, AbilityScore.Charisma },
        Feat.MagicInitiateCleric,
        new List<Skill> { Skill.Insight, Skill.Religion },
        new List<Tool> { Tool.CalligraphersSupplies },
        new List<EquipmentOption> {
            new EquipmentOption(
                new List<string> {
                    "Calligrapher’s Supplies",
                    "Book (prayers)",
                    "Holy Symbol",
                    "Parchment (10 sheets)",
                    "Robe"
                }, 8),
            new EquipmentOption(
                new List<string> { }, 50)
        }
    ) { }
}
