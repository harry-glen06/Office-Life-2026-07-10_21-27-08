using UnityEngine;
using System.Collections.Generic;  

public class DaySimulation
{
    private const int DayStart = 540;   // 9am
    private const int DayEnd = 1020;    // 5pm
    
    private const int MorningStart = 540;    // 9am
    private const int MorningEnd = 720;      // 12pm
    private const int AfternoonEnd = 1020;   // 5pm
    
    private EventDefinition scheduledEvent;
    private int scheduledTime;
    private bool eventFiredToday;

    private GameState game;              // holds the persistent employee + day counter
    private int clock = DayStart;
    private bool dayOver = false;

    private DayState state = DayState.Idle;
    private ActivityDefinition currentActivity;
    private int remainingMinutes;

    private float energyAccumulator = 0f;
    private float gainAccumulator = 0f;
    private float toiletAccumulator = 0f;
    private float activityToiletAccumulator = 0f; 
    
    private EventDefinition accidentEvent;

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
    
    public int AverageRelationship => game.AverageLikability();
    
    public int toilet => game.employee.toilet;
    private const float ToiletDrainPerMinute = 0.3f; 

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
        activityToiletAccumulator = 0f;
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
        
        toiletAccumulator += ToiletDrainPerMinute;
        while (toiletAccumulator >= 1f)
        {
            toiletAccumulator -= 1f;         
            game.employee.toilet -= 1;
        }
        game.employee.toilet = Mathf.Clamp(game.employee.toilet, 0, 100);
        
        if (game.employee.toilet <= 0 && accidentEvent != null)
        {
            pendingEvent = accidentEvent;
            game.employee.toilet = 85;
        }
        
        // event firing check
        if (scheduledEvent != null && !eventFiredToday && clock >= scheduledTime)
        {
            eventFiredToday = true;      // mark fired so it doesn't retrigger
            pendingEvent = scheduledEvent;   // signal to the UI
        }

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
    
    public bool CanAfford(ActivityDefinition activity)
    {
        if (activity == null) return false;
        return activity.CanAfford(game.employee, clock);
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
    
    bool IsEligibleToday(EventDefinition ev)
    {
        if (ev.requiredDay == DayRequirement.Any)
            return true;

        // does the event's required day match today?
        // DayName() returns "Monday".."Friday"; compare to the enum
        return ev.requiredDay.ToString() == game.DayName();
    }
    
    public void ScheduleEventForDay(List<EventDefinition> allEvents)
    {
        scheduledEvent = null;          // reset
        eventFiredToday = false;

        foreach (EventDefinition ev in allEvents)
        {
            if (!IsEligibleToday(ev)) continue;          // wrong day, skip

            if (Random.value <= ev.chance)               // roll its per-day chance
            {
                scheduledEvent = ev;
                scheduledTime = RandomTimeInWindow(ev.requiredTime);
                break;   // one per day — take the first winner
            }
        }
    }
    
    private EventDefinition pendingEvent;   // an event waiting to be displayed

    // DayUI calls this each frame; returns the pending event (and clears it), or null
    public EventDefinition ConsumePendingEvent()
    {
        EventDefinition ev = pendingEvent;
        pendingEvent = null;
        return ev;
    }
    
    // setter, called once from DayUI
    public void SetAccidentEvent(EventDefinition ev)
    {
        accidentEvent = ev;
    }
    
} 