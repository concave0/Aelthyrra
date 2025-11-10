public class WeaponType
{
    public string Name { get; set; }
    public string Category { get; set; } // e.g. "Simple", "Martial", "Ranged", etc.
    public string Damage { get; set; } // e.g. "1d8 slashing"
    public string Properties { get; set; } // e.g. "Finesse, Light"
    public int Range { get; set; } // For ranged weapons; 0 for melee

    public WeaponType(string name, string category, string damage, string properties = "", int range = 0)
    {
        Name = name;
        Category = category;
        Damage = damage;
        Properties = properties;
        Range = range;
    }

    public override string ToString()
    {
        string rangeInfo = Range > 0 ? $"Range: {Range}ft" : "Melee";
        return $"{Name} ({Category})\nDamage: {Damage}\nProperties: {Properties}\n{rangeInfo}";
    }
}