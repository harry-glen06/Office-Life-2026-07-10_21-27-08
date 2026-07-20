using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewActivity", menuName = "Office/Activity")]
public class ActivityDefinition : ScriptableObject
{
    public string activityName;
    public int timeCost;
    public int energyCost;
    public StatType affects;
    public int amount;

    public CoworkerDefinition targetCoworker;   // only used when affects == CoworkerRelationship

    // Affordability check on click. Reads energy + clock only, so Employee is fine here.
    public bool CanAfford(Employee e, int clock)
    {
        const int dayEnd = 1020;
        if ((clock + timeCost) > dayEnd) return false;
        if ((e.energy - energyCost) < 0) return false;
        return true;
    }

    // Does ONE minute of this activity. Takes GameState so it can reach both
    // the employee AND the coworker relationships.
    public void AdvanceOneMinute(GameState game, ref float energyAccumulator, ref float gainAccumulator)
    {
        // effectiveness from CURRENT energy
        const int threshold = 30;
        const float floor = 0.2f;
        float effectiveness;
        if (game.employee.energy >= threshold)
            effectiveness = 1f;
        else
            effectiveness = floor + (1f - floor) * ((float)game.employee.energy / threshold);

        // ENERGY: add this minute's slice, then apply whole points
        energyAccumulator += (float)energyCost / timeCost;
        while (energyAccumulator >= 1f)
        {
            energyAccumulator -= 1f;
            game.employee.energy -= 1;
        }
        while (energyAccumulator <= -1f)   // coffee restoring
        {
            energyAccumulator += 1f;
            game.employee.energy += 1;
        }
        game.employee.energy = Mathf.Clamp(game.employee.energy, 0, 100);

        // GAIN: add this minute's slice, then apply whole points
        gainAccumulator += ((float)amount / timeCost) * effectiveness;
        while (gainAccumulator >= 1f)
        {
            gainAccumulator -= 1f;
            if (affects == StatType.Career)
            {
                game.employee.career += 1;
            }
            else if (affects == StatType.CoworkerRelationship)
            {
                game.ChangeRelationship(targetCoworker, 1);   // one specific person
            }
            else // StatType.Relationships — the whole team
            {
                List<CoworkerDefinition> keys = new List<CoworkerDefinition>(game.relationships.Keys);
                foreach (CoworkerDefinition c in keys)
                    game.ChangeRelationship(c, 1);
            }
        }
    }
}