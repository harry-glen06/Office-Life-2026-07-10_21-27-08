// Lives the WHOLE game. Holds what persists across days.
using UnityEngine;

public class GameState
{
    public Employee employee = new Employee();
    public int dayNumber = 1;
    
    // How sleep works: recover some of the gap toward full.
    public void RecoverOvernight()
    {
        const float recoveryFraction = 0.75f;   // half the gap to full

        int recovered = Mathf.RoundToInt((100 - employee.energy) * recoveryFraction);
        employee.energy = employee.energy + recovered;
        employee.energy = Mathf.Clamp(employee.energy, 0, 100);
    }
}