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
        employee.toilet = 90;   
        
        // Did a weekend just pass? (we're now arriving at a Monday, past day 1)
        bool weekendJustHappened = (dayNumber > 1) && (DayName() == "Monday");

        if (weekendJustHappened)
        {
            employee.energy = 100;   // full weekend recharge
            return;
        }
        
        //Otherwise: normal partial overnight recovery
        const float recoveryFraction = 0.75f;  

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
    
    public int OverallLikability()
    {
        int total = 0;
        foreach (int value in relationships.Values)
            total += value;
        return total;   // or return total/relationships.Count for an average
    }

    public int AverageLikability()
    {
        if (relationships.Count == 0) return 0;
        return OverallLikability() / relationships.Count;
    } 
    
    public string DayName()
    {
        string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        return days[(dayNumber - 1) % 5];
    }
    
    public int WeekNumber()
    {
        return (dayNumber - 1) / 5 + 1;
    }
    
    public void ApplyEffect(Effect effect)
    {
        if (effect.affects == StatType.Career)
        {
            employee.career += effect.amount;
        }
        else if (effect.affects == StatType.Energy)
        {
            employee.energy += effect.amount;
            employee.energy = Mathf.Clamp(employee.energy, 0, 100);
        }
        else if (effect.affects == StatType.CoworkerRelationship)
        {
            ChangeRelationship(effect.targetCoworker, effect.amount);
        }
        else // Relationships — everyone
        {
            // copy the keys first: ChangeRelationship modifies the dictionary
            List<CoworkerDefinition> keys = new List<CoworkerDefinition>(relationships.Keys);
            foreach (CoworkerDefinition c in keys)
                ChangeRelationship(c, effect.amount);
        }
    }
}