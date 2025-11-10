public class ClassLevel
{
    public string ClassName { get; set; }
    public int Level { get; set; }
    public bool IsPrimaryClass { get; set; } // True if this is the character's original class
    public DateTime AddedOn { get; set; } // When this class was first added/multiclassed

    public ClassLevel(string className, bool isPrimary = false)
    {
        ClassName = className;
        Level = 1;
        IsPrimaryClass = isPrimary;
        AddedOn = DateTime.UtcNow;
    }
}
