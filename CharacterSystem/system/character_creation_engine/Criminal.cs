using System.Collections.Generic;

public class Criminal : Background
{
    public Criminal() : base(
        "Criminal",
        new List<AbilityScore> { AbilityScore.Dexterity, AbilityScore.Constitution, AbilityScore.Intelligence },
        Feat.Alert,
        new List<Skill> { Skill.SleightOfHand, Skill.Stealth },
        new List<Tool> { Tool.ThievesTools },
        new List<EquipmentOption> {
            new EquipmentOption(
                new List<string> {
                    "2 Daggers",
                    "Thieves’ Tools",
                    "Crowbar",
                    "2 Pouches",
                    "Traveler’s Clothes"
                }, 16),
            new EquipmentOption(
                new List<string> { }, 50)
        }
    ) { }
}

