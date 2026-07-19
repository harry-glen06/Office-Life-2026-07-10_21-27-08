// The day's LOGIC. Knows nothing about the screen.
public class DaySimulation
{
    private const int DayStart = 540;   // 9am
    private const int DayEnd = 1020;    // 5pm

    private Employee employee = new Employee();
    private int clock = DayStart;
    private bool dayOver = false;

    // Read-only windows for the UI to DISPLAY (it can look, not touch).
    public int Energy => employee.energy;
    public int Career => employee.career;
    public int Relationships => employee.relationships;
    public int Clock => clock;
    public bool IsDayOver => dayOver;

    // Player picked an activity. Returns true if it happened.
    public bool DoActivity(ActivityDefinition activity)
    {
        if (dayOver) return false;
        return activity.Perform(employee, ref clock);
    }

    // Advance one in-game minute. UI calls this once per real second.
    public void Tick()
    {
        if (dayOver) return;

        if (clock >= DayEnd)   // reached 5pm — end the day
        {
            dayOver = true;
            return;
        }

        clock += 1;
    }
}