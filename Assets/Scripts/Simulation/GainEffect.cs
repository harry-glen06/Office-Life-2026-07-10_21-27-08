[System.Serializable]
public class GainEffect
{
    public bool isSkill;          // true = build a skill, false = affect a stat
    public SkillType skill;       // used when isSkill
    public StatType stat;         // used when !isSkill
    public CoworkerDefinition targetCoworker;  // for CoworkerRelationship
    public int amount = 1;
}