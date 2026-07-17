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