using UnityEngine;
using System.Collections.Generic;

public class DaySimulation
{
    // ---------- Clock ----------
    private const int DayStart = 540;        // 9am
    private const int DayEnd = 1020;         // 5pm
    private const int MorningStart = 540;    // 9am
    private const int MorningEnd = 720;      // 12pm
    private const int AfternoonEnd = 1020;   // 5pm

    private const float ToiletDrainPerMinute = 0.3f;

    public enum DayState { Idle, Busy }

    // ---------- Day-scoped state ----------
    private GameState game;              // the persistent state this day belongs to
    private int clock = DayStart;
    private bool dayOver = false;

    private DayState state = DayState.Idle;
    private ActivityDefinition currentActivity;
    private int remainingMinutes;

    private float energyAccumulator = 0f;
    private float gainAccumulator = 0f;
    private float toiletAccumulator = 0f;            // passive drain
    private float activityToiletAccumulator = 0f;    // the current activity's toiletCost

    // ---------- Events ----------
    private EventDefinition scheduledEvent;   // won this morning's roll, waiting for its time
    private int scheduledTime;
    private bool eventFiredToday;

    private List<EventDefinition> triggerEvents = new List<EventDefinition>();
    private HashSet<EventDefinition> firedTriggersToday = new HashSet<EventDefinition>();

    private EventDefinition pendingEvent;     // waiting to be displayed by the UI


    // Constructor: receives the persistent game state.
    public DaySimulation(GameState game)
    {
        this.game = game;
    }


    // =====================================================================
    // Read-only windows for the UI
    // =====================================================================

    public int Energy => game.employee.energy;
    public int Career => game.employee.career;
    public int Relationships => game.OverallLikability();
    public int AverageRelationship => game.AverageLikability();
    public int Toilet => game.employee.toilet;

    public int Clock => clock;
    public bool IsDayOver => dayOver;

    public bool IsBusy => state == DayState.Busy;
    public int RemainingMinutes => remainingMinutes;
    public string CurrentActivityName => currentActivity != null ? currentActivity.activityName : "";
    public CharacterPose CurrentPose => currentActivity != null ? currentActivity.pose : CharacterPose.Idle;


    // =====================================================================
    // Activities
    // =====================================================================

    // Player picked an activity. Tries to START it, and says why if it can't.
    public ActivityResult DoActivity(ActivityDefinition activity)
    {
        if (activity == null) return ActivityResult.DayOver;
        if (dayOver) return ActivityResult.DayOver;
        if (state == DayState.Busy) return ActivityResult.AlreadyBusy;
        if (!activity.HasEnoughTime(clock)) return ActivityResult.NotEnoughTime;
        if (!activity.HasEnoughEnergy(game.employee)) return ActivityResult.TooTired;

        state = DayState.Busy;
        currentActivity = activity;
        remainingMinutes = activity.timeCost;
        energyAccumulator = 0f;
        gainAccumulator = 0f;
        activityToiletAccumulator = 0f;
        return ActivityResult.Started;
    }

    // Used by the UI to grey out buttons.
    public bool CanAfford(ActivityDefinition activity)
    {
        if (activity == null) return false;
        return activity.CanAfford(game.employee, clock);
    }


    // =====================================================================
    // The tick
    // =====================================================================

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

        DrainToilet();

        // Order matters: triggers are guaranteed consequences, so they win a
        // same-tick collision over the scheduled event.
        CheckScheduledEvent();
        CheckTriggerEvents();

        if (state == DayState.Busy)
        {
            currentActivity.AdvanceOneMinute(game, ref energyAccumulator, ref gainAccumulator, ref activityToiletAccumulator);

            remainingMinutes -= 1;
            if (remainingMinutes <= 0)
            {
                state = DayState.Idle;
                currentActivity = null;
            }
        }
    }

    // Bladder fills whether you're busy or not.
    void DrainToilet()
    {
        toiletAccumulator += ToiletDrainPerMinute;
        while (toiletAccumulator >= 1f)
        {
            toiletAccumulator -= 1f;
            game.employee.toilet -= 1;
        }
        game.employee.toilet = Mathf.Clamp(game.employee.toilet, 0, 100);
    }


    // =====================================================================
    // Events
    // =====================================================================

    // Rolls each eligible event's per-day chance and schedules at most one.
    public void ScheduleEventForDay(List<EventDefinition> allEvents)
    {
        scheduledEvent = null;
        eventFiredToday = false;

        foreach (EventDefinition ev in allEvents)
        {
            if (!IsEligibleToday(ev)) continue;

            if (Random.value <= ev.chance)
            {
                scheduledEvent = ev;
                scheduledTime = RandomTimeInWindow(ev.requiredTime);
                break;   // one per day — take the first winner
            }
        }
    }

    // The scheduled time is the EARLIEST it can fire; conditions are re-checked
    // each tick after that, so a conditional event waits until it's true.
    void CheckScheduledEvent()
    {
        if (scheduledEvent == null || eventFiredToday) return;
        if (clock < scheduledTime) return;
        if (!MeetsAllConditions(scheduledEvent)) return;

        eventFiredToday = true;
        pendingEvent = scheduledEvent;
    }

    // Guaranteed consequences: no chance roll, fire as soon as conditions hold.
    void CheckTriggerEvents()
    {
        foreach (EventDefinition ev in triggerEvents)
        {
            if (firedTriggersToday.Contains(ev)) continue;
            if (!MeetsAllConditions(ev)) continue;

            firedTriggersToday.Add(ev);
            pendingEvent = ev;
            return;   // one at a time — pendingEvent is a single slot
        }
    }

    bool IsEligibleToday(EventDefinition ev)
    {
        if (ev.requiredDay != DayRequirement.Any && ev.requiredDay.ToString() != game.DayName())
            return false;

        return MeetsAllConditions(ev);
    }

    bool MeetsAllConditions(EventDefinition ev)
    {
        foreach (StatCondition c in ev.conditions)
            if (!game.MeetsCondition(c)) return false;
        return true;
    }

    int RandomTimeInWindow(TimeRequirement window)
    {
        // Random.Range(min, max) for ints returns min..max-1 (max exclusive)
        if (window == TimeRequirement.Morning)
            return Random.Range(MorningStart, MorningEnd);
        else if (window == TimeRequirement.Afternoon)
            return Random.Range(MorningEnd, AfternoonEnd);
        else // Any
            return Random.Range(MorningStart, AfternoonEnd);
    }

    // DayUI calls this each tick; returns the pending event (and clears it), or null.
    public EventDefinition ConsumePendingEvent()
    {
        EventDefinition ev = pendingEvent;
        pendingEvent = null;
        return ev;
    }

    public void SetTriggerEvents(List<EventDefinition> events)
    {
        if (events != null)
            triggerEvents = events;
    }
}