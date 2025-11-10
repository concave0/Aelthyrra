using System.Collections.Generic;

public class ImmunityList
{
    private HashSet<ImmunityType> _immunities = new HashSet<ImmunityType>();

    public void Add(ImmunityType immunity)
    {
        _immunities.Add(immunity);
    }

    public void Remove(ImmunityType immunity)
    {
        _immunities.Remove(immunity);
    }

    public bool HasImmunity(ImmunityType immunity)
    {
        return _immunities.Contains(immunity);
    }

    public IEnumerable<ImmunityType> GetAllImmunities()
    {
        return _immunities;
    }
}