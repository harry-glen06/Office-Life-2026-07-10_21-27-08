using UnityEngine;

public enum StatType
{
    Career,
    Relationships
}

public class Activity
{
    public string name;
    public int timeCost;     // minutes the activity eats
    public int energyCost;   // energy spent (NEGATIVE for coffee = restores)
    public StatType affects; // which stat it moves
    public int amount;       // base stat gain before the energy penalty

    // A constructor, so making activities is clean.
    public Activity(string name, int timeCost, int energyCost, StatType affects, int amount)
    {
        this.name = name;
        this.timeCost = timeCost;
        this.energyCost = energyCost;
        this.affects = affects;
        this.amount = amount;
    }
    
    public bool Perform(Employee e, ref int clock)
    {
        const int dayEnd = 1020;      // 5pm in minutes
        const int threshold = 30;     // energy above this = full effectiveness
        const float floor = 0.2f;     // effectiveness at 0 energy

        // 1. AFFORDABILITY CHECK
        //    - would clock + timeCost push past dayEnd?
        //    - would e.energy - energyCost drop below 0?
        //    If either, return false.
        if ((clock + timeCost) > dayEnd || (e.energy - energyCost) < 0) {
            return false;
        }
        
        // 2. EFFECTIVENESS MULTIPLIER (flat, then linear falloff)
        float multiplier;
        if (e.energy >= threshold)
        {
            multiplier = 1f;
        }
        else
        {
            multiplier = floor + (1f - floor) * ((float)e.energy / threshold);
        }


        // 3. APPLY THE GAIN
        int gain = (int)(amount * multiplier);
        // add 'gain' to e.career or e.relationships depending on 'affects'
        if (affects == StatType.Career)
            e.career += gain;
        else
            e.relationships += gain;

        // 4. PAY THE COSTS
        e.energy -= energyCost;   // (negative cost = coffee = restores)
        e.energy = Mathf.Clamp(e.energy, 0, 100);
        
        clock += timeCost;
        
        return true;
    }
}