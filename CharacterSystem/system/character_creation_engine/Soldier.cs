using System.Collections.Generic;

public class Soldier : Background
{
    public Soldier() : base(
        "Soldier",
        new List<AbilityScore> { AbilityScore.Strength, AbilityScore.Dexterity, AbilityScore.Constitution },
        Feat.SavageAttacker,
        new List<Skill> { Skill.Athletics, Skill.Intimidation },
        new List<Tool> { Tool.GamingSet }, // "Choose one kind" can be handled in instantiation
        new List<EquipmentOption> {
            new EquipmentOption(
                new List<string> {
                    "Spear",
                    "Shortbow",
                    "20 Arrows",
                    "Gaming Set",
                    "Healer’s Kit",
                    "Quiver",
                    "Traveler’s Clothes"
                }, 14),
            new EquipmentOption(
                new List<string> { }, 50)
        }
    ) { }
}
