[System.Serializable]
public class Effect
{
    public StatType affects;              // Career, Relationships, or CoworkerRelationship
    public CoworkerDefinition targetCoworker;  // only used for CoworkerRelationship
    public int amount;                    // can be negative (−energy, −relationship)
}