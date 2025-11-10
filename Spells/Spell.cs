public class Spell
{
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Damage as dice notation or flat number
    public string Damage { get; set; } // e.g. "2d6 fire", "8 cold"
    
    // Status effects (e.g. "Stunned", "Poisoned")
    public List<string> StatusEffects { get; set; } = new List<string>();
    
    // Environmental effects (e.g. "Creates fog", "Freezes ground")
    public List<string> EnvironmentalEffects { get; set; } = new List<string>();
    
    // Area of Effect (e.g. "20ft radius", "line 30ft", "single target")
    public string AreaOfEffect { get; set; }
    
    // Range (e.g. "60ft", "Self", "Touch")
    public string Range { get; set; }
    
    // Does the spell cause advantage/disadvantage (e.g. "Attacks against target have advantage")
    public bool CausesAdvantage { get; set; }
    public bool CausesDisadvantage { get; set; }
    
    // Save DC for the target to resist the spell
    public int SaveDC { get; set; }
    
    // Spell attack bonus (used for attack rolls)
    public int SpellAttackBonus { get; set; }

    // Spell level (0 = cantrip, 1 = 1st-level, etc.)
    public int Level { get; set; }

    public Spell(
        string name,
        string description,
        string damage,
        string areaOfEffect,
        string range,
        int saveDC,
        int spellAttackBonus,
        int level,
        List<string> statusEffects = null,
        List<string> environmentalEffects = null,
        bool causesAdvantage = false,
        bool causesDisadvantage = false
    )
    {
        Name = name;
        Description = description;
        Damage = damage;
        AreaOfEffect = areaOfEffect;
        Range = range;
        SaveDC = saveDC;
        SpellAttackBonus = spellAttackBonus;
        Level = level;
        StatusEffects = statusEffects ?? new List<string>();
        EnvironmentalEffects = environmentalEffects ?? new List<string>();
        CausesAdvantage = causesAdvantage;
        CausesDisadvantage = causesDisadvantage;
    }

    public override string ToString()
    {
        return $"{Name} (Level {Level}): {Description}\n" +
               $"Damage: {Damage}\n" +
               $"Status Effects: {(StatusEffects.Count > 0 ? string.Join(", ", StatusEffects) : "None")}\n" +
               $"Environmental Effects: {(EnvironmentalEffects.Count > 0 ? string.Join(", ", EnvironmentalEffects) : "None")}\n" +
               $"Area of Effect: {AreaOfEffect}\n" +
               $"Range: {Range}\n" +
               $"Causes Advantage: {CausesAdvantage}\n" +
               $"Causes Disadvantage: {CausesDisadvantage}\n" +
               $"Save DC: {SaveDC}\n" +
               $"Spell Attack Bonus: {SpellAttackBonus}";
    }
}