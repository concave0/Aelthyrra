using System.Collections.Generic;

public class SpeciesList
{
    private readonly List<Species> _species = new List<Species>();

    public void AddSpecies(Species species)
    {
        _species.Add(species);
    }

    public void RemoveSpecies(Species species)
    {
        _species.Remove(species);
    }

    public List<string> ListSpeciesNames()
    {
        var names = new List<string>();
        foreach (var s in _species)
        {
            names.Add(s.Name);
        }
        return names;
    }

    public List<Species> GetAllSpecies()
    {
        return new List<Species>(_species);
    }
}


