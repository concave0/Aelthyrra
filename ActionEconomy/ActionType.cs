public enum ActionType
{
    Action,
    BonusAction,
    Reaction
}

public class Action
{
    public ActionType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    // You can add more properties for general use, such as cooldown, requirements, etc.

    public Action(ActionType type, string name, string description)
    {
        Type = type;
        Name = name;
        Description = description;
    }

    public override string ToString()
    {
        return $"{Type}: {Name}\n{Description}";
    }
}