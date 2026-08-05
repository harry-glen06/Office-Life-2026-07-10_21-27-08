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

    public int amount;          // gain-points produced over the activity's duration

    public CharacterPose pose;  // what the character does while performing this
    public bool adminImproves;  // coffee gets better at admin level 5

    // Everything this activity changes. One entry = one stat or skill nudge.
    public List<GainEffect> gains = new List<GainEffect>();
    
    public Sprite icon;


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
    // Convenience: what KINDS of thing does this activity do?
    // Used by efficiency multipliers and the skill-progress bar.
    // =====================================================================

    public bool BuildsAnySkill()
    {
        foreach (GainEffect g in gains)
            if (g.isSkill) return true;
        return false;
    }

    // The first skill this activity builds (for the progress bar), or null.
    public SkillType? FirstSkillBuilt()
    {
        foreach (GainEffect g in gains)
            if (g.isSkill) return g.skill;
        return null;
    }

    public bool AffectsCareer()
    {
        foreach (GainEffect g in gains)
            if (!g.isSkill && g.stat == StatType.Career) return true;
        return false;
    }

    public bool AffectsRelationships()
    {
        foreach (GainEffect g in gains)
            if (!g.isSkill && (g.stat == StatType.CoworkerRelationship || g.stat == StatType.Relationships))
                return true;
        return false;
    }


    // =====================================================================
    // One minute of this activity
    // =====================================================================

    public void AdvanceOneMinute(GameState game, ref float energyAccumulator, ref float gainAccumulator, ref float toiletAccumulator)
    {
        float energyCostToApply = energyCost;
        float toiletCostToApply = toiletCost;

        if (adminImproves && game.GetSkillLevel(SkillType.Administration) >= 5)
        {
            energyCostToApply *= 1.3f;   // more restore (energyCost is negative for coffee)
            toiletCostToApply *= 0.7f;   // less drain
        }

        // --- ENERGY ---
        energyAccumulator += energyCostToApply / timeCost;
        while (energyAccumulator >= 1f)
        {
            energyAccumulator -= 1f;
            game.employee.energy -= 1;
        }
        while (energyAccumulator <= -1f)
        {
            energyAccumulator += 1f;
            game.employee.energy += 1;
        }
        game.employee.energy = Mathf.Clamp(game.employee.energy, 0, 100);

        // --- TOILET ---
        toiletAccumulator += toiletCostToApply / timeCost;
        while (toiletAccumulator >= 1f)
        {
            toiletAccumulator -= 1f;
            game.employee.toilet -= 1;
        }
        while (toiletAccumulator <= -1f)
        {
            toiletAccumulator += 1f;
            game.employee.toilet += 1;
        }
        game.employee.toilet = Mathf.Clamp(game.employee.toilet, 0, 100);

        // --- GAIN: scaled by how effective you are right now ---
        gainAccumulator += ((float)amount / timeCost) * EfficiencyFor(game);
        while (gainAccumulator >= 1f)
        {
            gainAccumulator -= 1f;
            ApplyGain(game);
        }
    }

    // The total multiplier on this activity's gain right now. Used both to
    // APPLY gain and to SHOW efficiency, so they can't drift.
    public float EfficiencyFor(GameState game)
    {
        float efficiency = Effectiveness(game);

        if (AffectsRelationships())
            efficiency *= 1f + game.GetSkillLevel(SkillType.Charisma) * 0.1f;

        if (AffectsCareer() && game.GetSkillLevel(SkillType.Programming) >= 5)
            efficiency *= 1.5f;

        if (BuildsAnySkill() && game.GetSkillLevel(SkillType.Science) >= 7)
            efficiency *= 1.5f;

        return efficiency;
    }

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

    // Apply every effect this activity carries.
    void ApplyGain(GameState game)
    {
        foreach (GainEffect g in gains)
        {
            if (g.isSkill)
            {
                game.ChangeSkill(g.skill, g.amount);
            }
            else if (g.stat == StatType.Career)
            {
                game.employee.career += g.amount;
            }
            else if (g.stat == StatType.CoworkerRelationship)
            {
                if (g.targetCoworker != null)
                    game.ChangeRelationship(g.targetCoworker, g.amount);
            }
            else if (g.stat == StatType.Relationships)
            {
                List<CoworkerDefinition> keys = new List<CoworkerDefinition>(game.relationships.Keys);
                foreach (CoworkerDefinition c in keys)
                    game.ChangeRelationship(c, g.amount);
            }
            // Energy / Toilet aren't gains — handled by energyCost / toiletCost.
        }
    }
}