using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewActivity", menuName = "Office/Activity")]
public class ActivityDefinition : ScriptableObject
{
    private const int dayEnd = 1020;   // 5pm

    public string activityName;
    public int timeCost;
    public int energyCost;      // + drains, - restores (coffee)
    public int toiletCost;      // + drains bladder, - restores (toilet)

    public StatType affects;
    public CoworkerDefinition targetCoworker;   // only used when affects == CoworkerRelationship
    public int amount;

    public CharacterPose pose;  // what the character does while performing this


    // =====================================================================
    // Affordability — split so callers can say WHY something was refused
    // =====================================================================

    public bool HasEnoughTime(int clock)
    {
        return (clock + timeCost) <= dayEnd;
    }

    public bool HasEnoughEnergy(Employee e)
    {
        return (e.energy - energyCost) >= 0;
    }

    public bool CanAfford(Employee e, int clock)
    {
        return HasEnoughTime(clock) && HasEnoughEnergy(e);
    }


    // =====================================================================
    // One minute of this activity
    // =====================================================================

    // Accumulators are passed by ref so fractional progress carries between calls.
    public void AdvanceOneMinute(GameState game, ref float energyAccumulator, ref float gainAccumulator, ref float toiletAccumulator)
    {
        float effectiveness = Effectiveness(game);

        // --- ENERGY: add this minute's slice, then apply whole points ---
        energyAccumulator += (float)energyCost / timeCost;
        while (energyAccumulator >= 1f)
        {
            energyAccumulator -= 1f;
            game.employee.energy -= 1;
        }
        while (energyAccumulator <= -1f)   // restoring (coffee)
        {
            energyAccumulator += 1f;
            game.employee.energy += 1;
        }
        game.employee.energy = Mathf.Clamp(game.employee.energy, 0, 100);

        // --- TOILET: same shape ---
        toiletAccumulator += (float)toiletCost / timeCost;
        while (toiletAccumulator >= 1f)
        {
            toiletAccumulator -= 1f;
            game.employee.toilet -= 1;
        }
        while (toiletAccumulator <= -1f)   // restoring (using the toilet)
        {
            toiletAccumulator += 1f;
            game.employee.toilet += 1;
        }
        game.employee.toilet = Mathf.Clamp(game.employee.toilet, 0, 100);

        // --- GAIN: scaled by how effective you are right now ---
        gainAccumulator += ((float)amount / timeCost) * effectiveness;
        while (gainAccumulator >= 1f)
        {
            gainAccumulator -= 1f;
            ApplyGain(game);
        }
    }

    // Tired hurts across a wide range; a full bladder only bites at the end.
    float Effectiveness(GameState game)
    {
        const int energyThreshold = 30;
        const float energyFloor = 0.2f;
        float energyMult;
        if (game.employee.energy >= energyThreshold)
            energyMult = 1f;
        else
            energyMult = energyFloor + (1f - energyFloor) * ((float)game.employee.energy / energyThreshold);

        const int toiletThreshold = 15;
        const float toiletFloor = 0.4f;
        float toiletMult;
        if (game.employee.toilet >= toiletThreshold)
            toiletMult = 1f;
        else
            toiletMult = toiletFloor + (1f - toiletFloor) * ((float)game.employee.toilet / toiletThreshold);

        return energyMult * toiletMult;
    }

    void ApplyGain(GameState game)
    {
        if (affects == StatType.Career)
        {
            game.employee.career += 1;
        }
        else if (affects == StatType.CoworkerRelationship)
        {
            game.ChangeRelationship(targetCoworker, 1);
        }
        else if (affects == StatType.Relationships)
        {
            // copy the keys first: ChangeRelationship modifies the dictionary
            List<CoworkerDefinition> keys = new List<CoworkerDefinition>(game.relationships.Keys);
            foreach (CoworkerDefinition c in keys)
                game.ChangeRelationship(c, 1);
        }
        // Energy and Toilet aren't gains — they're handled by energyCost / toiletCost above.
    }
}