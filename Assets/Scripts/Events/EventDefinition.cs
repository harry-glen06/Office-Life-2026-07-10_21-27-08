using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Office/Event")]
public class EventDefinition : ScriptableObject
{
    public string title;              // "Your boss asks you to stay late..."
    [TextArea] public string description;
    public List<EventChoice> choices; // the options (2 for accept/decline)
}