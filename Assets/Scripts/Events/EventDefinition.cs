using UnityEngine;
using System.Collections.Generic;

public enum DayRequirement { Any, Monday, Tuesday, Wednesday, Thursday, Friday }
public enum TimeRequirement { Any, Morning, Afternoon }

[CreateAssetMenu(fileName = "NewEvent", menuName = "Office/Event")]
public class EventDefinition : ScriptableObject
{
    
    public string title;              // "Your boss asks you to stay late..."
    [TextArea] public string description;
    public List<EventChoice> choices; // the options (2 for accept/decline)
    public DayRequirement requiredDay = DayRequirement.Any;   // when can this fire
    public TimeRequirement requiredTime = TimeRequirement.Any;
    public float chance;
}
