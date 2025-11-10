public class MulticlassEvent
{
    public string ClassName { get; set; }
    public DateTime Timestamp { get; set; }
    public int LevelAfterChange { get; set; }

    public MulticlassEvent(string className, int level)
    {
        ClassName = className;
        LevelAfterChange = level;
        Timestamp = DateTime.UtcNow;
    }
}