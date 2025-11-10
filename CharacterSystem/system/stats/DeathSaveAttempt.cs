// import RollType here


public enum DeathSaveResult
{
    Success,
    Failure
}

public class DeathSaveAttempt
{
    public int Roll { get; }
    public RollType Type { get; }
    public DeathSaveResult Result { get; }

    public DeathSaveAttempt(int roll, RollType type, DeathSaveResult result)
    {
        Roll = roll;
        Type = type;
        Result = result;
    }
}
