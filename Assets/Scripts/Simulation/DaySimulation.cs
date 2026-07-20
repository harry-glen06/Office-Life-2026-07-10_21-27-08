public class DaySimulation
{
    private const int DayStart = 540;   // 9am
    private const int DayEnd = 1020;    // 5pm

    private GameState game;              // holds the persistent employee + day counter
    private int clock = DayStart;
    private bool dayOver = false;

    private DayState state = DayState.Idle;
    private ActivityDefinition currentActivity;
    private int remainingMinutes;

    private float energyAccumulator = 0f;
    private float gainAccumulator = 0f;

    public enum DayState { Idle, Busy }

    // Constructor: receives the persistent game state.
    public DaySimulation(GameState game)
    {
        this.game = game;
    }

    // Read-only windows for the UI to DISPLAY.
    public int Energy => game.employee.energy;
    public int Career => game.employee.career;
    public int Relationships => game.OverallLikability();
    public int Clock => clock;
    public bool IsDayOver => dayOver;

    public bool IsBusy => state == DayState.Busy;
    public int RemainingMinutes => remainingMinutes;
    public string CurrentActivityName => currentActivity != null ? currentActivity.activityName : "";

    // Player clicked an activity. Tries to START it.
    public bool DoActivity(ActivityDefinition activity)
    {
        if (activity == null) return false;   // ← guard: no activity, do nothing
        if (dayOver) return false;
        if (state == DayState.Busy) return false;
        if (!activity.CanAfford(game.employee, clock)) return false;

        state = DayState.Busy;
        currentActivity = activity;
        remainingMinutes = activity.timeCost;
        energyAccumulator = 0f;
        gainAccumulator = 0f;
        return true;
    }

    // Advance one in-game minute.
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
            currentActivity.AdvanceOneMinute(game, ref energyAccumulator, ref gainAccumulator);

            remainingMinutes -= 1;
            if (remainingMinutes <= 0)
            {
                state = DayState.Idle;
                currentActivity = null;
            }
        }
    }
    
    public bool CanAfford(ActivityDefinition activity)
    {
        if (activity == null) return false;
        return activity.CanAfford(game.employee, clock);
    }
}