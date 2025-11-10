using System;

public class HealthTracker
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int TemporaryHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public HealthTracker(int maxHP, int tempHP = 0)
    {
        MaxHP = maxHP;
        CurrentHP = maxHP;
        TemporaryHP = tempHP;
    }

    public void TakeDamage(int amount)
    {
        if (TemporaryHP > 0)
        {
            int usedTemp = Math.Min(TemporaryHP, amount);
            TemporaryHP -= usedTemp;
            amount -= usedTemp;
        }
        if (amount > 0)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }
    }

    public void Heal(int amount)
    {
        if (amount > 0)
        {
            CurrentHP += amount;
            if (CurrentHP > MaxHP) CurrentHP = MaxHP;
        }
    }

    public void AddTemporaryHP(int amount)
    {
        if (amount > TemporaryHP)
            TemporaryHP = amount; // D&D 5e: only highest temp HP applies
    }

    public void Reset(int maxHP)
    {
        MaxHP = maxHP;
        CurrentHP = maxHP;
        TemporaryHP = 0;
    }
}