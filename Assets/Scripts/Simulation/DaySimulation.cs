// The day's LOGIC. Knows nothing about the screen.
public class DaySimulation
{
    private const int DayStart = 540;   // 9am
    private const int DayEnd = 1020;    // 5pm

    private Employee employee = new Employee();
    private int clock = DayStart;
    private bool dayOver = false;
    
    private DayState state = DayState.Idle;      // start idle
    private ActivityDefinition currentActivity;  // what we're doing (null when idle)
    private int remainingMinutes;                // countdown until it finishes
    
    // Read-only windows for the UI to DISPLAY (it can look, not touch).
    public int Energy => employee.energy;
    public int Career => employee.career;
    public int Relationships => employee.relationships;
    public int Clock => clock;
    public bool IsDayOver => dayOver;
    
    public bool IsBusy => state == DayState.Busy;
    public int RemainingMinutes => remainingMinutes;
    
    public enum DayState { Idle, Busy }
    
    private float energyAccumulator = 0f;
    private float gainAccumulator = 0f;

    public string CurrentActivityName => currentActivity != null ? currentActivity.activityName : "";

    
    // Player picked an activity. Returns true if it happened.
    // Player clicked an activity. Tries to START it (doesn't finish it).
    public bool DoActivity(ActivityDefinition activity)
    {
        if (dayOver) return false;
        if (state == DayState.Busy) return false;        // already doing something
        if (!activity.CanAfford(employee, clock)) return false;  // can't afford it

        // Start the busy period. Effects apply later, on completion.
        state = DayState.Busy;
        currentActivity = activity;
        remainingMinutes = activity.timeCost;
        energyAccumulator = 0f;      // reset
        gainAccumulator = 0f;        // reset
        return true;
    }

    // Advance one in-game minute. UI calls this once per real second.
    public void Tick()
    {
        if (dayOver) return;

        if (clock >= DayEnd)
        {
            dayOver = true;
            return;
        }

        clock += 1;

        if (state == DayState.Busy)
        {
            // do one minute's worth of drain + gain
            currentActivity.AdvanceOneMinute(employee, ref energyAccumulator, ref gainAccumulator);

            remainingMinutes -= 1;
            if (remainingMinutes <= 0)
            {
                // activity finished — just return to idle (effects already applied gradually)
                state = DayState.Idle;
                currentActivity = null;
            }
        }
    }
}