using System.Collections.Generic;

public class SpellList
{
    // Holds the player's known or prepared spells
    public List<Spell> Spells { get; set; } = new List<Spell>();

    public SpellList() { }

    public SpellList(IEnumerable<Spell> spells)
    {
        Spells = new List<Spell>(spells);
    }

    public void AddSpell(Spell spell)
    {
        if (!Spells.Contains(spell))
            Spells.Add(spell);
    }

    public void RemoveSpell(Spell spell)
    {
        Spells.Remove(spell);
    }

    public override string ToString()
    {
        if (Spells.Count == 0)
            return "No spells known.";
        var lines = new List<string>();
        foreach (var spell in Spells)
        {
            lines.Add(spell.ToString());
        }
        return string.Join("\n\n", lines);
    }
}