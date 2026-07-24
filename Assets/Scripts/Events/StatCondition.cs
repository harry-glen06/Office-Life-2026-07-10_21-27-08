[System.Serializable]
public class StatCondition
{
    public StatType stat;
    public bool mustBeBelow;    // true = below threshold, false = above
    public int threshold;
    public CoworkerDefinition targetCoworker;   // only used when stat == CoworkerRelationship
}