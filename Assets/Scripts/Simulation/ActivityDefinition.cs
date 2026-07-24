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
    public int toiletCost;
    
    public CharacterPose pose;

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
    public void AdvanceOneMinute(GameState game, ref float energyAccumulator, ref float gainAccumulator, ref float toiletAccumulator)
    {
        // --- energy multiplier ---
        const int energyThreshold = 30;
        const float energyFloor = 0.2f;
        float energyMult;
        if (game.employee.energy >= energyThreshold)
            energyMult = 1f;
        else
            energyMult = energyFloor + (1f - energyFloor) * ((float)game.employee.energy / energyThreshold);
        
        // --- toilet multiplier (cliff, only bites below 15) ---
        const int toiletThreshold = 15;
        const float toiletFloor = 0.4f;
        float toiletMult;
        if (game.employee.toilet >= toiletThreshold)
            toiletMult = 1f;
        else
            toiletMult = toiletFloor + (1f - toiletFloor) * ((float)game.employee.toilet / toiletThreshold);
        
        // multiply them
        float effectiveness = energyMult * toiletMult;

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
        // TOILET: add this minute's slice, then apply whole points ---
        toiletAccumulator += (float)toiletCost / timeCost;
        while (toiletAccumulator >= 1f)          // draining (coffee, positive cost)
        {
            toiletAccumulator -= 1f;
            game.employee.toilet -= 1;
        }

        while (toiletAccumulator <= -1f)         // restoring (using the toilet, negative cost)
        {
            toiletAccumulator += 1f;
            game.employee.toilet += 1;
        }

        game.employee.toilet = Mathf.Clamp(game.employee.toilet, 0, 100);
    }
}