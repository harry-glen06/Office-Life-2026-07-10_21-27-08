using System.Collections.Generic;

[System.Serializable]
public class EventChoice
{
    public string label;              // "Accept" / "Decline"
    public List<Effect> effects;      // what this choice does (list — multiple effects)
}