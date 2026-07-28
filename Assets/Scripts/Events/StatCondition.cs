[System.Serializable]
public class StatCondition
{
    public StatType stat;
    public bool mustBeBelow;    // true = below threshold, false = above
    public int threshold;
    public CoworkerDefinition targetCoworker;   // only used when stat == CoworkerRelationship
    public bool checksSkill;      // if true, check a skill level instead of a stat
    public SkillType skill;
}