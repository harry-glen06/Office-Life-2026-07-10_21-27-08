using UnityEngine;

[CreateAssetMenu(fileName = "NewCoworker", menuName = "Office/Coworker")]
public class CoworkerDefinition : ScriptableObject
{
    public string coworkerName;        // "Boss", "Rival", etc.

    public int startingRelationship;   // rival -15, others 5–10
    public int energyCost;             // cost to talk to them (boss & rival higher)
    public int relationshipGain;       // how much a talk raises their relationship (rival low)
    public int careerBonus;            // boss > 0, others 0 — talking nudges career
    public int talkDuration;           // minutes a talk takes (boss shorter)
}