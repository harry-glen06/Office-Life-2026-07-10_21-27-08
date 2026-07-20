using UnityEngine;

// This attribute adds a right-click → Create menu entry, so you can make
// activity assets in the editor. THIS is the line that turns a class into
// authorable content.
[CreateAssetMenu(fileName = "NewActivity", menuName = "Office/Activity")]
public class ActivityDefinition : ScriptableObject
{
    // Data fields — these show up as editable boxes in the Inspector.
    public string activityName;
    public int timeCost;
    public int energyCost;
    public StatType affects;
    public int amount;
    
    public bool CanAfford(Employee e, int clock)
    {
        const int dayEnd = 1020;
        if ((clock + timeCost) > dayEnd) return false;   // not enough time before 5pm
        if ((e.energy - energyCost) < 0) return false;   // not enough energy
        return true;
    }
    
    public void Apply(Employee e)
    {
        const int threshold = 30;
        const float floor = 0.2f;

        float multiplier;
        if (e.energy >= threshold)
            multiplier = 1f;
        else
            multiplier = floor + (1f - floor) * ((float)e.energy / threshold);

        int gain = (int)(amount * multiplier);
        if (affects == StatType.Career)
            e.career += gain;
        else
            e.relationships += gain;

        e.energy -= energyCost;
        e.energy = Mathf.Clamp(e.energy, 0, 100);
    }
    
    // Does ONE minute of this activity. Accumulators passed by ref so
    // fractional progress carries between calls.
    public void AdvanceOneMinute(Employee e, ref float energyAccumulator, ref float gainAccumulator)
    {
        // effectiveness from CURRENT energy
        const int threshold = 30;
        const float floor = 0.2f;
        float effectiveness;
        if (e.energy >= threshold)
            effectiveness = 1f;
        else
            effectiveness = floor + (1f - floor) * ((float)e.energy / threshold);

        // ENERGY: add this minute's slice, then apply whole points 
        energyAccumulator += (float)energyCost / timeCost;
        while (energyAccumulator >= 1f)
        {
            energyAccumulator -= 1f;
            e.energy -= 1;
        }
        while (energyAccumulator <= -1f)   // coffee restoring
        {
            energyAccumulator += 1f;
            e.energy += 1;
        }
        e.energy = Mathf.Clamp(e.energy, 0, 100);

        // GAIN: add this minute's slice, then apply whole points 
        gainAccumulator += ((float)amount / timeCost) * effectiveness;
        while (gainAccumulator >= 1f)
        {
            gainAccumulator -= 1f;
            if (affects == StatType.Career)
                e.career += 1;
            else
                e.relationships += 1;
        }
    }
}