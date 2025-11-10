using System.Collections.Generic;
using System.Linq;

public class DeathSaveTracker
{
    private Dictionary<string, List<DeathSaveAttempt>> deathSaves = new Dictionary<string, List<DeathSaveAttempt>>();

    public void RecordDeathSave(string playerName, DeathSaveAttempt attempt)
    {
        if (!deathSaves.ContainsKey(playerName))
            deathSaves[playerName] = new List<DeathSaveAttempt>();
        deathSaves[playerName].Add(attempt);
    }

    public IReadOnlyList<DeathSaveAttempt> GetSaves(string playerName)
    {
        if (deathSaves.TryGetValue(playerName, out var saves))
            return saves;
        return new List<DeathSaveAttempt>();
    }

    public int CountSuccesses(string playerName)
        => GetSaves(playerName).Count(r => r.Result == DeathSaveResult.Success);

    public int CountFailures(string playerName)
        => GetSaves(playerName).Count(r => r.Result == DeathSaveResult.Failure);
}