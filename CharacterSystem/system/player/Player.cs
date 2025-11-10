using System;
using System.Collections.Generic;

public class Player
{
    public string Name { get; set; }
    public int STR { get; set; }
    public int DEX { get; set; }
    public int CON { get; set; }
    public int INT { get; set; }
    public int WIS { get; set; }
    public int CHR { get; set; }

    public AbilityScores AbilityScores { get; set; }
    public int ProficiencyBonus { get; set; }
    public int AC { get; set; }
    public int Movement { get; set; }
    public int Blindsight { get; set; }
    public int Darkvision { get; set; }
    public int Tremorsense { get; set; }
    public int Truesight { get; set; }
    public HashSet<WeaponType> WeaponProficiencies { get; set; }
    public PlayerSpellLists SpellList { get; set; }
    public SavingThrows Saves { get; set; }
    public Skills Skills { get; set; }
    public GatewayRollObserver RollGateway { get; private set; }

    public Player(
        string name,
        int str, int dex, int con, int intl, int wis, int chr,
        int proficiencyBonus = 2,
        int ac = 10,
        int blindsight = 0,
        int darkvision = 0,
        int tremorsense = 0,
        int truesight = 0,
        IEnumerable<WeaponType> weaponProficiencies = null,
        PlayerSpellLists spellList = null,
        bool isStrProficient = false, bool isDexProficient = false, bool isConProficient = false,
        bool isIntProficient = false, bool isWisProficient = false, bool isChrProficient = false)
    {
        Name = name;
        STR = str;
        DEX = dex;
        CON = con;
        INT = intl;
        WIS = wis;
        CHR = chr;
        AbilityScores = new AbilityScores(str, dex, con, intl, wis, chr);
        ProficiencyBonus = proficiencyBonus;
        AC = ac;
        Blindsight = blindsight;
        Darkvision = darkvision;
        Tremorsense = tremorsense;
        Truesight = truesight;
        WeaponProficiencies = weaponProficiencies != null ? new HashSet<WeaponType>(weaponProficiencies) : new HashSet<WeaponType>();
        SpellList = spellList ?? new PlayerSpellLists();
        Saves = new SavingThrows(str, dex, con, intl, wis, chr, proficiencyBonus,
            isStrProficient, isDexProficient, isConProficient, isIntProficient, isWisProficient, isChrProficient);
        Skills = new Skills(this);
        RollGateway = new GatewayRollObserver();
    }

    // Example integration: Prepare context before a skill check
    public void PrepareSkillCheck(Skill skill)
    {
        // Determine advantage/disadvantage from game logic, conditions, spells, environment, etc.
        bool hasAdvantage = false;
        bool hasDisadvantage = false;
        // TODO: Implement logic to determine advantage/disadvantage for the skill
        // e.g. hasAdvantage = IsBlessed || skill == Skill.Stealth && IsInvisible;
        // e.g. hasDisadvantage = IsPoisoned || skill == Skill.Perception && IsBlinded;
        RollGateway.ObserveContext(hasAdvantage, hasDisadvantage);
    }

    // Example: Roll skill check and return result
    public int RollSkillCheck(Skill skill)
    {
        PrepareSkillCheck(skill);
        int roll = RollGateway.RollD20();
        int bonus = Skills.GetSkillBonus(skill);
        Console.WriteLine($"{Name} rolls a {skill} check: d20={roll} + bonus={bonus} = {roll + bonus} ({RollGateway.LastDeterminedRollType})");
        return roll + bonus;
    }

    // Example: Passive Perception using Skills
    public int PassivePerception => Skills.PassivePerception();

    public override string ToString()
    {
        string senses = $"Blindsight: {(Blindsight > 0 ? Blindsight + "ft" : "None")}, " +
                        $"Darkvision: {(Darkvision > 0 ? Darkvision + "ft" : "None")}, " +
                        $"Tremorsense: {(Tremorsense > 0 ? Tremorsense + "ft" : "None")}, " +
                        $"Truesight: {(Truesight > 0 ? Truesight + "ft" : "None")}";

        string weaponInfo = WeaponProficiencies.Count > 0
            ? string.Join("\n", WeaponProficiencies)
            : "None";

        return $"Player: {Name}\n" +
               $"STR: {STR}\n" +
               $"DEX: {DEX}\n" +
               $"CON: {CON}\n" +
               $"INT: {INT}\n" +
               $"WIS: {WIS}\n" +
               $"CHR: {CHR}\n" +
               $"AC: {AC}\n" +
               $"{senses}\n" +
               $"Weapon Proficiencies:\n{weaponInfo}\n" +
               $"Spell List:\n{SpellList}\n" +
               $"{Saves}\nSkills:\n{Skills}\nPassive Perception: {PassivePerception}";
    }
}