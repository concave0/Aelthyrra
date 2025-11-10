using System.Collections.Generic;
using System.Linq;

// Tracks multiclassing requirements and checks if a player can multiclass into a given class
public static class MulticlassRuleKeeper
{
    // Example: requirements for each class
    private static Dictionary<string, Dictionary<string, int>> requirements = new Dictionary<string, Dictionary<string, int>>
    {
        p
        // Add other classes here
    };

    // Gets all classes the player has multiclassed into (from history)
    public static HashSet<string> GetMulticlassedClasses(Player player)
    {
        return player.MulticlassHistory.Select(e => e.ClassName).ToHashSet();
    }

    // Checks if the player meets the requirements to multiclass into targetClass
    public static bool CanMulticlass(Player player, string targetClass, out string reason)
    {
        reason = "";

        // Check if player already has this class (from history)
        var existingClasses = GetMulticlassedClasses(player);
        if (existingClasses.Contains(targetClass))
        {
            reason = $"Player already has class '{targetClass}'.";
            return false;
        }

        // Check requirements
        if (!requirements.ContainsKey(targetClass))
        {
            reason = $"Unknown class '{targetClass}'.";
            return false;
        }

        var reqs = requirements[targetClass];
        foreach (var req in reqs)
        {
            int stat = GetStatByName(player, req.Key);
            if (stat < req.Value)
            {
                reason = $"Insufficient {req.Key}: need {req.Value}, have {stat}.";
                return false;
            }
        }

        return true;
    }

    // Helper to get stat value by string name
    private static int GetStatByName(Player player, string statName)
    {
        return statName switch
        {
            "STR" => player.STR,
            "DEX" => player.DEX,
            "CON" => player.CON,
            "INT" => player.INT,
            "WIS" => player.WIS,
            "CHR" => player.CHR,
            _ => 0
        };
    }
}