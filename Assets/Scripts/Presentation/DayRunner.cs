using UnityEngine;

public class DayRunner : MonoBehaviour
{
    void Start()
    {
        // --- Make the character ---
        Employee e = new Employee();
        int clock = 540; // 9:00am in minutes

        // --- Define the activities ---
        // Activity(name, timeCost, energyCost, affects, amount)
        Activity work = new Activity("Work", 120, 45, StatType.Career, 7);
        Activity workLazy = new Activity("Work lazily", 120, 25, StatType.Career, 2);
        Activity socialise = new Activity("Socialise", 60, 25, StatType.Relationships, 3);
        Activity coffee = new Activity("Coffee", 10, -15, StatType.Career, 0); // no stat gain, just energy
        Activity skill = new Activity("Skill Grind", 60, 30, StatType.Career, 4);
        Activity lunchtime = new Activity("Have lunch", 45, -5, StatType.Career, 0);
        Activity coLunch = new Activity("Have lunch with coworkers", 50, 15, StatType.Relationships, 2);
        Activity suckUp = new Activity("Suck up to boss", 40, 45, StatType.Relationships, 2);

        // --- Run your routine, in order ---
        // Each call returns true if it happened, false if refused (out of time/energy).
        // Note the 'ref' on clock — required, same as in Perform.
        TryDo(work, e, ref clock);
        TryDo(coffee, e, ref clock);
        TryDo(socialise, e, ref clock);
        TryDo(skill, e, ref clock);
        TryDo(lunchtime, e, ref clock);
        // TryDo(coLunch, e, ref clock);
        TryDo(workLazy, e, ref clock);
        TryDo(suckUp, e, ref clock);

    }

    // A little helper so we log every attempt the same way.
    // Put this OUTSIDE Start(), but INSIDE the DayRunner class.
    void TryDo(Activity a, Employee e, ref int clock)
    {
        bool didIt = a.Perform(e, ref clock);
        if (didIt)
            Debug.Log($"{a.name} | energy: {e.energy} | career: {e.career} | rel: {e.relationships} | time: {clock}");
        else
            Debug.Log($"{a.name} REFUSED (not enough time or energy) | energy: {e.energy} | time: {clock}");
    }
}