public abstract class Species
{
    public string Name { get; }

    protected Species(string name)
    {
        Name = name;
    }
}

public class Dragonborn : Species
{
    public Dragonborn() : base("Dragonborn") { }
}

public class Dwarf : Species
{
    public Dwarf() : base("Dwarf") { }
}

public class Elf : Species
{
    public Elf() : base("Elf") { }
}

public class Gnome : Species
{
    public Gnome() : base("Gnome") { }
}

public class Goliath : Species
{
    public Goliath() : base("Goliath") { }
}

public class Halfling : Species
{
    public Halfling() : base("Halfling") { }
}

public class Human : Species
{
    public Human() : base("Human") { }
}

public class Orc : Species
{
    public Orc() : base("Orc") { }
}

public class Tiefling : Species
{
    public Tiefling() : base("Tiefling") { }
}