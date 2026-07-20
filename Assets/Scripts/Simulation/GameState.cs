// Lives the WHOLE game. Holds what persists across days.
using UnityEngine;
using System.Collections.Generic;   // for Dictionary

public class GameState
{
    public Employee employee = new Employee();
    public int dayNumber = 1;

    // Each coworker → your current relationship with them.
    public Dictionary<CoworkerDefinition, int> relationships = new Dictionary<CoworkerDefinition, int>();
    
    // How sleep works: recover some of the gap toward full.
    public void RecoverOvernight()
    {
        const float recoveryFraction = 0.75f;   // half the gap to full

        int recovered = Mathf.RoundToInt((100 - employee.energy) * recoveryFraction);
        employee.energy = employee.energy + recovered;
        employee.energy = Mathf.Clamp(employee.energy, 0, 100);
    }
    
    public void InitCoworkers(List<CoworkerDefinition> coworkers)
    {
        foreach (CoworkerDefinition c in coworkers)
            relationships[c] = c.startingRelationship;   // seed each at their start value
    }
    
    public int GetRelationship(CoworkerDefinition c)
    {
        return relationships[c];
    }

    public void ChangeRelationship(CoworkerDefinition c, int amount)
    {
        relationships[c] += amount;
    }
}