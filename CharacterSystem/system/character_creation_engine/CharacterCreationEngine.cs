using System;
using System.Collections.Generic;

public class CharacterCreationEngine
{
    public Player CreateCharacter(
        string name,
        int str, int dex, int con, int intl, int wis, int chr,
        int proficiencyBonus = 2,
        int ac = 10,
        int blindsight = 0,
        int darkvision = 0,
        int tremorsense = 0,
        int truesight = 0,
        int movement = 0;
        IEnumerable<WeaponType> weaponProficiencies = null,
        PlayerSpellLists spellList = null,
        List<SavingThrowProficiency> savingThrowProficiencies = null,
        List<Skill> skillProficiencies = null,
        Species species = null,
        Background background = null
    )
    {
        // Default proficiencies to false
        bool isStrProficient = false, isDexProficient = false, isConProficient = false,
             isIntProficient = false, isWisProficient = false, isChrProficient = false;

        // Set proficiency flags from input
        if (savingThrowProficiencies != null)
        {
            foreach (var prof in savingThrowProficiencies)
            {
                switch (prof)
                {
                    case SavingThrowProficiency.STR: isStrProficient = true; break;
                    case SavingThrowProficiency.DEX: isDexProficient = true; break;
                    case SavingThrowProficiency.CON: isConProficient = true; break;
                    case SavingThrowProficiency.INT: isIntProficient = true; break;
                    case SavingThrowProficiency.WIS: isWisProficient = true; break;
                    case SavingThrowProficiency.CHR: isChrProficient = true; break;
                }
            }
        }

        // Create Player object with all available values
        var player = new Player(
            name, str, dex, con, intl, wis, chr, proficiencyBonus,
            ac, blindsight, darkvision, tremorsense, truesight,
            weaponProficiencies,
            spellList,
            isStrProficient, isDexProficient, isConProficient,
            isIntProficient, isWisProficient, isChrProficient,
            species,
            background
        );

        // Set skill proficiencies
        if (skillProficiencies != null)
        {
            foreach (var skill in skillProficiencies)
            {
                player.Skills.SetProficiency(skill, true);
            }
        }

        return player;
    }
}

// Helper enum for saving throw proficiencies
public enum SavingThrowProficiency
{
    STR,
    DEX,
    CON,
    INT,
    WIS,
    CHR
}