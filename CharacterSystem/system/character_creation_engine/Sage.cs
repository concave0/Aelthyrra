using System.Collections.Generic;

public class Sage : Background
{
    public Sage() : base(
        "Sage",
        new List<AbilityScore> { AbilityScore.Constitution, AbilityScore.Intelligence, AbilityScore.Wisdom },
        Feat.MagicInitiateWizard,
        new List<Skill> { Skill.Arcana, Skill.History },
        new List<Tool> { Tool.CalligraphersSupplies },
        new List<EquipmentOption> {
            new EquipmentOption(
                new List<string> {
                    "Quarterstaff",
                    "Calligrapher’s Supplies",
                    "Book (history)",
                    "Parchment (8 sheets)",
                    "Robe"
                }, 8),
            new EquipmentOption(
                new List<string> { }, 50)
        }
    ) { }
}